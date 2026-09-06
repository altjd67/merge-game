using System;

namespace MergeBoard
{
    /// <summary>버전 3의 로컬 JSON 포맷이다. 버전 1·2의 유효한 진행 상태도 변환한다.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version;
        public int[] cells;
        public int coins;
        public int energy;
        public long energyRecoveryAnchorUtcSeconds;
        public string[] orderIds;
        public bool[] completedOrders;
        public bool generatorGuideCompleted;

        /// <summary>게임 상태를 독립된 직렬화 데이터로 변환한다.</summary>
        public static SaveData FromState(GameState state)
        {
            var data = new SaveData
            {
                version = 3, cells = new int[BoardModel.CellCount], coins = state.Coins,
                energy = state.Energy, energyRecoveryAnchorUtcSeconds = state.EnergyRecoveryAnchorUtcSeconds,
                orderIds = new string[state.Orders.Count], completedOrders = new bool[state.Orders.Count],
                generatorGuideCompleted = state.GeneratorGuideCompleted
            };
            for (int index = 0; index < data.cells.Length; index++) data.cells[index] = (int)state.Board[index];
            for (int index = 0; index < data.orderIds.Length; index++)
            {
                data.orderIds[index] = state.Orders[index].Id;
                data.completedOrders[index] = false;
            }
            return data;
        }

        /// <summary>포맷·값·주문 보상 일관성을 검증한다. 손상된 데이터는 예외로 거절한다.</summary>
        public GameState ToState()
        {
            if ((version < 1 || version > 3) || cells == null || cells.Length != BoardModel.CellCount || orderIds == null || orderIds.Length != 3 || completedOrders == null || completedOrders.Length != 3)
                throw new ArgumentException("지원하지 않거나 불완전한 저장 데이터입니다.");
            var stages = new ItemStage[BoardModel.CellCount];
            for (int index = 0; index < stages.Length; index++) stages[index] = (ItemStage)cells[index];
            var state = new GameState(new BoardModel(stages));
            if (version == 1)
            {
                if (state.Board.FindCells(ItemStage.SeedPack).Count != 0) throw new ArgumentException("구버전 아이템 값이 올바르지 않습니다.");
                int destination = state.Board.FindNearestEmptyCell(BoardModel.InitialGeneratorIndex);
                if (destination < 0) throw new ArgumentException("구버전 변환에 필요한 빈 칸이 없습니다.");
                state.Board.SetCell(destination, ItemStage.SeedPack);
            }
            else
            {
                if (state.Board.FindCells(ItemStage.SeedPack).Count != 1 || energy < 0 || energy > GameState.MaxEnergy ||
                    energyRecoveryAnchorUtcSeconds < 0 || energyRecoveryAnchorUtcSeconds > MergeGameController.MaximumUtcSeconds ||
                    (energy == GameState.MaxEnergy ? energyRecoveryAnchorUtcSeconds != 0 : energyRecoveryAnchorUtcSeconds == 0))
                    throw new ArgumentException("생성기 또는 에너지 저장 상태가 올바르지 않습니다.");
                state.Energy = energy;
                state.EnergyRecoveryAnchorUtcSeconds = energyRecoveryAnchorUtcSeconds;
            }
            int expectedCoins = 0;
            for (int index = 0; index < state.Orders.Count; index++)
            {
                var savedOrder = GameState.CreateOrder(orderIds[index]);
                if (version < 3)
                {
                    if (savedOrder.Id != state.Orders[index].Id) throw new ArgumentException("주문 ID가 일치하지 않습니다.");
                    if (completedOrders[index]) expectedCoins += savedOrder.Reward;
                    state.ReplaceOrder(index, completedOrders[index] ? NextOrderId(savedOrder.Id) : savedOrder.Id);
                }
                else
                {
                    if (completedOrders[index]) throw new ArgumentException("반복 주문에는 완료 상태를 저장하지 않습니다.");
                    state.ReplaceOrder(index, savedOrder.Id);
                }
            }
            if (coins < 0 || (version < 3 && coins != expectedCoins)) throw new ArgumentException("코인과 주문 상태가 일치하지 않습니다.");
            state.Coins = coins;
            state.GeneratorGuideCompleted = version < 3 || generatorGuideCompleted;
            return state;
        }

        private static string NextOrderId(string id) => id == "Order01" ? "Order02" : id == "Order02" ? "Order03" : "Order01";
    }
}
