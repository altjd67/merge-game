using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

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
        /// <summary>장면의 모든 TMP uGUI 문구에 적용하는 한글 글꼴이다.</summary>
        public TMP_FontAsset TextFont => textFont;
        public MergeGameController Controller { get; private set; }
        /// <summary>검증에서 씨앗팩 강조 재생 여부를 확인하기 위한 누적 횟수다.</summary>
        public int GeneratorGuidePulseCount { get; private set; }
        [SerializeField] private TMP_FontAsset textFont;
        private float nextGuidePulseAt;
        private long lastHudRenderUtcSecond = long.MinValue;
        private const float GuidePulseInterval = 1.5f;

        /// <summary>로컬 진행 상태를 한 번 복원한 뒤 장면 UI의 입력을 연결한다.</summary>
        private async UniTaskVoid Start()
        {
            // PC에서 창 포커스를 잃어도 생성기 시간과 제출 피드백은 계속 진행한다.
            Application.runInBackground = true;
            // OS 동적 글꼴의 런타임 Material은 직렬화되지 않으므로 실행 시 다시 생성한다.
            if (textFont == null) throw new System.InvalidOperationException("TMP 한글 폰트가 연결되지 않았습니다.");
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true)) text.font = textFont;
            await UnityEngine.Localization.Settings.LocalizationSettings.InitializationOperation.ToUniTask();
            await UniTask.Yield();
            ApplyStaticText();
            string saveKey = LocalSaveService.DefaultSaveKey;
#if UNITY_EDITOR
            // 검증은 사용자 저장과 분리하며, SessionState는 Play Mode 재진입에도 유지된다.
            saveKey = UnityEditor.SessionState.GetString("MergeBoard.VerificationSaveKey", saveKey);
