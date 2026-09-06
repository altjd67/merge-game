using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeBoard
{
    /// <summary>게임 상태와 장면의 uGUI View를 연결하는 진입점이다.</summary>
    public sealed class MergeGameBootstrap : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private OrderView orderView;
        [SerializeField] private HUDView hud;
        [SerializeField] private RectTransform screenRoot;
        public BoardModel Board => Controller.Board;
        public BoardView BoardView => boardView;
        public OrderView OrderView => orderView;
        public HUDView HUD => hud;
        public RectTransform ScreenRoot => screenRoot;
        public MergeGameController Controller { get; private set; }
        private Font runtimeFont;

        /// <summary>로컬 진행 상태를 한 번 복원한 뒤 장면 UI의 입력을 연결한다.</summary>
        private void Start()
        {
            // PC에서 창 포커스를 잃어도 생성기 시간과 제출 피드백은 계속 진행한다.
            Application.runInBackground = true;
            // OS 동적 글꼴의 런타임 Material은 직렬화되지 않으므로 실행 시 다시 생성한다.
            runtimeFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 32);
            foreach (var text in GetComponentsInChildren<Text>(true)) text.font = runtimeFont;
            string savePath = System.IO.Path.Combine(Application.persistentDataPath, "merge-board-v1.json");
#if UNITY_EDITOR
            // 검증은 사용자 저장과 분리하며, SessionState는 Play Mode 재진입에도 유지된다.
            savePath = UnityEditor.SessionState.GetString("MergeBoard.VerificationSavePath", savePath);
#elif DEVELOPMENT_BUILD
            // 개발 빌드 재실행 검증만 별도 파일을 사용하며 일반 빌드에는 포함되지 않는다.
            string verificationPath = System.Environment.GetEnvironmentVariable("MERGE_BOARD_VERIFY_SAVE");
            if (!string.IsNullOrEmpty(verificationPath)) savePath = verificationPath;
#endif
            var saveService = new LocalSaveService(savePath);
            Controller = new MergeGameController(saveService.Load(), saveService);
            string loadMessage = Controller.SaveMessage;
            Controller.RefreshEnergy(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            boardView.Dropped += HandleDrop;
            boardView.DragStateChanged += _ => RefreshViews();
            hud.GenerateClicked += HandleGenerate;
            orderView.GetClicked += HandleOrder;
            RefreshViews();
            if (loadMessage.Length > 0) hud.ShowMessage(loadMessage);
            else if (Controller.SaveMessage.Length > 0) hud.ShowMessage(Controller.SaveMessage);
#if DEVELOPMENT_BUILD
            Debug.Log("MVP_RESTORED " + JsonUtility.ToJson(SaveData.FromState(Controller.State)));
#endif
        }

        /// <summary>드롭 규칙을 처리하고 저장 결과 및 합성 표시를 갱신한다.</summary>
        private void HandleDrop(int source, int destination)
        {
            var result = Controller.Move(source, destination);
            RefreshViews();
            ShowResult(result.Merged ? BoardView.StageName(Board[destination]) + " 합성!" : result.Message);
            if (result.Merged) AnimatorFeedback.PlayScale(boardView.Cells[destination].Icon.transform, "Merge");
        }

        /// <summary>생성 명령과 저장을 완료한 뒤 클릭 피드백을 재생한다.</summary>
        private void HandleGenerate()
        {
            if (boardView.IsDragging) return;
            var result = Controller.Generate(Board.FindCells(ItemStage.SeedPack)[0], System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if (result.Success) AnimatorFeedback.PlayScale(hud.GenerateButton.transform, "Click");
            RefreshViews();
            ShowResult(result.Message);
        }

        /// <summary>주문 상태를 먼저 확정·저장하고 소비된 아이템의 표시만 슬롯으로 비행시킨다.</summary>
        private void HandleOrder(int orderIndex)
        {
            if (boardView.IsDragging) return;
            var result = Controller.SubmitOrder(orderIndex);
            if (result.Success)
            {
                AnimatorFeedback.PlayScale(orderView.GetButtons[orderIndex].transform, "Click");
                var card = orderView.Cards[orderIndex];
                Vector3 destination = card.TransformPoint(new Vector3(72, -26, 0));
                foreach (int index in result.ConsumedCells)
                {
                    var icon = boardView.Cells[index].Icon.rectTransform;
                    AnimatorFeedback.Fly(screenRoot, icon.TransformPoint(icon.rect.center), destination, Controller.State.Orders[orderIndex].RequiredStage);
                }
                AnimatorFeedback.ShowReward(screenRoot, card.TransformPoint(new Vector3(72, -78, 0)), result.Reward, runtimeFont);
            }
            RefreshViews();
            ShowResult(result.Success ? "+" + result.Reward + " 코인!" : "주문에 필요한 아이템이 부족합니다.");
        }

        private void ShowResult(string message) => hud.ShowMessage(Controller.SaveMessage.Length > 0 ? Controller.SaveMessage : message);

        /// <summary>최신 모델과 규칙 판정을 보드·주문·HUD에 전달한다.</summary>
        public void RefreshViews()
        {
            if (Controller == null) return;
            boardView.Render(Board);
            for (int index = 0; index < Controller.State.Orders.Count; index++)
            {
                var order = Controller.State.Orders[index];
                orderView.Render(index, order, Board.FindCells(order.RequiredStage).Count, !boardView.IsDragging && Controller.CanSubmit(index));
            }
            hud.Render(Controller.State.Coins, 0, !boardView.IsDragging && Controller.State.Energy > 0);
        }

        private void Update()
        {
            if (Controller == null) return;
            Controller.RefreshEnergy(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            hud.Render(Controller.State.Coins, 0, !boardView.IsDragging && Controller.State.Energy > 0);
        }

        private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) boardView?.CancelDrag(); }

        private void OnDestroy() { if (runtimeFont != null) Destroy(runtimeFont); }

        /// <summary>Editor 장면 준비 시 한 번 호출해 편집 가능한 uGUI 계층을 생성한다.</summary>
        public void BuildUI()
        {
            if (screenRoot != null) throw new System.InvalidOperationException("기존 UI는 덮어쓰지 않습니다.");
            var canvasRoot = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasRoot.transform.SetParent(transform, false);
            canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(480, 854);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            screenRoot = UIFactory.CreateRect(canvasRoot.transform, "작은 정원", Vector2.zero, new Vector2(480, 854));
            screenRoot.anchorMin = screenRoot.anchorMax = screenRoot.pivot = new Vector2(0.5f, 0.5f);
            screenRoot.gameObject.AddComponent<Image>().color = new Color32(244, 242, 225, 255);
            UIFactory.CreateText(screenRoot, "제목", new Vector2(24, 16), new Vector2(250, 42), "작은 정원", 26);
            var coins = UIFactory.CreateText(screenRoot, "코인", new Vector2(314, 20), new Vector2(142, 32), "0 코인", 20, TextAnchor.MiddleRight);
            var message = UIFactory.CreateText(screenRoot, "안내", new Vector2(20, 196), new Vector2(440, 28), "씨앗을 합쳐 주문을 완성해보세요.", 12);
            var boardRoot = UIFactory.CreateRect(screenRoot, "보드", new Vector2(16, 226), new Vector2(448, 576));
            boardView = boardRoot.gameObject.AddComponent<BoardView>();
            var cells = new CellView[BoardModel.CellCount];
            for (int index = 0; index < cells.Length; index++)
            {
                var cell = UIFactory.CreateRect(boardRoot, "칸 " + index, new Vector2(index % BoardModel.Columns * BoardView.CellPitch, index / BoardModel.Columns * BoardView.CellPitch), new Vector2(BoardView.CellSize, BoardView.CellSize));
                cell.gameObject.AddComponent<Image>().color = (index % BoardModel.Columns + index / BoardModel.Columns) % 2 == 0 ? new Color32(222, 231, 210, 255) : new Color32(210, 222, 198, 255);
                var icon = UIFactory.CreateItem(cell, "아이템", Vector2.zero, ItemStage.Empty);
                var label = UIFactory.CreateText(cell, "이름", new Vector2(0, 44), new Vector2(60, 16), "", 10, TextAnchor.MiddleCenter);
                cells[index] = cell.gameObject.AddComponent<CellView>();
                cells[index].Configure(boardView, index, icon, label);
            }
            boardView.Configure(cells, screenRoot);
            var ordersRoot = UIFactory.CreateRect(screenRoot, "주문 목록", new Vector2(16, 72), new Vector2(448, 122));
            orderView = ordersRoot.gameObject.AddComponent<OrderView>();
            var cards = new RectTransform[3]; var buttons = new Button[3]; var labels = new Text[3];
            var initial = new GameState();
            for (int index = 0; index < 3; index++)
            {
                var order = initial.Orders[index];
                cards[index] = UIFactory.CreateRect(ordersRoot, order.Id, new Vector2(index * 152, 0), new Vector2(144, 122));
                cards[index].gameObject.AddComponent<Image>().color = new Color32(255, 252, 239, 255);
                UIFactory.CreateItem(cards[index], "요청 아이템", new Vector2(42, 0), order.RequiredStage);
                labels[index] = UIFactory.CreateText(cards[index], "수량", new Vector2(0, 46), new Vector2(144, 20), "", 12, TextAnchor.MiddleCenter);
                buttons[index] = UIFactory.CreateButton(cards[index], "Get", new Vector2(14, 67), new Vector2(116, 28), "Get");
                UIFactory.CreateText(cards[index], "보상", new Vector2(0, 98), new Vector2(144, 20), "+" + order.Reward + " 코인", 10, TextAnchor.MiddleCenter);
            }
            orderView.Configure(cards, buttons, labels);
            var generate = UIFactory.CreateButton(screenRoot, "생성기", new Vector2(140, 809), new Vector2(200, 34), "씨앗 만들기");
            hud = screenRoot.gameObject.AddComponent<HUDView>();
            hud.Configure(generate, coins, message);
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            boardView.Render(initial.Board);
        }
    }
}
