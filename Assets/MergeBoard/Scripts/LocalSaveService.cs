using System;
using System.IO;
using UnityEngine;

namespace MergeBoard
{
    /// <summary>로컬 JSON 파일을 안전하게 교체하고, 읽기 실패 시 기본 게임 상태를 복원한다.</summary>
    public sealed class LocalSaveService
    {
        public string FilePath { get; }
        public string LastError { get; private set; } = "";

        /// <summary>저장 경로를 지정한다. 검증은 개인 저장과 분리된 경로를 전달한다.</summary>
        public LocalSaveService(string filePath) => FilePath = Path.GetFullPath(filePath);

        /// <summary>게임 시작 시 호출한다. 누락·손상·접근 실패는 기본 상태로 처리한다.</summary>
        public GameState Load()
        {
            LastError = "";
            try
            {
                if (!File.Exists(FilePath)) return new GameState();
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
                if (data == null) throw new ArgumentException("비어 있는 저장 데이터입니다.");
                var state = data.ToState();
                if (data.version < 3) Save(state);
                return state;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                LastError = "저장 데이터를 읽지 못해 기본 보드로 시작합니다.";
                return new GameState();
            }
        }

        /// <summary>임시 파일을 완전히 기록한 뒤 교체한다. 실패하면 기존 저장 파일을 보존한다.</summary>
        /// <returns>파일 기록 성공 여부. 실패 원인에 대한 사용자 안내는 LastError로 제공한다.</returns>
        public bool Save(GameState state)
        {
            LastError = "";
            string temporaryPath = FilePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var data = SaveData.FromState(state);
                data.ToState();
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath)) File.Replace(temporaryPath, FilePath, null);
                else File.Move(temporaryPath, FilePath);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                LastError = "저장하지 못했습니다. 저장 폴더의 권한과 여유 공간을 확인해주세요.";
                return false;
            }
        }
    }
}
