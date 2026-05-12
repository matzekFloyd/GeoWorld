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
        showBloodStain = false;
    }

    // Update is called once per frame
    void Update () {

        if (m_Player == null) return;
        maxCooldown = 50 / m_Player.getCurLevel();
        bloodTimerCooldown = 1.5f;
        manacost = m_Player.getCurLevel() * 5;
        updateCoolDown();


        if (m_Player.skillAvailable(6))
        {
            if (GameInput.SkillBloodRitualUp && requiredMana() && CanUseSkills())
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

        if (m_Player.curMana >= m_Player.maxMana)
        {
            healthToManaValue = 0;
        } else
        {
            healthToManaValue = m_Player.curHealth / 3.33f;

            m_Player.curHealth -= healthToManaValue;
            m_Player.curMana += healthToManaValue;
            if (healthToManaValue > 0.0001f)
                GameplaySfx.Instance?.PlayBloodRitualCast();
        }
     }

    void LateUpdate()
    {
        var hud = GameplayHudView.Instance;
        if (hud == null)
            return;
        bool on = showBloodStain && m_GameOver != null && !m_GameOver.playerDied && !m_GameOver.gameTimeIsOver;
        hud.ConfigureBloodRitualFx(on, bloodRitualTexture1, bloodRitualTexture2);
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
