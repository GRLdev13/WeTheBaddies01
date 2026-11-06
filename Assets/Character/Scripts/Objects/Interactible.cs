using UnityEngine;

// Legacy marker already attached to some scene objects.
// An attached component must inherit from MonoBehaviour; using an interface caused
// Unity's "ExtensionOfNativeClass" error. Add PickupItem to make an object pickable.
public class Interactible : MonoBehaviour
{
}
