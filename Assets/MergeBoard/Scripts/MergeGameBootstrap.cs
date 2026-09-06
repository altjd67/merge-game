using UnityEngine;
using UnityEngine.UIElements;

namespace MergeBoard
{
    public sealed class MergeGameBootstrap : MonoBehaviour
    {
        public BoardModel 보드 { get; private set; }
        public BoardView 보드화면 { get; private set; }
        public VisualElement 화면 { get; private set; }
        private PanelSettings 패널;
        private Font 글꼴;

        private void OnEnable()
        {
            보드 = new BoardModel();
            패널 = ScriptableObject.CreateInstance<PanelSettings>();
            패널.scaleMode = PanelScaleMode.ConstantPixelSize;
            패널.themeStyleSheet = Resources.Load<ThemeStyleSheet>("MergeTheme");
            var 문서 = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            문서.panelSettings = 패널;
            var 루트 = 문서.rootVisualElement;
            루트.Clear();
            루트.style.flexGrow = 1;
            루트.style.backgroundColor = (Color)new Color32(27, 42, 38, 255);
            화면 = new VisualElement { name = "머지화면" };
            화면.style.position = Position.Absolute;
            화면.style.width = 480;
            화면.style.height = 854;
            화면.style.backgroundColor = (Color)new Color32(244, 242, 225, 255);
            화면.style.color = (Color)new Color32(47, 65, 51, 255);
            글꼴 = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 32);
            화면.style.unityFontDefinition = FontDefinition.FromFont(글꼴);
            화면.style.transformOrigin = new TransformOrigin(0, 0);
            루트.Add(화면);
            루트.RegisterCallback<GeometryChangedEvent>(_ => 크기맞춤(루트));
            보드화면 = new BoardView();
            화면.Add(보드화면.루트);
            var 제목 = new Label("작은 정원");
            제목.style.fontSize = 26;
            제목.style.left = 24;
            제목.style.top = 20;
            제목.style.position = Position.Absolute;
            화면.Add(제목);
            보드화면.표시(보드);
        }

        private void 크기맞춤(VisualElement 루트)
        {
            float 배율 = Mathf.Min(루트.resolvedStyle.width / 480, 루트.resolvedStyle.height / 854);
            화면.style.scale = new Scale(new Vector3(배율, 배율, 1));
            화면.style.left = (루트.resolvedStyle.width - 480 * 배율) / 2;
            화면.style.top = (루트.resolvedStyle.height - 854 * 배율) / 2;
        }

        private void OnDisable()
        {
            if (패널 != null) Destroy(패널);
            if (글꼴 != null) Destroy(글꼴);
        }
    }
}
