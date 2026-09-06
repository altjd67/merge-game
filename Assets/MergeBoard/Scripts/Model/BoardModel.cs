using System;
using System.Collections.Generic;

namespace MergeBoard
{
    public enum ItemStage { Empty, Seed, Sprout, Flower }

    public sealed class BoardModel
    {
        public const int 열수 = 7;
        public const int 행수 = 9;
        public const int 칸수 = 열수 * 행수;
        private readonly ItemStage[] 칸;
        public ItemStage this[int 위치] => 칸[위치];

        public BoardModel() : this(new ItemStage[칸수])
        {
            for (int 위치 = 0; 위치 < 4; 위치++) 칸[위치] = ItemStage.Seed;
        }

        public BoardModel(ItemStage[] 원본)
        {
            if (원본 == null || 원본.Length != 칸수) throw new ArgumentException("보드는 63칸이어야 합니다.");
            foreach (var 단계 in 원본)
                if (단계 < ItemStage.Empty || 단계 > ItemStage.Flower) throw new ArgumentException("알 수 없는 단계입니다.");
            칸 = (ItemStage[])원본.Clone();
        }

        public static bool 유효위치(int 위치) => 위치 >= 0 && 위치 < 칸수;
        public static int 좌표변환(int 열, int 행) => 열 >= 0 && 열 < 열수 && 행 >= 0 && 행 < 행수 ? 행 * 열수 + 열 : -1;
        public ItemStage[] 복사() => (ItemStage[])칸.Clone();
        internal void 설정(int 위치, ItemStage 단계) => 칸[위치] = 단계;
        public List<int> 찾기(ItemStage 단계)
        {
            var 결과 = new List<int>();
            for (int 위치 = 0; 위치 < 칸수; 위치++) if (칸[위치] == 단계) 결과.Add(위치);
            return 결과;
        }
    }
}
