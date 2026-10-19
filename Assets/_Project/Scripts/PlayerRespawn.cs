using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform currentCheckpoint;

    [Header("Death Sound Effect")]
    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)] [SerializeField] private float deathVolume = 0.85f;

    private CharacterController controller;
    private PlayerMovement movement;
    private AudioSource deathAudioSource;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();

        deathAudioSource = GetComponent<AudioSource>();
        if (deathAudioSource == null)
        {
            deathAudioSource = gameObject.AddComponent<AudioSource>();
        }
        deathAudioSource.playOnAwake = false;
        deathAudioSource.spatialBlend = 0f; // 2D audio for crisp, direct player feedback

#if UNITY_EDITOR
        if (deathSound == null)
        {
            deathSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/myinstants.mp3");
        }
#endif
    }

    public void SetCheckpoint(Transform newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
    }

    public void Respawn()
    {
        if (currentCheckpoint == null) return;

        // Play the death sound effect only when dying
        PlayDeathSound();

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

    private void PlayDeathSound()
    {
        if (deathSound != null && deathAudioSource != null)
        {
            deathAudioSource.PlayOneShot(deathSound, deathVolume);
        }
    }
}
