using UnityEngine;
using UnityEngine.UI;

namespace BerserkerSets.UI
{
    internal sealed class InputPromptStrip
    {
        private static Sprite _circleSprite;
        private readonly GameObject _root;
        private readonly Font _font;

        public InputPromptStrip(Transform parent, Vector2 position, Vector2 size, Font font)
        {
            _font = font;
            _root = new GameObject("InputPrompts", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            _root.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)_root.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            HorizontalLayoutGroup layout = _root.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        public void Set(params string[] keyActionPairs)
        {
            for (int i = _root.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(_root.transform.GetChild(i).gameObject);
            for (int i = 0; i + 1 < keyActionPairs.Length; i += 2)
                CreatePrompt(keyActionPairs[i], keyActionPairs[i + 1]);
        }

        public void SetVisible(bool visible) => _root.SetActive(visible);

        private void CreatePrompt(string key, string action)
        {
            bool faceButton = key == "A" || key == "B";
            float keyWidth = faceButton ? 21f : Mathf.Max(27f, 10f + key.Length * 7f);
            float actionWidth = Mathf.Max(28f, action.Length * 5.8f);
            var pair = new GameObject("Prompt", typeof(RectTransform), typeof(LayoutElement));
            pair.transform.SetParent(_root.transform, false);
            RectTransform pairRect = (RectTransform)pair.transform;
            pairRect.sizeDelta = new Vector2(keyWidth + actionWidth + 5f, 23f);
            LayoutElement element = pair.GetComponent<LayoutElement>();
            element.preferredWidth = pairRect.sizeDelta.x;
            element.preferredHeight = 23f;

            var keyObject = new GameObject("Keycap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            keyObject.transform.SetParent(pair.transform, false);
            RectTransform keyRect = (RectTransform)keyObject.transform;
            keyRect.anchorMin = new Vector2(0f, 0.5f);
            keyRect.anchorMax = new Vector2(0f, 0.5f);
            keyRect.pivot = new Vector2(0f, 0.5f);
            keyRect.anchoredPosition = Vector2.zero;
            keyRect.sizeDelta = new Vector2(keyWidth, 20f);
            Image keyImage = keyObject.GetComponent<Image>();
            keyImage.sprite = faceButton ? GetCircleSprite() : null;
            keyImage.color = key == "A"
                ? new Color(0.18f, 0.58f, 0.28f, 1f)
                : key == "B" ? new Color(0.72f, 0.18f, 0.15f, 1f) : new Color(0.13f, 0.085f, 0.045f, 1f);
            Outline outline = keyObject.GetComponent<Outline>();
            outline.effectColor = faceButton ? new Color(0.95f, 0.95f, 0.90f, 0.9f) : new Color(0.85f, 0.56f, 0.18f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            CreateText(keyObject.transform, key, Vector2.zero, new Vector2(keyWidth, 20f), 10, TextAnchor.MiddleCenter,
                new Color(1f, 0.78f, 0.35f, 1f));

            CreateText(pair.transform, action, new Vector2(keyWidth + 4f, 0f), new Vector2(actionWidth, 23f), 10,
                TextAnchor.MiddleLeft, new Color(0.88f, 0.78f, 0.62f, 1f));
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "BerserkerSetsRoundButton";
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.46f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float alpha = Mathf.Clamp01(radius + 1f - Vector2.Distance(new Vector2(x, y), center));
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            _circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _circleSprite.name = "BerserkerSetsRoundButtonSprite";
            return _circleSprite;
        }

        private Text CreateText(Transform parent, string value, Vector2 position, Vector2 size, int fontSize,
            TextAnchor alignment, Color color)
        {
            var obj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)obj.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Text text = obj.GetComponent<Text>();
            text.text = value;
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }
    }
}
