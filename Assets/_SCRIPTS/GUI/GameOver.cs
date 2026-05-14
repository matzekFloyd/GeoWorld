using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif


/// <summary>
/// Round timer, kill counters, end-game UI. Updates <see cref="GameSession"/> each frame so other systems
/// do not need to reference this type for “run still active” checks.
/// </summary>
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
    /// <summary>Living bosses defeated (not mixed into <see cref="greaterEnemyKillCounter"/>).</summary>
    public int bossKillCounter;
    /// <summary>Cumulative bonus score from boss kills (<see cref="GameBalanceHelper.BossScoreBonusOnKill"/> per boss).</summary>
    public int bossBonusScoreTotal;

    public Text gameOverText;
    public Text scoreBoardTextEnemies;
    public Text scoreBoardTextGreaterEnemies;
    public Text pressButtonToCloseGame;

    private bool m_TimeFrozen;

    Text m_BossHudCounter;
    Text m_BossScoreboardCounter;


    // Use this for initialization
    void Start () {
        playerDied = false;
        gameTimeIsOver = false;
        GameBalanceHelper.Register(gameBalance);
        timeLeft = GameBalanceHelper.RoundDurationSeconds;
        GameSession.EnsureForScene();
        player = GameObject.FindGameObjectWithTag("Player1");
        if (player != null)
            m_Player = player.GetComponent<PlayerCharacter>();
        GameSession.Instance?.SyncRunState(playerDied, gameTimeIsOver);
        EnsureBossHudCounter();
        LayoutTopRightHudCounters();
    }

    void EnsureBossHudCounter()
    {
        if (m_BossHudCounter != null)
            return;
        if (textGreaterEnemyCounter == null)
            return;
        var go = Instantiate(textGreaterEnemyCounter.gameObject, textGreaterEnemyCounter.transform.parent);
        go.name = "BossKillCounterHud";
        m_BossHudCounter = go.GetComponent<Text>();
        if (m_BossHudCounter != null)
            m_BossHudCounter.text = "Bosses defeated: 0";
    }

    void EnsureBossScoreboardText()
    {
        if (m_BossScoreboardCounter != null)
            return;
        var template = scoreBoardTextEnemies != null ? scoreBoardTextEnemies : scoreBoardTextGreaterEnemies;
        if (template == null)
            return;
        var go = Instantiate(template.gameObject, template.transform.parent);
        go.name = "BossScoreboardLine";
        m_BossScoreboardCounter = go.GetComponent<Text>();
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
        y -= lineHeight;
        AnchorTopRightLine(m_BossHudCounter, new Vector2(-insetX, y));
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

    /// <summary>
    /// Rounded whole seconds (same as the legacy HUD) as <c>m:ss</c> for readability on long rounds.
    /// </summary>
    static string FormatRoundCountdownForHud(float timeLeftSeconds)
    {
        int total = Mathf.Max(0, Mathf.RoundToInt(timeLeftSeconds));
        int minutes = total / 60;
        int seconds = total % 60;
        return minutes + ":" + seconds.ToString("00");
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

        GameSession.Instance?.SyncRunState(playerDied, gameTimeIsOver);

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
            if (m_BossHudCounter != null) m_BossHudCounter.enabled = false;

            if (playerDied)
                ApplyEndGameUi("Game Over! You died!");
            else
                ApplyEndGameUi("Congratulations! You saved GeoWorld!");

            SetQuitInstructions();
            LayoutEndGameScoreboardAndQuit();
            HandleQuitRequest();
        }
        else
        {
            timeLeft -= Time.deltaTime;
            if (textTimer != null)
                textTimer.text = "Time left: " + FormatRoundCountdownForHud(timeLeft);
            if (textEnemyCounter != null)
                textEnemyCounter.text = "Enemies killed: " + enemyKillCounter;
            if (textGreaterEnemyCounter != null)
                textGreaterEnemyCounter.text = "Greater Enemies killed: " + greaterEnemyKillCounter;
            if (m_BossHudCounter != null)
            {
                m_BossHudCounter.text = bossBonusScoreTotal > 0
                    ? "Bosses defeated: " + bossKillCounter + "  (+" + bossBonusScoreTotal + " boss score)"
                    : "Bosses defeated: " + bossKillCounter;
            }
        }

    }

    void ApplyEndGameUi(string title)
    {
        if (scoreBoardTextEnemies != null)
            scoreBoardTextEnemies.text = "Enemies killed: " + enemyKillCounter;
        if (scoreBoardTextGreaterEnemies != null)
            scoreBoardTextGreaterEnemies.text = "Greater Enemies killed: " + greaterEnemyKillCounter;
        EnsureBossScoreboardText();
        if (m_BossScoreboardCounter != null)
        {
            m_BossScoreboardCounter.gameObject.SetActive(true);
            m_BossScoreboardCounter.text = bossBonusScoreTotal > 0
                ? "Bosses defeated: " + bossKillCounter + "  (+" + bossBonusScoreTotal + " boss score)"
                : "Bosses defeated: " + bossKillCounter;
        }
        if (gameOverText != null)
            gameOverText.text = title;
    }

    /// <summary>
    /// Vertical order: Enemies killed → Bosses defeated → Greater enemies killed → quit instruction (last).
    /// Called after <see cref="SetQuitInstructions"/> so the final line shows the key/tab message.
    /// </summary>
    void LayoutEndGameScoreboardAndQuit()
    {
        if (scoreBoardTextEnemies == null)
            return;

        var refRt = scoreBoardTextEnemies.rectTransform;
        float x = refRt.anchoredPosition.x;
        float y = refRt.anchoredPosition.y;
        const float gap = 6f;

        void PlaceStatLine(Text line)
        {
            if (line == null)
                return;
            var rt = line.rectTransform;
            MatchScoreboardStatLayout(rt, refRt);
            rt.anchoredPosition = new Vector2(x, y);
            y -= PreferredScoreboardLineHeight(line) + gap;
        }

        PlaceStatLine(scoreBoardTextEnemies);
        PlaceStatLine(m_BossScoreboardCounter);
        PlaceStatLine(scoreBoardTextGreaterEnemies);

        if (pressButtonToCloseGame == null)
            return;

        var pr = pressButtonToCloseGame.rectTransform;
        pr.anchorMin = refRt.anchorMin;
        pr.anchorMax = refRt.anchorMax;
        pr.pivot = refRt.pivot;
        pr.anchoredPosition = new Vector2(x, y);
    }

    static void MatchScoreboardStatLayout(RectTransform line, RectTransform reference)
    {
        line.anchorMin = reference.anchorMin;
        line.anchorMax = reference.anchorMax;
        line.pivot = reference.pivot;
        line.sizeDelta = reference.sizeDelta;
    }

    static float PreferredScoreboardLineHeight(Text line)
    {
        if (line == null)
            return 32f;
        float h = LayoutUtility.GetPreferredHeight(line.rectTransform);
        if (h > 2f)
            return h;
        return Mathf.Max(line.fontSize * 1.35f, 28f);
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
