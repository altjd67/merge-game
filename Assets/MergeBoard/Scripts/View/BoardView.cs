using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MergeBoard
{
    /// <summary>63칸을 표시하고 포인터 드래그를 칸 인덱스 명령으로 전달한다.</summary>
    public sealed class BoardView
    {
        public const float CellPitch = 64;
        public const float CellSize = 60;
        public VisualElement Root { get; } = new VisualElement { name = "보드" };
        public VisualElement[] Cells { get; } = new VisualElement[BoardModel.CellCount];
        private readonly Label[] labels = new Label[BoardModel.CellCount];
        private readonly ItemVisual[] icons = new ItemVisual[BoardModel.CellCount];
        public event Action<int, int> Dropped;
        public event Action<bool> DragStateChanged;
        public bool IsDragging => source >= 0;
        private int source = -1;
        private int pointerId = -1;
        private ItemVisual dragIcon;

        public BoardView()
        {
            Root.style.width = 448;
            Root.style.height = 576;
            Root.style.position = Position.Absolute;
            Root.style.left = 16;
            Root.style.top = 226;
            for (int index = 0; index < BoardModel.CellCount; index++)
            {
                var element = new VisualElement { name = "칸" + index };
                element.style.position = Position.Absolute;
                element.style.left = index % BoardModel.Columns * CellPitch;
                element.style.top = index / BoardModel.Columns * CellPitch;
                element.style.width = CellSize;
                element.style.height = CellSize;
                element.style.backgroundColor = (Color)((index % 7 + index / 7) % 2 == 0 ? new Color32(222, 231, 210, 255) : new Color32(210, 222, 198, 255));
                element.style.borderTopLeftRadius = element.style.borderTopRightRadius = 9;
                element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = 9;
                icons[index] = new ItemVisual();
                element.Add(icons[index]);
                labels[index] = new Label();
                labels[index].pickingMode = PickingMode.Ignore;
                labels[index].style.position = Position.Absolute;
                labels[index].style.bottom = 2;
                labels[index].style.width = Length.Percent(100);
                labels[index].style.fontSize = 10;
                labels[index].style.unityTextAlign = TextAnchor.MiddleCenter;
                element.Add(labels[index]);
                Cells[index] = element;
                int selectedIndex = index;
                element.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0 || IsDragging || icons[selectedIndex].Stage == ItemStage.Empty) return;
                    source = selectedIndex;
                    pointerId = evt.pointerId;
                    dragIcon = new ItemVisual { Stage = icons[selectedIndex].Stage };
                    dragIcon.style.position = Position.Absolute;
                    Root.Add(dragIcon);
                    icons[source].style.visibility = Visibility.Hidden;
                    labels[source].style.visibility = Visibility.Hidden;
                    Root.CapturePointer(pointerId);
                    MovePointer(evt.position);
                    DragStateChanged?.Invoke(true);
                    evt.StopPropagation();
                });
                Root.Add(element);
            }
            Root.RegisterCallback<PointerMoveEvent>(evt => { if (evt.pointerId == pointerId) MovePointer(evt.position); });
            Root.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!IsDragging || evt.pointerId != pointerId || evt.button != 0) return;
                int startIndex = source;
                var position = Root.WorldToLocal(evt.position);
                int column = Mathf.FloorToInt(position.x / CellPitch);
                int row = Mathf.FloorToInt(position.y / CellPitch);
                int destination = BoardModel.ToIndex(column, row);
                if (position.x - column * CellPitch > CellSize || position.y - row * CellPitch > CellSize) destination = -1;
                CancelDrag();
                Dropped?.Invoke(startIndex, destination);
                evt.StopPropagation();
            });
            Root.RegisterCallback<PointerCancelEvent>(_ => CancelDrag());
            Root.RegisterCallback<PointerCaptureOutEvent>(_ => CancelDrag());
        }

        private void MovePointer(Vector2 panelPosition)
        {
            if (!IsDragging) return;
            Vector2 position = Root.WorldToLocal(panelPosition);
            dragIcon.style.left = position.x - 30;
            dragIcon.style.top = position.y - 24;
        }

        /// <summary>포인터 캡처와 임시 표시를 해제한다. 보드 상태는 변경하지 않는다.</summary>
        public void CancelDrag()
        {
            if (!IsDragging) return;
            icons[source].style.visibility = Visibility.Visible;
            labels[source].style.visibility = Visibility.Visible;
            source = -1;
            int releasedPointerId = pointerId;
            pointerId = -1;
            dragIcon?.RemoveFromHierarchy();
            Root.ReleasePointer(releasedPointerId);
            DragStateChanged?.Invoke(false);
        }

        /// <summary>현재 모델의 아이템 단계와 이름을 모든 칸에 표시한다.</summary>
        public void Render(BoardModel board)
        {
            for (int index = 0; index < Cells.Length; index++)
            {
                icons[index].Stage = board[index];
                icons[index].MarkDirtyRepaint();
                labels[index].text = StageName(board[index]);
            }
        }

        /// <summary>아이템 단계의 한국어 표시 이름을 반환한다.</summary>
        public static string StageName(ItemStage stage) => stage switch
        {
            ItemStage.Seed => "씨앗", ItemStage.Sprout => "새싹", ItemStage.Flower => "꽃", _ => ""
        };
    }

    /// <summary>단계에 맞는 간단한 벡터 아이템을 그리는 표시 요소다.</summary>
    public sealed class ItemVisual : VisualElement
    {
        public ItemStage Stage;
        public ItemVisual()
        {
            pickingMode = PickingMode.Ignore;
            style.width = 60;
            style.height = 48;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            if (Stage == ItemStage.Empty) return;
            var painter = context.painter2D;
            void Circle(float x, float y, float radius, Color color)
            {
                painter.fillColor = color;
                painter.BeginPath();
                painter.Arc(new Vector2(x, y), radius, 0, 360);
                painter.Fill();
            }
            if (Stage == ItemStage.Seed)
            {
                Circle(30, 26, 11, new Color32(133, 88, 54, 255));
                Circle(27, 22, 4, new Color32(204, 163, 104, 255));
                return;
            }
            painter.strokeColor = new Color32(57, 115, 70, 255);
            painter.lineWidth = 4;
            painter.BeginPath(); painter.MoveTo(new Vector2(30, 40)); painter.LineTo(new Vector2(30, 20)); painter.Stroke();
            Circle(23, 29, 7, new Color32(106, 162, 87, 255));
            Circle(36, 25, 7, new Color32(79, 143, 81, 255));
            if (Stage != ItemStage.Flower) return;
            for (int petal = 0; petal < 5; petal++)
            {
                float angle = petal * Mathf.PI * 2 / 5;
                Circle(30 + Mathf.Cos(angle) * 8, 16 + Mathf.Sin(angle) * 8, 7, new Color32(232, 134, 151, 255));
            }
            Circle(30, 16, 6, new Color32(249, 212, 112, 255));
        }
    }
}
