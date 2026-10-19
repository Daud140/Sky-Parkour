using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [SerializeField] private AudioSource winAudio;
    private bool isFinished = false;

    public void ResetFinish()
    {
        isFinished = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFinished || !other.CompareTag("Player")) return;

        // In Hard mode, require 2 checkpoints before allowing completion. In Easy mode, no checkpoints are required.
        if (GameUI.Instance != null && GameUI.Instance.RequiresCheckpoints && GameUI.Instance.ClaimedCheckpoints < 2)
        {
            return;
        }

        isFinished = true;
        if (winAudio != null) winAudio.Play();

        GameUI.Instance?.ShowVictoryScreen();
    }
}
