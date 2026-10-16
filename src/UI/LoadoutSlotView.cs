using System;
using BerserkerSets.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BerserkerSets.UI
{
    internal sealed class LoadoutSlotView
    {
        private static readonly Color EmptyColor = new Color(0.105f, 0.075f, 0.045f, 0.98f);
        private static readonly Color ReadyColor = new Color(0.16f, 0.105f, 0.055f, 0.98f);
        private static readonly Color MissingColor = new Color(0.30f, 0.075f, 0.045f, 0.98f);
        private static readonly Color FocusColor = new Color(0.48f, 0.255f, 0.055f, 1f);

        private readonly Image _background;
        private readonly Image _icon;
        private readonly Text _glyph;
        private readonly Outline _outline;
        private readonly Action<PointerEventData.InputButton> _clicked;

        public GameObject GameObject { get; }

        public LoadoutSlotView(Transform parent, string name, Vector2 position, Vector2 size,
            Font font, Action<PointerEventData.InputButton> clicked)
        {
            _clicked = clicked;
            GameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button), typeof(EventTrigger));
            GameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)GameObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            _background = GameObject.GetComponent<Image>();
            _background.color = EmptyColor;
            Button button = GameObject.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = _background;
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            _outline = GameObject.GetComponent<Outline>();
            _outline.effectColor = new Color(0.37f, 0.22f, 0.08f, 0.9f);
            _outline.effectDistance = new Vector2(1f, -1f);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(GameObject.transform, false);
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(40f, 40f);
            _icon = iconObject.GetComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;

            var glyphObject = new GameObject("Glyph", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            glyphObject.transform.SetParent(GameObject.transform, false);
            RectTransform glyphRect = (RectTransform)glyphObject.transform;
            glyphRect.anchorMin = Vector2.zero;
            glyphRect.anchorMax = Vector2.one;
            glyphRect.offsetMin = Vector2.zero;
            glyphRect.offsetMax = Vector2.zero;
            _glyph = glyphObject.GetComponent<Text>();
            _glyph.font = font;
            _glyph.fontSize = 25;
            _glyph.alignment = TextAnchor.MiddleCenter;
            _glyph.color = new Color(0.67f, 0.49f, 0.22f, 0.8f);
            _glyph.raycastTarget = false;

            EventTrigger trigger = GameObject.GetComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(data => _clicked(((PointerEventData)data).button));
            trigger.triggers.Add(entry);
        }

        public void Set(LoadoutItemReference reference, ItemDrop.ItemData resolved, bool assigning, bool focused)
        {
            bool available = reference != null && resolved != null;
            bool missing = reference != null && resolved == null;
            _icon.sprite = available ? resolved.GetIcon() : null;
            _icon.enabled = _icon.sprite != null;
            _glyph.text = reference == null ? "+" : missing ? "?" : string.Empty;
            _glyph.color = missing
                ? new Color(1f, 0.35f, 0.22f, 0.95f)
                : new Color(0.67f, 0.49f, 0.22f, 0.75f);

            _background.color = assigning || focused
                ? FocusColor
                : missing ? MissingColor : available ? ReadyColor : EmptyColor;
            _outline.effectColor = assigning || focused
                ? new Color(1f, 0.66f, 0.18f, 1f)
                : new Color(0.37f, 0.22f, 0.08f, 0.9f);
            _outline.effectDistance = assigning || focused ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
        }
    }
}
