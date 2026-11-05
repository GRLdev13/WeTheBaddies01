using UnityEngine;

// Apres PlayerController : animer la vitesse reelle, pas seulement la touche pressee.
// Ainsi, marcher contre un mur ramene progressivement le personnage au repos.
[DefaultExecutionOrder(50)]
[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class PlayerLocomotionAnimator : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float walkingSpeed = 4f;
    [SerializeField, Min(0f)] private float smoothing = 0.12f;
    private CharacterController controller;
    private Animator animator;
    private static readonly int MoveX = Animator.StringToHash("MoveX");
    private static readonly int MoveY = Animator.StringToHash("MoveY");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        // Le CharacterController reste responsable de la position et des collisions.
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void LateUpdate()
    {
        Vector3 local = transform.InverseTransformDirection(controller.velocity);
        Vector2 motion = Vector2.ClampMagnitude(new Vector2(local.x, local.z) / walkingSpeed, 1f);
        animator.SetFloat(MoveX, motion.x, smoothing, Time.deltaTime);
        animator.SetFloat(MoveY, motion.y, smoothing, Time.deltaTime);
    }
}
