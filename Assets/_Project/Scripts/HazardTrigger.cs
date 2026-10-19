using UnityEngine;

public class HazardTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerRespawn>()?.Respawn();
        }
    }
}
