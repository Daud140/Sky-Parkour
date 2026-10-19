using UnityEngine;

public class HazardTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var respawn = other.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                respawn.Die();
            }
        }
    }
}
