using UnityEngine;
using System.Collections;

public class HealSelf : SkillBasic {

    public Texture2D healTexture;
    private float healTimer;
    private float healTimerCooldown;
    private bool showHealTexture;

    private float healingAmount;
    public AudioClip healSound;

	// Use this for initialization
	void Start () {
        curCooldown = 0;
        healTimer = 1;
        showHealTexture = false;
    }

    // Update is called once per frame
    void Update () {

        if (m_Player == null) return;
        maxCooldown = 4;
        healTimerCooldown = m_Player.getCurLevel() / 2f;
        manacost = m_Player.getCurLevel() * 25f;
        healingAmount = m_Player.getCurLevel() * 150f;
        updateCoolDown();

        if (m_Player.skillAvailable(2))
        {
            if (GameInput.SkillHealUp && requiredMana() && CanUseSkills())
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
            m_Player.curMana -= manacost;

            GameplaySfx.Instance?.PlayHealCast(healSound);
            m_Player.changeCurrentHealth(healingAmount);
     }

    public float getCurrentMaxHealth()
    {
        return m_Player.getMaxHealth();
    }

    public float getHealingAmount()
    {
        return healingAmount;
    }

    void LateUpdate()
    {
        var hud = GameplayHudView.Instance;
        if (hud == null)
            return;
        bool on = showHealTexture && CanUseSkills();
        hud.ConfigureHealFlash(on, healTexture);
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