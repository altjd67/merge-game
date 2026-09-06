using UnityEngine;
using UnityEngine.UI;

namespace MergeBoard
{
    /// <summary>Animator 클립의 진행도를 동적 UI 목적지에 적용한다. 게임 상태는 변경하지 않는다.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorFeedback : MonoBehaviour
    {
        [SerializeField] private float progress;
        public float Progress => progress;
        private Vector2 start;
        private Vector2 destination;
        private RectTransform rect;
        private CanvasGroup canvasGroup;
        private bool moving;
        private bool fading;

        /// <summary>장면에 연결된 Animator에서 클릭 또는 합성 배율 클립을 처음부터 재생한다.</summary>
        public static void PlayScale(Transform target, string stateName)
        {
            var animator = target.GetComponent<Animator>();
            if (animator == null) return;
            animator.Play(stateName, 0, 0);
            animator.Update(0);
        }

        /// <summary>소비된 아이템의 표시를 복제해 보드 위치에서 주문 슬롯까지 비행시킨다.</summary>
        public static AnimatorFeedback Fly(RectTransform overlay, Vector3 sourceWorld, Vector3 targetWorld, ItemStage stage)
        {
            var icon = UIFactory.CreateItem(overlay, "주문 제출 비행", Vector2.zero, stage);
            return StartMotion(icon.rectTransform, overlay.InverseTransformPoint(sourceWorld), overlay.InverseTransformPoint(targetWorld), "Flight", false);
        }

        /// <summary>보상 텍스트를 위로 이동시키며 서서히 사라지게 한다.</summary>
        public static AnimatorFeedback ShowReward(RectTransform overlay, Vector3 targetWorld, int reward, Font font)
        {
            var text = UIFactory.CreateText(overlay, "코인 보상", Vector2.zero, new Vector2(144, 36), "+" + reward + " 코인", 22, TextAnchor.MiddleCenter);
            text.font = font;
            text.color = new Color32(174, 109, 29, 255);
            var position = (Vector2)overlay.InverseTransformPoint(targetWorld);
            return StartMotion(text.rectTransform, position, position + Vector2.up * 54, "Reward", true);
        }

        private static AnimatorFeedback StartMotion(RectTransform target, Vector2 from, Vector2 to, string state, bool fade)
        {
            target.anchorMin = target.anchorMax = target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = from;
            var feedback = target.gameObject.AddComponent<AnimatorFeedback>();
            feedback.rect = target;
            feedback.start = from;
            feedback.destination = to;
            feedback.fading = fade;
            feedback.moving = true;
            feedback.canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            feedback.canvasGroup.blocksRaycasts = false;
            var animator = feedback.GetComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("MergeFeedback");
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Play(state, 0, 0);
            animator.Update(0);
            return feedback;
        }

        private void LateUpdate()
        {
            if (!moving) return;
            float value = Mathf.Clamp01(progress);
            rect.anchoredPosition = Vector2.LerpUnclamped(start, destination, value);
            if (!fading) rect.anchoredPosition += Vector2.up * (Mathf.Sin(value * Mathf.PI) * 60);
            else canvasGroup.alpha = 1 - value;
            if (value >= 0.999f) Destroy(gameObject);
        }
    }
}
