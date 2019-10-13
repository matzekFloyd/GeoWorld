using UnityEngine;
using System.Collections;

public class HealSelf : SkillBasic {

    public Texture2D healTexture;
    private float healTimer;
    private float healTimerCooldown;
    private bool showHealTexture;

    private float healingAmount;
    public AudioClip healSound;
    AudioSource a;

	// Use this for initialization
	void Start () {
        curCooldown = 0;
        healTimer = 1;
        player = GameObject.FindGameObjectWithTag("Player1");
        a = GetComponent<AudioSource>();
        showHealTexture = false;
    }

    // Update is called once per frame
    void Update () {

        maxCooldown = 4;
        healTimerCooldown = player.GetComponent<PlayerCharacter>().getCurLevel() / 2f;
        manacost = player.GetComponent<PlayerCharacter>().getCurLevel() * 25f;
        healingAmount = player.GetComponent<PlayerCharacter>().getCurLevel() * 150f;
        updateCoolDown();

        if (player.GetComponent<PlayerCharacter>().skillAvailable(2))
        {
            if (Input.GetKeyUp(KeyCode.Q) && requiredMana() && this.gameObject.GetComponent<GameOver>().playerDied == false && this.gameObject.GetComponent<GameOver>().gameTimeIsOver == false)
            {
                if (curCooldown == 0)
                {
                    showHealTexture = true;
                    heal();
                    curCooldown = maxCooldown;
                    healTimer = healTimerCooldown;

                }

            }
        }
        if (showHealTexture) calculateHealTextureCooldown();

    }

    public void heal()
    {
            player.GetComponent<PlayerCharacter>().curMana -= manacost;

            a.PlayOneShot(healSound, 1F);
            player.GetComponent<PlayerCharacter>().changeCurrentHealth(healingAmount);
     }

    public float getCurrentMaxHealth()
    {
        float mh = player.GetComponent<PlayerCharacter>().getMaxHealth();
        return mh;
    }

    public float getHealingAmount()
    {
        return healingAmount;
    }

    void OnGUI()
    {
        if (showHealTexture && this.gameObject.GetComponent<GameOver>().playerDied == false && this.gameObject.GetComponent<GameOver>().gameTimeIsOver == false)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), healTexture);
        }
    }

    protected void calculateHealTextureCooldown()
    {
        if (healTimer > 0)
        {
            healTimer -= Time.deltaTime;
        }
        if (healTimer < 0)
        {
            healTimer = 0;
            showHealTexture = false;
        }
        if (healTimer == 0)
        {
            showHealTexture = false;
        }

    }

}