using UnityEngine;

// Un objet ramassable ne connait pas les touches : le joueur decide quand le prendre.
[RequireComponent(typeof(Rigidbody))]
public class PickupItem : MonoBehaviour
{
    [SerializeField] private string itemName = "Objet";
    [SerializeField] private Vector3 heldPosition;
    [SerializeField] private Vector3 heldEulerAngles;
    private Rigidbody body;
    private Collider[] colliders;
    private bool[] colliderStates;

    public string ItemName => itemName;
    public bool IsHeld { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        colliderStates = new bool[colliders.Length];
    }

    public void Configure(string label, Vector3 position, Vector3 angles)
    {
        itemName = label;
        heldPosition = position;
        heldEulerAngles = angles;
    }

    public void PickUp(Transform hand)
    {
        if (IsHeld || hand == null) return;
        if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        // La physique ne doit pas pousser le joueur pendant que l'objet suit la main.
        body.isKinematic = true;
        body.useGravity = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }
        transform.SetParent(hand, true);
        transform.localPosition = heldPosition;
        transform.localRotation = Quaternion.Euler(heldEulerAngles);
        IsHeld = true;
    }

    public void Drop(Vector3 position)
    {
        if (!IsHeld) return;
        // Detacher conserve la taille et la rotation visibles, puis restitue la physique.
        transform.SetParent(null, true);
        transform.position = position;
        for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = colliderStates[i];
        // Un objet depose redevient toujours dynamique : la gravite le fait tomber.
        body.isKinematic = false;
        body.useGravity = true;
        if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        IsHeld = false;
    }
}

