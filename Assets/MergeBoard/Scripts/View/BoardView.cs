using System;
using UnityEngine;

namespace MergeBoard
{
    /// <summary>보드의 uGUI 표시와 드래그 입력을 관리한다. 이동·합성 판정은 Controller에 전달한다.</summary>
    public sealed class BoardView : MonoBehaviour
    {
        public const float CellPitch = 64;
        public const float CellSize = 60;
        [SerializeField] private CellView[] cells;
        [SerializeField] private RectTransform dragLayer;
        public CellView[] Cells => cells;
        public bool IsDragging => source >= 0;
        public event Action<int, int> Dropped;
        public event Action<bool> DragStateChanged;
        public event Action<int> Clicked;
        private int source = -1;
        private ItemGraphic ghost;
        private int activePointerId;
        private readonly ItemStage[] renderedStages = new ItemStage[BoardModel.CellCount];
        private bool hasRenderedBoard;

        /// <summary>장면 생성 시 칸 참조와 화면 최상단 드래그 계층을 연결한다.</summary>
        public void Configure(CellView[] cellViews, RectTransform overlay)
        {
            cells = cellViews;
            dragLayer = overlay;
            hasRenderedBoard = false;
        }

        /// <summary>현재 모델의 단계와 이름을 모든 칸에 표시한다.</summary>
        /// <param name="force">true면 이전 렌더링 결과와 같아도 모든 셀의 표시를 다시 적용한다.</param>
        public void Render(BoardModel board, bool force = false)
        {
            for (int index = 0; index < cells.Length; index++)
            {
                ItemStage stage = board[index];
                if (!force && hasRenderedBoard && renderedStages[index] == stage) continue;
                cells[index].Render(stage, index == source, force);
                renderedStages[index] = stage;
            }
            hasRenderedBoard = true;
        }

        /// <summary>드래그 중이 아니면 클릭한 칸을 전달한다. 아이템 규칙은 Controller가 검사한다.</summary>
        public void Click(int index)
        {
            if (!IsDragging && BoardModel.IsValidIndex(index)) Clicked?.Invoke(index);
        }

        /// <summary>아이템 표시만 복제하여 드래그를 시작한다. 원본 모델은 드롭 전까지 유지한다.</summary>
        public void BeginDrag(int index, UnityEngine.EventSystems.PointerEventData evt)
        {
            if (IsDragging || cells[index].Stage == ItemStage.Empty || evt.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left) return;
            source = index;
            activePointerId = evt.pointerId;
            ghost ??= UIFactory.CreateItem(dragLayer, "드래그 아이템", Vector2.zero, ItemStage.Empty);
            ghost.gameObject.SetActive(true);
            ghost.Stage = cells[index].Stage;
            ghost.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cells[index].SetItemVisible(false);
            MoveDrag(evt);
            DragStateChanged?.Invoke(true);
        }

        /// <summary>포인터 화면 좌표를 Canvas 내부 좌표로 바꾸어 드래그 표시를 이동한다.</summary>
        public void MoveDrag(UnityEngine.EventSystems.PointerEventData evt)
        {
            if (!IsDragging || evt.pointerId != activePointerId) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, evt.position, evt.pressEventCamera, out var local))
            {
                ghost.rectTransform.anchorMin = ghost.rectTransform.anchorMax = dragLayer.pivot;
                ghost.rectTransform.anchoredPosition = local;
            }
        }

        /// <summary>드롭 좌표를 칸으로 변환한 뒤 표시를 복원하고 규칙 처리를 요청한다.</summary>
        public void EndDrag(UnityEngine.EventSystems.PointerEventData evt)
        {
            if (!IsDragging || evt.pointerId != activePointerId) return;
            int start = source;
            int destination = -1;
            for (int index = 0; index < cells.Length; index++)
                if (RectTransformUtility.RectangleContainsScreenPoint(cells[index].Rect, evt.position, evt.pressEventCamera)) { destination = index; break; }
            CancelDrag(false);
            Dropped?.Invoke(start, destination);
        }

        /// <summary>포커스 상실·입력 취소 시 임시 표시를 정리하며 모델은 바꾸지 않는다.</summary>
        public void CancelDrag(bool notify = true)
        {
            if (!IsDragging) return;
            cells[source].SetItemVisible(true);
            source = -1;
            if (ghost != null) ghost.gameObject.SetActive(false);
            if (notify) DragStateChanged?.Invoke(false);
        }

        private void OnDisable() => CancelDrag();

        /// <summary>아이템 단계의 한국어 표시 이름을 반환한다.</summary>
        public static string StageName(ItemStage stage) => GameText.StageName(stage);
    }
}
