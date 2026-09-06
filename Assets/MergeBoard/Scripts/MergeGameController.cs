using System;

namespace MergeBoard
{
    /// <summary>보드 명령의 성공 여부와 표시할 합성·실패 정보를 전달한다.</summary>
    public readonly struct MoveResult
    {
        public bool Success { get; }
        public bool Merged { get; }
        public string Message { get; }
        public MoveResult(bool success, bool merged, string message) { Success = success; Merged = merged; Message = message; }
    }

    /// <summary>주문 완료 시 소비한 칸 목록과 보상을 View에 전달한다.</summary>
    public readonly struct OrderResult
    {
        public bool Success { get; }
        public int[] ConsumedCells { get; }
        public int Reward { get; }
        public OrderResult(int[] cells, int reward) { Success = true; ConsumedCells = cells; Reward = reward; }
    }

    /// <summary>UI에 의존하지 않고 이동과 합성의 게임 규칙 및 상태 변경을 처리한다.</summary>
    public sealed class MergeGameController
    {
        public const double GeneratorCooldown = 2;
        public GameState State { get; }
        public BoardModel Board => State.Board;
        private readonly Random random = new Random();
        private double nextGenerationTime;
        private readonly LocalSaveService saveService;
        public string SaveMessage => saveService?.LastError ?? "";

        public MergeGameController(BoardModel board = null) : this(new GameState(board), null) { }

        /// <summary>복원한 상태와 저장 서비스를 연결한다. 검증에서는 저장 서비스를 생략할 수 있다.</summary>
        public MergeGameController(GameState state, LocalSaveService saveService)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            this.saveService = saveService;
        }

        /// <summary>호출자가 전달한 단조 증가 시각으로 생성기 대기시간을 계산한다.</summary>
        public double CooldownRemaining(double now) => Math.Max(0, nextGenerationTime - now);

        /// <summary>무작위 빈 칸에 씨앗을 생성한다. 실패 시 보드와 대기시간을 유지한다.</summary>
        public MoveResult Generate(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now)) return new MoveResult(false, false, "잘못된 시간입니다.");
            if (CooldownRemaining(now) > 0) return new MoveResult(false, false, "생성기 충전을 기다려주세요.");
            var emptyCells = Board.FindCells(ItemStage.Empty);
            if (emptyCells.Count == 0) return new MoveResult(false, false, "보드에 빈 칸이 없습니다.");
            Board.SetCell(emptyCells[random.Next(emptyCells.Count)], ItemStage.Seed);
            nextGenerationTime = now + GeneratorCooldown;
            Persist();
            return new MoveResult(true, false, "씨앗이 자랄 준비를 마쳤어요.");
        }

        /// <summary>미완료 주문에 필요한 수량이 보드에 모였는지 판정한다.</summary>
        public bool CanSubmit(int orderIndex)
        {
            if (orderIndex < 0 || orderIndex >= State.Orders.Count) return false;
            var order = State.Orders[orderIndex];
            return !order.Completed && Board.FindCells(order.RequiredStage).Count >= order.RequiredCount;
        }

        /// <summary>Get 클릭 시 수량을 재검사하고 아이템 소비·주문 완료·코인 보상을 한 번에 처리한다.</summary>
        /// <returns>성공 시 비행 표시의 출발점인 소비 칸 목록. 거절 시 기본 실패 결과.</returns>
        public OrderResult SubmitOrder(int orderIndex)
        {
            if (!CanSubmit(orderIndex)) return default;
            var order = State.Orders[orderIndex];
            var consumed = Board.FindCells(order.RequiredStage).GetRange(0, order.RequiredCount).ToArray();
            foreach (int index in consumed) Board.SetCell(index, ItemStage.Empty);
            order.Completed = true;
            State.Coins += order.Reward;
            Persist();
            return new OrderResult(consumed, order.Reward);
        }

        /// <summary>빈 칸으로 이동하거나 동일 단계 두 개를 합성한다. 거절 시 상태를 유지한다.</summary>
        /// <param name="source">드래그를 시작한 칸의 인덱스.</param>
        /// <param name="destination">드롭한 칸의 인덱스. 보드 밖은 음수로 전달할 수 있다.</param>
        /// <returns>이동·합성 여부와 사용자 안내.</returns>
        public MoveResult Move(int source, int destination)
        {
            if (!BoardModel.IsValidIndex(source) || !BoardModel.IsValidIndex(destination)) return new MoveResult(false, false, "보드 안에 놓아주세요.");
            var stage = Board[source];
            if (stage == ItemStage.Empty) return new MoveResult(false, false, "빈 칸은 이동할 수 없습니다.");
            if (source == destination) return new MoveResult(false, false, "");
            bool merged = Board[destination] == stage && stage < ItemStage.Flower;
            if (Board[destination] != ItemStage.Empty && !merged)
                return new MoveResult(false, false, "같은 단계의 씨앗이나 새싹을 겹쳐주세요.");
            Board.SetCell(destination, merged ? stage + 1 : stage);
            Board.SetCell(source, ItemStage.Empty);
            Persist();
            return new MoveResult(true, merged, "");
        }

        private void Persist() => saveService?.Save(State);
    }
}
