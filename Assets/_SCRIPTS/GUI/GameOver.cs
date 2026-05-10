using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif


public class GameOver : MonoBehaviour {

    [Tooltip("Optional tuning asset. Create via Assets → Create → GeoWorld → Game Balance.")]
    [SerializeField] private GameBalance gameBalance;

    private GameObject player;
    private PlayerCharacter m_Player;

    public bool playerDied;
    public bool gameTimeIsOver;

    public Texture2D blackTexture;

    private float timeLeft;

    public Text textTimer;
    public Text textEnemyCounter;
    public Text textGreaterEnemyCounter;

    public int enemyKillCounter;
    public int greaterEnemyKillCounter;

    public Text gameOverText;
    public Text scoreBoardTextEnemies;
    public Text scoreBoardTextGreaterEnemies;
    public Text pressButtonToCloseGame;

    private bool m_TimeFrozen;


    // Use this for initialization
    void Start () {
        playerDied = false;
        gameTimeIsOver = false;
        GameBalanceHelper.Register(gameBalance);
        timeLeft = GameBalanceHelper.RoundDurationSeconds;
        player = GameObject.FindGameObjectWithTag("Player1");
        if (player != null)
            m_Player = player.GetComponent<PlayerCharacter>();
        LayoutTopRightHudCounters();
    }

    /// <summary>
    /// Pin round timer and kill counters to the top-right (scene references may leave them mid-canvas).
    /// </summary>
    void LayoutTopRightHudCounters()
    {
        const float insetX = 14f;
        const float insetY = 14f;
        const float lineHeight = 32f;
        float y = -insetY;
        AnchorTopRightLine(textTimer, new Vector2(-insetX, y));
        y -= lineHeight;
        AnchorTopRightLine(textEnemyCounter, new Vector2(-insetX, y));
        y -= lineHeight;
        AnchorTopRightLine(textGreaterEnemyCounter, new Vector2(-insetX, y));
    }

    static void AnchorTopRightLine(Text t, Vector2 anchoredPosition)
    {
        if (t == null)
            return;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = anchoredPosition;
        t.alignment = TextAnchor.UpperRight;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
    }

    // Update is called once per frame
    void Update () {

        if (player == null || m_Player == null)
            return;

        if (timeLeft <= 0) {
            gameTimeIsOver = true;
        }

        if (m_Player.curHealth <= 0)
        {
            playerDied = true;
        }

        if (playerDied || gameTimeIsOver)
        {
            if (!m_TimeFrozen)
            {
                m_TimeFrozen = true;
                Time.timeScale = 0f;
            }
            if (textTimer != null) textTimer.enabled = false;
            if (textEnemyCounter != null) textEnemyCounter.enabled = false;
            if (textGreaterEnemyCounter != null) textGreaterEnemyCounter.enabled = false;

            if (playerDied)
                ApplyEndGameUi("Game Over! You died!");
            else
                ApplyEndGameUi("Congratulations! You saved GeoWorld!");

            SetQuitInstructions();
            HandleQuitRequest();
        }
        else
        {
            timeLeft -= Time.deltaTime;
            if (textTimer != null)
                textTimer.text = "Time left: " + Mathf.Round(timeLeft);
            if (textEnemyCounter != null)
                textEnemyCounter.text = "Enemies killed: " + enemyKillCounter;
            if (textGreaterEnemyCounter != null)
                textGreaterEnemyCounter.text = "Greater Enemies killed: " + greaterEnemyKillCounter;
        }

    }

    void ApplyEndGameUi(string title)
    {
        if (scoreBoardTextEnemies != null)
            scoreBoardTextEnemies.text = "Enemies killed: " + enemyKillCounter;
        if (scoreBoardTextGreaterEnemies != null)
            scoreBoardTextGreaterEnemies.text = "Greater Enemies killed: " + greaterEnemyKillCounter;
        if (gameOverText != null)
            gameOverText.text = title;
    }

    void SetQuitInstructions()
    {
        if (pressButtonToCloseGame == null) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        pressButtonToCloseGame.text = "Thanks for playing! You can close this browser tab.";
#else
        pressButtonToCloseGame.text = "Press 'esc' to close the Game";
#endif
    }

    void HandleQuitRequest()
    {
        if (!GameInput.PauseOrQuitUp) return;
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#elif UNITY_WEBGL
        // Browsers ignore Application.Quit; UI text explains closing the tab.
#else
        Application.Quit();
#endif
    }



}
