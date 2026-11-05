using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HoldingPoseSetup
{
    [MenuItem("School/Use original arms and animate holding")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Quitter Play avant modification.");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player == null) throw new InvalidOperationException("PlayerController introuvable.");
        var animator = player.GetComponent<Animator>();
        if (animator == null || !animator.isHuman) throw new InvalidOperationException("Avatar humanoide necessaire.");
        var motion = player.GetComponentInChildren<FirstPersonArms>(true);
        if (motion == null) throw new InvalidOperationException("Balancement introuvable.");
        Undo.RegisterFullObjectHierarchyUndo(player.gameObject, "Original arms holding pose");
        Transform anchor = Array.Find(motion.GetComponentsInChildren<Transform>(true), t => t.name == "HandAnchor");
        if (anchor == null) throw new InvalidOperationException("HandAnchor introuvable.");
        anchor.SetParent(motion.transform, false);
        anchor.localPosition = new Vector3(0.23f, -0.50f, 0.35f);
        anchor.localRotation = Quaternion.identity;
        foreach (string name in new[] { "LeftArm", "RightArm" })
        {
            Transform extra = motion.transform.Find(name);
            if (extra != null) Undo.DestroyObjectImmediate(extra.gameObject);
        }
        var holding = player.GetComponent<PlayerHoldingAnimator>();
        if (holding == null) holding = Undo.AddComponent<PlayerHoldingAnimator>(player.gameObject);
        holding.Configure(anchor, player.playercamera.transform);
        player.GetComponent<PlayerInteractBehavior>().SetHandAnchor(anchor);
        var controller = animator.runtimeAnimatorController as AnimatorController;
        if (controller == null) throw new InvalidOperationException("AnimatorController introuvable.");
        var layers = controller.layers;
        layers[0].iKPass = true;
        controller.layers = layers;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        animator.applyRootMotion = false;
        // Conserver le visage visible ; placer l'oeil devant la geometrie du crane.
        foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                EditorUtility.SetDirty(renderer);
            }
        }
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        player.playercamera.transform.position = head.position + player.transform.up * 0.09f + player.transform.forward * 0.22f;
        player.playercamera.nearClipPlane = 0.03f;
        EditorUtility.SetDirty(holding);
        EditorUtility.SetDirty(controller);
        PrefabUtility.SaveAsPrefabAsset(motion.gameObject, "Assets/FirstPersonLesson/Prefabs/FirstPersonArms.prefab");
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(player.gameObject.scene);
        Debug.Log("Holding pose installed: original right arm, no extra arm meshes, wobble preserved.");
    }
}
