using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public enum Difficulty
{
    None,
    Easy,
    Hard
}

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    [Header("Level Containers")]
    [SerializeField] private GameObject courseEasy;
    [SerializeField] private GameObject courseHard;

    [Header("HUD Elements")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI checkpointText;
    [SerializeField] private TextMeshProUGUI modeBadgeText;

    [Header("Victory Screen")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI finalTimeText;

    [Header("Difficulty Selection UI")]
    [SerializeField] private GameObject difficultyPanel;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.45f;
    private AudioSource musicAudioSource;

    private Difficulty currentDifficulty = Difficulty.None;
    private bool isSelectingDifficulty = true;
    private bool isRunning = false;
    private float elapsedTime;
    private int claimedCheckpoints = 0;

    public Difficulty CurrentDifficulty => currentDifficulty;
    public bool IsSelectingDifficulty => isSelectingDifficulty;
    public bool IsRunning => isRunning;
    public bool RequiresCheckpoints => currentDifficulty == Difficulty.Hard;
    public int ClaimedCheckpoints => claimedCheckpoints;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        // Enforce 60 FPS cap & VSync for optimal laptop thermals, battery life, and smooth gameplay
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;

        // Start in difficulty selection mode before playing
        isSelectingDifficulty = true;
        isRunning = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (difficultyPanel != null) difficultyPanel.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);

        // Play looping horror background music throughout the game
        SetupBackgroundMusic();

        // Auto-fix EventSystem input module dynamically at runtime
        FixEventSystem();
    }

    private void SetupBackgroundMusic()
    {
        musicAudioSource = GetComponent<AudioSource>();
        if (musicAudioSource == null)
        {
            musicAudioSource = gameObject.AddComponent<AudioSource>();
        }

        musicAudioSource.playOnAwake = false;
        musicAudioSource.loop = true;
        musicAudioSource.spatialBlend = 0f; // 2D BGM heard uniformly across the whole game
        musicAudioSource.volume = musicVolume;

#if UNITY_EDITOR
        if (backgroundMusic == null)
        {
            backgroundMusic = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Horror.mp3");
        }
#endif

        if (backgroundMusic != null)
        {
            musicAudioSource.clip = backgroundMusic;
            if (!musicAudioSource.isPlaying)
            {
                musicAudioSource.Play();
            }
        }
    }

    private void FixEventSystem()
    {
        var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es != null)
        {
            var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
            {
                Destroy(standalone);
            }

#if ENABLE_INPUT_SYSTEM
            var inputModule = es.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }
#endif
        }
    }

    public void SelectDifficulty(Difficulty difficulty)
    {
        currentDifficulty = difficulty;
        isSelectingDifficulty = false;
        isRunning = true;
        elapsedTime = 0f;
        claimedCheckpoints = 0;

        if (courseEasy != null) courseEasy.SetActive(difficulty == Difficulty.Easy);
        if (courseHard != null) courseHard.SetActive(difficulty == Difficulty.Hard);

        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);

        if (difficulty == Difficulty.Easy)
        {
            if (checkpointText != null) checkpointText.text = "Difficulty: EASY (2 Jumps • No Checkpoints)";
            if (modeBadgeText != null) modeBadgeText.text = "<color=#00e676>EASY MODE</color>";
        }
        else
        {
            if (checkpointText != null) checkpointText.text = "Checkpoints: 0 / 2";
            if (modeBadgeText != null) modeBadgeText.text = "<color=#ff5722>HARD MODE</color>";
        }

        if (timerText != null) timerText.text = "Time: 00:00";

        // Reset player to starting checkpoint & lock cursor
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var respawn = player.GetComponent<PlayerRespawn>();
            if (respawn != null) respawn.RespawnToStart();

            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.LockCursor();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenDifficultyMenu()
    {
        isSelectingDifficulty = true;
        isRunning = false;

        if (difficultyPanel != null) difficultyPanel.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);

        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var respawn = player.GetComponent<PlayerRespawn>();
            if (respawn != null) respawn.RespawnToStart();

            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.UnlockCursor();
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (isSelectingDifficulty)
        {
            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Keyboard shortcuts to pick difficulty
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                {
                    SelectDifficulty(Difficulty.Easy);
                    return;
                }
                if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame || Keyboard.current.hKey.wasPressedThisFrame)
                {
                    SelectDifficulty(Difficulty.Hard);
                    return;
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.E))
            {
                SelectDifficulty(Difficulty.Easy);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.H))
            {
                SelectDifficulty(Difficulty.Hard);
                return;
            }
