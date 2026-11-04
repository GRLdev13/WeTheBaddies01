using UnityEngine;

public class PlayerInteractBehavior : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(0.1f)] private float interactionDistance = 4f;
    private const int InteractibleLayer = 6;

    public Collider CurrentTarget { get; private set; }

    private void Awake()
    {
        ResolveCamera();
    }

    private void LateUpdate()
    {
        if (!ResolveCamera())
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hasHit = FindHit(ray, out RaycastHit hit);
        CurrentTarget = hasHit && hit.collider.gameObject.layer == InteractibleLayer ? hit.collider : null;
        Vector3 end = hasHit ? hit.point : ray.GetPoint(interactionDistance);
        Debug.DrawLine(ray.origin, end, CurrentTarget != null ? Color.green : Color.red);

        if (CurrentTarget != null && Cursor.lockState == CursorLockMode.Locked && Input.GetKeyDown(KeyCode.E))
        {
            Destroy(CurrentTarget.gameObject);
            CurrentTarget = null;
        }
    }

    private bool ResolveCamera()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        return playerCamera != null;
    }

    // Include obstacles so interactibles cannot be targeted through walls.
    // Ignore the player's own capsule and any colliders attached to its children.
    private bool FindHit(Ray ray, out RaycastHit nearest)
    {
        nearest = default;
        float nearestDistance = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(ray, interactionDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearestDistance)
                continue;
            nearest = hit;
            nearestDistance = hit.distance;
        }
        return nearestDistance < float.PositiveInfinity;
    }

    private void OnDrawGizmos()
    {
        if (!ResolveCamera())
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hasHit = FindHit(ray, out RaycastHit hit);
        bool canInteract = hasHit && hit.collider.gameObject.layer == InteractibleLayer;
        Vector3 end = hasHit ? hit.point : ray.GetPoint(interactionDistance);
        Gizmos.color = canInteract ? Color.green : Color.red;
        Gizmos.DrawLine(ray.origin, end);
        Gizmos.DrawWireSphere(end, 0.035f);
    }
}
