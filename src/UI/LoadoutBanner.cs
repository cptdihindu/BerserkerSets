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
        private Image _mainIcon;
        private Image _offIcon;
        private BannerLifetime _lifetime;

        public bool Show(string message, Sprite mainIcon, Sprite offIcon)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;
            if (_root == null && !Create()) return false;
            _text.text = message;
            SetIcon(_mainIcon, mainIcon);
            SetIcon(_offIcon, offIcon);
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
            _mainIcon = null;
            _offIcon = null;
            _lifetime = null;
        }

        private bool Create()
        {
            try
            {
                if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null) return false;
                Transform parent = GUIManager.CustomGUIFront.transform;
                _root = new GameObject("BerserkerSetsSwitchBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Shadow), typeof(Outline), typeof(CanvasGroup), typeof(BannerLifetime));
                _root.transform.SetParent(parent, false);

                RectTransform rect = (RectTransform)_root.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(34f, -126f);
                rect.sizeDelta = new Vector2(292f, 54f);

                Image background = _root.GetComponent<Image>();
                background.color = new Color(0.075f, 0.040f, 0.020f, 0.96f);
                background.raycastTarget = false;
                Shadow shadow = _root.GetComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
                shadow.effectDistance = new Vector2(4f, -4f);
                Outline border = _root.GetComponent<Outline>();
                border.effectColor = new Color(0.70f, 0.42f, 0.13f, 1f);
                border.effectDistance = new Vector2(2f, -2f);

                CreateBar(_root.transform, "TopBronze", new Vector2(0f, -3f));
                CreateBar(_root.transform, "BottomBronze", new Vector2(0f, 3f));

                var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textObject.transform.SetParent(_root.transform, false);
                RectTransform textRect = (RectTransform)textObject.transform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(15f, 6f);
                textRect.offsetMax = new Vector2(-90f, -6f);
                _text = textObject.GetComponent<Text>();
                _text.font = GUIManager.Instance.AveriaSerifBold;
                _text.fontSize = 16;
                _text.color = new Color(1f, 0.86f, 0.50f, 1f);
                _text.alignment = TextAnchor.MiddleCenter;
                _text.resizeTextForBestFit = true;
                _text.resizeTextMinSize = 13;
                _text.raycastTarget = false;

                CreateVerticalDivider(_root.transform, -87f);
                _mainIcon = CreateIcon(_root.transform, "MainIcon", new Vector2(-63f, 0f));
                _offIcon = CreateIcon(_root.transform, "OffIcon", new Vector2(-22f, 0f));

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

        private static Image CreateIcon(Transform parent, string name, Vector2 position)
        {
            var frameObject = new GameObject(name + "Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            frameObject.transform.SetParent(parent, false);
            RectTransform frameRect = (RectTransform)frameObject.transform;
            frameRect.anchorMin = new Vector2(1f, 0.5f);
            frameRect.anchorMax = new Vector2(1f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = position;
            frameRect.sizeDelta = new Vector2(37f, 40f);
            Image frame = frameObject.GetComponent<Image>();
            frame.color = new Color(0.13f, 0.075f, 0.035f, 0.98f);
            frame.raycastTarget = false;
            Outline outline = frameObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.54f, 0.31f, 0.09f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(frameObject.transform, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(32f, 32f);
            Image image = obj.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private static void CreateBar(Transform parent, string name, Vector2 position)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = name.StartsWith("Top") ? new Vector2(0f, 1f) : new Vector2(0f, 0f);
            rect.anchorMax = name.StartsWith("Top") ? new Vector2(1f, 1f) : new Vector2(1f, 0f);
            rect.pivot = name.StartsWith("Top") ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(0f, 2f);
            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.92f, 0.57f, 0.18f, 0.92f);
            image.raycastTarget = false;
        }

        private static void CreateVerticalDivider(Transform parent, float x)
        {
            var obj = new GameObject("BronzeDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(2f, 38f);
            Image image = obj.GetComponent<Image>();
            image.color = new Color(0.64f, 0.36f, 0.10f, 0.95f);
            image.raycastTarget = false;
        }

        private static void SetIcon(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
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
