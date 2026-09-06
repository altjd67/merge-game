using System;
using UnityEditor;
using UnityEngine;

namespace MergeBoard.Editor
{
    /// <summary>PlayerPrefs 기반 진행 저장과 복원을 검증한다.</summary>
    public static class SaveVerification
    {
        [MenuItem("머지 MVP/저장 검증")]
        public static void VerifySave()
        {
            var service = new LocalSaveService(CreateVerificationKey("SaveVerification"));
            PlayerPrefs.DeleteKey(service.SaveKey);
            var game = new MergeGameController(service.Load(), service);
            Check(game.State.Board.FindCells(ItemStage.Seed).Count == 4, "저장 데이터가 없을 때 기본 보드 복원");

            game.Generate(BoardModel.InitialGeneratorIndex, 1000);
            CheckEqual(game.State, service.Load(), "생성 직후 저장");
            int empty = game.Board.FindCells(ItemStage.Empty)[0];
            game.Move(0, empty);
            CheckEqual(game.State, service.Load(), "이동 직후 저장");
            game.Move(empty, 1);
            CheckEqual(game.State, service.Load(), "합성 직후 저장");
            game.SubmitOrder(0);
            Check(service.Load().Coins == 20, "주문 보상 복원");

            PlayerPrefs.SetString(service.SaveKey, "잘못된 JSON");
            Check(service.Load().Coins == 0 && service.LastError.Length > 0, "손상 데이터 기본 복원");
            VerifyMigration();
            PlayerPrefs.DeleteKey(service.SaveKey);
            PlayerPrefs.Save();
            Debug.Log("MVP 저장 검증 PASS: PlayerPrefs 저장, 복원, 손상 데이터, 버전 마이그레이션");
        }

        private static void VerifyMigration()
        {
            var service = new LocalSaveService(CreateVerificationKey("MigrationVerification"));
            var legacy = SaveData.FromState(new GameState());
            legacy.version = 1;
            legacy.cells[31] = 1;
            legacy.cells[10] = 3;
            legacy.completedOrders[0] = true;
            legacy.coins = 20;
            PlayerPrefs.SetString(service.SaveKey, JsonUtility.ToJson(legacy));

            var restored = service.Load();
            Check(restored.Board[31] == ItemStage.Seed && restored.Board[10] == ItemStage.Flower &&
                restored.Board[23] == ItemStage.SeedPack && restored.Coins == 20 &&
                restored.Orders[0].Id == "Order02" && restored.Energy == 100 && restored.GeneratorGuideCompleted,
                "버전 1 진행 데이터 복원");
            PlayerPrefs.DeleteKey(service.SaveKey);
        }

        /// <summary>모든 저장 상태 필드가 같은지 비교한다.</summary>
        public static void CheckEqual(GameState expected, GameState actual, string description)
        {
            Check(JsonUtility.ToJson(SaveData.FromState(expected)) == JsonUtility.ToJson(SaveData.FromState(actual)), description);
        }

        /// <summary>다음 Play Mode에서 개인 진행 상태와 분리된 PlayerPrefs 키를 사용한다.</summary>
        public static void PrepareIsolatedPlay()
        {
            Check(!Application.isPlaying, "Play Mode 밖에서 격리 저장 키 준비");
            string saveKey = CreateVerificationKey("PlayVerification");
            PlayerPrefs.DeleteKey(saveKey);
            SessionState.SetString("MergeBoard.VerificationSaveKey", saveKey);
        }

        /// <summary>Play Mode 재시작 뒤 비교할 현재 상태를 기억한다.</summary>
        public static void RememberPlayState()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<MergeGameBootstrap>();
            SessionState.SetString("MergeBoard.ExpectedSave", JsonUtility.ToJson(SaveData.FromState(game.Controller.State)));
        }

        /// <summary>Play Mode 재시작 후 PlayerPrefs 저장 상태가 복원되었는지 검증한다.</summary>
        public static void VerifyPlayReload()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<MergeGameBootstrap>();
            var expected = new MergeGameController(JsonUtility.FromJson<SaveData>(SessionState.GetString("MergeBoard.ExpectedSave", "")).ToState(), null);
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            expected.RefreshEnergy(now);
            game.Controller.RefreshEnergy(now);
            CheckEqual(expected.State, game.Controller.State, "재시작 후 진행 상태 복원");
        }

        /// <summary>오프라인 에너지 회복 검증용 PlayerPrefs 데이터를 준비한다.</summary>
        public static void PrepareOfflinePlay()
        {
            PrepareIsolatedPlay();
            var data = SaveData.FromState(new GameState());
            data.energy = 50;
            data.energyRecoveryAnchorUtcSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 250;
            var service = new LocalSaveService(SessionState.GetString("MergeBoard.VerificationSaveKey", ""));
            Check(service.Save(data.ToState()), "오프라인 검증 데이터 저장");
            SessionState.SetString("MergeBoard.ExpectedSave", JsonUtility.ToJson(data));
        }

        /// <summary>격리 검증에 사용한 PlayerPrefs 데이터와 세션 값을 정리한다.</summary>
        public static void EndIsolatedPlay()
        {
            string saveKey = SessionState.GetString("MergeBoard.VerificationSaveKey", "");
            if (!string.IsNullOrEmpty(saveKey)) PlayerPrefs.DeleteKey(saveKey);
            PlayerPrefs.Save();
            SessionState.EraseString("MergeBoard.VerificationSaveKey");
            SessionState.EraseString("MergeBoard.ExpectedSave");
        }

        private static string CreateVerificationKey(string suffix) => "MergeBoard." + suffix + "." + Guid.NewGuid().ToString("N");
        private static void Check(bool condition, string description) => MergeBoardVerification.Assert(condition, description);
    }
}
