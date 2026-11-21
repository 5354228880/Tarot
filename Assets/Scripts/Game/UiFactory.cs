using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Tarot.Game
{
    public static class UiFactory
    {
        public static readonly Color Bg = new Color(0.08f, 0.08f, 0.12f);
        public static readonly Color Panel = new Color(0.14f, 0.14f, 0.21f);
        public static readonly Color PanelAlt = new Color(0.20f, 0.20f, 0.29f);
        public static readonly Color PlayerCard = new Color(0.62f, 0.78f, 0.94f);
        public static readonly Color EnemyCard = new Color(0.88f, 0.55f, 0.55f);
        public static readonly Color Accent = new Color(0.96f, 0.83f, 0.55f);
        public static readonly Color Text = new Color(0.95f, 0.95f, 0.98f);
        public static readonly Color Muted = new Color(0.65f, 0.65f, 0.72f);
        public static readonly Color Good = new Color(0.55f, 0.90f, 0.65f);
        public static readonly Color Bad = new Color(0.95f, 0.45f, 0.45f);

        // Global readability scale for runtime-built UI.
        public const float FontScale = 1.22f;

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureCamera()
        {
            if (Object.FindFirstObjectByType<Camera>() != null) return;
            var go = new GameObject("Main Camera", typeof(Camera));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bg;
            cam.transform.position = new Vector3(0, 0, -10);
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color, bool raycast = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return (RectTransform)go.transform;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, string text, int size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size * FontScale;
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.enableWordWrapping = true;
            return label;
        }

        public static Button CreateButton(Transform parent, string name, string label, Color color, Vector2 pos, Vector2 size,
            UnityEngine.Events.UnityAction onClick)
        {
            var rt = CreatePanel(parent, name, color);
            SetAnchored(rt, pos, size);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            var text = CreateText(rt, "Label", label, 22, Text, TextAlignmentOptions.Center);
            Stretch((RectTransform)text.transform, 8, 4, 8, 4);
            return button;
        }

        public static void SetAnchored(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Object.Destroy(child);
            }
        }

        public static void SetButtonLabel(Button button, string text)
        {
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = text;
        }

        public static void ScaleFonts(Transform root, float factor)
        {
            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                label.fontSize *= factor;
            }
        }
    }
}
