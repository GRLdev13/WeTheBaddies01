using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputManager))]
public class PlayerController : MonoBehaviour
{
    public Camera playercamera;
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -20f;
    private CharacterController controller;
    private PlayerInputManager input;
    private float verticalSpeed;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = GetComponent<PlayerInputManager>();
        if (playercamera == null)
            playercamera = GetComponentInChildren<Camera>();
    }

    private void LateUpdate()
    {
        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;

        Vector2 movement = input.MoveInput;
        Vector3 velocity = (transform.right * movement.x + transform.forward * movement.y) * moveSpeed;
        velocity.y = verticalSpeed;
        controller.Move(velocity * Time.deltaTime);
    }
}
