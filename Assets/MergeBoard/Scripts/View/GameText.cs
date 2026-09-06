using UnityEngine.Localization.Settings;

namespace MergeBoard
{
    /// <summary>MergeBoard 문자열 테이블의 키를 통해 현재 로케일 문구를 반환합니다.</summary>
    public static class GameText
    {
        public const string TableName = "MergeBoard";

        /// <summary>현재 선택된 로케일에서 키와 형식 인자에 맞는 문구를 가져옵니다.</summary>
        public static string Get(string key, params object[] arguments) =>
            LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key, arguments);

        /// <summary>아이템 단계를 문자열 테이블 키에 대응하는 표시 이름으로 변환합니다.</summary>
        public static string StageName(ItemStage stage) => stage switch
        {
            ItemStage.Seed => Get("item.seed"),
            ItemStage.Sprout => Get("item.sprout"),
            ItemStage.Flower => Get("item.flower"),
            ItemStage.SeedPack => Get("item.seed_pack"),
            _ => string.Empty,
        };
    }
}
