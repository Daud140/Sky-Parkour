using System.Collections;
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform startCheckpoint;
    [SerializeField] private Transform currentCheckpoint;

    [Header("Death Sound Effect")]
    [SerializeField] private AudioClip deathSound;
    [Range(0f, 1f)] [SerializeField] private float deathVolume = 0.85f;

    [Header("Death Timing & Settings")]
    [SerializeField] private float fallThreshold = -3.5f;
    [SerializeField] private float deathDelay = 0.6f;

    private CharacterController controller;
    private PlayerMovement movement;
    private AudioSource deathAudioSource;
    private bool isDying = false;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();

        if (startCheckpoint == null && currentCheckpoint != null)
        {
            startCheckpoint = currentCheckpoint;
        }

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

    private void Update()
    {
        // Don't trigger fall death if already dying or if not running (e.g. in difficulty menu or victory screen)
        if (GameUI.Instance != null && !GameUI.Instance.IsRunning) return;

        // Detect falling below the course into the canyon abyss
        if (!isDying && transform.position.y < fallThreshold)
        {
            Die();
        }
    }

    public void SetStartCheckpoint(Transform start)
    {
        startCheckpoint = start;
        if (currentCheckpoint == null)
        {
            currentCheckpoint = start;
        }
    }

    public void SetCheckpoint(Transform newCheckpoint)
    {
        currentCheckpoint = newCheckpoint;
    }

    public void RespawnToStart()
    {
        StopAllCoroutines();
        isDying = false;

        if (startCheckpoint != null)
        {
            currentCheckpoint = startCheckpoint;
        }

        if (controller != null && currentCheckpoint != null)
        {
            controller.enabled = false;
            transform.position = currentCheckpoint.position;
            transform.rotation = currentCheckpoint.rotation;
            controller.enabled = true;
        }

        if (movement != null)
        {
            movement.enabled = true;
            movement.ResetVelocity();
        }
    }

    /// <summary>
    /// Triggered immediately upon death (hitting hazard, lava, or falling).
    /// Plays the death audio instantly at the moment of death, then respawns after a short delay.
    /// </summary>
    public void Die()
    {
        if (isDying || currentCheckpoint == null) return;
        StartCoroutine(DeathRoutine());
    }

    public void Respawn()
    {
        Die();
    }

    private IEnumerator DeathRoutine()
    {
        isDying = true;

        // 1. Play the death sound effect IMMEDIATELY just as the player dies
        PlayDeathSound();

        // 2. Halt movement and controller so player doesn't wander while dying
        if (movement != null)
        {
            movement.enabled = false;
            movement.ResetVelocity();
        }
        if (controller != null)
        {
            controller.enabled = false;
        }

        // 3. Wait for the death sound duration so the audio plays as you die, BEFORE respawning
        yield return new WaitForSeconds(deathDelay);

        // 4. Now perform the respawn back at the checkpoint
        if (currentCheckpoint != null)
        {
            transform.position = currentCheckpoint.position;
            transform.rotation = currentCheckpoint.rotation;
        }

        if (controller != null)
        {
            controller.enabled = true;
        }

        if (movement != null)
        {
            movement.enabled = true;
            movement.ResetVelocity();
        }

        isDying = false;
    }

    private void PlayDeathSound()
    {
        if (deathSound != null && deathAudioSource != null)
        {
            deathAudioSource.PlayOneShot(deathSound, deathVolume);
        }
    }
}
