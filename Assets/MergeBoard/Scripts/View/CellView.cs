using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeBoard
{
    /// <summary>한 칸의 아이템과 이름을 표시하고 uGUI 드래그 이벤트를 BoardView에 전달한다.</summary>
    public sealed class CellView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private int index;
        [SerializeField] private ItemGraphic icon;
        [SerializeField] private Text label;
        public RectTransform Rect => (RectTransform)transform;
        public ItemStage Stage => icon.Stage;
        public ItemGraphic Icon => icon;

        /// <summary>장면 생성 시 소속 보드·칸 인덱스·표시 요소를 연결한다.</summary>
        public void Configure(BoardView board, int cellIndex, ItemGraphic item, Text nameLabel)
        {
            boardView = board; index = cellIndex; icon = item; label = nameLabel;
        }

        /// <summary>아이템 단계와 표시 이름을 갱신하고 드래그 중인 원본은 숨긴다.</summary>
        public void Render(ItemStage stage, bool dragging)
        {
            icon.Stage = stage;
            label.text = BoardView.StageName(stage);
            SetItemVisible(!dragging);
        }

        /// <summary>아이템과 이름의 표시만 변경한다.</summary>
        public void SetItemVisible(bool visible) { icon.enabled = visible; label.enabled = visible; }
        public void OnBeginDrag(PointerEventData eventData) => boardView.BeginDrag(index, eventData);
        public void OnDrag(PointerEventData eventData) => boardView.MoveDrag(eventData);
        public void OnEndDrag(PointerEventData eventData) => boardView.EndDrag(eventData);
    }
}
