using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace MergeBoard.Editor
{
    /// <summary>MergeBoard에서 사용하는 한국어·영어 문자열 테이블과 로케일을 생성·보완합니다.</summary>
    public static class MergeBoardLocalizationTableBuilder
    {
        private const string AssetDirectory = "Assets/MergeBoard/Localization";

        [MenuItem("머지 MVP/로컬라이제이션 테이블 생성")]
        /// <summary>한국어·영어 로케일과 MergeBoard 문자열 테이블을 만들고 누락된 키를 추가합니다.</summary>
        public static void CreateOrUpdate()
        {
            var korean = GetOrCreateLocale(SystemLanguage.Korean, "ko");
            var english = GetOrCreateLocale(SystemLanguage.English, "en");
            var collection = LocalizationEditorSettings.GetStringTableCollection(GameText.TableName);
            if (collection == null)
                collection = LocalizationEditorSettings.CreateStringTableCollection(GameText.TableName, AssetDirectory, new List<Locale> { korean, english });

            AddMissingEntries(collection.GetTable(korean.Identifier) as StringTable, KoreanEntries);
            AddMissingEntries(collection.GetTable(english.Identifier) as StringTable, EnglishEntries);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            Debug.Log("MergeBoard 한국어·영어 로컬라이제이션 테이블 준비 완료");
        }

        private static Locale GetOrCreateLocale(SystemLanguage language, string code)
        {
            var locale = LocalizationEditorSettings.GetLocale(language);
            if (locale != null) return locale;
            locale = Locale.CreateLocale(language);
            AssetDatabase.CreateAsset(locale, $"{AssetDirectory}/{code}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static void AddMissingEntries(StringTable table, KeyValuePair<string, string>[] entries)
        {
            foreach (var entry in entries)
                if (table.GetEntry(entry.Key) == null) table.AddEntry(entry.Key, entry.Value);
            EditorUtility.SetDirty(table);
        }

        private static readonly KeyValuePair<string, string>[] KoreanEntries =
        {
            new("title", "작은 정원"),
            new("item.seed", "씨앗"), new("item.sprout", "새싹"), new("item.flower", "꽃"), new("item.seed_pack", "씨앗팩"),
            new("hud.coins", "{0} 코인"), new("hud.energy", "에너지 {0}/{1}"), new("hud.maximum", "최대"),
            new("order.status", "{0} {1}/{2}"), new("order.reward", "+{0} 코인"), new("order.get", "Get"),
            new("guide.generator", "씨앗팩을 터치해 씨앗을 만들어보세요"),
            new("message.invalid_time", "잘못된 시간입니다."), new("message.tap_generator", "씨앗팩을 눌러주세요."),
            new("message.board_full", "보드 가득 참"), new("message.not_enough_energy", "에너지가 부족합니다."),
            new("message.generated", "씨앗이 자랄 준비를 마쳤어요."), new("message.drop_on_board", "보드 안에 놓아주세요."),
            new("message.empty_cannot_move", "빈 칸은 이동할 수 없습니다."), new("message.flower_maximum", "꽃은 최대 레벨입니다."),
            new("message.merge_same_stage", "같은 단계의 씨앗이나 새싹을 겹쳐주세요."), new("message.order_insufficient", "주문에 필요한 아이템이 부족합니다."),
            new("message.order_completed", "+{0} 코인!"),
        };

        private static readonly KeyValuePair<string, string>[] EnglishEntries =
        {
            new("title", "Little Garden"),
            new("item.seed", "Seed"), new("item.sprout", "Sprout"), new("item.flower", "Flower"), new("item.seed_pack", "Seed Pack"),
            new("hud.coins", "{0} Coins"), new("hud.energy", "Energy {0}/{1}"), new("hud.maximum", "Max"),
            new("order.status", "{0} {1}/{2}"), new("order.reward", "+{0} Coins"), new("order.get", "Get"),
            new("guide.generator", "Tap the seed pack to make a seed"),
            new("message.invalid_time", "The device time is invalid."), new("message.tap_generator", "Tap the seed pack."),
            new("message.board_full", "Board is full"), new("message.not_enough_energy", "Not enough energy."),
            new("message.generated", "The seed is ready to grow."), new("message.drop_on_board", "Place it on the board."),
            new("message.empty_cannot_move", "An empty cell cannot be moved."), new("message.flower_maximum", "Flower is the maximum level."),
            new("message.merge_same_stage", "Merge two seeds or sprouts of the same stage."), new("message.order_insufficient", "Not enough items for this order."),
            new("message.order_completed", "+{0} Coins!"),
        };
    }
}
