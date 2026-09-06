using System;
using System.Collections.Generic;

namespace MergeBoard
{
    /// <summary>빈 칸과 하나의 아이템 계열에 속한 세 단계를 구분한다.</summary>
    public enum ItemStage { Empty, Seed, Sprout, Flower }

    /// <summary>좌측 상단 기준 7열 × 9행의 칸 상태를 보관한다.</summary>
    public sealed class BoardModel
    {
        public const int Columns = 7;
        public const int Rows = 9;
        public const int CellCount = Columns * Rows;
        private readonly ItemStage[] cells;
        public ItemStage this[int index] => cells[index];

        /// <summary>첫 행의 앞 4칸에 씨앗을 배치한 초기 보드를 만든다.</summary>
        public BoardModel() : this(new ItemStage[CellCount])
        {
            for (int index = 0; index < 4; index++) cells[index] = ItemStage.Seed;
        }

        /// <summary>63칸과 단계 범위를 검증하고 전달된 배열을 복사한다.</summary>
        public BoardModel(ItemStage[] source)
        {
            if (source == null || source.Length != CellCount) throw new ArgumentException("보드는 63칸이어야 합니다.");
            foreach (var stage in source)
                if (stage < ItemStage.Empty || stage > ItemStage.Flower) throw new ArgumentException("알 수 없는 단계입니다.");
            cells = (ItemStage[])source.Clone();
        }

        public static bool IsValidIndex(int index) => index >= 0 && index < CellCount;
        /// <summary>열·행을 인덱스로 변환하며, 보드 밖 좌표는 -1을 반환한다.</summary>
        public static int ToIndex(int column, int row) => column >= 0 && column < Columns && row >= 0 && row < Rows ? row * Columns + column : -1;
        /// <summary>저장과 검증에 사용할 독립된 칸 배열을 반환한다.</summary>
        public ItemStage[] CopyCells() => (ItemStage[])cells.Clone();
        internal void SetCell(int index, ItemStage stage) => cells[index] = stage;
        /// <summary>요청 단계의 칸을 인덱스 오름차순으로 반환한다. Empty는 빈 칸 검색이다.</summary>
        public List<int> FindCells(ItemStage stage)
        {
            var result = new List<int>();
            for (int index = 0; index < CellCount; index++) if (cells[index] == stage) result.Add(index);
            return result;
        }
    }
}
