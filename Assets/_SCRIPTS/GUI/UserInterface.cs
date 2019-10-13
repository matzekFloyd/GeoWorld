using UnityEngine;
using System.Collections;

public class UserInterface : MonoBehaviour {

    private GameObject player;
    private GameObject enemy;

    private float curPlayerHealth;
    private float maxPlayerHealth;
    private float playerHealthBarLength;

    private float curPlayerMana;
    private float maxPlayerMana;
    private float playerManaBarLength;

    private float curPlayerLevel;
    private float maxPlayerLevel;

    private float playerExp;
    private float playerExpNeededForLevelUp;
    private float playerExpBarLength;
    private float maxPlayerExpBarLength;

    private int maxNumberOfSkills;

    private float skillIconWidth;
    private float maxBarLength;

    public Texture2D crosshairImage;
    public Texture2D healthBarTexture;
    public Texture2D manaBarTexture;
    public Texture2D expTexture;
    public Texture2D singleShotTexture;
    public Texture2D sprayShotTexture;
    public Texture2D geoPhysicsTexture;
    public Texture2D healTexture;
    public Texture2D fireBallTexture;
    public Texture2D bloodRitualTexture;
    public Texture2D freezeTimeTexture;
    public Texture2D geoManiaTexture;
    public Texture2D backgroundTexture;
    public Texture2D frameTexture;

    public Texture2D bloodTexture1;
    public Texture2D bloodTexture2;
    public Texture2D bloodTexture3;

    // Use this for initialization
    void Start () {

        player = GameObject.FindGameObjectWithTag("Player1");
        maxBarLength = Screen.width / 4;
        maxNumberOfSkills = 8;
        skillIconWidth = maxBarLength / maxNumberOfSkills;
    }

    // Update is called once per frame
    void Update () {

        curPlayerLevel = player.GetComponent<PlayerCharacter>().getCurLevel();
        maxPlayerLevel = player.GetComponent<PlayerCharacter>().getMaxLevel();
        curPlayerHealth = player.GetComponent<PlayerCharacter>().getCurHealth();
        maxPlayerHealth = player.GetComponent<PlayerCharacter>().getMaxHealth();
        curPlayerMana = player.GetComponent<PlayerCharacter>().getCurMana();
        maxPlayerMana = player.GetComponent<PlayerCharacter>().getMaxMana();
        playerExp = player.GetComponent<PlayerCharacter>().getCurExp();
        playerExpNeededForLevelUp = player.GetComponent<PlayerCharacter>().getExpNeededForLevelUp();

        adjustBarLength();
    }

