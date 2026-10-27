using UnityEngine;

public class DoorLink : MonoBehaviour
{
    public AttachableTarget wheel;
    public GameObject door;

    private void Update()
    {
        if (wheel != null && wheel.IsLocked && door != null && door.activeSelf)
            door.SetActive(false);
    }
}
