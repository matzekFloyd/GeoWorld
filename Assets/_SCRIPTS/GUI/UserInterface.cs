using UnityEngine;

/// <summary>
/// Drives the uGUI <see cref="GameplayHudView"/> with player/skill data. Layout is created under a child
/// <c>GameplayHUD</c> Canvas at runtime; assign a <see cref="GameplayHudView"/> on this object only if you
/// customize the hierarchy in the Editor.
/// </summary>
public class UserInterface : MonoBehaviour
{
    const int SkillCount = 8;
    static readonly int[] SkillMinLevels = { 1, 1, 1, 2, 4, 6, 8, 10 };

    private GameObject player;

    private PlayerCharacter m_Player;
    private GameOver m_GameOver;
    private GeoShot m_GeoShot;
    private GeoBlast m_GeoBlast;
    private GeoPhysics m_GeoPhysics;
    private HealSelf m_HealSelf;
    private Meteor m_Meteor;
    private BloodRitual m_BloodRitual;
    private FreezeTime m_FreezeTime;

    private float curPlayerHealth;
    private float maxPlayerHealth;
    private float curPlayerMana;
    private float maxPlayerMana;
    private float curPlayerLevel;
    private float maxPlayerLevel;
    private float playerExp;
    private float playerExpNeededForLevelUp;

    GameplayHudView _hud;

    readonly string[] _keyLabels = new string[SkillCount];
    readonly Sprite[] _skillIcons = new Sprite[SkillCount];
    readonly string[] _skillMana = new string[SkillCount];
    readonly string[] _skillDmg = new string[SkillCount];
    readonly string[] _skillHeal = new string[SkillCount];
    readonly string[] _skillCdMax = new string[SkillCount];
    readonly string[] _skillCdCur = new string[SkillCount];

    Sprite _frameSprite;

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

    void Awake()
    {
        _hud = GetComponent<GameplayHudView>();
        if (_hud == null)
            _hud = gameObject.AddComponent<GameplayHudView>();
        if (GetComponent<CombatFeedback>() == null)
            gameObject.AddComponent<CombatFeedback>();
        if (GetComponent<FloatingDamageNumberPool>() == null)
            gameObject.AddComponent<FloatingDamageNumberPool>();
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player1");
        if (player != null)
        {
            m_Player = player.GetComponent<PlayerCharacter>();
            m_GeoShot = player.GetComponent<GeoShot>();
            m_GeoBlast = player.GetComponent<GeoBlast>();
            m_GeoPhysics = player.GetComponent<GeoPhysics>();
            m_HealSelf = player.GetComponent<HealSelf>();
            m_Meteor = player.GetComponent<Meteor>();
            m_BloodRitual = player.GetComponent<BloodRitual>();
            m_FreezeTime = player.GetComponent<FreezeTime>();
        }
        m_GameOver = GetComponent<GameOver>();

        _hud.EnsureBuilt(this);
        var dmgPool = GetComponent<FloatingDamageNumberPool>();
        if (dmgPool != null && _hud.DamageNumbersHost != null)
            dmgPool.BindAndPrewarm(_hud.DamageNumbersHost);
        if (crosshairImage != null)
            _hud.ApplyCrosshair(GameplayHudView.SpriteFromTexture(crosshairImage));
        _hud.ApplyBarSprites(
            GameplayHudView.SpriteFromTexture(healthBarTexture),
            GameplayHudView.SpriteFromTexture(manaBarTexture),
            GameplayHudView.SpriteFromTexture(expTexture));
        _frameSprite = GameplayHudView.SpriteFromTexture(frameTexture);

        _keyLabels[0] = "M1";
        _keyLabels[1] = "M2";
        _keyLabels[2] = "";
        _keyLabels[3] = "Q";
        _keyLabels[4] = "E";
        _keyLabels[5] = "R";
        _keyLabels[6] = "F";
        _keyLabels[7] = "";
        CacheStaticSkillSprites();
    }

    void CacheStaticSkillSprites()
    {
        _skillIcons[0] = GameplayHudView.SpriteFromTexture(singleShotTexture);
        _skillIcons[1] = GameplayHudView.SpriteFromTexture(sprayShotTexture);
        _skillIcons[2] = GameplayHudView.SpriteFromTexture(geoPhysicsTexture);
        _skillIcons[3] = GameplayHudView.SpriteFromTexture(healTexture);
        _skillIcons[4] = GameplayHudView.SpriteFromTexture(fireBallTexture);
        _skillIcons[5] = GameplayHudView.SpriteFromTexture(bloodRitualTexture);
        _skillIcons[6] = GameplayHudView.SpriteFromTexture(freezeTimeTexture);
        _skillIcons[7] = GameplayHudView.SpriteFromTexture(geoManiaTexture);
    }

