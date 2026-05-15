using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

public class GeoPhysics : SkillBasic {

    // Shared move curve: approaches caps smoothly so high levels still gain (no hard cap cliff).
    const float MoveScaleLevelCeiling = 18f;

    const float WalkSpeedAtLevel1 = 11f;
    const float WalkSpeedMax = 36f;

    const float JumpSpeedAtLevel1 = 11f;
    const float JumpSpeedMax = 52f;

    const float GravityAtLowLevel = 2f;
    const float GravityMultiplierMin = 0.78f;
    const float GravityFloorAtLevel = 20f;

    int curPlayerLevel;
    int _lastAppliedLevel = -1;

    FirstPersonController _firstPerson;
    float _baselineWalk = 1f;
    float _baselineRun = 1f;

    void Start () {
        if (player != null)
            _firstPerson = player.GetComponent<FirstPersonController>();

        if (_firstPerson != null)
        {
            _baselineWalk = Mathf.Max(0.01f, _firstPerson.m_WalkSpeed);
            _baselineRun = Mathf.Max(0.01f, _firstPerson.m_RunSpeed);
        }

        GeoPhysicsPlayerVfx.EnsureOn(gameObject);
    }

    void Update () {
        if (m_Player == null || _firstPerson == null)
            return;

        curPlayerLevel = m_Player.getCurLevel();

        if (m_Player.skillAvailable(GameBalanceHelper.SkillUnlockGeoPhysics) && curPlayerLevel != _lastAppliedLevel)
            enhanceCharacterStatistics();
    }

    public void enhanceCharacterStatistics()
    {
        if (_firstPerson == null || m_Player == null)
            return;

        float walk = calculateMovementSpeedBuff(curPlayerLevel);
        _firstPerson.m_WalkSpeed = walk;
        _firstPerson.m_RunSpeed = _baselineRun * (walk / _baselineWalk);
        _firstPerson.m_JumpSpeed = calculateJumpSpeedBuff(curPlayerLevel);
        _firstPerson.m_GravityMultiplier = calculateGravityMultiplier(curPlayerLevel);
        _lastAppliedLevel = curPlayerLevel;
    }

    /// <summary>0 at level 0, → 1 as level increases (diminishing returns, never fully flat before cap).</summary>
    static float MoveProgress(int playerLevel)
    {
        int lv = Mathf.Max(1, playerLevel);
        return 1f - Mathf.Exp(-lv / MoveScaleLevelCeiling);
    }

    static float LerpWithMoveProgress(float atLevel1, float max, int playerLevel)
    {
        return atLevel1 + (max - atLevel1) * MoveProgress(playerLevel);
    }

    public float calculateMovementSpeedBuff(int playerLevel)
    {
        return LerpWithMoveProgress(WalkSpeedAtLevel1, WalkSpeedMax, playerLevel);
    }

    public float calculateJumpSpeedBuff(int playerLevel)
    {
        return LerpWithMoveProgress(JumpSpeedAtLevel1, JumpSpeedMax, playerLevel);
    }

    public float calculateGravityMultiplier(int playerLevel)
    {
        int lv = Mathf.Max(1, playerLevel);
        float t = Mathf.Clamp01(lv / GravityFloorAtLevel);
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(GravityAtLowLevel, GravityMultiplierMin, t);
    }

    public float getGeoPhysicsHealthReg()
    {
        return m_Player != null ? m_Player.GetPassiveHealthRegenPerSecond() : 0f;
    }
}
