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

        // In Hard mode (2 checkpoints), Medium mode (1 checkpoint), Easy mode (0 checkpoints)
        if (GameUI.Instance != null && GameUI.Instance.ClaimedCheckpoints < GameUI.Instance.TargetCheckpoints)
        {
            return;
        }

        isFinished = true;
        if (winAudio != null) winAudio.Play();

        GameUI.Instance?.ShowVictoryScreen();
    }
}
