using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MergeBoard
{
    /// <summary>가로 주문 슬롯과 Get 버튼을 표시한다. 활성 조건은 Controller에서 전달받는다.</summary>
    public sealed class OrderView : MonoBehaviour
    {
        [SerializeField] private RectTransform[] cards;
        [SerializeField] private Button[] getButtons;
        [SerializeField] private TextMeshProUGUI[] statusLabels;
        private ItemGraphic[] requiredItems;
        private TextMeshProUGUI[] rewardLabels;
        private TextMeshProUGUI[] buttonLabels;
        private bool clickHandlersBound;
        public RectTransform[] Cards => cards;
        public Button[] GetButtons => getButtons;
        public event Action<int> GetClicked;

        /// <summary>장면에 생성된 주문 슬롯·버튼·상태 문구 참조를 연결한다.</summary>
        public void Configure(RectTransform[] cardViews, Button[] buttons, TextMeshProUGUI[] labels)
        {
            cards = cardViews; getButtons = buttons; statusLabels = labels;
            CacheReferences();
            BindClickHandlers();
        }

        private void Awake()
        {
            CacheReferences();
            BindClickHandlers();
        }

        private void CacheReferences()
        {
            if (cards == null || getButtons == null) return;
            requiredItems = new ItemGraphic[cards.Length];
            rewardLabels = new TextMeshProUGUI[cards.Length];
            buttonLabels = new TextMeshProUGUI[getButtons.Length];
            for (int index = 0; index < cards.Length; index++)
            {
                requiredItems[index] = cards[index].GetComponentInChildren<ItemGraphic>(true);
            }
            for (int index = 0; index < getButtons.Length; index++)
                buttonLabels[index] = getButtons[index].GetComponentInChildren<TextMeshProUGUI>(true);
            for (int index = 0; index < cards.Length; index++)
            {
                foreach (var label in cards[index].GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (label == statusLabels[index] || label == buttonLabels[index]) continue;
                    rewardLabels[index] = label;
                    break;
                }
            }
        }

        private void BindClickHandlers()
        {
            if (clickHandlersBound || getButtons == null) return;
            for (int index = 0; index < getButtons.Length; index++)
            {
                int selected = index;
                getButtons[index].onClick.AddListener(() => GetClicked?.Invoke(selected));
            }
            clickHandlersBound = true;
        }

        /// <summary>현재 주문의 아이템·수량·보상과 Controller가 판정한 버튼 활성 여부를 갱신한다.</summary>
        public void Render(int index, OrderModel order, int availableCount, bool canSubmit)
        {
            requiredItems[index].Stage = order.RequiredStage;
            statusLabels[index].text = $"{BoardView.StageName(order.RequiredStage)} {availableCount}/{order.RequiredCount}";
            rewardLabels[index].text = "+" + order.Reward + " 코인";
            buttonLabels[index].text = "Get";
            getButtons[index].interactable = canSubmit;
        }
    }
}
