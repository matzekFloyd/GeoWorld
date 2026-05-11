using UnityEngine;
using System.Collections;

public class BaseCharacter : MonoBehaviour
{
    protected int curLevel;
    protected int maxLevel;
    
    public float curHealth;
    public float maxHealth;
    public float baseHealthRegeneration;
    protected float healthBarLength;
    
    // Use this for initialization
    void Start()
    {
        curLevel = 0;
        maxLevel = 10;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int getCurLevel()
    {
        return curLevel;
    }

    public int getMaxLevel()
    {
        return maxLevel;
    }

    public float getCurHealth()
    {
        return curHealth;
    }

    public float getMaxHealth()
    {
        return maxHealth;
    }

    public bool iAmDead()
    {
        if (curHealth <= 0) return true;
        return false;
    }

    protected void ApplyHealthChange(float change)
    {
        curHealth += change;

        if (curHealth > maxHealth)
            curHealth = maxHealth;

        if (maxHealth < 1)
            maxHealth = 1;

        healthBarLength = (Screen.width / 4) * (curHealth / (float)maxHealth);
    }

    public virtual void changeCurrentHealth(float change)
    {
        ApplyHealthChange(change);
    }
}
