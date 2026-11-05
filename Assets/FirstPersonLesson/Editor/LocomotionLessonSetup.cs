using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// Un graphe limite a la locomotion pour apprendre les Blend Trees progressivement.
public static class LocomotionLessonSetup
{
    [MenuItem("School/Install player locomotion animations")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Quitter Play avant installation.");
        var player = Object.FindAnyObjectByType<PlayerController>();
        var animator = player.GetComponent<Animator>();
        const string path = "Assets/FirstPersonLesson/PlayerLocomotion.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float);
            controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            var tree = new BlendTree
            {
                name = "Idle and directional walking",
                blendType = BlendTreeType.FreeformCartesian2D,
                blendParameter = "MoveX", blendParameterY = "MoveY",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(tree, controller);
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character/Animations/Idle/Idle.anim");
            var walk = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character/Animations/Locomotion/walk_fwd.anim");
            var strafe = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Character/Animations/Locomotion/strafe.anim");
            // Recul = marche jouee a l'envers ; le clip lateral fourni va a gauche.
            tree.children = new[]
            {
                new ChildMotion { motion = idle, position = Vector2.zero, timeScale = 1 },
                new ChildMotion { motion = walk, position = Vector2.up, timeScale = 1 },
                new ChildMotion { motion = walk, position = Vector2.down, timeScale = -1 },
                new ChildMotion { motion = strafe, position = Vector2.right, timeScale = 1, mirror = true },
                new ChildMotion { motion = strafe, position = Vector2.left, timeScale = 1 }
            };
            var state = controller.layers[0].stateMachine.AddState("Locomotion");
            state.motion = tree;
            controller.layers[0].stateMachine.defaultState = state;
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (player.GetComponent<PlayerLocomotionAnimator>() == null) player.gameObject.AddComponent<PlayerLocomotionAnimator>();
        EditorUtility.SetDirty(animator);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        AssetDatabase.SaveAssets();
    }
}
