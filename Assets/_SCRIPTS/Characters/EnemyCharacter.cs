using UnityEngine;
using System.Collections;

public class EnemyCharacter : BaseCharacter {
    
    private GameObject player;
    public float expOnKill;
    public Color originalColor;
    public bool iAmGreaterEnemy;
    
    // Use this for initialization
    void Start () {

        originalColor = this.gameObject.GetComponent<Renderer>().material.color;
        player = GameObject.FindGameObjectWithTag("Player1");

        setEnemyStatistics(player.GetComponent<PlayerCharacter>().getCurLevel(), player.GetComponent<PlayerCharacter>().getExpNeededForLevelUp());
    }

    // Update is called once per frame
    void Update () {
        changeCurrentHealth(0);
    }

    public void setEnemyStatistics(float curPlayerLevel, float expNeededForLevelUp)
    {
        iAmGreaterEnemy = this.gameObject.GetComponent<GreaterEnemyAI>() != null;

        if (iAmGreaterEnemy)
        {
            maxHealth = curPlayerLevel * 250 * Random.Range(20f, 30f);
            curHealth = maxHealth;
            expOnKill = curPlayerLevel * 300;
        }
        else
        {
            maxHealth = curPlayerLevel * 25 * Random.Range(2f, 3f);
            curHealth = maxHealth;
            expOnKill = curPlayerLevel * 15;
        }

        //Bewegungsgeschwindigkeit der Gegner
        this.gameObject.GetComponent<EnemyAI>().moveSpeed = Random.Range(7.5f, 20f);

        //Geschwindigkeit mit der sich die Gegner (in die Richtung des Spielers) drehen
        this.gameObject.GetComponent<EnemyAI>().rotationSpeed = Random.Range(5f, 15f);

        //DAMAGE WERT DER MONSTER ZWISCHEN EINEM ZWANZIGSTEL UND EINEM ZEHNTEL DER MAXIMALEN LEBENSPUNKTE DES SPIELERS
        //this.gameObject.GetComponent<EnemyAI>().damage = Random.Range(player.GetComponent<PlayerCharacter>().getMaxHealth() / 20, player.GetComponent<PlayerCharacter>().getMaxHealth() / 10);
        this.gameObject.GetComponent<EnemyAI>().damage = Random.Range(player.GetComponent<PlayerCharacter>().getCurLevel() * 7.5f, player.GetComponent<PlayerCharacter>().getCurLevel() * 12.5f);

    }

    public float getExpOnKill()
    {
        return expOnKill;
    }

}