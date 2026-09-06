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
        }

        /// <summary>보드·이동·합성의 성공 경로와 경계·실패 경로를 검사한다.</summary>
        [MenuItem("머지 MVP/규칙 검증")]
        public static void VerifyRules()
        {
            var Board = new BoardModel();
            Assert(Board.CopyCells().Length == 63, "63칸");
            Assert(Board.FindCells(ItemStage.Seed).Count == 4, "초기 씨앗 4개");
            Assert(Board.FindCells(ItemStage.Empty).Count == 59, "초기 빈 칸 59개");
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
            var generator = new MergeGameController(new BoardModel(new ItemStage[63]));
            Assert(generator.Generate(0).Success && generator.Board.FindCells(ItemStage.Seed).Count == 1, "빈 보드 생성");
            Assert(!generator.Generate(1.99).Success && generator.Generate(2).Success, "2초 쿨다운 경계");
            var fullCells = new ItemStage[63];
            for (int index = 0; index < fullCells.Length; index++) fullCells[index] = ItemStage.Seed;
            var fullBoard = new MergeGameController(new BoardModel(fullCells));
            Assert(!fullBoard.Generate(0).Success && fullBoard.CooldownRemaining(0) == 0, "가득 찬 보드 거절과 대기시간 유지");
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
            Debug.Log("MVP 규칙 검증 PASS: 보드·이동·합성·실패 복귀·생성기·2초 경계·가득 찬 보드·주문·수량 부족·중복 방지·230코인");
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

        /// <summary>최초 uGUI 전환 시 글꼴과 편집 가능한 UI 계층을 현재 MVP 장면에 저장한다.</summary>
        public static void PrepareUGUI()
        {
            const string fontPath = "Assets/MergeBoard/Resources/KoreanFont.fontsettings";
            if (AssetDatabase.LoadAssetAtPath<Font>(fontPath) == null)
                AssetDatabase.CreateAsset(Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Arial" }, 32), fontPath);
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