    void Update()
    {
        if (m_Player == null || _hud == null)
            return;

        curPlayerLevel = m_Player.getCurLevel();
        maxPlayerLevel = m_Player.getMaxLevel();
        curPlayerHealth = m_Player.getCurHealth();
        maxPlayerHealth = m_Player.getMaxHealth();
        curPlayerMana = m_Player.getCurMana();
        maxPlayerMana = m_Player.getMaxMana();
        playerExp = m_Player.getCurExp();
        playerExpNeededForLevelUp = m_Player.getExpNeededForLevelUp();

        BuildSkillStrings();

        bool show = m_GameOver != null && !m_GameOver.playerDied && !m_GameOver.gameTimeIsOver;
        _hud.RefreshGameplay(
            show,
            Mathf.RoundToInt(curPlayerLevel),
            Mathf.RoundToInt(maxPlayerLevel),
            curPlayerHealth,
            maxPlayerHealth,
            curPlayerMana,
            maxPlayerMana,
            playerExp,
            playerExpNeededForLevelUp,
            bloodTexture1,
            bloodTexture2,
            bloodTexture3,
            _keyLabels,
            _skillIcons,
            _frameSprite,
            _skillMana,
            _skillDmg,
            _skillHeal,
            _skillCdMax,
            _skillCdCur);
    }

    void BuildSkillStrings()
    {
        int lv = Mathf.RoundToInt(curPlayerLevel);
        for (int i = 0; i < SkillCount; i++)
        {
            if (lv < SkillMinLevels[i])
            {
                _skillMana[i] = "";
                _skillDmg[i] = "";
                _skillHeal[i] = "";
                _skillCdMax[i] = "";
                _skillCdCur[i] = "";
                continue;
            }

            _skillMana[i] = "";
            _skillDmg[i] = "";
            _skillHeal[i] = "";
            _skillCdMax[i] = "";
            _skillCdCur[i] = "";

            if (i == 0 && m_GeoShot != null)
            {
                _skillMana[i] = m_GeoShot.manacost.ToString("F0");
                _skillDmg[i] = m_GeoShot.getGeoShotDmg().ToString();
                _skillHeal[i] = "";
                _skillCdMax[i] = m_GeoShot.maxCooldown.ToString("F2");
                _skillCdCur[i] = m_GeoShot.curCooldown.ToString("F2");
            }
            else if (i == 1 && m_GeoBlast != null)
            {
                _skillMana[i] = m_GeoBlast.manacost.ToString("F0");
                _skillDmg[i] = m_GeoBlast.getGeoBlastDmg().ToString();
                _skillHeal[i] = "";
                _skillCdMax[i] = m_GeoBlast.maxCooldown.ToString("F2");
                _skillCdCur[i] = m_GeoBlast.curCooldown.ToString("F2");
            }
            else if (i == 2 && m_GeoPhysics != null)
            {
                _skillMana[i] = "";
                _skillDmg[i] = "";
                _skillHeal[i] = m_GeoPhysics.getGeoPhysicsHealthReg().ToString("F1") + "/s";
                _skillCdMax[i] = "";
                _skillCdCur[i] = "";
            }
            else if (i == 3 && m_HealSelf != null)
            {
                _skillMana[i] = m_HealSelf.manacost.ToString("F0");
                _skillDmg[i] = "";
                _skillHeal[i] = m_HealSelf.getHealingAmount().ToString("F0");
                _skillCdMax[i] = m_HealSelf.maxCooldown.ToString("F0");
                _skillCdCur[i] = m_HealSelf.curCooldown.ToString("F1");
            }
            else if (i == 4 && m_Meteor != null)
            {
                _skillMana[i] = m_Meteor.manacost.ToString("F0");
                _skillDmg[i] = m_Meteor.getMeteorDamage().ToString("F0");
                _skillHeal[i] = "";
                _skillCdMax[i] = m_Meteor.maxCooldown.ToString("F1");
                _skillCdCur[i] = m_Meteor.curCooldown.ToString("F1");
            }
            else if (i == 5 && m_BloodRitual != null)
            {
                _skillMana[i] = m_BloodRitual.manacost.ToString("F0");
                _skillDmg[i] = "";
                _skillHeal[i] = "";
                _skillCdMax[i] = m_BloodRitual.maxCooldown.ToString("F0");
                _skillCdCur[i] = m_BloodRitual.curCooldown.ToString("F1");
            }
            else if (i == 6 && m_FreezeTime != null)
            {
                _skillMana[i] = m_FreezeTime.manacost.ToString("F0");
                _skillDmg[i] = "-";
                _skillHeal[i] = "-";
                _skillCdMax[i] = m_FreezeTime.maxCooldown.ToString("F0");
                _skillCdCur[i] = m_FreezeTime.curCooldown.ToString("F1");
            }
            else if (i == 7)
            {
                _skillMana[i] = "";
                _skillDmg[i] = "";
                _skillHeal[i] = "";
                _skillCdMax[i] = "";
                _skillCdCur[i] = "";
            }
        }
    }
}