#endif
            return;
        }

        if (!isRunning)
        {
            // Victory screen input handling: R to restart current level, M to open menu
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    RestartCurrentLevel();
                    return;
                }
                if (Keyboard.current.mKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    OpenDifficultyMenu();
                    return;
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space))
            {
                RestartCurrentLevel();
                return;
            }
            if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape))
            {
                OpenDifficultyMenu();
                return;
            }
#endif
            return;
        }

        // Running: track elapsed time
        elapsedTime += Time.deltaTime;
        int mins = Mathf.FloorToInt(elapsedTime / 60);
        int secs = Mathf.FloorToInt(elapsedTime % 60);
        if (timerText != null)
        {
            timerText.text = $"Time: {mins:00}:{secs:00}";
        }

        // Allow pressing M during gameplay to quickly return to Difficulty Select
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            OpenDifficultyMenu();
        }
#else
        if (Input.GetKeyDown(KeyCode.M))
        {
            OpenDifficultyMenu();
        }
#endif
    }

    public void RegisterCheckpoint()
    {
        if (currentDifficulty != Difficulty.Hard) return;

        claimedCheckpoints++;
        if (checkpointText != null)
        {
            checkpointText.text = $"Checkpoints: {claimedCheckpoints} / 2";
        }
    }

    public void ShowVictoryScreen()
    {
        isRunning = false;
        if (winPanel != null) winPanel.SetActive(true);

        string modeLabel = (currentDifficulty == Difficulty.Easy) ? "<color=#00e676>[EASY MODE]</color>" : "<color=#ff5722>[HARD MODE]</color>";
        if (finalTimeText != null && timerText != null)
        {
            finalTimeText.text = $"{modeLabel}\nCompleted in: {timerText.text.Replace("Time: ", "")}\n<size=18><color=#99c2ff>Press [R] to Play Again  •  Press [M] to Change Level</color></size>";
        }

        FixEventSystem();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartCurrentLevel()
    {
        if (currentDifficulty == Difficulty.None)
        {
            OpenDifficultyMenu();
            return;
        }

        if (winPanel != null) winPanel.SetActive(false);

        // Reset all finish lines in the active scene so they can trigger again
        var finishLines = Object.FindObjectsByType<FinishLine>(FindObjectsSortMode.None);
        foreach (var fl in finishLines)
        {
            fl.ResetFinish();
        }

        SelectDifficulty(currentDifficulty);
    }

    private void OnGUI()
    {
        // 1. Difficulty Selection Interface (Opens first before playing)
        if (isSelectingDifficulty)
        {
            // Dark vignette backdrop
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

            float panelWidth = Mathf.Min(680f, Screen.width - 40f);
            float panelHeight = 460f;
            float px = (Screen.width - panelWidth) / 2f;
            float py = (Screen.height - panelHeight) / 2f;

            // Panel Background
            GUI.backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
            GUI.Box(new Rect(px, py, panelWidth, panelHeight), GUIContent.none);
            GUI.backgroundColor = Color.white;

            // Title
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(1.0f, 0.65f, 0.2f);
            GUI.Label(new Rect(px, py + 22, panelWidth, 42), "🌋 SKY PARKOUR", titleStyle);

            GUIStyle subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            subtitleStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
            GUI.Label(new Rect(px, py + 68, panelWidth, 28), "CHOOSE YOUR DIFFICULTY TO START", subtitleStyle);

            // Two Cards: Left = Easy, Right = Hard
            float cardWidth = (panelWidth - 60f) / 2f;
            float cardHeight = 270f;
            float cardY = py + 115f;

            // --- EASY CARD ---
            float easyX = px + 20f;
            GUI.backgroundColor = new Color(0.05f, 0.25f, 0.15f, 0.85f);
            GUI.Box(new Rect(easyX, cardY, cardWidth, cardHeight), GUIContent.none);
            GUI.backgroundColor = Color.white;

            GUIStyle cardHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            cardHeader.normal.textColor = new Color(0.2f, 1.0f, 0.5f);
            GUI.Label(new Rect(easyX, cardY + 15, cardWidth, 32), "🟢 EASY LEVEL", cardHeader);

            GUIStyle cardBody = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.UpperCenter
            };
            cardBody.normal.textColor = new Color(0.9f, 0.95f, 0.9f);
            GUI.Label(new Rect(easyX + 15, cardY + 55, cardWidth - 30, 110),
                "• Beginner friendly\n" +
                "• Exactly 2 simple jumps\n" +
                "• No checkpoints needed\n" +
                "• Pure speedrun practice", cardBody);

            GUIStyle btnEasyStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            btnEasyStyle.normal.textColor = Color.white;
            GUI.backgroundColor = new Color(0.1f, 0.75f, 0.35f);
            if (GUI.Button(new Rect(easyX + 15, cardY + 195, cardWidth - 30, 55), "PLAY EASY  [1 / E]", btnEasyStyle))
            {
                SelectDifficulty(Difficulty.Easy);
            }
            GUI.backgroundColor = Color.white;

            // --- HARD CARD ---
            float hardX = px + 40f + cardWidth;
            GUI.backgroundColor = new Color(0.3f, 0.10f, 0.05f, 0.85f);
            GUI.Box(new Rect(hardX, cardY, cardWidth, cardHeight), GUIContent.none);
            GUI.backgroundColor = Color.white;

            GUIStyle hardHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            hardHeader.normal.textColor = new Color(1.0f, 0.45f, 0.15f);
            GUI.Label(new Rect(hardX, cardY + 15, cardWidth, 32), "🔥 HARD LEVEL", hardHeader);

            GUIStyle hardBody = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                alignment = TextAnchor.UpperCenter
            };
            hardBody.normal.textColor = new Color(0.95f, 0.9f, 0.85f);
            GUI.Label(new Rect(hardX + 15, cardY + 55, cardWidth - 30, 110),
                "• Full Volcanic Storm Canyon\n" +
                "• 3 challenging stages\n" +
                "• Moving hazards & propellers\n" +
                "• 2 intermediate checkpoints", hardBody);

            GUIStyle btnHardStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            btnHardStyle.normal.textColor = Color.white;
            GUI.backgroundColor = new Color(0.95f, 0.32f, 0.08f);
            if (GUI.Button(new Rect(hardX + 15, cardY + 195, cardWidth - 30, 55), "PLAY HARD  [2 / H]", btnHardStyle))
            {
                SelectDifficulty(Difficulty.Hard);
            }
            GUI.backgroundColor = Color.white;

            // Footer note
            GUIStyle footerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            footerStyle.normal.textColor = new Color(0.6f, 0.6f, 0.7f);
            GUI.Label(new Rect(px, py + panelHeight - 34, panelWidth, 24), "Press [1] or [E] for Easy  |  Press [2] or [H] for Hard", footerStyle);
        }
        else if (!isRunning)
        {
            // 2. Victory Screen Buttons
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold
            };
            btnStyle.normal.textColor = Color.white;

            float btnWidth = 240;
            float btnHeight = 52;
            float y = (Screen.height / 2f) + 60f;

            GUI.backgroundColor = new Color(0.18f, 0.55f, 0.95f);
            if (GUI.Button(new Rect((Screen.width / 2f) - btnWidth - 10f, y, btnWidth, btnHeight), "PLAY AGAIN  [R]", btnStyle))
            {
                RestartCurrentLevel();
            }

            GUI.backgroundColor = new Color(0.85f, 0.42f, 0.12f);
            if (GUI.Button(new Rect((Screen.width / 2f) + 10f, y, btnWidth, btnHeight), "CHANGE LEVEL  [M]", btnStyle))
            {
                OpenDifficultyMenu();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
