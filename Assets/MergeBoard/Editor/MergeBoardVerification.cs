using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MergeBoard.Editor
{
    public static class MergeBoardVerification
    {
        [MenuItem("머지 MVP/실행 장면 만들기")]
        public static void 장면만들기()
        {
            const string 경로 = "Assets/MergeBoard/Scenes/MergeBoard.unity";
            if (System.IO.File.Exists(경로)) throw new InvalidOperationException("기존 장면은 덮어쓰지 않습니다.");
            if (EditorSceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("현재 장면을 먼저 저장해주세요.");
            if (!AssetDatabase.IsValidFolder("Assets/MergeBoard/Scenes")) AssetDatabase.CreateFolder("Assets/MergeBoard", "Scenes");
            var 장면 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("머지 게임").AddComponent<MergeGameBootstrap>();
            var 카메라 = new GameObject("카메라").AddComponent<Camera>();
            카메라.clearFlags = CameraClearFlags.SolidColor;
            카메라.backgroundColor = new Color32(27, 42, 38, 255);
            EditorSceneManager.SaveScene(장면, 경로);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(경로, true) };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("머지 MVP/규칙 검증")]
        public static void 규칙검증()
        {
            var 보드 = new BoardModel();
            확인(보드.복사().Length == 63, "63칸");
            확인(보드.찾기(ItemStage.Seed).Count == 4, "초기 씨앗 4개");
            확인(보드.찾기(ItemStage.Empty).Count == 59, "초기 빈 칸 59개");
            확인(BoardModel.좌표변환(6, 8) == 62 && BoardModel.좌표변환(7, 0) == -1 && BoardModel.좌표변환(0, -1) == -1, "좌표 경계");
            var 복사 = 보드.복사(); 복사[0] = ItemStage.Flower;
            확인(보드[0] == ItemStage.Seed, "조회 복사본 격리");
            Debug.Log("MVP 규칙 검증 PASS: 보드 크기, 초기 배치, 좌표 경계, 복사본 격리");
        }

        public static void 확인(bool 조건, string 항목)
        {
            if (!조건) throw new InvalidOperationException("MVP 검증 실패: " + 항목);
        }
    }
}
