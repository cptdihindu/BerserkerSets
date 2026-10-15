using System;
using System.Collections.Generic;
using System.Reflection;
using BerserkerSets.Configuration;
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
        private const float PanelWidth = 260f;
        private const float PanelHeight = 302f;
        private const float EdgeMargin = 8f;
        private const float DefaultPanelGap = 20f;

        private sealed class Row
        {
            public Image Background;
            public LoadoutSlotView Main;
            public LoadoutSlotView Off;
        }

        private readonly LoadoutManager _manager;
        private readonly ModConfig _config;
        private readonly List<Row> _rows = new List<Row>();
        private GameObject _root;
        private RectTransform _inventoryAnchor;
        private int _assignSet = -1;
        private LoadoutSlot _assignSlot;

        public LoadoutPanel(LoadoutManager manager, ModConfig config)
        {
            _manager = manager;
            _config = config;
            _manager.Changed += Refresh;
            _config.PanelPositionX.SettingChanged += OnLayoutSettingChanged;
            _config.PanelPositionY.SettingChanged += OnLayoutSettingChanged;
            _config.PanelScale.SettingChanged += OnLayoutSettingChanged;
        }

        public void Show(InventoryGui gui)
        {
            if (!_config.EnableInventoryPanel.Value || gui == null) return;
            if (_root == null) Create(gui);
            if (_root != null)
            {
                PositionPanel();
                _root.SetActive(true);
                Refresh();
            }
        }

        public void Hide()
        {
            CancelAssignment();
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
            _rows.Clear();
            _assignSet = -1;
        }

        public bool HandleSelectedInventoryItem(ItemDrop.ItemData item)
        {
            if (_assignSet < 0) return false;
            string message;
            bool assigned = _manager.Assign(_assignSet, _assignSlot, item, out message);
            Plugin.Instance?.ShowHud(message);
            if (assigned) CancelAssignment();
            else Refresh();
            return true;
        }

        private void Create(InventoryGui gui)
        {
            _inventoryAnchor = FindInventoryPanelRect(gui);
            Transform parent = _inventoryAnchor != null ? _inventoryAnchor : gui.transform;
            _root = new GameObject("BerserkerSetsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _root.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)_root.transform;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            _root.GetComponent<Image>().color = new Color(0.08f, 0.055f, 0.035f, 0.96f);

            Font font = GUIManager.Instance?.AveriaSerif;
            CreateText(_root.transform, "Berserker Sets", new Vector2(7f, -4f), new Vector2(246f, 32f), font, 20, TextAnchor.MiddleCenter);
            CreateText(_root.transform, "Left: assign   Right: clear", new Vector2(7f, -34f), new Vector2(246f, 20f), font, 12, TextAnchor.MiddleCenter);

            for (int i = 0; i < CharacterLoadoutData.SetCount; ++i)
            {
                int setIndex = i;
                float y = -58f - i * 47f;
                var rowObject = new GameObject($"Set{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                rowObject.transform.SetParent(_root.transform, false);
                RectTransform rowRect = (RectTransform)rowObject.transform;
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(0f, 1f);
                rowRect.pivot = new Vector2(0f, 1f);
                rowRect.anchoredPosition = new Vector2(7f, y);
                rowRect.sizeDelta = new Vector2(246f, 43f);
                Image rowBackground = rowObject.GetComponent<Image>();
                rowBackground.color = new Color(0.18f, 0.14f, 0.09f, 0.8f);
                CreateText(rowObject.transform, $"Set {i + 1}", new Vector2(4f, 0f), new Vector2(43f, 43f), font, 13, TextAnchor.MiddleLeft);

                var row = new Row { Background = rowBackground };
                row.Main = new LoadoutSlotView(rowObject.transform, "MainHand", new Vector2(47f, 0f), new Vector2(96f, 43f), font,
                    button => OnSlotClick(setIndex, LoadoutSlot.MainHand, button));
                row.Off = new LoadoutSlotView(rowObject.transform, "OffHand", new Vector2(147f, 0f), new Vector2(96f, 43f), font,
                    button => OnSlotClick(setIndex, LoadoutSlot.OffHand, button));
                _rows.Add(row);
            }
            PositionPanel();
        }

        private void OnSlotClick(int setIndex, LoadoutSlot slot, PointerEventData.InputButton button)
        {
            if (button == PointerEventData.InputButton.Right)
            {
                _manager.Clear(setIndex, slot);
                if (_assignSet == setIndex && _assignSlot == slot) CancelAssignment();
                Plugin.Instance?.ShowHud($"Cleared Set {setIndex + 1} {FormatSlot(slot)}.");
                return;
            }
            if (button != PointerEventData.InputButton.Left) return;
            _assignSet = setIndex;
            _assignSlot = slot;
            Plugin.Instance?.ShowHud($"Select an inventory item for Set {setIndex + 1} {FormatSlot(slot)}.");
            Refresh();
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
            bool placeRight = rightPixels >= desiredPixels || rightPixels >= leftPixels;
            float availablePixels = placeRight ? rightPixels : leftPixels;
            float fittingScale = availablePixels / (PanelWidth * pixelsPerLocalUnit);
            float scale = Mathf.Clamp(Mathf.Min(preferredScale, fittingScale), 0.55f, preferredScale);
            panel.localScale = new Vector3(scale, scale, 1f);

            panel.anchorMin = new Vector2(placeRight ? 1f : 0f, 1f);
            panel.anchorMax = panel.anchorMin;
            panel.pivot = new Vector2(placeRight ? 0f : 1f, 1f);
            Vector2 defaultPosition = new Vector2(placeRight ? DefaultPanelGap : -DefaultPanelGap, 0f);
            panel.anchoredPosition = defaultPosition + new Vector2(
                _config.PanelPositionX.Value,
                _config.PanelPositionY.Value);
        }

        private void OnLayoutSettingChanged(object sender, EventArgs args)
        {
            PositionPanel();
        }

        private void CancelAssignment()
        {
            _assignSet = -1;
            Refresh();
        }

        private void Refresh()
        {
            if (_root == null || !_root.activeSelf || !_manager.IsReady || _manager.Data == null) return;
            for (int i = 0; i < _rows.Count; ++i)
            {
                LoadoutSet set = _manager.Data.Sets[i];
                _rows[i].Background.color = i == _manager.Data.LastActiveSetIndex
                    ? new Color(0.52f, 0.27f, 0.05f, 0.95f)
                    : new Color(0.18f, 0.14f, 0.09f, 0.8f);
                _rows[i].Main.Set(set.MainHand, _manager.Resolve(set.MainHand), _assignSet == i && _assignSlot == LoadoutSlot.MainHand);
                _rows[i].Off.Set(set.OffHand, _manager.Resolve(set.OffHand), _assignSet == i && _assignSlot == LoadoutSlot.OffHand);
            }
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
