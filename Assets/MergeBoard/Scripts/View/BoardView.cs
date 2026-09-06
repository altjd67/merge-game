using UnityEngine;
using UnityEngine.UIElements;

namespace MergeBoard
{
    public sealed class BoardView
    {
        public const float 칸간격 = 64;
        public const float 칸크기 = 60;
        public VisualElement 루트 { get; } = new VisualElement { name = "보드" };
        public VisualElement[] 칸 { get; } = new VisualElement[BoardModel.칸수];
        private readonly Label[] 이름 = new Label[BoardModel.칸수];
        private readonly ItemVisual[] 그림 = new ItemVisual[BoardModel.칸수];

        public BoardView()
        {
            루트.style.width = 448;
            루트.style.height = 576;
            루트.style.position = Position.Absolute;
            루트.style.left = 16;
            루트.style.top = 226;
            for (int 위치 = 0; 위치 < BoardModel.칸수; 위치++)
            {
                var 요소 = new VisualElement { name = "칸" + 위치 };
                요소.style.position = Position.Absolute;
                요소.style.left = 위치 % BoardModel.열수 * 칸간격;
                요소.style.top = 위치 / BoardModel.열수 * 칸간격;
                요소.style.width = 칸크기;
                요소.style.height = 칸크기;
                요소.style.backgroundColor = (Color)((위치 % 7 + 위치 / 7) % 2 == 0 ? new Color32(222, 231, 210, 255) : new Color32(210, 222, 198, 255));
                요소.style.borderTopLeftRadius = 요소.style.borderTopRightRadius = 9;
                요소.style.borderBottomLeftRadius = 요소.style.borderBottomRightRadius = 9;
                그림[위치] = new ItemVisual();
                요소.Add(그림[위치]);
                이름[위치] = new Label();
                이름[위치].pickingMode = PickingMode.Ignore;
                이름[위치].style.position = Position.Absolute;
                이름[위치].style.bottom = 2;
                이름[위치].style.width = Length.Percent(100);
                이름[위치].style.fontSize = 10;
                이름[위치].style.unityTextAlign = TextAnchor.MiddleCenter;
                요소.Add(이름[위치]);
                칸[위치] = 요소;
                루트.Add(요소);
            }
        }

        public void 표시(BoardModel 보드)
        {
            for (int 위치 = 0; 위치 < 칸.Length; 위치++)
            {
                그림[위치].단계 = 보드[위치];
                그림[위치].MarkDirtyRepaint();
                이름[위치].text = 단계이름(보드[위치]);
            }
        }

        public static string 단계이름(ItemStage 단계) => 단계 switch
        {
            ItemStage.Seed => "씨앗", ItemStage.Sprout => "새싹", ItemStage.Flower => "꽃", _ => ""
        };
    }

    // 원본 프로젝트의 아트를 복사하지 않는 작은 벡터 아이템 표시다.
    public sealed class ItemVisual : VisualElement
    {
        public ItemStage 단계;
        public ItemVisual()
        {
            pickingMode = PickingMode.Ignore;
            style.width = 60;
            style.height = 48;
            generateVisualContent += 그리기;
        }

        private void 그리기(MeshGenerationContext 문맥)
        {
            if (단계 == ItemStage.Empty) return;
            var 붓 = 문맥.painter2D;
            void 원(float 가로, float 세로, float 반지름, Color 색)
            {
                붓.fillColor = 색;
                붓.BeginPath();
                붓.Arc(new Vector2(가로, 세로), 반지름, 0, 360);
                붓.Fill();
            }
            if (단계 == ItemStage.Seed)
            {
                원(30, 26, 11, new Color32(133, 88, 54, 255));
                원(27, 22, 4, new Color32(204, 163, 104, 255));
                return;
            }
            붓.strokeColor = new Color32(57, 115, 70, 255);
            붓.lineWidth = 4;
            붓.BeginPath(); 붓.MoveTo(new Vector2(30, 40)); 붓.LineTo(new Vector2(30, 20)); 붓.Stroke();
            원(23, 29, 7, new Color32(106, 162, 87, 255));
            원(36, 25, 7, new Color32(79, 143, 81, 255));
            if (단계 != ItemStage.Flower) return;
            for (int 꽃잎 = 0; 꽃잎 < 5; 꽃잎++)
            {
                float 각도 = 꽃잎 * Mathf.PI * 2 / 5;
                원(30 + Mathf.Cos(각도) * 8, 16 + Mathf.Sin(각도) * 8, 7, new Color32(232, 134, 151, 255));
            }
            원(30, 16, 6, new Color32(249, 212, 112, 255));
        }
    }
}
