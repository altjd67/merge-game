using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeBoard.Editor
{
    /// <summary>실제 uGUI 입력과 시간 경과로 전체 MVP 루프를 실행하고 화면을 기록한다.</summary>
    public static class PlayLoopVerification
    {
        public static string Status { get; private set; } = "미실행";
        public static string CaptureDirectory { get; private set; }
        private static IEnumerator<double> steps;
        private static double nextStep;
        private static double nextCapture;
        private static int frame;

        /// <summary>격리된 초기 보드에서 전체 루프를 시작한다. 이후 Editor update에서 진행한다.</summary>
        public static void Begin()
        {
            if (!Application.isPlaying || steps != null) throw new InvalidOperationException("Play Mode에서 검증을 한 번만 실행해주세요.");
            var game = UnityEngine.Object.FindFirstObjectByType<MergeGameBootstrap>();
            MergeBoardVerification.Assert(game.Controller.State.Coins == 0 && game.Board.FindCells(ItemStage.Seed).Count == 4, "격리 초기 상태");
            CaptureDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PlayCapture", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(CaptureDirectory);
            Status = "실행 중";
            frame = 0; nextCapture = nextStep = 0;
            steps = Run(game).GetEnumerator();
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!Application.isPlaying) { Finish("중단: Play Mode 종료"); return; }
            double now = EditorApplication.timeSinceStartup;
            if (now >= nextCapture)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(CaptureDirectory, $"frame-{frame++:D4}.png"));
                nextCapture = now + 0.1;
            }
            if (now < nextStep) return;
            try
            {
                if (!steps.MoveNext()) { Finish("PASS"); return; }
                nextStep = now + steps.Current;
            }
            catch (Exception error) { Debug.LogException(error); Finish("FAIL: " + error.Message); }
        }

        private static void Finish(string result)
        {
            EditorApplication.update -= Tick;
            steps?.Dispose(); steps = null;
            Status = result;
            Debug.Log("MVP 전체 플레이 검증: " + result);
        }

        private static IEnumerable<double> Run(MergeGameBootstrap game)
        {
            yield return 0.6;
            MergeBoardVerification.Assert(game.HUD.EnergyText == "에너지 100/100", "초기 에너지 HUD");
            MergeBoardVerification.Drag(game, 31, 32);
            MergeBoardVerification.Assert(game.Board[32] == ItemStage.SeedPack && game.Controller.State.Energy == 100, "씨앗팩 드래그 이동 무소모");
            MergeBoardVerification.Drag(game, 32, 32);
            ExecuteEvents.Execute(game.BoardView.Cells[32].gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            MergeBoardVerification.Assert(game.Controller.State.Energy == 100, "드래그 후 중복 클릭 억제");
            ClickCell(game, 0);
            MergeBoardVerification.Assert(game.Controller.State.Energy == 100, "일반 아이템 클릭 무소모");
            MergeBoardVerification.Drag(game, 0, 7);
            yield return 0.4;
            MergeBoardVerification.Drag(game, 7, -1);
            MergeBoardVerification.Assert(game.Board[7] == ItemStage.Seed, "보드 밖 드롭 복귀");
            yield return 2.2;
            for (int orderIndex = 0; orderIndex < 3; orderIndex++)
            {
                var order = game.Controller.State.Orders[orderIndex];
                while (!game.Controller.CanSubmit(orderIndex))
                {
                    bool merged = false;
                    for (var stage = ItemStage.Seed; stage < order.RequiredStage; stage++)
                    {
                        var matching = game.Board.FindCells(stage);
                        if (matching.Count < 2) continue;
                        MergeBoardVerification.Drag(game, matching[0], matching[1]);
                        var animator = game.BoardView.Cells[matching[1]].Icon.GetComponent<Animator>();
                        MergeBoardVerification.Assert(animator.GetCurrentAnimatorStateInfo(0).IsName("Merge"), "합성 Animator 재생");
                        MergeBoardVerification.Assert(!game.HUD.MessageText.Contains("합성!"), "합성 성공 토스트 없음");
                        yield return 0.5;
                        merged = true;
                        break;
                    }
                    if (merged) continue;
                    int before = game.Board.FindCells(ItemStage.Seed).Count;
                    int energyBefore = game.Controller.State.Energy;
                    ClickCell(game, 32);
                    ClickCell(game, 32);
                    MergeBoardVerification.Assert(game.Board.FindCells(ItemStage.Seed).Count == before + 2 && game.Controller.State.Energy == energyBefore - 2, "씨앗팩 연속 클릭과 에너지 차감");
                    MergeBoardVerification.Assert(game.BoardView.Cells[32].Icon.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("Click"), "생성기 클릭 Animator");
                    yield return 0.3;
                }
                MergeBoardVerification.Assert(game.OrderView.GetButtons[orderIndex].interactable, "수량 충족 Get 활성화");
                yield return 0.45;
                int coinsBefore = game.Controller.State.Coins;
                int countBefore = game.Board.FindCells(order.RequiredStage).Count;
                Click(game.OrderView.GetButtons[orderIndex]);
                MergeBoardVerification.Assert(game.Controller.State.Coins == coinsBefore + order.Reward && game.Board.FindCells(order.RequiredStage).Count == countBefore - order.RequiredCount, "Get 소비·보상");
                Click(game.OrderView.GetButtons[orderIndex]);
                MergeBoardVerification.Assert(game.Controller.State.Coins == coinsBefore + order.Reward, "애니메이션 중 중복 보상 방지");
                yield return 0.2;
                var effects = UnityEngine.Object.FindObjectsByType<AnimatorFeedback>(FindObjectsSortMode.None);
                MergeBoardVerification.Assert(effects.Length == order.RequiredCount + 1, "수량만큼 아이템 비행과 보상 텍스트");
                foreach (var effect in effects)
                {
                    MergeBoardVerification.Assert(effect.Progress > 0 && effect.Progress < 1, "Animator 비행·보상 진행도");
                }
                yield return 1.2;
                MergeBoardVerification.Assert(UnityEngine.Object.FindObjectsByType<AnimatorFeedback>(FindObjectsSortMode.None).Length == 0, "효과 종료 후 임시 오브젝트 정리");
            }
            MergeBoardVerification.Assert(game.Controller.State.Coins == 230, "세 주문 총 230코인");
            // 실제 생성 입력으로 보드를 채운 후 실패 토스트와 소모 불변을 검사한다.
            int emptyCount = game.Board.FindCells(ItemStage.Empty).Count;
            for (int index = 0; index < emptyCount; index++)
            {
                ClickCell(game, 32);
                MergeBoardVerification.Assert(game.Board.FindCells(ItemStage.Empty).Count == emptyCount - index - 1, "보드 채우기 생성 성공");
            }
            int remainingEnergy = game.Controller.State.Energy;
            ClickCell(game, 32);
            MergeBoardVerification.Assert(game.HUD.MessageText == "보드 가득 참" && game.HUD.IsToastVisible && game.Controller.State.Energy == remainingEnergy, "가득 찬 보드 토스트와 에너지 불변");
            yield return 1;
            yield return 1.2;
            MergeBoardVerification.Assert(!game.HUD.IsToastVisible, "토스트 자동 숨김");
            SaveVerification.RememberPlayState();
            yield return 1.5;
        }

        /// <summary>uGUI Button의 실제 포인터 클릭 처리기를 실행한다.</summary>
        public static void Click(Button button)
        {
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }

        /// <summary>셀의 실제 포인터 누름·클릭 경로로 입력한다.</summary>
        public static void ClickCell(MergeGameBootstrap game, int index)
        {
            var target = game.BoardView.Cells[index].gameObject;
            var evt = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, pointerId = -1 };
            ExecuteEvents.Execute(target, evt, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, evt, ExecuteEvents.pointerClickHandler);
        }
    }
}
