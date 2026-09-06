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

    /// <summary>UI에 의존하지 않고 이동과 합성의 게임 규칙 및 상태 변경을 처리한다.</summary>
    public sealed class MergeGameController
    {
        public BoardModel Board { get; }
        public MergeGameController(BoardModel board = null) => Board = board ?? new BoardModel();

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
            return new MoveResult(true, merged, "");
        }
    }
}
