using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeBoard
{
    /// <summary>주문 제출 연출에 사용하는 아이콘과 보상 텍스트를 재사용하는 UI 오브젝트 풀입니다.</summary>
    public sealed class UIFeedbackPool : MonoBehaviour
    {
        private readonly Stack<ItemGraphic> availableItems = new();
        private readonly Stack<TextMeshProUGUI> availableRewards = new();

        /// <summary>오버레이에 연결된 풀을 반환하고, 없으면 해당 오버레이에 생성합니다.</summary>
        public static UIFeedbackPool GetOrCreate(RectTransform overlay)
        {
            var pool = overlay.GetComponent<UIFeedbackPool>();
            return pool != null ? pool : overlay.gameObject.AddComponent<UIFeedbackPool>();
        }

        /// <summary>비행 연출용 아이콘을 획득합니다. 반환된 아이콘은 활성 상태이며 호출자가 stage를 설정합니다.</summary>
        public ItemGraphic AcquireItem()
        {
            var item = availableItems.Count > 0 ? availableItems.Pop() : CreateItem();
            item.gameObject.SetActive(true);
            item.transform.SetAsLastSibling();
            return item;
        }

        /// <summary>보상 연출용 텍스트를 획득합니다. 반환된 텍스트는 활성 상태입니다.</summary>
        public TextMeshProUGUI AcquireReward()
        {
            var reward = availableRewards.Count > 0 ? availableRewards.Pop() : CreateReward();
            reward.gameObject.SetActive(true);
            reward.transform.SetAsLastSibling();
            return reward;
        }

        /// <summary>종료된 연출 오브젝트를 종류에 맞는 비활성 풀로 반납합니다.</summary>
        public void Release(AnimatorFeedback feedback)
        {
            var item = feedback.GetComponent<ItemGraphic>();
            if (item != null)
            {
                item.Stage = ItemStage.Empty;
                availableItems.Push(item);
            }
            else
            {
                var reward = feedback.GetComponent<TextMeshProUGUI>();
                if (reward == null) return;
                reward.text = string.Empty;
                availableRewards.Push(reward);
            }

            feedback.gameObject.SetActive(false);
        }

        private ItemGraphic CreateItem()
        {
            var item = UIFactory.CreateItem(transform, "주문 제출 비행", Vector2.zero, ItemStage.Empty);
            PrepareFeedback(item.gameObject);
            return item;
        }

        private TextMeshProUGUI CreateReward()
        {
            var reward = UIFactory.CreateText(transform, "코인 보상", Vector2.zero, new Vector2(144, 36), string.Empty, 22, TextAnchor.MiddleCenter);
            PrepareFeedback(reward.gameObject);
            return reward;
        }

        private static void PrepareFeedback(GameObject target)
        {
            target.AddComponent<CanvasGroup>().blocksRaycasts = false;
            target.AddComponent<AnimatorFeedback>();
        }
    }
}
