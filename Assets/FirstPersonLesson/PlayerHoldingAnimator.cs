using UnityEngine;

// Apply an IK pose to the ORIGINAL arm. The item and hand share the same animated target.
[RequireComponent(typeof(Animator), typeof(PlayerInteractBehavior))]
public class PlayerHoldingAnimator : MonoBehaviour
{
    [SerializeField] private Transform gripTarget;
    [SerializeField] private Transform view;
    [SerializeField, Min(0.01f)] private float transitionTime = 0.18f;
    [SerializeField] private Vector3 gripLocalPosition = new Vector3(0.23f, -0.12f, 0.38f);
    // The grip target represents the handle center, not the wrist joint.
    [SerializeField] private Vector3 palmGripOffset = new Vector3(-0.10f, 0.01f, -0.035f);
    [SerializeField, Range(0f, 1f)] private float fingerGrip = 1f;
    private readonly HumanBodyBones[] gripBones = {
        HumanBodyBones.RightIndexProximal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexDistal,
        HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleDistal,
        HumanBodyBones.RightRingProximal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingDistal,
        HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleDistal,
        HumanBodyBones.RightThumbProximal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbDistal
    };
    private Transform[] fingers;
    private Quaternion[] openFingerRotations;
    private float previousGripWeight;
    private Animator animator;
    private PlayerInteractBehavior interaction;
    private Quaternion handRotationInView;
    private Quaternion ikToWristRotation;
    private bool wristBasisReady;
    private float weight;
    private Vector3 gripRestPosition;
    private Transform upperArm, forearm, wrist;
    private float armReach;
    public float HoldWeight => weight;
    public void Configure(Transform target, Transform cameraTransform) { gripTarget = target; view = cameraTransform; }
    private void Awake()
    {
        animator = GetComponent<Animator>();
        interaction = GetComponent<PlayerInteractBehavior>();
        if (view == null) view = GetComponentInChildren<Camera>()?.transform;
        if (!animator.isHuman || view == null) return;
        fingers = new Transform[gripBones.Length];
        openFingerRotations = new Quaternion[gripBones.Length];
        for (int i = 0; i < gripBones.Length; i++)
        {
            fingers[i] = animator.GetBoneTransform(gripBones[i]);
            if (fingers[i] != null) openFingerRotations[i] = fingers[i].localRotation;
        }
        gripRestPosition = gripLocalPosition;
        upperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        forearm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        wrist = animator.GetBoneTransform(HumanBodyBones.RightHand);
        armReach = Vector3.Distance(upperArm.position, forearm.position) + Vector3.Distance(forearm.position, wrist.position);
        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Transform elbow = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        if (hand != null && elbow != null)
        {
            Quaternion pose = Quaternion.FromToRotation(hand.position - elbow.position, view.forward) * hand.rotation;
            handRotationInView = Quaternion.Inverse(view.rotation) * pose;
        }
    }
    private void Start()
    {
        if (!animator.isHuman || view == null) return;
        animator.Update(0);
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        view.position = head.position + transform.up * 0.09f + transform.forward * 0.22f;
    }
    private void Update()
    {
        bool holding = interaction.HeldItem != null && interaction.HeldItem.IsHeld;
        weight = Mathf.MoveTowards(weight, holding ? 1 : 0, Time.deltaTime / transitionTime);
    }
    private void LateUpdate()
    {
        if (fingers == null) return;
        float gripWeight = Mathf.SmoothStep(0f, 1f, weight) * fingerGrip;
        if (gripWeight <= 0f && previousGripWeight <= 0f) return;
        for (int i = 0; i < fingers.Length; i++)
        {
            if (fingers[i] == null) continue;
            // This rig's fingers extend along local -X and close around local -Y.
            float curl = i < 12 ? (i % 3 == 0 ? -50f : i % 3 == 1 ? -65f : -35f) : (i == 12 ? -10f : -25f);
            Quaternion closed = openFingerRotations[i] * Quaternion.Euler(0f, curl, 0f);
            fingers[i].localRotation = Quaternion.Slerp(openFingerRotations[i], closed, gripWeight);
        }
        previousGripWeight = gripWeight;
    }
    private void OnAnimatorIK(int layerIndex)
    {
        if (layerIndex != 0 || !animator.isHuman || gripTarget == null || view == null) return;
        if (!wristBasisReady)
        {
            // Humanoid IK effector axes differ from this skeleton's wrist axes.
            ikToWristRotation = Quaternion.Inverse(animator.GetIKRotation(AvatarIKGoal.RightHand)) * wrist.rotation;
            wristBasisReady = true;
        }
        float blend = Mathf.SmoothStep(0, 1, weight);
        Vector3 desired = gripTarget.parent.TransformPoint(gripRestPosition);
        Quaternion rotation = view.rotation * handRotationInView;
        Vector3 palmOffset = rotation * ikToWristRotation * palmGripOffset;
        Vector3 wristTarget = desired - palmOffset;
        Vector3 reachable = upperArm.position + Vector3.ClampMagnitude(wristTarget - upperArm.position, armReach * 0.92f) + palmOffset;
        // The item shares the reachable target, keeping the grip aligned with the hand.
        gripTarget.position = reachable;
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, blend);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, blend);
        Quaternion gripRotation = view.rotation * handRotationInView;
        animator.SetIKPosition(AvatarIKGoal.RightHand, gripTarget.position - gripRotation * ikToWristRotation * palmGripOffset);
        animator.SetIKRotation(AvatarIKGoal.RightHand, view.rotation * handRotationInView);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, blend * 0.65f);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow,
            view.position + view.right * 0.48f - view.up * 0.44f + view.forward * 0.18f);
    }
}
