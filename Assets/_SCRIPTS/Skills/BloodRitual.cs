using UnityEngine;
using System.Collections;

public class BloodRitual : SkillBasic{

    public Texture2D bloodRitualTexture1;
    public Texture2D bloodRitualTexture2;
    private float bloodTimer;
    private float bloodTimerCooldown;
    private bool showBloodStain;

    // Use this for initialization
    void Start () {
        curCooldown = 0;
        bloodTimer = 1;
        player = GameObject.FindGameObjectWithTag("Player1");
        showBloodStain = false;
    }

    // Update is called once per frame
    void Update () {

        maxCooldown = 50 / player.GetComponent<PlayerCharacter>().getCurLevel();
        bloodTimerCooldown = 1.5f;
        manacost = player.GetComponent<PlayerCharacter>().getCurLevel() * 5;
        updateCoolDown();


        if (player.GetComponent<PlayerCharacter>().skillAvailable(6))
        {
            if (Input.GetKeyUp(KeyCode.R) && requiredMana() && this.gameObject.GetComponent<GameOver>().playerDied == false && this.gameObject.GetComponent<GameOver>().gameTimeIsOver == false)
            {
                if (curCooldown == 0)
                {
                    showBloodStain = true;
                    convertHealthToMana();
                    curCooldown = maxCooldown;
                    bloodTimer = bloodTimerCooldown;
                }

            }
        }
        if (showBloodStain) calculateBloodTextureCooldown();



    }

    public void convertHealthToMana()
    {
        float healthToManaValue;

        if (player.GetComponent<PlayerCharacter>().curMana >= player.GetComponent<PlayerCharacter>().maxMana)
        {
            healthToManaValue = 0;
        } else
        {
            healthToManaValue = player.GetComponent<PlayerCharacter>().curHealth / 3.33f;

            player.GetComponent<PlayerCharacter>().curHealth -= healthToManaValue;
            player.GetComponent<PlayerCharacter>().curMana += healthToManaValue;
        }
     }

    void OnGUI()
    {
        if (showBloodStain && this.gameObject.GetComponent<GameOver>().playerDied == false && this.gameObject.GetComponent<GameOver>().gameTimeIsOver == false)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bloodRitualTexture1);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bloodRitualTexture2);
        }
    }

    protected void calculateBloodTextureCooldown()
    {
        if (bloodTimer > 0)
        {
            bloodTimer -= Time.deltaTime;
        }
        if (bloodTimer < 0)
        {
            bloodTimer = 0;
            showBloodStain = false;
        }
        if (bloodTimer == 0)
        {
            showBloodStain = false;
        }

    }
}
