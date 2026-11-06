using UnityEngine;

// Step 1: read the AZERTY keyboard without moving the character directly.
public class PlayerInputManager : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }

    private void Update()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            MoveInput = Vector2.zero;
            return;
        }

        float horizontal = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
        float vertical = (Input.GetKey(KeyCode.Z) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        // Limit the vector length to 1 to prevent faster diagonal movement.
        MoveInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
    }

    private void OnDisable() => MoveInput = Vector2.zero;
}

