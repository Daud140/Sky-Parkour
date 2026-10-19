using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI checkpointText;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI finalTimeText;

    private float elapsedTime;
    private int claimedCheckpoints = 0;
    private bool isRunning = true;

    public int ClaimedCheckpoints => claimedCheckpoints;
    public bool IsRunning => isRunning;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;

        // Auto-fix EventSystem input module dynamically at runtime
        FixEventSystem();
    }

    private void FixEventSystem()
    {
        var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
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

    private void Update()
    {
        if (!isRunning)
        {
            // Allow restarting instantly by pressing R, Enter, or Space
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame ||
                    Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                    Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    RestartGame();
                    return;
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                RestartGame();
                return;
            }
#endif
            return;
        }

        elapsedTime += Time.deltaTime;
        int mins = Mathf.FloorToInt(elapsedTime / 60);
        int secs = Mathf.FloorToInt(elapsedTime % 60);
        if (timerText != null)
        {
            timerText.text = $"Time: {mins:00}:{secs:00}";
        }
    }

    public void RegisterCheckpoint()
    {
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
        if (finalTimeText != null && timerText != null)
        {
            finalTimeText.text = $"Completed in: {timerText.text.Replace("Time: ", "")}\n<size=20><color=#99c2ff>Click button or Press [R] to Play Again</color></size>";
        }

        // Ensure EventSystem is clean and ready for UI clicks
        FixEventSystem();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(currentSceneName))
        {
            SceneManager.LoadScene(currentSceneName);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    private void OnGUI()
    {
        // Fail-safe button rendered directly via IMGUI (immune to EventSystem / InputModule crashes)
        if (!isRunning)
        {
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 20;
            btnStyle.fontStyle = FontStyle.Bold;
            btnStyle.normal.textColor = Color.white;
            btnStyle.hover.textColor = Color.yellow;

            float btnWidth = 260;
            float btnHeight = 60;
            float x = (Screen.width - btnWidth) / 2f;
            float y = (Screen.height / 2f) + 60f;

            GUI.backgroundColor = new Color(0.18f, 0.55f, 0.95f);
            if (GUI.Button(new Rect(x, y, btnWidth, btnHeight), "PLAY AGAIN (or [R])", btnStyle))
            {
                RestartGame();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
