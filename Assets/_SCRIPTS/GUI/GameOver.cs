using UnityEngine;
using System.Collections;
using UnityEngine.UI;

using UnityEngine.SceneManagement;


public class GameOver : MonoBehaviour {

    private GameObject player;

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


    // Use this for initialization
    void Start () {
        playerDied = false;
        gameTimeIsOver = false;
        player = GameObject.FindGameObjectWithTag("Player1");
        timeLeft = 900;
    }

    // Update is called once per frame
    void Update () {

        if (timeLeft <= 0) {
            gameTimeIsOver = true;
        }

        if (player.GetComponent<PlayerCharacter>().curHealth <= 0)
        {
            playerDied = true;
        }

        if (playerDied || gameTimeIsOver)
        {
            textTimer.enabled = false;
            textEnemyCounter.enabled = false;
            textGreaterEnemyCounter.enabled = false;

        } else
        {
            timeLeft -= Time.deltaTime;
            textTimer.text = "Time left: " + Mathf.Round(timeLeft);
            textEnemyCounter.text = "Enemies killed: " + enemyKillCounter;
            textGreaterEnemyCounter.text = "Greater Enemies killed: " + greaterEnemyKillCounter;
        }

    }

    void OnGUI()
    {
        if (playerDied)
        {
            Time.timeScale = 0.0f;

            scoreBoardTextEnemies.text = "Enemies killed: " + enemyKillCounter;
            scoreBoardTextGreaterEnemies.text = "Greater Enemies killed: " + greaterEnemyKillCounter;
            gameOverText.text = "Game Over! You died!";

            pressButtonToCloseGame.text = "Press 'esc' to close the Game";
            if (Input.GetKeyUp(KeyCode.Escape)) Application.Quit();

         } else if (gameTimeIsOver)
        {
            Time.timeScale = 0.0f;

            scoreBoardTextEnemies.text = "Enemies killed: " + enemyKillCounter;
            scoreBoardTextGreaterEnemies.text = "Greater Enemies killed: " + greaterEnemyKillCounter;
            gameOverText.text = "Congratulations! You saved GeoWorld!";

            pressButtonToCloseGame.text = "Press 'esc' to close the Game";
            if (Input.GetKeyUp(KeyCode.Escape)) Application.Quit();

        }

    }



}
