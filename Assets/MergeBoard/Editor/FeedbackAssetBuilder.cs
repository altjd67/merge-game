using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MergeBoard.Editor
{
    /// <summary>편집 가능한 AnimationClip·AnimatorController를 만들고 장면 아이템·버튼에 연결한다.</summary>
    public static class FeedbackAssetBuilder
    {
        /// <summary>누락된 피드백 에셋만 생성하며 기존 클립 편집 내용은 덮어쓰지 않는다.</summary>
        [MenuItem("머지 MVP/Animator 피드백 준비")]
        public static void PrepareFeedback()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Play Mode를 종료한 뒤 준비해주세요.");
            var game = Object.FindFirstObjectByType<MergeGameBootstrap>();
            if (game == null) throw new System.InvalidOperationException("MVP 장면을 먼저 열어주세요.");
            if (!AssetDatabase.IsValidFolder("Assets/MergeBoard/Animations")) AssetDatabase.CreateFolder("Assets/MergeBoard", "Animations");
            const string controllerPath = "Assets/MergeBoard/Resources/MergeFeedback.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var machine = controller.layers[0].stateMachine;
                var idle = machine.AddState("Idle"); idle.motion = CreateScaleClip("Idle", new[] { new Keyframe(0, 1), new Keyframe(0.1f, 1) });
                machine.defaultState = idle;
                var click = machine.AddState("Click"); click.motion = CreateScaleClip("Click", new[] { new Keyframe(0, 1), new Keyframe(0.08f, 0.88f), new Keyframe(0.22f, 1) });
                var merge = machine.AddState("Merge"); merge.motion = CreateScaleClip("Merge", new[] { new Keyframe(0, 1), new Keyframe(0.12f, 1.3f), new Keyframe(0.32f, 1) });
                ReturnToIdle(click, idle); ReturnToIdle(merge, idle);
                var flight = machine.AddState("Flight"); flight.motion = CreateMotionClip("Flight", 0.65f);
                var reward = machine.AddState("Reward"); reward.motion = CreateMotionClip("Reward", 1.1f);
            }
            foreach (var cell in game.BoardView.Cells) Attach(cell.Icon.gameObject, controller);
            foreach (var button in game.OrderView.GetButtons) Attach(button.gameObject, controller);
            Attach(game.HUD.GenerateButton.gameObject, controller);
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
        }

        private static void Attach(GameObject target, RuntimeAnimatorController controller)
        {
            var animator = target.GetComponent<Animator>();
            if (animator == null) animator = target.AddComponent<Animator>();
            var rect = target.GetComponent<RectTransform>();
            var center = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition += Vector2.Scale(rect.sizeDelta, center - rect.pivot);
            rect.pivot = center;
            animator.runtimeAnimatorController = controller;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);
        }

        private static void ReturnToIdle(AnimatorState state, AnimatorState idle)
        {
            var transition = state.AddTransition(idle);
            transition.hasExitTime = true; transition.exitTime = 1; transition.duration = 0;
        }

        private static AnimationClip CreateScaleClip(string name, Keyframe[] keys)
        {
            var clip = new AnimationClip { name = name, frameRate = 60 };
            foreach (string axis in new[] { "x", "y" })
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalScale." + axis), new AnimationCurve(keys));
            return SaveClip(clip);
        }

        private static AnimationClip CreateMotionClip(string name, float duration)
        {
            var clip = new AnimationClip { name = name, frameRate = 60 };
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(AnimatorFeedback), "progress"), AnimationCurve.EaseInOut(0, 0, duration, 1));
            return SaveClip(clip);
        }

        private static AnimationClip SaveClip(AnimationClip clip)
        {
            string path = "Assets/MergeBoard/Animations/" + clip.name + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null) { Object.DestroyImmediate(clip); return existing; }
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }
    }
}
