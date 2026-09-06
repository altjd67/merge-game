using System;
using System.Collections.Generic;

namespace MergeBoard
{
    /// <summary>보드·고정 주문·코인·에너지와 회복 기준 시각을 하나의 진행 상태로 묶는다.</summary>
    public sealed class GameState
    {
        public BoardModel Board { get; }
        public const int OrderTemplateCount = 3;
        private readonly OrderModel[] orders;
        public IReadOnlyList<OrderModel> Orders { get; }
        public int Coins { get; internal set; }
        public const int MaxEnergy = 100;
        public int Energy { get; internal set; } = MaxEnergy;
        public long EnergyRecoveryAnchorUtcSeconds { get; internal set; }
        public bool GeneratorGuideCompleted { get; internal set; }

        /// <summary>지정 보드 또는 기본 보드와 미완료 주문, 0코인으로 시작한다.</summary>
        public GameState(BoardModel board = null)
        {
            Board = board ?? new BoardModel();
            orders = new[]
            {
                CreateOrder("Order01"), CreateOrder("Order02"), CreateOrder("Order03")
            };
            Orders = Array.AsReadOnly(orders);
        }

        /// <summary>저장된 ID 또는 무작위 선택 결과로 고정 주문 템플릿을 만든다.</summary>
        public static OrderModel CreateOrder(string id) => id switch
        {
            "Order01" => new OrderModel("Order01", ItemStage.Sprout, 1, 20),
            "Order02" => new OrderModel("Order02", ItemStage.Flower, 1, 60),
            "Order03" => new OrderModel("Order03", ItemStage.Flower, 2, 150),
            _ => throw new ArgumentException("알 수 없는 주문 ID입니다.")
        };

        internal void ReplaceOrder(int index, string id) => orders[index] = CreateOrder(id);
    }
}
