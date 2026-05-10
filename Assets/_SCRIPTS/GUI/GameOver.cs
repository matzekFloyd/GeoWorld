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

        } else
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

    void OnGUI()
    {
        if (playerDied)
        {
            if (scoreBoardTextEnemies != null)
                scoreBoardTextEnemies.text = "Enemies killed: " + enemyKillCounter;
            if (scoreBoardTextGreaterEnemies != null)
                scoreBoardTextGreaterEnemies.text = "Greater Enemies killed: " + greaterEnemyKillCounter;
            if (gameOverText != null)
                gameOverText.text = "Game Over! You died!";

            SetQuitInstructions();
            HandleQuitRequest();

         } else if (gameTimeIsOver)
        {
            if (scoreBoardTextEnemies != null)
                scoreBoardTextEnemies.text = "Enemies killed: " + enemyKillCounter;
            if (scoreBoardTextGreaterEnemies != null)
                scoreBoardTextGreaterEnemies.text = "Greater Enemies killed: " + greaterEnemyKillCounter;
            if (gameOverText != null)
                gameOverText.text = "Congratulations! You saved GeoWorld!";

            SetQuitInstructions();
            HandleQuitRequest();

        }

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
