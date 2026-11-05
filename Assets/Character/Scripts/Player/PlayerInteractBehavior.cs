using UnityEngine;

// Adaptation locale de PlayerItemHolder / HeldItem : viser, prendre, suivre la main, deposer.
public class PlayerInteractBehavior : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(0.1f)] private float interactionDistance = 3f;
    [SerializeField] private Transform handAnchor;
    private PickupItem target;
    public PickupItem HeldItem { get; private set; }
    public Collider CurrentTarget { get; private set; }
    public void SetHandAnchor(Transform hand) => handAnchor = hand;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
    }

    private void LateUpdate()
    {
        CurrentTarget = null;
        target = null;
        if (playerCamera == null || Cursor.lockState != CursorLockMode.Locked) return;
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float nearest = interactionDistance;
        // On teste AUSSI les murs : ignorer les obstacles permettrait de prendre au travers.
        foreach (RaycastHit hit in Physics.RaycastAll(ray, interactionDistance,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
            nearest = hit.distance;
            CurrentTarget = hit.collider;
        }
        if (CurrentTarget != null) target = CurrentTarget.GetComponentInParent<PickupItem>();
        if (target == null || target.IsHeld) CurrentTarget = null;
        Debug.DrawRay(ray.origin, ray.direction * nearest, target != null ? Color.green : Color.red);
        if (Input.GetKeyDown(KeyCode.G)) TryDrop();
        if (Input.GetKeyDown(KeyCode.E)) TryPickUp();
    }

    public bool TryPickUp()
    {
        // Prendre un nouvel objet depose automatiquement celui qui occupe la main.
        if (target == null || target.IsHeld || handAnchor == null) return false;
        if (HeldItem != null && !DropHeldItem(target)) return false;
        target.PickUp(handAnchor);
        HeldItem = target;
        return true;
    }

    public bool TryDrop() => DropHeldItem(null);

    private bool DropHeldItem(PickupItem itemBeingPickedUp)
    {
        if (HeldItem == null || playerCamera == null) return false;
        // Cherche un volume libre DEVANT la capsule, sans traverser un mur.
        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        Vector3 origin = transform.position + Vector3.up;
        Vector3 position = origin + forward * 1.1f;
        // Le nouvel objet va rejoindre la main : il ne bloque pas la place liberee.
        foreach (RaycastHit hit in Physics.RaycastAll(origin, forward, 1.1f,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.collider.transform.IsChildOf(transform)
                && (itemBeingPickedUp == null || !hit.collider.transform.IsChildOf(itemBeingPickedUp.transform))) return false;
        foreach (Collider obstacle in Physics.OverlapSphere(position, 0.5f,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!obstacle.transform.IsChildOf(transform)
                && (itemBeingPickedUp == null || !obstacle.transform.IsChildOf(itemBeingPickedUp.transform))) return false;
        HeldItem.Drop(position);
        HeldItem = null;
        return true;
    }

    private void OnDisable()
    {
        // Evite de perdre un objet quand on desactive le personnage pendant le jeu.
        if (HeldItem != null)
        {
            HeldItem.Drop(transform.position + transform.forward * 1.2f + Vector3.up);
            HeldItem = null;
        }
    }
}


