using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class VerifyHoldingPose
{
    const string Report = "C:/Users/Guillaume-2026/Documents/ChatGPT/School Project/FeatureTransfer/holding-check.txt";
    static int stage, frames;
    static float firstX, maxSway;
    static double deadline;
    static PlayerController player;
    static PickupItem item;
    static Transform anchor;
    static VerifyHoldingPose()
    {
        EditorApplication.update += Tick;
    }
    [MenuItem("School/Verify original-arm holding")]
    public static void Begin()
    {
        File.WriteAllText(Report, "Holding pose verification started\n");
        SessionState.SetBool("VerifyOriginalHold", true);
        EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool("VerifyOriginalHold", false) || !EditorApplication.isPlaying) return;
        if (stage == 0)
        {
            deadline = EditorApplication.timeSinceStartup + 20;
            player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            player.enabled = false;
            player.GetComponent<PlayerInputManager>().enabled = false;
            player.playercamera.GetComponent<FirstPersonLook>().enabled = false;
            var interaction = player.GetComponent<PlayerInteractBehavior>();
            interaction.enabled = false;
            item = UnityEngine.Object.FindAnyObjectByType<PickupItem>();
            typeof(PlayerInteractBehavior).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(interaction, item);
            bool picked = interaction.TryPickUp();
            anchor = item.transform.parent;
            firstX = anchor.parent.localPosition.x;
            File.AppendAllText(Report, "picked=" + picked + "; renderersIncludingHeldItem=" + player.GetComponentInChildren<FirstPersonArms>().GetComponentsInChildren<Renderer>().Length + "\n");
            stage = 1; frames = 0;
        }
        else if (stage == 1)
        {
            player.GetComponent<CharacterController>().Move(player.transform.right * 0.012f - Vector3.up * 0.02f);
            maxSway = Mathf.Max(maxSway, Mathf.Abs(anchor.parent.localPosition.x - firstX));
            if (++frames < 100 && EditorApplication.timeSinceStartup < deadline) return;
            var animator = player.GetComponent<Animator>();
            var shoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var elbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            var wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
            int hiddenParts = 0;
            foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>())
                if (renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly) hiddenParts++;
            float eyeAhead = Vector3.Dot(player.playercamera.transform.position - animator.GetBoneTransform(HumanBodyBones.Head).position, player.transform.forward);
            File.AppendAllText(Report, "shadowsOnlyParts=" + hiddenParts + "; cameraAheadOfHead=" + eyeAhead + "\n");
            File.AppendAllText(Report, "camera=" + player.playercamera.transform.position + "; shoulder=" + shoulder.position + "; elbow=" + elbow.position + "; wrist=" + wrist.position + "; head=" + animator.GetBoneTransform(HumanBodyBones.Head).position + "; target=" + anchor.position + "\n");
            float distance = Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.RightHand).position, anchor.position);
            float forward = Vector3.Dot(anchor.position - player.playercamera.transform.position, player.playercamera.transform.forward);
            File.AppendAllText(Report, "holdWeight=" + player.GetComponent<PlayerHoldingAnimator>().HoldWeight + "; handError=" + distance + "; gripForward=" + forward + "; wobble=" + maxSway + "\n");
            var camera = player.playercamera;
            var rt = RenderTexture.GetTemporary(960, 540, 24);
            var old = camera.targetTexture;
            camera.targetTexture = rt; camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var image = new Texture2D(960, 540, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(Report), "holding-original-arm.png"), image.EncodeToPNG());
            camera.targetTexture = old; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(image);
            item.Drop(player.transform.position + player.transform.forward * 1.2f + Vector3.up);
            stage = 2; frames = 0;
        }
        else if (stage == 2 && ++frames > 40)
        {
            File.AppendAllText(Report, "releasedWeight=" + player.GetComponent<PlayerHoldingAnimator>().HoldWeight + "; dropped=" + !item.IsHeld + "\n");
            SessionState.SetBool("VerifyOriginalHold", false);
            stage = 0;
            EditorApplication.isPlaying = false;
        }
    }
}