#endif
            var saveService = new LocalSaveService(saveKey);
            Controller = new MergeGameController(saveService.Load(), saveService);
            string loadMessage = Controller.SaveMessage;
            Controller.RefreshEnergy(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            boardView.Dropped += HandleDrop;
            boardView.DragStateChanged += _ => RefreshViews();
            boardView.Clicked += HandleGenerate;
            orderView.GetClicked += HandleOrder;
            RefreshViews();
            nextGuidePulseAt = Time.unscaledTime + 0.5f;
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
            if (!result.Success && result.Message.Length > 0)
            {
                if (Controller.SaveMessage.Length > 0) hud.ShowMessage(Controller.SaveMessage);
                else AnimatorFeedback.ShowFloatingMessage(screenRoot, ItemWorldCenter(BoardModel.IsValidIndex(destination) ? destination : source), GameText.Get(result.Message), textFont);
            }
            if (result.Merged) AnimatorFeedback.PlayScale(boardView.Cells[destination].Icon.transform, "Merge");
        }

        /// <summary>생성 명령과 저장을 완료한 뒤 클릭 피드백을 재생한다.</summary>
        private void HandleGenerate(int index)
        {
            if (boardView.IsDragging || Board[index] != ItemStage.SeedPack) return;
            int destination = Board.FindNearestEmptyCell(index);
            Vector3 generatorWorld = ItemWorldCenter(index);
            var result = Controller.Generate(index, System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if (result.Success) AnimatorFeedback.PlayScale(boardView.Cells[index].Icon.transform, "Click");
            RefreshViews();
            if (result.Success && destination >= 0)
            {
                boardView.Cells[destination].SetItemVisible(false);
                AnimatorFeedback.Fly(screenRoot, generatorWorld, ItemWorldCenter(destination), ItemStage.Seed, 2f);
                ShowGeneratedItem(destination);
            }
            ShowResult(result.Success ? "" : GameText.Get(result.Message));
        }

        /// <summary>생성 비행이 끝난 뒤 도착 칸의 씨앗을 표시하고 짧게 확대한다.</summary>
        private async UniTaskVoid ShowGeneratedItem(int destination)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(AnimatorFeedback.FastFlightDuration), DelayType.UnscaledDeltaTime);
            if (Board[destination] != ItemStage.Seed) return;
            boardView.Cells[destination].SetItemVisible(true);
            AnimatorFeedback.PlayScale(boardView.Cells[destination].Icon.transform, "Merge");
        }

        /// <summary>칸 아이콘의 중앙 월드 좌표를 반환한다.</summary>
        private Vector3 ItemWorldCenter(int index)
        {
            var icon = boardView.Cells[index].Icon.rectTransform;
            return icon.TransformPoint(icon.rect.center);
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
                    AnimatorFeedback.Fly(screenRoot, icon.TransformPoint(icon.rect.center), destination, result.ConsumedStage);
                }
                AnimatorFeedback.ShowReward(screenRoot, card.TransformPoint(new Vector3(72, -78, 0)), result.Reward, textFont);
            }
            RefreshViews();
            ShowResult(result.Success ? GameText.Get("message.order_completed", result.Reward) : GameText.Get("message.order_insufficient"));
        }

        /// <summary>장면에 직렬화된 제목·가이드 문구를 문자열 테이블 값으로 갱신합니다.</summary>
        private void ApplyStaticText()
        {
            var title = screenRoot.Find("제목")?.GetComponent<TextMeshProUGUI>();
            if (title != null) title.text = GameText.Get("title");
            hud.LocalizeStaticText();
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
                int availableCount = Board.CountCells(order.RequiredStage);
                orderView.Render(index, order, availableCount, !boardView.IsDragging && availableCount >= order.RequiredCount);
            }
            RenderHUD(true);
            hud.RenderGeneratorGuide(!Controller.State.GeneratorGuideCompleted);
        }

        private void Update()
        {
            if (Controller == null) return;
            if (Controller.RefreshEnergy(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()))
            {
                RefreshViews();
                if (Controller.SaveMessage.Length > 0) hud.ShowMessage(Controller.SaveMessage);
            }
            RenderHUD();
            PlayGeneratorGuide();
        }

        private void RenderHUD(bool force = false)
        {
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!force && now == lastHudRenderUtcSecond) return;
            hud.Render(Controller.State.Coins, Controller.State.Energy, GameState.MaxEnergy, Controller.RecoveryRemaining(now));
            lastHudRenderUtcSecond = now;
        }

        /// <summary>첫 씨앗 생성 전까지 씨앗팩을 Animator로 주기적으로 강조한다.</summary>
        private void PlayGeneratorGuide()
        {
            bool visible = !Controller.State.GeneratorGuideCompleted;
            hud.RenderGeneratorGuide(visible);
            if (!visible || boardView.IsDragging || Time.unscaledTime < nextGuidePulseAt) return;
            int generator = Board.FindFirstCell(ItemStage.SeedPack);
            if (generator >= 0)
            {
                AnimatorFeedback.PlayScale(boardView.Cells[generator].Icon.transform, "Click");
                GeneratorGuidePulseCount++;
            }
            nextGuidePulseAt = Time.unscaledTime + GuidePulseInterval;
        }

        private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) boardView?.CancelDrag(); }

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
            UIFactory.CreateText(screenRoot, "제목", new Vector2(24, 4), new Vector2(210, 32), "작은 정원", 24);
            var coins = UIFactory.CreateText(screenRoot, "코인", new Vector2(314, 20), new Vector2(142, 32), "0 코인", 20, TextAnchor.MiddleRight);
            var energy = UIFactory.CreateText(screenRoot, "에너지", new Vector2(24, 38), new Vector2(174, 26), "에너지 100/100", 15);
            var recovery = UIFactory.CreateText(screenRoot, "회복 시간", new Vector2(204, 38), new Vector2(72, 26), "최대", 14);
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
            var cards = new RectTransform[3]; var buttons = new Button[3]; var labels = new TextMeshProUGUI[3];
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
            var toast = UIFactory.CreateRect(screenRoot, "토스트", new Vector2(90, 410), new Vector2(300, 52));
            var background = toast.gameObject.AddComponent<Image>();
            background.color = new Color32(36, 49, 39, 235); background.raycastTarget = false;
            var message = UIFactory.CreateText(toast, "안내", new Vector2(8, 0), new Vector2(284, 52), "", 16, TextAnchor.MiddleCenter);
            message.color = Color.white;
            var guide = UIFactory.CreateRect(screenRoot, "씨앗팩 가이드", new Vector2(94, 196), new Vector2(292, 30));
            var guideBackground = guide.gameObject.AddComponent<Image>();
            guideBackground.color = new Color32(82, 139, 77, 235); guideBackground.raycastTarget = false;
            var guideText = UIFactory.CreateText(guide, "문구", new Vector2(6, 0), new Vector2(280, 30), "씨앗팩을 터치해 씨앗을 만들어보세요", 13, TextAnchor.MiddleCenter);
            guideText.color = Color.white;
            hud = screenRoot.gameObject.AddComponent<HUDView>();
            hud.Configure(coins, energy, recovery, message, toast.gameObject, guide.gameObject);
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)).transform.SetParent(transform, false);
            boardView.Render(initial.Board);
        }
    }
}
