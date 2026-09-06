using UnityEngine;
using UnityEngine.UI;

namespace MergeBoard
{
    /// <summary>씨앗·새싹·꽃·씨앗팩을 단순한 메시로 그리는 uGUI Graphic이다.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ItemGraphic : MaskableGraphic
    {
        [SerializeField] private ItemStage stage;
        public ItemStage Stage
        {
            get => stage;
            set { if (stage == value) return; stage = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (stage == ItemStage.Empty) return;
            if (stage == ItemStage.SeedPack)
            {
                Rectangle(mesh, 12, 5, 36, 37, new Color32(173, 123, 69, 255));
                Rectangle(mesh, 14, 8, 32, 29, new Color32(247, 216, 151, 255));
                Rectangle(mesh, 14, 8, 32, 7, new Color32(82, 139, 77, 255));
                Circle(mesh, 25, 26, 6, new Color32(133, 88, 54, 255));
                Circle(mesh, 36, 29, 5, new Color32(157, 101, 58, 255));
                return;
            }
            if (stage == ItemStage.Seed)
            {
                Circle(mesh, 30, 26, 11, new Color32(133, 88, 54, 255));
                Circle(mesh, 27, 22, 4, new Color32(204, 163, 104, 255));
                return;
            }
            for (int y = 20; y <= 40; y += 3) Circle(mesh, 30, y, 2, new Color32(57, 115, 70, 255));
            Circle(mesh, 23, 29, 7, new Color32(106, 162, 87, 255));
            Circle(mesh, 36, 25, 7, new Color32(79, 143, 81, 255));
            if (stage != ItemStage.Flower) return;
            for (int petal = 0; petal < 5; petal++)
            {
                float angle = petal * Mathf.PI * 2 / 5;
                Circle(mesh, 30 + Mathf.Cos(angle) * 8, 16 + Mathf.Sin(angle) * 8, 7, new Color32(232, 134, 151, 255));
            }
            Circle(mesh, 30, 16, 6, new Color32(249, 212, 112, 255));
        }

        private void Rectangle(VertexHelper mesh, float x, float y, float width, float height, Color32 tint)
        {
            var bounds = rectTransform.rect;
            float left = bounds.xMin + x, top = bounds.yMax - y;
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, top), tint, Vector2.zero);
            mesh.AddVert(new Vector3(left + width, top), tint, Vector2.zero);
            mesh.AddVert(new Vector3(left + width, top - height), tint, Vector2.zero);
            mesh.AddVert(new Vector3(left, top - height), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }

        private void Circle(VertexHelper mesh, float x, float y, float radius, Color32 tint)
        {
            const int segments = 24;
            var bounds = rectTransform.rect;
            var center = new Vector2(bounds.xMin + x, bounds.yMax - y);
            int start = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.zero);
            for (int index = 0; index <= segments; index++)
            {
                float angle = index * Mathf.PI * 2 / segments;
                mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                if (index > 0) mesh.AddTriangle(start, start + index, start + index + 1);
            }
        }
    }
}
