using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Material activeMaterial;
    [SerializeField] private MeshRenderer visualRenderer;
    [SerializeField] private AudioSource audioSource;
    private bool isClaimed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isClaimed || !other.CompareTag("Player")) return;

        isClaimed = true;
        if (visualRenderer != null && activeMaterial != null)
            visualRenderer.material = activeMaterial;

        if (audioSource != null) audioSource.Play();

        other.GetComponent<PlayerRespawn>()?.SetCheckpoint(transform);
        GameUI.Instance?.RegisterCheckpoint();
    }
}
