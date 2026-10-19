using UnityEngine;

public class Oscillator : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 moveDirection = Vector3.up;
    public float moveDistance = 0f;
    public float moveSpeed = 2f;

    [Header("Rotation")]
    public Vector3 rotationSpeed = Vector3.zero;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (moveDistance > 0f)
        {
            float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
            transform.position = startPosition + moveDirection.normalized * offset;
        }

        if (rotationSpeed != Vector3.zero)
        {
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);
        }
    }
}
