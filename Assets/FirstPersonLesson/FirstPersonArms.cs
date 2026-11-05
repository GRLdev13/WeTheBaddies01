using UnityEngine;

// Balancement de l'objet uniquement : les bras visibles sont ceux du personnage.
[DefaultExecutionOrder(100)]
public class FirstPersonArms : MonoBehaviour
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private float frequency = 8f;
    [SerializeField] private float amplitude = 0.018f;
    private Vector3 restPosition;
    private float phase;
    private void Awake()
    {
        restPosition = transform.localPosition;
        if (controller == null) controller = GetComponentInParent<CharacterController>();
    }
    private void Update()
    {
        Vector3 velocity = controller != null ? controller.velocity : Vector3.zero;
        velocity.y = 0;
        bool walking = controller != null && controller.isGrounded && velocity.sqrMagnitude > 0.01f;
        if (walking) phase += Time.deltaTime * frequency;
        Vector3 offset = walking ? new Vector3(Mathf.Sin(phase) * amplitude,
            Mathf.Cos(phase * 2f) * amplitude, 0) : Vector3.zero;
        transform.localPosition = Vector3.Lerp(transform.localPosition, restPosition + offset,
            1f - Mathf.Exp(-14f * Time.deltaTime));
    }
}
