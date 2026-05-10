using UnityEngine;
using System.Collections;

public class SkillBasic : MonoBehaviour {

    protected GameObject player;
    protected PlayerCharacter m_Player;
    protected GameOver m_GameOver;

    public float manacost;
    public float curCooldown;
    public float maxCooldown;

    // Use this for initialization
    void Start () {
        manacost = 0;    
    }

    // Update is called once per frame
    void Update () {
    }

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player1");
        if (player != null)
        {
            m_Player = player.GetComponent<PlayerCharacter>();
            m_GameOver = player.GetComponent<GameOver>();
        }
        curCooldown = 0;
    }

    protected bool CanUseSkills()
    {
        return m_GameOver != null && !m_GameOver.playerDied && !m_GameOver.gameTimeIsOver;
    }

    protected void updateCoolDown()
    {
        if (curCooldown > 0)
        {
            curCooldown -= Time.deltaTime;
        }
        if (curCooldown < 0)
        {
            curCooldown = 0;
        }
    }

    protected bool requiredMana()
    {
        return m_Player != null && m_Player.curMana >= manacost;
    }

    protected bool geoManiaActivated()
    {
        return m_Player != null && m_Player.skillAvailable(10);
    }
}
