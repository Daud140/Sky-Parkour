using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [SerializeField] private AudioSource winAudio;
    private bool isFinished = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isFinished || !other.CompareTag("Player")) return;

        // Require 2 checkpoints before allowing course completion
        if (GameUI.Instance != null && GameUI.Instance.ClaimedCheckpoints < 2)
        {
            return;
        }

        isFinished = true;
        if (winAudio != null) winAudio.Play();

        GameUI.Instance?.ShowVictoryScreen();
    }
}
