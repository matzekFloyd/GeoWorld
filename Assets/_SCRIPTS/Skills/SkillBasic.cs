using UnityEngine;
using System.Collections;

public class SkillBasic : MonoBehaviour {

    protected GameObject player;

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
        curCooldown = 0;
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
        return player.GetComponent<PlayerCharacter>().curMana >= manacost;
    }

    protected bool geoManiaActivated()
    {
        return player.GetComponent<PlayerCharacter>().skillAvailable(10);
    }
}
