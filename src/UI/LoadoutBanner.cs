using System;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace BerserkerSets.UI
{
    internal sealed class LoadoutBanner
    {
        private GameObject _root;
        private Text _text;
        private BannerLifetime _lifetime;

        public bool Show(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            if (_root == null && !Create()) return false;
            _text.text = message;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _lifetime.Display(2.5f);
            return true;
        }

        public void Dispose()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            _text = null;
            _lifetime = null;
        }

        private bool Create()
        {
            try
            {
                if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null) return false;
                Transform parent = GUIManager.CustomGUIFront.transform;
                _root = new GameObject("BerserkerSetsSwitchBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(BannerLifetime));
                _root.transform.SetParent(parent, false);

                RectTransform rect = (RectTransform)_root.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(22f, -82f);
                rect.sizeDelta = new Vector2(390f, 52f);

                Image background = _root.GetComponent<Image>();
                background.color = new Color(0.055f, 0.035f, 0.02f, 0.90f);
                background.raycastTarget = false;

                var accentObject = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                accentObject.transform.SetParent(_root.transform, false);
                RectTransform accentRect = (RectTransform)accentObject.transform;
                accentRect.anchorMin = new Vector2(0f, 0f);
                accentRect.anchorMax = new Vector2(0f, 1f);
                accentRect.pivot = new Vector2(0f, 0.5f);
                accentRect.anchoredPosition = Vector2.zero;
                accentRect.sizeDelta = new Vector2(5f, 0f);
                Image accent = accentObject.GetComponent<Image>();
                accent.color = new Color(0.95f, 0.55f, 0.12f, 1f);
                accent.raycastTarget = false;

                var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textObject.transform.SetParent(_root.transform, false);
                RectTransform textRect = (RectTransform)textObject.transform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(16f, 4f);
                textRect.offsetMax = new Vector2(-10f, -4f);
                _text = textObject.GetComponent<Text>();
                _text.font = GUIManager.Instance.AveriaSerifBold;
                _text.fontSize = 17;
                _text.color = Color.white;
                _text.alignment = TextAnchor.MiddleLeft;
                _text.resizeTextForBestFit = true;
                _text.resizeTextMinSize = 12;
                _text.raycastTarget = false;

                _lifetime = _root.GetComponent<BannerLifetime>();
                _lifetime.Group = _root.GetComponent<CanvasGroup>();
                _root.SetActive(false);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Instance?.LogSource.LogWarning($"Could not create the loadout switch banner: {ex.Message}");
                Dispose();
                return false;
            }
        }
    }

    internal sealed class BannerLifetime : MonoBehaviour
    {
        private const float FadeSeconds = 0.35f;
        private float _hideAt;

        public CanvasGroup Group { get; set; }

        public void Display(float seconds)
        {
            _hideAt = Time.unscaledTime + seconds;
            if (Group != null) Group.alpha = 1f;
        }

        private void Update()
        {
            float remaining = _hideAt - Time.unscaledTime;
            if (remaining <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }
            if (Group != null) Group.alpha = Mathf.Clamp01(remaining / FadeSeconds);
        }
    }
}
