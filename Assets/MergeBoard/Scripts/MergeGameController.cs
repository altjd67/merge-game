using System;

namespace MergeBoard
{
    /// <summary>게임 상태 변경 뒤 진행 데이터를 기록하고, 저장 실패 안내를 제공하는 최소 계약입니다.</summary>
    public interface IGameStateSaver
    {
        /// <summary>현재 게임 상태를 영속 저장소에 기록합니다.</summary>
        /// <returns>저장 성공 여부입니다.</returns>
        bool Save(GameState state);

        /// <summary>가장 최근 저장 시도의 사용자 안내 메시지입니다.</summary>
        string LastError { get; }
    }

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
        public ItemStage ConsumedStage { get; }
        public string ReplacementOrderId { get; }
        public OrderResult(int[] cells, int reward, ItemStage stage, string replacementOrderId)
        {
            Success = true; ConsumedCells = cells; Reward = reward;
            ConsumedStage = stage; ReplacementOrderId = replacementOrderId;
        }
    }

    /// <summary>UI에 의존하지 않고 이동과 합성의 게임 규칙 및 상태 변경을 처리한다.</summary>
    public sealed class MergeGameController
    {
        public const long EnergyRecoverySeconds = 120;
        public const long MaximumUtcSeconds = 253402300799;
        public GameState State { get; }
        public BoardModel Board => State.Board;
        private readonly IGameStateSaver saveService;
        private readonly Random random;
        public string SaveMessage => saveService?.LastError ?? "";

        public MergeGameController(BoardModel board = null) : this(new GameState(board), null, null) { }

        /// <summary>복원한 상태와 저장 서비스를 연결한다. 검증에서는 저장 서비스를 생략할 수 있다.</summary>
        public MergeGameController(GameState state, IGameStateSaver saveService, Random random = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            this.saveService = saveService;
            this.random = random ?? new Random();
        }

        /// <summary>UTC 경과 시간으로 실행·종료 중 에너지를 회복하고 남은 초를 보존한다. 실제 변경 시에만 저장한다.</summary>
        public bool RefreshEnergy(long now)
        {
            if (!IsValidTime(now) || State.Energy == GameState.MaxEnergy) return false;
            long anchor = State.EnergyRecoveryAnchorUtcSeconds;
            if (anchor == 0 || now < anchor)
            {
                State.EnergyRecoveryAnchorUtcSeconds = now;
                Persist();
                return true;
            }
            int recovered = (int)Math.Min(GameState.MaxEnergy - State.Energy, (now - anchor) / EnergyRecoverySeconds);
            if (recovered == 0) return false;
            State.Energy += recovered;
            State.EnergyRecoveryAnchorUtcSeconds = State.Energy == GameState.MaxEnergy ? 0 : anchor + recovered * EnergyRecoverySeconds;
            Persist();
            return true;
        }

        /// <summary>회복 갱신 후 HUD에 표시할 다음 회복까지의 초를 반환한다. 최대치에서는 0이다.</summary>
        public long RecoveryRemaining(long now) => State.Energy == GameState.MaxEnergy ? 0 :
            Math.Max(0, Math.Min(EnergyRecoverySeconds, EnergyRecoverySeconds - (now - State.EnergyRecoveryAnchorUtcSeconds)));

        /// <summary>씨앗팩 근처 빈 칸에 씨앗을 만들고 성공당 에너지 1을 차감한다. 생성 쿨타임은 없다.</summary>
        public MoveResult Generate(int generatorIndex, long now)
        {
            if (!IsValidTime(now)) return new MoveResult(false, false, "잘못된 시간입니다.");
            if (!BoardModel.IsValidIndex(generatorIndex) || Board[generatorIndex] != ItemStage.SeedPack)
                return new MoveResult(false, false, "씨앗팩을 눌러주세요.");
            RefreshEnergy(now);
            int destination = Board.FindNearestEmptyCell(generatorIndex);
            if (destination < 0) return new MoveResult(false, false, "보드 가득 참");
            if (State.Energy == 0) return new MoveResult(false, false, "에너지가 부족합니다.");
            Board.SetCell(destination, ItemStage.Seed);
            if (State.Energy == GameState.MaxEnergy) State.EnergyRecoveryAnchorUtcSeconds = now;
            State.Energy--;
            State.GeneratorGuideCompleted = true;
            Persist();
            return new MoveResult(true, false, "씨앗이 자랄 준비를 마쳤어요.");
        }

        private static bool IsValidTime(long now) => now > 0 && now <= MaximumUtcSeconds;

        /// <summary>미완료 주문에 필요한 수량이 보드에 모였는지 판정한다.</summary>
        public bool CanSubmit(int orderIndex)
        {
            if (orderIndex < 0 || orderIndex >= State.Orders.Count) return false;
            var order = State.Orders[orderIndex];
            return Board.FindCells(order.RequiredStage).Count >= order.RequiredCount;
        }

        /// <summary>Get 클릭 시 수량을 재검사하고 아이템 소비·주문 완료·코인 보상을 한 번에 처리한다.</summary>
        /// <returns>성공 시 비행 표시의 출발점인 소비 칸 목록. 거절 시 기본 실패 결과.</returns>
        public OrderResult SubmitOrder(int orderIndex)
        {
            if (!CanSubmit(orderIndex)) return default;
            var order = State.Orders[orderIndex];
            var consumed = Board.FindCells(order.RequiredStage).GetRange(0, order.RequiredCount).ToArray();
            foreach (int index in consumed) Board.SetCell(index, ItemStage.Empty);
            State.Coins += order.Reward;
            string replacementId = PickReplacementOrderId(order.Id);
            State.ReplaceOrder(orderIndex, replacementId);
            Persist();
            return new OrderResult(consumed, order.Reward, order.RequiredStage, replacementId);
        }

        private string PickReplacementOrderId(string currentId)
        {
            int current = currentId == "Order01" ? 0 : currentId == "Order02" ? 1 : 2;
            int offset = random.Next(GameState.OrderTemplateCount - 1) + 1;
            return "Order0" + ((current + offset) % GameState.OrderTemplateCount + 1);
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
