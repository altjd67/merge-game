using System;
using System.Collections.Generic;

namespace MergeBoard
{
    /// <summary>보드, 고정 주문 세 개, 코인을 하나의 진행 상태로 묶는다.</summary>
    public sealed class GameState
    {
        public BoardModel Board { get; }
        public IReadOnlyList<OrderModel> Orders { get; }
        public int Coins { get; internal set; }

        /// <summary>지정 보드 또는 기본 보드와 미완료 주문, 0코인으로 시작한다.</summary>
        public GameState(BoardModel board = null)
        {
            Board = board ?? new BoardModel();
            Orders = Array.AsReadOnly(new[]
            {
                new OrderModel("Order01", ItemStage.Sprout, 1, 20),
                new OrderModel("Order02", ItemStage.Flower, 1, 60),
                new OrderModel("Order03", ItemStage.Flower, 2, 150)
            });
        }
    }
}
