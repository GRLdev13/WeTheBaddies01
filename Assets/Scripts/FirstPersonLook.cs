using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField, Min(0f)] private float sensitivity = 2f;
    [SerializeField, Range(1f, 89f)] private float pitchLimit = 85f;
    private float pitch;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReleaseCursor();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState != CursorLockMode.Locked || player == null)
            return;

        player.Rotate(Vector3.up, Input.GetAxisRaw("Mouse X") * sensitivity, Space.World);
        pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * sensitivity, -pitchLimit, pitchLimit);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void OnDisable() => ReleaseCursor();

    private static void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
