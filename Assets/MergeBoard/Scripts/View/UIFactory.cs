using UnityEngine;
using UnityEngine.UI;

namespace MergeBoard
{
    /// <summary>장면 생성에 반복 사용되는 uGUI 요소의 좌표·글꼴·색상 설정을 모은다.</summary>
    public static class UIFactory
    {
        /// <summary>부모 좌측 상단을 기준으로 위치와 크기가 고정된 RectTransform을 만든다.</summary>
        public static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
            return rect;
        }

        /// <summary>한국어 동적 글꼴을 사용하는 표시 전용 Text를 만든다.</summary>
        public static Text CreateText(Transform parent, string name, Vector2 position, Vector2 size, string content, int fontSize, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = CreateRect(parent, name, position, size).gameObject.AddComponent<Text>();
            text.font = Resources.Load<Font>("KoreanFont");
            text.fontSize = fontSize;
            text.text = content;
            text.color = new Color32(47, 65, 51, 255);
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Image와 Text를 가진 uGUI Button을 만든다.</summary>
        public static Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size, string label)
        {
            var rect = CreateRect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color32(80, 127, 86, 255);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = new Color(1, 1, 1, 0.4f);
            button.colors = colors;
            var text = CreateText(rect, "문구", Vector2.zero, size, label, 14, TextAnchor.MiddleCenter);
            text.color = Color.white;
            return button;
        }

        /// <summary>별도 텍스처 없이 단계별 아이템을 그리는 uGUI Graphic을 만든다.</summary>
        public static ItemGraphic CreateItem(Transform parent, string name, Vector2 position, ItemStage stage)
        {
            var icon = CreateRect(parent, name, position, new Vector2(60, 48)).gameObject.AddComponent<ItemGraphic>();
            icon.raycastTarget = false;
            icon.Stage = stage;
            return icon;
        }
    }
}
