using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FreezeTime : SkillBasic
{

    public Texture2D freezeTexture;
    private float freezeTextureTimer;
    private float freezeTextureTimerCooldown;
    private bool showFreezeTexture;

    public List<GameObject> enemiesToFreeze;
    public float duration;



    // Use this for initialization
    void Start()
    {
        curCooldown = 0;
        freezeTextureTimer = 1;
        showFreezeTexture = false;

        enemiesToFreeze = new List<GameObject>();

        GameObject[] go = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in go)
        {
            AddTarget(enemy);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Player == null) return;
        maxCooldown = 100 / m_Player.getCurLevel();
        freezeTextureTimerCooldown = 1.5f;
        duration = m_Player.getCurLevel() / 4f;
        manacost = m_Player.getCurLevel() * 30;
        updateCoolDown();


        if (m_Player.skillAvailable(8))
        {
            if (GameInput.SkillFreezeTimeUp && requiredMana() && CanUseSkills())
            {
                if (curCooldown == 0)
                {
                    showFreezeTexture = true;
                    freezeTime();
                    curCooldown = maxCooldown;
                    freezeTextureTimer = freezeTextureTimerCooldown;
                }

            }

        }
        if (showFreezeTexture) calculateFreezeTextureCooldown();
    }

    public void freezeTime()
    {

        m_Player.curMana -= manacost;

        for (int i = 0; i < enemiesToFreeze.Count; i++)
        {
            enemiesToFreeze[i].GetComponent<EnemyAI>().freeze(duration);
        }
            
    }

    public void AddTarget(GameObject targetsToFreeze)
    {
        enemiesToFreeze.Add(targetsToFreeze);
    }

    void OnGUI()
    {
        if (showFreezeTexture && m_GameOver != null && !m_GameOver.playerDied && !m_GameOver.gameTimeIsOver)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), freezeTexture);
        }
    }

    protected void calculateFreezeTextureCooldown()
    {
        if (freezeTextureTimer > 0)
        {
            freezeTextureTimer -= Time.deltaTime;
        }
        if (freezeTextureTimer < 0)
        {
            freezeTextureTimer = 0;
            showFreezeTexture = false;
        }
        if (freezeTextureTimer == 0)
        {
            showFreezeTexture = false;
        }

    }
}