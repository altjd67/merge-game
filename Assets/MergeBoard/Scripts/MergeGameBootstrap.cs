using UnityEngine;
using UnityEngine.UIElements;

namespace MergeBoard
{
    /// <summary>게임의 진입점으로 Controller와 UI를 생성하고 입력·표시를 연결한다.</summary>
    public sealed class MergeGameBootstrap : MonoBehaviour
    {
        public BoardModel Board { get; private set; }
        public BoardView BoardView { get; private set; }
        public VisualElement ScreenRoot { get; private set; }
        public MergeGameController Controller { get; private set; }
        private Label messageLabel;
        private PanelSettings panelSettings;
        private Font font;

        private void OnEnable()
        {
            Controller = new MergeGameController();
            Board = Controller.Board;
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("MergeTheme");
            var document = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            var Root = document.rootVisualElement;
            Root.Clear();
            Root.style.flexGrow = 1;
            Root.style.backgroundColor = (Color)new Color32(27, 42, 38, 255);
            ScreenRoot = new VisualElement { name = "머지화면" };
            ScreenRoot.style.position = Position.Absolute;
            ScreenRoot.style.width = 480;
            ScreenRoot.style.height = 854;
            ScreenRoot.style.backgroundColor = (Color)new Color32(244, 242, 225, 255);
            ScreenRoot.style.color = (Color)new Color32(47, 65, 51, 255);
            font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 32);
            ScreenRoot.style.unityFontDefinition = FontDefinition.FromFont(font);
            ScreenRoot.style.transformOrigin = new TransformOrigin(0, 0);
            Root.Add(ScreenRoot);
            Root.RegisterCallback<GeometryChangedEvent>(_ => FitScreen(Root));
            BoardView = new BoardView();
            ScreenRoot.Add(BoardView.Root);
            var title = new Label("작은 정원");
            title.style.fontSize = 26;
            title.style.left = 24;
            title.style.top = 20;
            title.style.position = Position.Absolute;
            ScreenRoot.Add(title);
            messageLabel = new Label("씨앗을 드래그해 같은 씨앗과 합쳐보세요.");
            messageLabel.style.position = Position.Absolute;
            messageLabel.style.top = 202;
            messageLabel.style.left = 20;
            messageLabel.style.fontSize = 12;
            ScreenRoot.Add(messageLabel);
            BoardView.Dropped += (source, destination) =>
            {
                var result = Controller.Move(source, destination);
                BoardView.Render(Board);
                messageLabel.text = result.Merged ? BoardView.StageName(Board[destination]) + " 합성!" : result.Message;
            };
            BoardView.Render(Board);
        }

        private void FitScreen(VisualElement Root)
        {
            float scale = Mathf.Min(Root.resolvedStyle.width / 480, Root.resolvedStyle.height / 854);
            ScreenRoot.style.scale = new Scale(new Vector3(scale, scale, 1));
            ScreenRoot.style.left = (Root.resolvedStyle.width - 480 * scale) / 2;
            ScreenRoot.style.top = (Root.resolvedStyle.height - 854 * scale) / 2;
        }

        private void OnDisable()
        {
            BoardView?.CancelDrag();
            if (panelSettings != null) Destroy(panelSettings);
            if (font != null) Destroy(font);
        }

        private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) BoardView?.CancelDrag(); }
    }
}
