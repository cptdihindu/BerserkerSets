using System;
using System.Collections.Generic;
using System.Reflection;
using BerserkerSets.Configuration;
using BerserkerSets.Input;
using BerserkerSets.Loadouts;
using BerserkerSets.Models;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BerserkerSets.UI
{
    internal sealed class LoadoutPanel
    {
        private const float PanelWidth = 238f;
        private const float PanelHeight = 326f;
        private const float EdgeMargin = 8f;
        private const float DefaultPanelGap = 20f;

        private static readonly FieldInfo PlayerGridField = typeof(InventoryGui).GetField(
            "m_playerGrid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo GridSelectionField = typeof(InventoryGrid).GetField(
            "m_selected", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo GridWidthField = typeof(InventoryGrid).GetField(
            "m_width", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo UiGroupsField = typeof(InventoryGui).GetField(
            "m_uiGroups", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo InventoryGroupField = typeof(InventoryGui).GetField(
            "m_inventoryGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo ActiveGroupField = typeof(InventoryGui).GetField(
            "m_activeGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly MethodInfo SetActiveGroupMethod = typeof(InventoryGui).GetMethod(
            "SetActiveGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(int), typeof(bool) }, null);

        private sealed class Row
        {
            public Image Background;
            public LoadoutSlotView Main;
            public LoadoutSlotView Off;
        }

        private readonly LoadoutManager _manager;
        private readonly ModConfig _config;
        private readonly InputController _input;
        private readonly List<Row> _rows = new List<Row>();
        private GameObject _root;
        private RectTransform _inventoryAnchor;
        private InventoryGui _inventoryGui;
        private InventoryGrid _playerGrid;
        private UIGroupHandler _uiGroup;
        private int _inventoryGroupIndex;
        private int _uiGroupIndex = -1;
        private int _lastObservedActiveGroup = int.MinValue;
        private Outline _panelOutline;
        private Image _titleBackground;
        private Text _heading;
        private InputPromptStrip _prompt;
        private Text _feedbackText;
        private float _feedbackUntil;
        private int _assignSet = -1;
        private LoadoutSlot _assignSlot;
        private bool _panelFocused;
        private int _focusSet;
        private LoadoutSlot _focusSlot;
        private Vector2i _lastInventorySelection = new Vector2i(0, 0);
        private bool _placedRight;
        private bool _lastPromptWasGamepad;
        private bool _dragging;
        private Vector2 _dragStartScreen;
        private Vector2 _dragStartOffset;
        private Vector2 _dragCurrentOffset;
        private Vector2 _defaultAnchoredPosition;

        public bool IsVisible => _root != null && _root.activeSelf;

        public LoadoutPanel(LoadoutManager manager, ModConfig config, InputController input)
        {
            _manager = manager;
            _config = config;
            _input = input;
            _manager.Changed += Refresh;
            _config.PanelPositionX.SettingChanged += OnLayoutSettingChanged;
            _config.PanelPositionY.SettingChanged += OnLayoutSettingChanged;
            _config.PanelScale.SettingChanged += OnLayoutSettingChanged;
        }

        public void Show(InventoryGui gui)
        {
            if (!_config.EnableInventoryPanel.Value || gui == null) return;
            _inventoryGui = gui;
            _playerGrid = PlayerGridField?.GetValue(gui) as InventoryGrid;
            if (_root == null) Create(gui);
            else if (_uiGroup == null)
            {
                _uiGroup = _root.GetComponent<UIGroupHandler>();
                RegisterUiGroup(gui);
            }
            if (_root != null)
            {
                PositionPanel();
                _root.SetActive(true);
                UpdatePrompt(true);
                Refresh();
            }
        }

        public void Tick()
        {
            if (_root == null || !_root.activeSelf) return;
            bool gamepad = _input.IsGamepadActive;
            int activeIndex = GetActiveGroupIndex();
            bool groupActive = gamepad && activeIndex >= 0 && activeIndex == _uiGroupIndex;
            if (activeIndex != _lastObservedActiveGroup)
            {
                _lastObservedActiveGroup = activeIndex;
                Plugin.Instance?.LogSource.LogInfo(
                    $"Inventory controller group changed: active={activeIndex}, berserker={_uiGroupIndex}, " +
                    $"handlerActive={(_uiGroup != null && _uiGroup.IsActive)}, gamepad={gamepad}.");
            }
            if (groupActive != _panelFocused)
            {
                _panelFocused = groupActive;
                if (groupActive)
                {
                    _lastInventorySelection = GetInventorySelection();
                    _focusSet = 0;
                    _focusSlot = LoadoutSlot.MainHand;
                }
                Refresh();
            }
            if (gamepad != _lastPromptWasGamepad) UpdatePrompt(true);
            if (_feedbackText != null && _feedbackText.gameObject.activeSelf && Time.unscaledTime >= _feedbackUntil)
            {
                _feedbackText.gameObject.SetActive(false);
                _prompt?.SetVisible(true);
                UpdatePrompt(true);
            }
        }

        // InventoryGui checks the gamepad back button before UpdateGamepad. Catch it at
        // InventoryGui.Update so B clears/cancels instead of closing the whole inventory.
        public bool HandleEarlyCancelInput()
        {
            if (_root == null || !_root.activeSelf || !_input.IsGamepadActive || !_input.IsUiCancelDown())
                return false;

            if (_assignSet >= 0)
            {
                int set = _assignSet;
                LoadoutSlot slot = _assignSlot;
                CancelAssignment();
                ShowPanelFeedback($"Assignment cancelled: Set {set + 1} {FormatSlot(slot)}.", false);
                Plugin.Instance?.LogSource.LogInfo(
                    $"Early controller B consumed: cancelled Set {set + 1} {FormatSlot(slot)} assignment.");
                return true;
            }

            if (GetActiveGroupIndex() != _uiGroupIndex) return false;
            _manager.Clear(_focusSet, _focusSlot);
            ShowPanelFeedback($"Cleared Set {_focusSet + 1} {FormatSlot(_focusSlot)}.", false);
            Plugin.Instance?.LogSource.LogInfo(
                $"Early controller B consumed: cleared Set {_focusSet + 1} {FormatSlot(_focusSlot)}.");
            Refresh();
            return true;
        }

        // Called before Valheim handles its inventory gamepad input. True means the command was consumed.
        public bool HandleGamepadInput()
        {
            if (_root == null || !_root.activeSelf || !_input.IsGamepadActive) return false;
            UiCommand command = _input.GetUiCommand();
            if (command == UiCommand.None) return false;

            int activeIndex = GetActiveGroupIndex();
            Plugin.Instance?.LogSource.LogInfo(
                $"Inventory controller command={command}, activeGroup={activeIndex}, " +
                $"berserkerGroup={_uiGroupIndex}, assigning={_assignSet >= 0}.");

            if (command == UiCommand.PreviousGroup || command == UiCommand.NextGroup)
            {
                return CycleUiGroup(command == UiCommand.NextGroup ? 1 : -1);
            }

            if (_assignSet >= 0 && !_panelFocused && command == UiCommand.Cancel)
            {
                CancelAssignment();
                Plugin.Instance?.ShowHud("Loadout assignment cancelled.");
                return true;
            }
            if (_assignSet >= 0 && !_panelFocused) return false;

            if (activeIndex != _uiGroupIndex) return false;
            _panelFocused = true;

            switch (command)
            {
                case UiCommand.Up:
                    _focusSet = Mathf.Max(0, _focusSet - 1);
                    break;
                case UiCommand.Down:
                    _focusSet = Mathf.Min(CharacterLoadoutData.SetCount - 1, _focusSet + 1);
                    break;
                case UiCommand.Left:
                    _focusSlot = LoadoutSlot.MainHand;
                    break;
                case UiCommand.Right:
                    _focusSlot = LoadoutSlot.OffHand;
                    break;
                case UiCommand.Accept:
                    BeginAssignment(_focusSet, _focusSlot, true);
                    break;
                case UiCommand.Cancel:
                    _manager.Clear(_focusSet, _focusSlot);
                    Plugin.Instance?.LogSource.LogInfo(
                        $"Controller cleared Set {_focusSet + 1} {FormatSlot(_focusSlot)}.");
                    Plugin.Instance?.ShowHud($"Cleared Set {_focusSet + 1} {FormatSlot(_focusSlot)}.");
                    break;
            }
            Plugin.Instance?.LogSource.LogInfo(
                $"Berserker slot focus: Set {_focusSet + 1}, {FormatSlot(_focusSlot)}, command={command}.");
            Refresh();
            return true;
        }

        public void Hide()
        {
            CancelAssignment();
            _panelFocused = false;
            _inventoryGui = null;
            _playerGrid = null;
            _lastObservedActiveGroup = int.MinValue;
            if (_root != null) _root.SetActive(false);
        }

        public void Dispose()
        {
            _manager.Changed -= Refresh;
            _config.PanelPositionX.SettingChanged -= OnLayoutSettingChanged;
            _config.PanelPositionY.SettingChanged -= OnLayoutSettingChanged;
            _config.PanelScale.SettingChanged -= OnLayoutSettingChanged;
            DestroyView();
        }

        public void DestroyView()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _inventoryAnchor = null;
            _inventoryGui = null;
            _playerGrid = null;
            _uiGroup = null;
            _uiGroupIndex = -1;
            _lastObservedActiveGroup = int.MinValue;
            _panelOutline = null;
            _titleBackground = null;
            _heading = null;
            _rows.Clear();
            _assignSet = -1;
            _panelFocused = false;
        }

        public bool HandleSelectedInventoryItem(ItemDrop.ItemData item)
        {
            if (_assignSet < 0) return false;
            int assignedSet = _assignSet;
            LoadoutSlot assignedSlot = _assignSlot;
            string message;
            bool assigned = _manager.Assign(_assignSet, _assignSlot, item, out message);
            Plugin.Instance?.LogSource.LogInfo(
                $"Inventory item selection for Berserker assignment: assigned={assigned}, " +
                $"item='{item?.m_shared?.m_name ?? "<null>"}', message='{message}'.");
            Plugin.Instance?.ShowHud(message);
            ShowPanelFeedback(message, !assigned);
            if (assigned)
            {
                CancelAssignment();
                ReturnFocusToBerserker(assignedSet, assignedSlot);
            }
            else Refresh();
            return true;
        }

        private void Create(InventoryGui gui)
        {
            _inventoryAnchor = FindInventoryPanelRect(gui);
            Transform parent = _inventoryAnchor != null ? _inventoryAnchor : gui.transform;
            _root = new GameObject("BerserkerSetsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(CanvasGroup), typeof(UIGroupHandler));
            _root.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)_root.transform;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            _root.GetComponent<Image>().color = new Color(0.055f, 0.032f, 0.018f, 0.985f);
            _panelOutline = _root.GetComponent<Outline>();
            _panelOutline.effectColor = new Color(0.72f, 0.43f, 0.12f, 1f);
            _panelOutline.effectDistance = new Vector2(2f, -2f);

            Font font = GUIManager.Instance?.AveriaSerif;
            _titleBackground = CreateImage(_root.transform, "TitleBar", new Vector2(4f, -4f), new Vector2(230f, 38f),
                new Color(0.20f, 0.095f, 0.032f, 1f));
            AddDragHandlers(_titleBackground.gameObject);
            _heading = CreateText(_titleBackground.transform, "◆  BERSERKER SETS  ◆", Vector2.zero, new Vector2(230f, 38f), font, 18, TextAnchor.MiddleCenter);
            _heading.color = new Color(1f, 0.72f, 0.27f, 1f);

            _prompt = new InputPromptStrip(_root.transform, new Vector2(7f, -44f), new Vector2(224f, 25f), font);
            _feedbackText = CreateText(_root.transform, string.Empty, new Vector2(8f, -43f), new Vector2(222f, 25f), font, 10, TextAnchor.MiddleCenter);
            _feedbackText.color = new Color(1f, 0.73f, 0.28f, 1f);
            _feedbackText.resizeTextForBestFit = true;
            _feedbackText.resizeTextMinSize = 8;
            _feedbackText.gameObject.SetActive(false);
            Text mainHeader = CreateText(_root.transform, "MAIN", new Vector2(54f, -68f), new Vector2(78f, 17f), font, 10, TextAnchor.MiddleCenter);
            Text offHeader = CreateText(_root.transform, "OFF", new Vector2(142f, -68f), new Vector2(78f, 17f), font, 10, TextAnchor.MiddleCenter);
            mainHeader.color = offHeader.color = new Color(0.70f, 0.49f, 0.21f, 1f);

            string[] numerals = { "I", "II", "III", "IV", "V" };
            for (int i = 0; i < CharacterLoadoutData.SetCount; ++i)
            {
                int setIndex = i;
                float y = -87f - i * 46f;
                Image rowBackground = CreateImage(_root.transform, $"Set{i + 1}", new Vector2(7f, y), new Vector2(224f, 42f),
                    new Color(0.105f, 0.062f, 0.032f, 0.94f));
                Text number = CreateText(rowBackground.transform, numerals[i], new Vector2(2f, 0f), new Vector2(40f, 42f), font, 14, TextAnchor.MiddleCenter);
                number.color = new Color(0.92f, 0.66f, 0.27f, 1f);

                var row = new Row { Background = rowBackground };
                row.Main = new LoadoutSlotView(rowBackground.transform, "MainHand", new Vector2(45f, 0f), new Vector2(82f, 42f), font,
                    button => OnSlotClick(setIndex, LoadoutSlot.MainHand, button));
                row.Off = new LoadoutSlotView(rowBackground.transform, "OffHand", new Vector2(133f, 0f), new Vector2(82f, 42f), font,
                    button => OnSlotClick(setIndex, LoadoutSlot.OffHand, button));
                _rows.Add(row);
            }
            _uiGroup = _root.GetComponent<UIGroupHandler>();
            _uiGroup.m_defaultElement = _rows[0].Main.GameObject;
            _uiGroup.m_setDefaultOnKBM = false;
            _uiGroup.m_resetActiveElementOnStateChange = true;
            RegisterUiGroup(gui);
            PositionPanel();
        }

        private void OnSlotClick(int setIndex, LoadoutSlot slot, PointerEventData.InputButton button)
        {
            _panelFocused = false;
            if (button == PointerEventData.InputButton.Right)
            {
                _manager.Clear(setIndex, slot);
                if (_assignSet == setIndex && _assignSlot == slot) CancelAssignment();
                Plugin.Instance?.ShowHud($"Cleared Set {setIndex + 1} {FormatSlot(slot)}.");
                return;
            }
            if (button == PointerEventData.InputButton.Left) BeginAssignment(setIndex, slot, false);
        }

        private void BeginAssignment(int setIndex, LoadoutSlot slot, bool returnToInventory)
        {
            _assignSet = setIndex;
            _assignSlot = slot;
            Plugin.Instance?.ShowHud($"Select an inventory item for Set {setIndex + 1} {FormatSlot(slot)}.");
            Plugin.Instance?.LogSource.LogInfo(
                $"Controller assignment started: Set {setIndex + 1}, {FormatSlot(slot)}, returnToInventory={returnToInventory}.");
            if (returnToInventory) ReturnFocusToInventory();
            UpdatePrompt(true);
            Refresh();
        }

        private void ReturnFocusToInventory()
        {
            _panelFocused = false;
            try { SetActiveGroupMethod?.Invoke(_inventoryGui, new object[] { _inventoryGroupIndex, true }); }
            catch (Exception ex) { Plugin.Instance?.LogSource.LogWarning($"Could not return controller focus to inventory: {ex.Message}"); }
            _playerGrid?.SetGamepadSelection(_lastInventorySelection);
            Plugin.Instance?.LogSource.LogInfo(
                $"Controller focus returned to inventory group {_inventoryGroupIndex}; selection={_lastInventorySelection}.");
        }

        private void ReturnFocusToBerserker(int setIndex, LoadoutSlot slot)
        {
            _focusSet = Mathf.Clamp(setIndex, 0, CharacterLoadoutData.SetCount - 1);
            _focusSlot = slot;
            try
            {
                SetActiveGroupMethod?.Invoke(_inventoryGui, new object[] { _uiGroupIndex, true });
                var groups = UiGroupsField?.GetValue(_inventoryGui) as UIGroupHandler[];
                if (groups != null)
                {
                    for (int i = 0; i < groups.Length; ++i)
                        if (groups[i] != null) groups[i].SetActive(i == _uiGroupIndex);
                }
                _panelFocused = true;
                _lastObservedActiveGroup = _uiGroupIndex;
                Plugin.Instance?.LogSource.LogInfo(
                    $"Controller focus returned to Berserker group {_uiGroupIndex}: " +
                    $"Set {_focusSet + 1} {FormatSlot(_focusSlot)}.");
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogSource.LogWarning($"Could not return controller focus to Berserker Sets: {ex.Message}");
            }
            Refresh();
        }

        private void RegisterUiGroup(InventoryGui gui)
        {
            try
            {
                var groups = UiGroupsField?.GetValue(gui) as UIGroupHandler[] ?? new UIGroupHandler[0];
                UIGroupHandler inventoryGroup = InventoryGroupField?.GetValue(gui) as UIGroupHandler;
                _inventoryGroupIndex = Mathf.Max(0, Array.IndexOf(groups, inventoryGroup));
                if (Array.IndexOf(groups, _uiGroup) < 0)
                {
                    var expanded = new UIGroupHandler[groups.Length + 1];
                    Array.Copy(groups, expanded, groups.Length);
                    expanded[groups.Length] = _uiGroup;
                    UiGroupsField?.SetValue(gui, expanded);
                    groups = expanded;
                }
                _uiGroupIndex = Array.IndexOf(groups, _uiGroup);
                _uiGroup.SetActive(false);
                _lastObservedActiveGroup = int.MinValue;
                Plugin.Instance?.LogSource.LogInfo(
                    $"Registered Berserker Sets inventory group: index={_uiGroupIndex}, " +
                    $"inventoryIndex={_inventoryGroupIndex}, groupCount={groups.Length}, active={GetActiveGroupIndex()}.");
                for (int i = 0; i < groups.Length; ++i)
                {
                    string groupName = groups[i] != null ? groups[i].gameObject.name : "<null>";
                    Plugin.Instance?.LogSource.LogInfo($"Inventory UI group [{i}] = {groupName}.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogSource.LogError($"Could not register Berserker Sets inventory UI group: {ex}");
            }
        }

        private int GetActiveGroupIndex()
        {
            if (_inventoryGui == null || ActiveGroupField == null) return -1;
            try { return (int)ActiveGroupField.GetValue(_inventoryGui); }
            catch (Exception ex)
            {
                Plugin.Instance?.LogSource.LogWarning($"Could not read active inventory UI group: {ex.Message}");
                return -1;
            }
        }

        private bool CycleUiGroup(int direction)
        {
            try
            {
                var groups = UiGroupsField?.GetValue(_inventoryGui) as UIGroupHandler[];
                if (groups == null || groups.Length == 0 || SetActiveGroupMethod == null)
                {
                    Plugin.Instance?.LogSource.LogWarning("Cannot cycle inventory UI groups: group array or SetActiveGroup is unavailable.");
                    return false;
                }

                int current = GetActiveGroupIndex();
                if (current < 0 || current >= groups.Length) current = _inventoryGroupIndex;
                int target = (current + direction + groups.Length) % groups.Length;
                SetActiveGroupMethod.Invoke(_inventoryGui, new object[] { target, true });

                // Valheim knows its original groups, but explicitly synchronizing all handlers also
                // guarantees that a group appended by the mod receives its selected state.
                for (int i = 0; i < groups.Length; ++i)
                {
                    if (groups[i] != null) groups[i].SetActive(i == target);
                }

                _panelFocused = target == _uiGroupIndex;
                if (_panelFocused)
                {
                    _lastInventorySelection = GetInventorySelection();
                    _focusSet = Mathf.Clamp(_focusSet, 0, CharacterLoadoutData.SetCount - 1);
                }
                _lastObservedActiveGroup = target;
                Plugin.Instance?.LogSource.LogInfo(
                    $"Controller UI group cycle: {current} -> {target} of {groups.Length}; " +
                    $"berserkerSelected={_panelFocused}, handlerActive={(_uiGroup != null && _uiGroup.IsActive)}.");
                Refresh();
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogSource.LogError($"Controller UI group cycle failed: {ex}");
                return false;
            }
        }

        private Vector2i GetInventorySelection()
        {
            if (_playerGrid == null || GridSelectionField == null) return new Vector2i(0, 0);
            try { return (Vector2i)GridSelectionField.GetValue(_playerGrid); }
            catch { return new Vector2i(0, 0); }
        }

        private int GetInventoryWidth()
        {
            if (_playerGrid == null || GridWidthField == null) return 1;
            try { return Mathf.Max(1, (int)GridWidthField.GetValue(_playerGrid)); }
            catch { return 1; }
        }

        private void PositionPanel()
        {
            if (_root == null) return;
            RectTransform panel = (RectTransform)_root.transform;
            float preferredScale = Mathf.Clamp(_config.PanelScale.Value, 0.55f, 1.25f);
            float leftPixels = Screen.width * 0.5f;
            float rightPixels = leftPixels;
            float pixelsPerLocalUnit = 1f;

            if (_inventoryAnchor != null)
            {
                Canvas canvas = _inventoryAnchor.GetComponentInParent<Canvas>();
                Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                var corners = new Vector3[4];
                _inventoryAnchor.GetWorldCorners(corners);
                Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 topRight = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                leftPixels = Mathf.Max(0f, bottomLeft.x - EdgeMargin);
                rightPixels = Mathf.Max(0f, Screen.width - topRight.x - EdgeMargin);
                Vector2 unit0 = RectTransformUtility.WorldToScreenPoint(camera, _inventoryAnchor.TransformPoint(Vector3.zero));
                Vector2 unit1 = RectTransformUtility.WorldToScreenPoint(camera, _inventoryAnchor.TransformPoint(Vector3.right));
                pixelsPerLocalUnit = Mathf.Max(0.01f, Mathf.Abs(unit1.x - unit0.x));
            }

            float desiredPixels = PanelWidth * pixelsPerLocalUnit * preferredScale;
            _placedRight = rightPixels >= desiredPixels || rightPixels >= leftPixels;
            float availablePixels = _placedRight ? rightPixels : leftPixels;
            float fittingScale = availablePixels / (PanelWidth * pixelsPerLocalUnit);
            float scale = Mathf.Clamp(Mathf.Min(preferredScale, fittingScale), 0.55f, preferredScale);
            panel.localScale = new Vector3(scale, scale, 1f);
            panel.anchorMin = new Vector2(_placedRight ? 1f : 0f, 1f);
            panel.anchorMax = panel.anchorMin;
            panel.pivot = new Vector2(_placedRight ? 0f : 1f, 1f);
            _defaultAnchoredPosition = new Vector2(_placedRight ? DefaultPanelGap : -DefaultPanelGap, 0f);
            if (!_dragging)
            {
                panel.anchoredPosition = _defaultAnchoredPosition + new Vector2(
                    _config.PanelPositionX.Value,
                    _config.PanelPositionY.Value);
            }
        }

        private void AddDragHandlers(GameObject title)
        {
            EventTrigger trigger = title.GetComponent<EventTrigger>() ?? title.AddComponent<EventTrigger>();
            var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDrag.callback.AddListener(data => OnHeaderBeginDrag((PointerEventData)data));
            trigger.triggers.Add(beginDrag);
            var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            drag.callback.AddListener(data => OnHeaderDrag((PointerEventData)data));
            trigger.triggers.Add(drag);
            var endDrag = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDrag.callback.AddListener(data => OnHeaderEndDrag((PointerEventData)data));
            trigger.triggers.Add(endDrag);
            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(data =>
            {
                var pointer = (PointerEventData)data;
                if (pointer.button != PointerEventData.InputButton.Right) return;
                _config.PanelPositionX.Value = 0f;
                _config.PanelPositionY.Value = 0f;
                Plugin.Instance?.ShowHud("Berserker Sets panel position reset.");
            });
            trigger.triggers.Add(click);
        }

        private void OnHeaderBeginDrag(PointerEventData data)
        {
            _dragging = true;
            _dragStartScreen = data.position;
            _dragStartOffset = new Vector2(_config.PanelPositionX.Value, _config.PanelPositionY.Value);
            _dragCurrentOffset = _dragStartOffset;
        }

        private void OnHeaderDrag(PointerEventData data)
        {
            Canvas canvas = _root != null ? _root.GetComponentInParent<Canvas>() : null;
            float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            _dragCurrentOffset = _dragStartOffset + (data.position - _dragStartScreen) / scale;
            if (_root != null)
            {
                ((RectTransform)_root.transform).anchoredPosition = _defaultAnchoredPosition + _dragCurrentOffset;
            }
        }

        private void OnHeaderEndDrag(PointerEventData data)
        {
            if (!_dragging) return;
            _dragging = false;
            _config.PanelPositionX.Value = _dragCurrentOffset.x;
            _config.PanelPositionY.Value = _dragCurrentOffset.y;
        }

        private void OnLayoutSettingChanged(object sender, EventArgs args) => PositionPanel();

        private void CancelAssignment()
        {
            if (_assignSet >= 0)
                Plugin.Instance?.LogSource.LogInfo(
                    $"Controller assignment cancelled for Set {_assignSet + 1} {FormatSlot(_assignSlot)}.");
            _assignSet = -1;
            UpdatePrompt(true);
            Refresh();
        }

        private void ShowPanelFeedback(string message, bool warning)
        {
            if (_feedbackText == null || string.IsNullOrWhiteSpace(message)) return;
            _feedbackText.text = message;
            _feedbackText.color = warning
                ? new Color(1f, 0.43f, 0.28f, 1f)
                : new Color(1f, 0.76f, 0.32f, 1f);
            _feedbackUntil = Time.unscaledTime + 3.5f;
            _prompt?.SetVisible(false);
            _feedbackText.gameObject.SetActive(true);
        }

        private void UpdatePrompt(bool force)
        {
            if (_prompt == null) return;
            bool gamepad = _input.IsGamepadActive;
            if (!force && gamepad == _lastPromptWasGamepad) return;
            _lastPromptWasGamepad = gamepad;
            if (_assignSet >= 0)
            {
                if (gamepad) _prompt.Set("A", "Assign", "B", "Cancel");
                else _prompt.Set("LMB", "Assign", "ESC", "Cancel");
            }
            else
            {
                if (gamepad) _prompt.Set("A", "Assign", "B", "Clear", "LB/RB", "Group");
                else _prompt.Set("LMB", "Assign", "RMB", "Clear", "DRAG", "Move");
            }
        }

        private void Refresh()
        {
            if (_root == null || !_root.activeSelf || !_manager.IsReady || _manager.Data == null) return;
            UpdatePrompt(false);
            if (_panelOutline != null)
            {
                _panelOutline.effectColor = _panelFocused
                    ? new Color(1f, 0.72f, 0.20f, 1f)
                    : new Color(0.48f, 0.27f, 0.09f, 1f);
                _panelOutline.effectDistance = _panelFocused ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
            }
            if (_titleBackground != null)
                _titleBackground.color = _panelFocused ? new Color(0.34f, 0.16f, 0.035f, 1f) : new Color(0.20f, 0.095f, 0.032f, 1f);
            if (_heading != null)
                _heading.color = _panelFocused ? new Color(1f, 0.88f, 0.48f, 1f) : new Color(1f, 0.72f, 0.27f, 1f);
            for (int i = 0; i < _rows.Count; ++i)
            {
                LoadoutSet set = _manager.Data.Sets[i];
                _rows[i].Background.color = i == _manager.Data.LastActiveSetIndex
                    ? new Color(0.30f, 0.14f, 0.035f, 0.98f)
                    : new Color(0.105f, 0.062f, 0.032f, 0.94f);
                _rows[i].Main.Set(set.MainHand, _manager.Resolve(set.MainHand),
                    _assignSet == i && _assignSlot == LoadoutSlot.MainHand,
                    _panelFocused && _focusSet == i && _focusSlot == LoadoutSlot.MainHand);
                _rows[i].Off.Set(set.OffHand, _manager.Resolve(set.OffHand),
                    _assignSet == i && _assignSlot == LoadoutSlot.OffHand,
                    _panelFocused && _focusSet == i && _focusSlot == LoadoutSlot.OffHand);
            }
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = obj.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string value, Vector2 position, Vector2 size, Font font, int fontSize, TextAnchor alignment)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = obj.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform FindInventoryPanelRect(InventoryGui gui)
        {
            FieldInfo field = typeof(InventoryGui).GetField("m_player", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var component = field?.GetValue(gui) as Component;
            RectTransform grid = component != null ? component.transform as RectTransform : null;
            if (grid == null) return gui.transform as RectTransform;

            RectTransform best = grid;
            float bestWidth = GetScreenWidth(grid);
            Transform current = grid.parent;
            while (current != null && current != gui.transform)
            {
                RectTransform candidate = current as RectTransform;
                float width = candidate != null ? GetScreenWidth(candidate) : 0f;
                if (candidate != null && candidate.rect.height > 0f && width >= bestWidth && width <= Screen.width * 0.85f)
                {
                    best = candidate;
                    bestWidth = width;
                }
                current = current.parent;
            }
            return best;
        }

        private static float GetScreenWidth(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Mathf.Abs(
                RectTransformUtility.WorldToScreenPoint(camera, corners[2]).x -
                RectTransformUtility.WorldToScreenPoint(camera, corners[0]).x);
        }

        private static string FormatSlot(LoadoutSlot slot) => slot == LoadoutSlot.MainHand ? "main hand" : "off hand";
    }
}
