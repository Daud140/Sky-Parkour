using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform currentCheckpoint;
    private CharacterController controller;
    private PlayerMovement movement;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();
    }

    public void SetCheckpoint(Transform newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
    }

    public void Respawn()
    {
        if (currentCheckpoint == null) return;
        
        // Disable CharacterController before updating position to avoid position override
        controller.enabled = false;
        transform.position = currentCheckpoint.position;
        transform.rotation = currentCheckpoint.rotation;
        controller.enabled = true;

        if (movement != null)
        {
            movement.ResetVelocity();
        }
    }
}
