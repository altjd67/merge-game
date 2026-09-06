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
        public RectTransform[] Cards => cards;
        public Button[] GetButtons => getButtons;
        public event Action<int> GetClicked;

        /// <summary>장면에 생성된 주문 슬롯·버튼·상태 문구 참조를 연결한다.</summary>
        public void Configure(RectTransform[] cardViews, Button[] buttons, TextMeshProUGUI[] labels)
        {
            cards = cardViews; getButtons = buttons; statusLabels = labels;
        }

        private void Awake()
        {
            for (int index = 0; index < getButtons.Length; index++)
            {
                int selected = index;
                getButtons[index].onClick.AddListener(() => GetClicked?.Invoke(selected));
            }
        }

        /// <summary>현재 주문의 아이템·수량·보상과 Controller가 판정한 버튼 활성 여부를 갱신한다.</summary>
        public void Render(int index, OrderModel order, int availableCount, bool canSubmit)
        {
            cards[index].Find("RequiredItem").GetComponent<ItemGraphic>().Stage = order.RequiredStage;
            statusLabels[index].text = $"{BoardView.StageName(order.RequiredStage)} {availableCount}/{order.RequiredCount}";
            cards[index].Find("Reward").GetComponent<TextMeshProUGUI>().text = "+" + order.Reward + " 코인";
            getButtons[index].GetComponentInChildren<TextMeshProUGUI>().text = "Get";
            getButtons[index].interactable = canSubmit;
        }
    }
}
