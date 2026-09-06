using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MergeBoard
{
    /// <summary>Animator 클립의 진행도를 동적 UI 목적지에 적용한다. 게임 상태는 변경하지 않는다.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class AnimatorFeedback : MonoBehaviour
    {
        public const float FlightDuration = 0.65f;
        public const float FastFlightDuration = FlightDuration * 0.5f;
        [SerializeField] private float progress;
        public float Progress => progress;
        private Vector2 start;
        private Vector2 destination;
        private RectTransform rect;
        private CanvasGroup canvasGroup;
        private UIFeedbackPool pool;
        private bool moving;
        private bool fading;
        private static RuntimeAnimatorController feedbackController;

        /// <summary>장면에 연결된 Animator에서 클릭 또는 합성 배율 클립을 처음부터 재생한다.</summary>
        public static void PlayScale(Transform target, string stateName)
        {
            var animator = target.GetComponent<Animator>();
            if (animator == null) return;
            animator.Play(stateName, 0, 0);
            animator.Update(0);
        }

        /// <summary>소비된 아이템의 표시를 복제해 보드 위치에서 주문 슬롯까지 비행시킨다.</summary>
        public static AnimatorFeedback Fly(RectTransform overlay, Vector3 sourceWorld, Vector3 targetWorld, ItemStage stage, float speed = 1f)
        {
            var pool = UIFeedbackPool.GetOrCreate(overlay);
            var icon = pool.AcquireItem();
            icon.Stage = stage;
            return StartMotion(icon.GetComponent<AnimatorFeedback>(), pool, icon.rectTransform,
                overlay.InverseTransformPoint(sourceWorld), overlay.InverseTransformPoint(targetWorld), "Flight", false, speed);
        }

        /// <summary>보상 텍스트를 위로 이동시키며 서서히 사라지게 한다.</summary>
        public static AnimatorFeedback ShowReward(RectTransform overlay, Vector3 targetWorld, int reward, TMP_FontAsset font)
        {
            var pool = UIFeedbackPool.GetOrCreate(overlay);
            var text = pool.AcquireReward();
            text.font = font;
            text.text = "+" + reward + " 코인";
            text.color = new Color32(174, 109, 29, 255);
            var position = (Vector2)overlay.InverseTransformPoint(targetWorld);
            return StartMotion(text.GetComponent<AnimatorFeedback>(), pool, text.rectTransform,
                position, position + Vector2.up * 54, "Reward", true);
        }

        private static AnimatorFeedback StartMotion(AnimatorFeedback feedback, UIFeedbackPool pool, RectTransform target, Vector2 from, Vector2 to, string state, bool fade, float speed = 1f)
        {
            target.anchorMin = target.anchorMax = target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = from;
            feedback.rect = target;
            feedback.start = from;
            feedback.destination = to;
            feedback.fading = fade;
            feedback.moving = true;
            feedback.pool = pool;
            feedback.progress = 0;
            feedback.canvasGroup = target.GetComponent<CanvasGroup>();
            feedback.canvasGroup.alpha = 1;
            feedback.canvasGroup.blocksRaycasts = false;
            var animator = feedback.GetComponent<Animator>();
            feedbackController ??= Resources.Load<RuntimeAnimatorController>("MergeFeedback");
            animator.runtimeAnimatorController = feedbackController;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = speed;
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
            if (value >= 0.999f)
            {
                moving = false;
                pool.Release(this);
            }
        }
    }
}
