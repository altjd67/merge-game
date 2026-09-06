using System;
#if !UNITY_WEBGL
using System.IO;
#endif
using UnityEngine;

namespace MergeBoard
{
    /// <summary>PlayerPrefs에 JSON 진행 데이터를 저장하고, 읽기 실패 시 기본 게임 상태를 복원한다.</summary>
    public sealed class LocalSaveService
    {
        public const string DefaultSaveKey = "MergeBoard.Progress";

        public string SaveKey { get; }
        public string LastError { get; private set; } = "";

        /// <summary>PlayerPrefs 저장 키를 지정한다. 검증에서는 개인 진행 상태와 분리된 키를 전달한다.</summary>
        public LocalSaveService(string saveKey = DefaultSaveKey)
        {
            if (string.IsNullOrWhiteSpace(saveKey)) throw new ArgumentException("저장 키가 비어 있습니다.", nameof(saveKey));
            SaveKey = saveKey;
        }

        /// <summary>게임 시작 시 진행 상태를 읽는다. 데이터가 없거나 손상되면 기본 상태로 시작한다.</summary>
        public GameState Load()
        {
            LastError = "";
            try
            {
                string json = PlayerPrefs.GetString(SaveKey, "");
                bool migratedFromLegacyFile = false;
#if !UNITY_WEBGL
                if (string.IsNullOrEmpty(json)) migratedFromLegacyFile = TryLoadLegacyJson(out json);
#endif
                if (string.IsNullOrEmpty(json)) return new GameState();
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) throw new ArgumentException("비어 있는 저장 데이터입니다.");
                var state = data.ToState();
                if (data.version < 3 || migratedFromLegacyFile) Save(state);
                return state;
            }
            catch (Exception)
            {
                LastError = "저장 데이터를 읽지 못해 기본 보드로 시작합니다.";
                return new GameState();
            }
        }

        /// <summary>현재 게임 상태를 JSON으로 직렬화해 PlayerPrefs에 즉시 기록한다.</summary>
        /// <returns>저장 성공 여부. 실패 원인은 LastError로 제공한다.</returns>
        public bool Save(GameState state)
        {
            LastError = "";
            try
            {
                var data = SaveData.FromState(state);
                data.ToState();
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception)
            {
                LastError = "저장하지 못했습니다. 기기 저장 공간과 권한을 확인해 주세요.";
                return false;
            }
        }

#if !UNITY_WEBGL
        /// <summary>PlayerPrefs 전환 전의 JSON 저장 파일을 읽어 최초 실행 시 진행 상태를 이전한다.</summary>
        private static bool TryLoadLegacyJson(out string json)
        {
            json = "";
            string legacyPath = Path.Combine(Application.persistentDataPath, "merge-board-v1.json");
            if (!File.Exists(legacyPath)) return false;
            json = File.ReadAllText(legacyPath);
            return !string.IsNullOrEmpty(json);
        }
#endif
    }
}
