using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MergeBoard.Editor
{
    /// <summary>개인 저장을 건드리지 않고 파일 저장·실패·재실행 복원을 검증한다.</summary>
    public static class SaveVerification
    {
        /// <summary>각 상태 변경 직후 파일 일치와 손상·쓰기 실패의 기본 상태 복원을 검사한다.</summary>
        [MenuItem("머지 MVP/저장 검증")]
        public static void VerifySave()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/SaveVerification", Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(root);
            var service = new LocalSaveService(Path.Combine(root, "progress.json"));
            var game = new MergeGameController(service.Load(), service);
            Check(service.Load().Board.FindCells(ItemStage.Seed).Count == 4, "누락 파일 초기 배치");
            game.Generate(0); CheckEqual(game.State, service.Load(), "생성 직후 저장");
            int empty = game.Board.FindCells(ItemStage.Empty)[0];
            game.Move(0, empty); CheckEqual(game.State, service.Load(), "이동 직후 저장");
            game.Move(empty, 1); CheckEqual(game.State, service.Load(), "합성 직후 저장");
            game.SubmitOrder(0); CheckEqual(game.State, service.Load(), "주문 완료 직후 저장");
            Check(service.Load().Coins == 20, "보상 복원");
            string before = File.ReadAllText(service.FilePath);
            game.Move(-1, 0); game.SubmitOrder(0);
            Check(File.ReadAllText(service.FilePath) == before, "실패 명령은 저장 불변");

            // 임시 파일 자리에 디렉터리를 만들어 쓰기 실패를 재현한다.
            Directory.CreateDirectory(service.FilePath + ".tmp");
            Check(!service.Save(game.State) && File.ReadAllText(service.FilePath) == before && service.LastError.Length > 0, "쓰기 실패 시 기존 파일 보존");
            game.Generate(2);
            Check(game.Board.FindCells(ItemStage.Seed).Count == 4 && game.SaveMessage.Length > 0, "저장 실패 시 현재 플레이 유지와 안내");

            File.WriteAllText(service.FilePath, "잘못된 JSON");
            Check(service.Load().Coins == 0 && service.LastError.Length > 0, "손상 JSON 기본 복원");
            var data = SaveData.FromState(new GameState());
            data.version = 99; CheckRejected(service, data, "미지원 버전");
            data.version = 1; data.cells = new int[1]; CheckRejected(service, data, "잘못된 칸 수");
            data = SaveData.FromState(new GameState()); data.cells[0] = 99; CheckRejected(service, data, "잘못된 단계");
            data = SaveData.FromState(new GameState()); data.coins = -1; CheckRejected(service, data, "음수 코인");
            data = SaveData.FromState(new GameState()); data.orderIds[0] = "unknown"; CheckRejected(service, data, "알 수 없는 주문");
            data = SaveData.FromState(new GameState()); data.completedOrders[0] = true; CheckRejected(service, data, "코인·완료 상태 불일치");
            Debug.Log("MVP 저장 검증 PASS: 변경 직후 파일 일치·실패 불변·손상 복원·버전·수량·단계·주문·쓰기 실패 보존");
        }

        private static void CheckRejected(LocalSaveService service, SaveData data, string description)
        {
            File.WriteAllText(service.FilePath, JsonUtility.ToJson(data));
            var restored = service.Load();
            Check(restored.Board.FindCells(ItemStage.Seed).Count == 4 && restored.Coins == 0 && service.LastError.Length > 0, description);
        }

        /// <summary>모든 저장 대상 필드가 같은지 비교한다.</summary>
        public static void CheckEqual(GameState expected, GameState actual, string description)
        {
            Check(JsonUtility.ToJson(SaveData.FromState(expected)) == JsonUtility.ToJson(SaveData.FromState(actual)), description);
        }

        private static void Check(bool condition, string description) => MergeBoardVerification.Assert(condition, description);

        /// <summary>다음 Play Mode 실행을 새로운 격리 저장 경로로 설정한다.</summary>
        public static void PrepareIsolatedPlay()
        {
            Check(!Application.isPlaying, "격리 경로는 Play Mode 밖에서 준비");
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/PlayVerification", Guid.NewGuid().ToString("N"), "progress.json"));
            SessionState.SetString("MergeBoard.VerificationSavePath", path);
        }

        /// <summary>재진입 비교를 위해 현재 Play Mode의 저장 상태를 기억한다.</summary>
        public static void RememberPlayState()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<MergeGameBootstrap>();
            SessionState.SetString("MergeBoard.ExpectedSave", JsonUtility.ToJson(SaveData.FromState(game.Controller.State)));
        }

        /// <summary>Play Mode를 종료·재진입한 뒤 모든 저장 필드가 유지됐는지 검사한다.</summary>
        public static void VerifyPlayReload()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<MergeGameBootstrap>();
            Check(SessionState.GetString("MergeBoard.ExpectedSave", "") == JsonUtility.ToJson(SaveData.FromState(game.Controller.State)), "재진입 보드·코인·주문 복원");
            Debug.Log("MVP 재실행 복원 PASS: 보드 63칸·코인·주문 완료 상태 일치");
        }

        /// <summary>검증 이후 정상 저장 경로로 돌린다. 검증 파일은 Logs에 남긴다.</summary>
        public static void EndIsolatedPlay()
        {
            SessionState.EraseString("MergeBoard.VerificationSavePath");
            SessionState.EraseString("MergeBoard.ExpectedSave");
        }
    }
}
