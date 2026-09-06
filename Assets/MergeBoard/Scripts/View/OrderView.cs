using System;
using UnityEngine;
using UnityEngine.UI;

namespace MergeBoard
{
    /// <summary>가로 주문 슬롯과 Get 버튼을 표시한다. 활성 조건은 Controller에서 전달받는다.</summary>
    public sealed class OrderView : MonoBehaviour
    {
        [SerializeField] private RectTransform[] cards;
        [SerializeField] private Button[] getButtons;
        [SerializeField] private Text[] statusLabels;
        public RectTransform[] Cards => cards;
        public Button[] GetButtons => getButtons;
        public event Action<int> GetClicked;

        /// <summary>장면에 생성된 주문 슬롯·버튼·상태 문구 참조를 연결한다.</summary>
        public void Configure(RectTransform[] cardViews, Button[] buttons, Text[] labels)
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

        /// <summary>수량·완료 표시와 Controller가 판정한 버튼 활성 여부를 갱신한다.</summary>
        public void Render(int index, OrderModel order, int availableCount, bool canSubmit)
        {
            statusLabels[index].text = order.Completed ? "완료" : $"{BoardView.StageName(order.RequiredStage)} {availableCount}/{order.RequiredCount}";
            getButtons[index].GetComponentInChildren<Text>().text = order.Completed ? "완료" : "Get";
            getButtons[index].interactable = canSubmit;
        }
    }
}
