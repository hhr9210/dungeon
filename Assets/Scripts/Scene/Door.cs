using UnityEngine;

public class Door : MonoBehaviour
{
    public float closedRotationY = 0f;

    public float openRotationY = -90f;

    public float rotationSpeed = 2f;

    public string playerTag = "Player";

    private Quaternion targetRotation;
    private bool isOpen = false;

    void Start()
    {
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, closedRotationY, transform.rotation.eulerAngles.z);
        targetRotation = transform.rotation;
    }

    void Update()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log(other.name + " entered the door trigger. Opening door.");
            targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, openRotationY, transform.rotation.eulerAngles.z);
            isOpen = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log(other.name + " exited the door trigger. Closing door.");
            targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, closedRotationY, transform.rotation.eulerAngles.z);
            isOpen = false;
        }
    }
}
