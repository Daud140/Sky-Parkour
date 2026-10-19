using UnityEngine;

/// <summary>
/// Plays and loops atmospheric background horror music for the entire game.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioClip musicClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 0.45f;
    [SerializeField] private bool loop = true;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = loop;
        audioSource.spatialBlend = 0f; // 2D BGM heard uniformly across the game
        audioSource.volume = volume;

#if UNITY_EDITOR
        if (musicClip == null)
        {
            musicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Horror.mp3");
        }
#endif

        if (musicClip != null)
        {
            audioSource.clip = musicClip;
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }
}