    void OnGUI()
    {
        if(this.gameObject.GetComponent<GameOver>().playerDied == false && this.gameObject.GetComponent<GameOver>().gameTimeIsOver == false)
        {
            GUI.color = Color.white;

            //CLASS/NAME GUI
            GUI.Box(new Rect(10, 10, 100, 20), "GeoMancer ");
            GUI.Box(new Rect(110, 10, Screen.width / 4, 20), "Level " + curPlayerLevel);

            //HEALTH GUI
            GUI.Box(new Rect(10, 30, 100, 20), "Health: ");
            GUI.DrawTexture(new Rect(110, 30, playerHealthBarLength, 20), healthBarTexture);
            GUI.Box(new Rect(110, 30, maxBarLength, 20), (int)curPlayerHealth + "/" + (int)maxPlayerHealth);

            //MANA GUI
            GUI.Box(new Rect(10, 50, 100, 20), "Mana: ");
            GUI.DrawTexture(new Rect(110, 50, playerManaBarLength, 20), manaBarTexture);
            GUI.Box(new Rect(110, 50, maxBarLength, 20), (int)curPlayerMana + "/" + (int)maxPlayerMana);

            //EXP GUI
            GUI.Box(new Rect(10, 70, 100, 20), "Experience: ");
            if (curPlayerLevel < maxPlayerLevel)
            {
                GUI.DrawTexture(new Rect(110, 70, playerExpBarLength, 20), expTexture);
                GUI.Box(new Rect(110, 70, maxBarLength, 20), (int)playerExp + "/" + (int)playerExpNeededForLevelUp);
            }
            else
            {
                GUI.DrawTexture(new Rect(110, 70, maxBarLength, 20), expTexture);
                GUI.Box(new Rect(110, 70, maxBarLength, 20), "MAX LEVEL REACHED");
            }

            //SKILLS GUI

            if (curPlayerLevel >= 1)
            {
                GUI.Box(new Rect(10, 90, 100, 70), "Skills: ");
                GUI.Box(new Rect(10, 160, 100, 20), "Manacost: ");
                GUI.Box(new Rect(10, 180, 100, 20), "Damage: ");
                GUI.Box(new Rect(10, 200, 100, 20), "Heal: ");
                GUI.Box(new Rect(10, 220, 100, 40), "Cooldown: ");

                GUI.Box(new Rect(110, 90, skillIconWidth, 70), backgroundTexture);
                GUI.Box(new Rect(110, 90, skillIconWidth, 70), "M1");
                GUI.DrawTexture(new Rect(110, 110, skillIconWidth, 50), singleShotTexture);
                GUI.DrawTexture(new Rect(110, 110, skillIconWidth, 50), frameTexture);
                GUI.Box(new Rect(110, 160, skillIconWidth, 20), "" + player.GetComponent<GeoShot>().manacost.ToString("F0"));
                GUI.Box(new Rect(110, 180, skillIconWidth, 20), "" + player.GetComponent<GeoShot>().getGeoShotDmg());
                GUI.Box(new Rect(110, 200, skillIconWidth, 20), "");
                GUI.Box(new Rect(110, 220, skillIconWidth, 20), "" + player.GetComponent<GeoShot>().maxCooldown.ToString("F2"));
                GUI.Box(new Rect(110, 240, skillIconWidth, 20), "" + player.GetComponent<GeoShot>().curCooldown.ToString("F2"));

                GUI.Box(new Rect(110 + skillIconWidth, 90, skillIconWidth, 70), backgroundTexture);
                GUI.Box(new Rect(110 + skillIconWidth, 90, skillIconWidth, 70), "M2");
                GUI.DrawTexture(new Rect(110 + skillIconWidth, 110, skillIconWidth, 50), sprayShotTexture);
                GUI.DrawTexture(new Rect(110 + skillIconWidth, 110, skillIconWidth, 50), frameTexture);
                GUI.Box(new Rect(110 + skillIconWidth, 160, skillIconWidth, 20), "" + player.GetComponent<GeoBlast>().manacost);
                GUI.Box(new Rect(110 + skillIconWidth, 180, skillIconWidth, 20), "" + player.GetComponent<GeoBlast>().getGeoBlastDmg());
                GUI.Box(new Rect(110 + skillIconWidth, 200, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + skillIconWidth, 220, skillIconWidth, 20), "" + player.GetComponent<GeoBlast>().maxCooldown.ToString("F2"));
                GUI.Box(new Rect(110 + skillIconWidth, 240, skillIconWidth, 20), "" + player.GetComponent<GeoBlast>().curCooldown.ToString("F2"));

                GUI.Box(new Rect(110 + 2 * skillIconWidth, 90, skillIconWidth, 70), backgroundTexture);
                GUI.Box(new Rect(110 + 2 * skillIconWidth, 90, skillIconWidth, 70), "");
                GUI.DrawTexture(new Rect(110 + 2 * skillIconWidth, 110, skillIconWidth, 50), geoPhysicsTexture);
                GUI.DrawTexture(new Rect(110 + 2 * skillIconWidth, 110, skillIconWidth, 50), frameTexture);
                GUI.Box(new Rect(110 + 2 * skillIconWidth, 160, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 2 * skillIconWidth, 180, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 2 * skillIconWidth, 200, skillIconWidth, 20), "" + player.GetComponent<GeoPhysics>().getGeoPhysicsHealthReg().ToString("F1")+"/s");
                GUI.Box(new Rect(110 + 2 * skillIconWidth, 220, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 2 * skillIconWidth, 240, skillIconWidth, 20), "");
            }

            if (curPlayerLevel >= 2)
            {
                GUI.Box(new Rect(110 + 3 * skillIconWidth, 90, skillIconWidth, 70), backgroundTexture);
                GUI.Box(new Rect(110 + 3 * skillIconWidth, 90, skillIconWidth, 70), "Q");
                GUI.DrawTexture(new Rect(110 + 3 * skillIconWidth, 110, skillIconWidth, 50), healTexture);
                GUI.DrawTexture(new Rect(110 + 3 * skillIconWidth, 110, skillIconWidth, 50), frameTexture);

                GUI.Box(new Rect(110 + 3 * skillIconWidth, 160, skillIconWidth, 20), "" + player.GetComponent<HealSelf>().manacost.ToString("F0"));
                GUI.Box(new Rect(110 + 3 * skillIconWidth, 180, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 3 * skillIconWidth, 200, skillIconWidth, 20), "" + player.GetComponent<HealSelf>().getHealingAmount().ToString("F0"));
                GUI.Box(new Rect(110 + 3 * skillIconWidth, 220, skillIconWidth, 20), "" + player.GetComponent<HealSelf>().maxCooldown.ToString("F0"));
                GUI.Box(new Rect(110 + 3 * skillIconWidth, 240, skillIconWidth, 20), "" + player.GetComponent<HealSelf>().curCooldown.ToString("F1"));


            }

            if (curPlayerLevel >= 4)
            {
                GUI.Box(new Rect(110 + 4 * skillIconWidth, 90, skillIconWidth, 70), backgroundTexture);
                GUI.Box(new Rect(110 + 4 * skillIconWidth, 90, skillIconWidth, 70), "E");
                GUI.DrawTexture(new Rect(110 + 4 * skillIconWidth, 110, skillIconWidth, 50), fireBallTexture);
                GUI.DrawTexture(new Rect(110 + 4 * skillIconWidth, 110, skillIconWidth, 50), frameTexture);

                GUI.Box(new Rect(110 + 4 * skillIconWidth, 160, skillIconWidth, 20), "" + player.GetComponent<Meteor>().manacost.ToString("F0"));
                GUI.Box(new Rect(110 + 4 * skillIconWidth, 180, skillIconWidth, 20), "" + player.GetComponent<Meteor>().getMeteorDamage().ToString("F0"));
                GUI.Box(new Rect(110 + 4 * skillIconWidth, 200, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 4 * skillIconWidth, 220, skillIconWidth, 20), "" + player.GetComponent<Meteor>().maxCooldown.ToString("F1"));
                GUI.Box(new Rect(110 + 4 * skillIconWidth, 240, skillIconWidth, 20), "" + player.GetComponent<Meteor>().curCooldown.ToString("F1"));

            }

            if (curPlayerLevel >= 6)
            {
                GUI.Box(new Rect(110 + 5 * skillIconWidth, 90, skillIconWidth, 70), backgroundTexture);
                GUI.Box(new Rect(110 + 5 * skillIconWidth, 90, skillIconWidth, 70), "R");
                GUI.DrawTexture(new Rect(110 + 5 * skillIconWidth, 110, skillIconWidth, 50), bloodRitualTexture);
                GUI.DrawTexture(new Rect(110 + 5 * skillIconWidth, 110, skillIconWidth, 50), frameTexture);

                GUI.Box(new Rect(110 + 5 * skillIconWidth, 160, skillIconWidth, 20), "" + player.GetComponent<BloodRitual>().manacost.ToString("F0"));
                GUI.Box(new Rect(110 + 5 * skillIconWidth, 180, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 5 * skillIconWidth, 200, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 5 * skillIconWidth, 220, skillIconWidth, 20), "" + player.GetComponent<BloodRitual>().maxCooldown.ToString("F0"));
                GUI.Box(new Rect(110 + 5 * skillIconWidth, 240, skillIconWidth, 20), "" + player.GetComponent<BloodRitual>().curCooldown.ToString("F1"));
            }


            if (curPlayerLevel >= 8)
            {
                GUI.Box(new Rect(110 + 6 * skillIconWidth, 90, skillIconWidth, 70), backgroundTexture);
                GUI.Box(new Rect(110 + 6 * skillIconWidth, 90, skillIconWidth, 70), "F");
                GUI.DrawTexture(new Rect(110 + 6 * skillIconWidth, 110, skillIconWidth, 50), freezeTimeTexture);
                GUI.DrawTexture(new Rect(110 + 6 * skillIconWidth, 110, skillIconWidth, 50), frameTexture);

                GUI.Box(new Rect(110 + 6 * skillIconWidth, 160, skillIconWidth, 20), "" + player.GetComponent<FreezeTime>().manacost.ToString("F0"));
                GUI.Box(new Rect(110 + 6 * skillIconWidth, 180, skillIconWidth, 20), "-");
                GUI.Box(new Rect(110 + 6 * skillIconWidth, 200, skillIconWidth, 20), "-");
                GUI.Box(new Rect(110 + 6 * skillIconWidth, 220, skillIconWidth, 20), "" + player.GetComponent<FreezeTime>().maxCooldown.ToString("F0"));
                GUI.Box(new Rect(110 + 6 * skillIconWidth, 240, skillIconWidth, 20), "" + player.GetComponent<FreezeTime>().curCooldown.ToString("F1"));
            }

            if (curPlayerLevel >= 10)
            {
                GUI.Box(new Rect(110 + 7 * skillIconWidth, 90, skillIconWidth, 70), backgroundTexture);
                GUI.Box(new Rect(110 + 7 * skillIconWidth, 90, skillIconWidth, 70), "");
                GUI.DrawTexture(new Rect(110 + 7 * skillIconWidth, 110, skillIconWidth, 50), geoManiaTexture);
                GUI.DrawTexture(new Rect(110 + 7 * skillIconWidth, 110, skillIconWidth, 50), frameTexture);

                GUI.Box(new Rect(110 + 7 * skillIconWidth, 160, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 7 * skillIconWidth, 180, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 7 * skillIconWidth, 200, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 7 * skillIconWidth, 220, skillIconWidth, 20), "");
                GUI.Box(new Rect(110 + 7 * skillIconWidth, 240, skillIconWidth, 20), "");
            }

            //FADENKREUZ
            float xMin = (Screen.width / 2) - (crosshairImage.width / 2);
            float yMin = (Screen.height / 2) - (crosshairImage.height / 2);
            GUI.DrawTexture(new Rect(xMin, yMin, crosshairImage.width, crosshairImage.height), crosshairImage);

            //BLUTFLECKEN
            if (curPlayerHealth <= (maxPlayerHealth * 0.3f) && curPlayerHealth >= (maxPlayerHealth * 0.2f))
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bloodTexture1);
            }
            else if (curPlayerHealth <= (maxPlayerHealth * 0.2f) && curPlayerHealth >= (maxPlayerHealth * 0.1f))
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bloodTexture2);
            }
            else if (curPlayerHealth <= (maxPlayerHealth * 0.1f) && curPlayerHealth >= 0)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bloodTexture3);
            }
        }
    }

    private void adjustBarLength()
    {
        playerHealthBarLength = (Screen.width / 4) * (curPlayerHealth / (float)maxPlayerHealth);
        playerManaBarLength = (Screen.width / 4) * (curPlayerMana / (float)maxPlayerMana);
        playerExpBarLength = (Screen.width / 4) * (playerExp / (float)playerExpNeededForLevelUp);
    }

}
