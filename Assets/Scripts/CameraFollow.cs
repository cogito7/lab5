using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;        // Player
    public Vector3 offset = new Vector3(0, 10, -10);
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(target.position);
    }
}
