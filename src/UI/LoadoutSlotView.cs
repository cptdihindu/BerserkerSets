using System;
using BerserkerSets.Loadouts;
using BerserkerSets.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BerserkerSets.UI
{
    internal sealed class LoadoutSlotView
    {
        private readonly Image _background;
        private readonly Image _icon;
        private readonly Text _label;
        private readonly Action<PointerEventData.InputButton> _clicked;

        public GameObject GameObject { get; }

        public LoadoutSlotView(Transform parent, string name, Vector2 position, Vector2 size,
            Font font, Action<PointerEventData.InputButton> clicked)
        {
            _clicked = clicked;
            GameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(EventTrigger));
            GameObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)GameObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            _background = GameObject.GetComponent<Image>();
            _background.color = new Color(0.12f, 0.10f, 0.08f, 0.95f);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(GameObject.transform, false);
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(4f, 0f);
            iconRect.sizeDelta = new Vector2(38f, 38f);
            _icon = iconObject.GetComponent<Image>();
            _icon.preserveAspect = true;
            _icon.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(GameObject.transform, false);
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(46f, 2f);
            labelRect.offsetMax = new Vector2(-3f, -2f);
            _label = labelObject.GetComponent<Text>();
            _label.font = font;
            _label.fontSize = 14;
            _label.alignment = TextAnchor.MiddleLeft;
            _label.color = Color.white;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 9;
            _label.raycastTarget = false;

            EventTrigger trigger = GameObject.GetComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(data => _clicked(((PointerEventData)data).button));
            trigger.triggers.Add(entry);
        }

        public void Set(LoadoutItemReference reference, ItemDrop.ItemData resolved, bool assigning)
        {
            if (reference == null)
            {
                _label.text = "Empty";
                _label.color = new Color(0.75f, 0.75f, 0.75f);
                _icon.enabled = false;
                _background.color = assigning ? new Color(0.55f, 0.38f, 0.08f, 0.98f) : new Color(0.12f, 0.10f, 0.08f, 0.95f);
                return;
            }

            bool available = resolved != null;
            _label.text = available ? ItemResolver.GetDisplayName(resolved) : reference.DisplayName + "\n(Missing)";
            _label.color = available ? Color.white : new Color(1f, 0.45f, 0.40f);
            Sprite icon = available ? resolved.GetIcon() : null;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _background.color = assigning
                ? new Color(0.55f, 0.38f, 0.08f, 0.98f)
                : available ? new Color(0.12f, 0.10f, 0.08f, 0.95f) : new Color(0.34f, 0.09f, 0.07f, 0.95f);
        }
    }
}
