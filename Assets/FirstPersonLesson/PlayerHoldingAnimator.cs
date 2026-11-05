using UnityEngine;

// Pose IK sur le bras ORIGINAL. L'objet et la main visent le meme point anime.
[RequireComponent(typeof(Animator), typeof(PlayerInteractBehavior))]
public class PlayerHoldingAnimator : MonoBehaviour
{
    [SerializeField] private Transform gripTarget;
    [SerializeField] private Transform view;
    [SerializeField, Min(0.01f)] private float transitionTime = 0.18f;
    [SerializeField] private Vector3 gripLocalPosition = new Vector3(0.23f, -0.12f, 0.38f);
    private Animator animator;
    private PlayerInteractBehavior interaction;
    private Quaternion handRotationInView;
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
    private void OnAnimatorIK(int layerIndex)
    {
        if (layerIndex != 0 || !animator.isHuman || gripTarget == null || view == null) return;
        float blend = Mathf.SmoothStep(0, 1, weight);
        Vector3 desired = gripTarget.parent.TransformPoint(gripRestPosition);
        Vector3 reachable = upperArm.position + Vector3.ClampMagnitude(desired - upperArm.position, armReach * 0.92f);
        // L'objet partage la cible atteignable : aucun ecart entre la prise et la main.
        gripTarget.position = reachable;
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, blend);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, blend);
        animator.SetIKPosition(AvatarIKGoal.RightHand, gripTarget.position);
        animator.SetIKRotation(AvatarIKGoal.RightHand, view.rotation * handRotationInView);
        animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, blend * 0.65f);
        animator.SetIKHintPosition(AvatarIKHint.RightElbow,
            view.position + view.right * 0.48f - view.up * 0.44f + view.forward * 0.18f);
    }
}
