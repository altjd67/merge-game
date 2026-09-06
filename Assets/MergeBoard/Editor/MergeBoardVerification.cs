using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MergeBoard.Editor
{
    /// <summary>장면 준비와 UI에 의존하지 않는 게임 규칙 검증을 제공한다.</summary>
    public static class MergeBoardVerification
    {
        /// <summary>기존 장면을 덮어쓰지 않고 최초 MVP 실행 장면을 생성한다.</summary>
        [MenuItem("머지 MVP/실행 장면 만들기")]
        public static void CreateScene()
        {
            const string path = "Assets/MergeBoard/Scenes/MergeBoard.unity";
            if (System.IO.File.Exists(path)) throw new InvalidOperationException("기존 장면은 덮어쓰지 않습니다.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("현재 장면을 먼저 저장해주세요.");
            if (!AssetDatabase.IsValidFolder("Assets/MergeBoard/Scenes")) AssetDatabase.CreateFolder("Assets/MergeBoard", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("머지 게임").AddComponent<MergeGameBootstrap>();
            var camera = new GameObject("카메라").AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(27, 42, 38, 255);
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            AssetDatabase.SaveAssets();
            PrepareUGUI();
            FeedbackAssetBuilder.PrepareFeedback();
        }

        /// <summary>보드·이동·합성의 성공 경로와 경계·실패 경로를 검사한다.</summary>
        [MenuItem("머지 MVP/규칙 검증")]
        public static void VerifyRules()
        {
            var Board = new BoardModel();
            Assert(Board.CopyCells().Length == 63, "63칸");
            Assert(Board.FindCells(ItemStage.Seed).Count == 4, "초기 씨앗 4개");
            Assert(Board.FindCells(ItemStage.Empty).Count == 58 && Board[31] == ItemStage.SeedPack, "초기 빈 칸 58개와 씨앗팩");
            Assert(BoardModel.ToIndex(6, 8) == 62 && BoardModel.ToIndex(7, 0) == -1 && BoardModel.ToIndex(0, -1) == -1, "좌표 경계");
            var CopyCells = Board.CopyCells(); CopyCells[0] = ItemStage.Flower;
            Assert(Board[0] == ItemStage.Seed, "조회 복사본 격리");
            var controller = new MergeGameController();
            Assert(controller.Move(0, 62).Success && controller.Board[0] == ItemStage.Empty, "빈 칸 이동");
            Assert(controller.Move(62, 1).Merged && controller.Board[1] == ItemStage.Sprout, "씨앗 합성");
            Assert(!controller.Move(1, 2).Success && controller.Board[1] == ItemStage.Sprout, "다른 단계 거절");
            Assert(controller.Move(2, 3).Merged && controller.Move(1, 3).Merged && controller.Board[3] == ItemStage.Flower, "꽃 합성");
            Assert(!controller.Move(3, 3).Success && !controller.Move(3, -1).Success && !controller.Move(0, 1).Success, "자기 칸·보드 밖·빈 원본 거절");
            var flowerCells = new ItemStage[63]; flowerCells[0] = flowerCells[1] = ItemStage.Flower;
            Assert(!new MergeGameController(new BoardModel(flowerCells)).Move(0, 1).Success, "최종 단계 합성 거절");
            var generatorCells = new ItemStage[63]; generatorCells[31] = ItemStage.SeedPack;
            var generator = new MergeGameController(new BoardModel(generatorCells));
            Assert(generator.Generate(31, 1000).Success && generator.Board[23] == ItemStage.Seed, "체비쇼프 대각선과 동률 인덱스");
            Assert(generator.Generate(31, 1000).Success && generator.State.Energy == 98, "동일 시각 연속 생성과 성공당 차감");
            Assert(generator.Move(31, 62).Success && generator.Generate(62, 1000).Success && generator.Board[54] == ItemStage.Seed, "생성기 이동과 가장자리 최근접");
            Assert(!generator.Move(62, 54).Success && !generator.Move(54, 62).Success, "생성기 합성 거절");
            Assert(!generator.Generate(31, 1000).Success && !generator.Generate(-1, 1000).Success && !generator.Generate(62, 0).Success, "잘못된 생성 입력");
            var fullCells = new ItemStage[63];
            for (int index = 0; index < fullCells.Length; index++) fullCells[index] = ItemStage.Seed;
            fullCells[31] = ItemStage.SeedPack;
            var fullBoard = new MergeGameController(new BoardModel(fullCells));
            var rejected = fullBoard.Generate(31, 1000);
            Assert(!rejected.Success && rejected.Message == "보드 가득 참" && fullBoard.State.Energy == 100 && fullBoard.State.EnergyRecoveryAnchorUtcSeconds == 0, "가득 찬 보드와 에너지 불변");
            fullCells[0] = ItemStage.Empty;
            var distant = new MergeGameController(new BoardModel(fullCells));
            Assert(distant.Generate(31, 1000).Success && distant.Board[0] == ItemStage.Seed, "멀리 있는 유일 빈 칸");
            VerifyEnergy();
            var orderCells = new ItemStage[63];
            orderCells[0] = ItemStage.Sprout;
            orderCells[1] = orderCells[2] = orderCells[3] = ItemStage.Flower;
            var orders = new MergeGameController(new BoardModel(orderCells));
            Assert(orders.CanSubmit(0) && orders.CanSubmit(1) && orders.CanSubmit(2), "주문 세 개 활성 조건");
            Assert(orders.SubmitOrder(0).Reward == 20 && orders.SubmitOrder(1).Reward == 60 && orders.SubmitOrder(2).Reward == 150, "정확한 주문 보상");
            Assert(orders.State.Coins == 230 && orders.Board.FindCells(ItemStage.Empty).Count == 63, "총 보상과 수량 소비");
            Assert(!orders.SubmitOrder(2).Success && orders.State.Coins == 230, "중복 보상 방지");
            var insufficientCells = new ItemStage[63]; insufficientCells[0] = ItemStage.Flower;
            var insufficient = new MergeGameController(new BoardModel(insufficientCells));
            Assert(!insufficient.CanSubmit(2) && !insufficient.SubmitOrder(2).Success && insufficient.Board[0] == ItemStage.Flower, "수량 부족 시 소비 없음");
            Debug.Log("MVP 규칙 검증 PASS: 씨앗팩·체비쇼프·연속 생성·에너지 경계·가득 찬 보드·이동·합성·주문·230코인");
        }

        /// <summary>실제 대기 없이 UTC 경계값을 주입하여 소모·회복·최대치와 시각 역행을 검사한다.</summary>
        public static void VerifyEnergy()
        {
            var game = new MergeGameController();
            Assert(game.State.Energy == 100 && !game.RefreshEnergy(1000), "초기 최대 에너지");
            game.Generate(31, 1000);
            Assert(game.RecoveryRemaining(1000) == 120 && !game.RefreshEnergy(1119), "첫 소모와 119초 경계");
            game.Generate(31, 1119);
            Assert(game.State.Energy == 98 && game.RecoveryRemaining(1119) == 1, "추가 소모 시 타이머 보존");
            Assert(game.RefreshEnergy(1120) && game.State.Energy == 99 && game.RecoveryRemaining(1120) == 120, "120초 회복");
            Assert(!game.RefreshEnergy(1120) && game.RefreshEnergy(1240) && game.State.Energy == 100 && game.State.EnergyRecoveryAnchorUtcSeconds == 0, "이중 회복 방지와 최대 타이머 정리");
            game.Generate(31, 2000);
            Assert(game.RecoveryRemaining(2000) == 120, "최대치 초과 시간 미적립");
            var data = SaveData.FromState(new GameState());
            data.energy = 0; data.energyRecoveryAnchorUtcSeconds = 1000;
            game = new MergeGameController(data.ToState(), null);
            Assert(!game.Generate(31, 1119).Success && game.State.Energy == 0, "에너지 0 거절");
            Assert(game.Generate(31, 1120).Success && game.State.Energy == 0, "회복 직후 즉시 생성");
            data.energy = 50;
            game = new MergeGameController(data.ToState(), null);
            Assert(game.RefreshEnergy(1250) && game.State.Energy == 52 && game.RecoveryRemaining(1250) == 110, "여러 구간과 나머지 초");
            Assert(game.RefreshEnergy(900) && game.State.Energy == 52 && game.RecoveryRemaining(900) == 120, "시각 역행 무회복 보정");
            Assert(game.RefreshEnergy(MergeGameController.MaximumUtcSeconds) && game.State.Energy == 100, "장기 미접속 상한");
            Assert(!game.RefreshEnergy(long.MaxValue), "잘못된 시각 거절");
        }

        /// <summary>검증 조건을 만족하지 않으면 항목 이름을 담은 예외를 발생시킨다.</summary>
        public static void Assert(bool condition, string description)
        {
            if (!condition) throw new InvalidOperationException("MVP 검증 실패: " + description);
        }

        /// <summary>Play Mode의 실제 UI 포인터 이벤트로 이동·합성·원위치 복귀를 검증한다.</summary>
        [MenuItem("머지 MVP/플레이 드래그 검증")]
        public static void VerifyDragInPlayMode()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<MergeGameBootstrap>();
            Assert(Application.isPlaying && game != null, "Play Mode 진입");
            Assert(game.Board[0] == ItemStage.Seed && game.Board[1] == ItemStage.Seed, "초기 보드에서 검증 시작");
            Drag(game, 0, 7);
            Assert(game.Board[7] == ItemStage.Seed && game.Board[0] == ItemStage.Empty, "포인터 이동");
            Drag(game, 7, 1);
            Assert(game.Board[1] == ItemStage.Sprout && game.Board[7] == ItemStage.Empty, "포인터 합성");
            Drag(game, 1, 2);
            Assert(game.Board[1] == ItemStage.Sprout && game.Board[2] == ItemStage.Seed, "잘못된 드롭 복귀");
            Drag(game, 1, -1);
            Assert(game.Board[1] == ItemStage.Sprout && !game.BoardView.IsDragging, "보드 밖 복귀와 캡처 해제");
            Drag(game, 2, 3); Drag(game, 1, 3);
            Assert(game.Board[3] == ItemStage.Flower, "포인터 꽃 합성");
            Debug.Log("MVP Play Mode 드래그 PASS: UI 포인터 이동·합성·실패 복귀·꽃 합성");
        }

        /// <summary>실제 UI 이벤트 경로를 사용해 검증용 드래그를 한 번 수행한다.</summary>
        public static void Drag(MergeGameBootstrap game, int source, int destination)
        {
            Canvas.ForceUpdateCanvases();
            var cell = game.BoardView.Cells[source];
            Vector2 start = RectTransformUtility.WorldToScreenPoint(null, cell.Rect.TransformPoint(cell.Rect.rect.center));
            Vector2 end = destination < 0 ? new Vector2(-100, -100) : RectTransformUtility.WorldToScreenPoint(null, game.BoardView.Cells[destination].Rect.TransformPoint(game.BoardView.Cells[destination].Rect.rect.center));
            var evt = new PointerEventData(EventSystem.current) { position = start, button = PointerEventData.InputButton.Left, pointerId = -1 };
            ExecuteEvents.Execute(cell.gameObject, evt, ExecuteEvents.beginDragHandler);
            evt.position = end;
            ExecuteEvents.Execute(cell.gameObject, evt, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(cell.gameObject, evt, ExecuteEvents.endDragHandler);
        }

        /// <summary>최초 uGUI 전환 시 편집 가능한 UI 계층을 현재 MVP 장면에 저장한다.</summary>
        public static void PrepareUGUI()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<MergeGameBootstrap>();
            Assert(!Application.isPlaying && game != null, "MVP 장면 준비");
            game.BuildUI();
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
        }
    }
}
