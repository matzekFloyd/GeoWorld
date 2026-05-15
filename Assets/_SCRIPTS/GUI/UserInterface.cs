using System.Globalization;
using UnityEngine;

/// <summary>
/// Drives the uGUI <see cref="GameplayHudView"/> with player/skill data. Layout is created under a child
/// <c>GameplayHUD</c> Canvas at runtime; assign a <see cref="GameplayHudView"/> on this object only if you
/// customize the hierarchy in the Editor.
/// </summary>
/// <remarks>
/// <b>HUD number conventions</b> (see <see cref="BuildSkillStrings"/>):
/// <list type="bullet">
/// <item><description>Mana cost, damage, heal amounts: whole numbers (<c>F0</c>).</description></item>
/// <item><description>Cooldown max: invariant <c>0.#</c>; empty when max is 0 (no CD row).</description></item>
/// <item><description>Cooldown remaining (for overlay): same <c>0.#</c> while on cooldown; empty when <c>curCooldown</c> ≤ <c>0.02</c> (same cutoff as <see cref="GameplayHudView"/>), so no misleading <c>0.00</c> when usable.</description></item>
/// <item><description>Mid-round Escape (release) opens a minimal pause overlay via <see cref="GameplayPause"/>; end-of-run quit copy stays on <see cref="GameOver"/>.</description></item>
/// <item><description>Recent damage taken (rolling window) and Heal-skill amounts appear as bold text left/right of the crosshair via <see cref="GameplayHudView"/>.</description></item>
/// </list>
/// </remarks>
public class UserInterface : MonoBehaviour
{
    const int SkillCount = GameBalanceHelper.SkillSlotCount;
    /// <summary>Shown in damage/heal HUD sub-columns when a skill has no meaningful value there (not a minus sign).</summary>
    const string HudNotApplicableStat = "N/A";

    private GameObject player;

    private PlayerCharacter m_Player;
    private GeoShot m_GeoShot;
    private GeoBlast m_GeoBlast;
    private GeoPhysics m_GeoPhysics;
    private HealSelf m_HealSelf;
    private Meteor m_Meteor;
    private BloodRitual m_BloodRitual;
    private FreezeTime m_FreezeTime;
    private GeoMania m_GeoMania;

    private float curPlayerHealth;
    private float maxPlayerHealth;
    private float curPlayerMana;
    private float maxPlayerMana;
    private float curPlayerLevel;
    private float maxPlayerLevel;
    private float playerExp;
    private float playerExpNeededForLevelUp;

    GameplayHudView _hud;
    MinimapRadar _minimap;
    GameplayPause _gameplayPause;

    readonly string[] _keyLabels = new string[SkillCount];
    readonly Sprite[] _skillIcons = new Sprite[SkillCount];
    readonly string[] _skillMana = new string[SkillCount];
    readonly string[] _skillDmg = new string[SkillCount];
    readonly string[] _skillHeal = new string[SkillCount];
    readonly string[] _skillCdMax = new string[SkillCount];
    readonly string[] _skillCdCur = new string[SkillCount];
    readonly bool[] _skillInsufficientMana = new bool[SkillCount];

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
        _minimap = GetComponent<MinimapRadar>();
        if (GetComponent<CombatFeedback>() == null)
            gameObject.AddComponent<CombatFeedback>();
        if (GetComponent<FloatingDamageNumberPool>() == null)
            gameObject.AddComponent<FloatingDamageNumberPool>();
        if (GetComponent<MinimapRadar>() == null)
            gameObject.AddComponent<MinimapRadar>();
        if (GetComponent<GameplaySfx>() == null)
            gameObject.AddComponent<GameplaySfx>();
        _gameplayPause = GetComponent<GameplayPause>();
        if (_gameplayPause == null)
            _gameplayPause = gameObject.AddComponent<GameplayPause>();
    }

    void Start()
    {
        GameSession.EnsureForScene();
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
            m_GeoMania = player.GetComponent<GeoMania>();
        }
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

        GameInput.FillSkillStripKeyLabels(_keyLabels);
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
        if (_gameplayPause != null)
        {
            _gameplayPause.SyncIfRunInactive();
            _gameplayPause.TryToggleRunPause();
        }

        if (m_Player == null || _hud == null)
            return;

        GameInput.FillSkillStripKeyLabels(_keyLabels);

        curPlayerLevel = m_Player.getCurLevel();
        maxPlayerLevel = m_Player.getMaxLevel();
        curPlayerHealth = m_Player.getCurHealth();
        maxPlayerHealth = m_Player.getMaxHealth();
        curPlayerMana = m_Player.getCurMana();
        maxPlayerMana = m_Player.getMaxMana();
        playerExp = m_Player.getCurExp();
        playerExpNeededForLevelUp = m_Player.getExpNeededForLevelUp();

        BuildSkillStrings();
        FillSkillInsufficientManaFlags();

        var session = GameSession.Instance;
        bool show = session != null && session.IsRunActive;
        if (_minimap != null)
            _minimap.SetGameplayHudVisible(show);
        if (show)
        {
            if (m_GeoMania == null && player != null)
                m_GeoMania = player.GetComponent<GeoMania>();
            bool geoMania = m_Player.skillAvailable(GameBalanceHelper.SkillUnlockGeoMania);
            var maniaColor = m_GeoMania != null
                ? m_GeoMania.ManiaCrosshairColor
                : new Color(0.65f, 0.12f, 0.12f, 1f);
            _hud.SetGeoManiaActive(geoMania, maniaColor);
        }
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
            _skillCdCur,
            _skillInsufficientMana);
    }

    void FillSkillInsufficientManaFlags()
    {
        for (int i = 0; i < SkillCount; i++)
            _skillInsufficientMana[i] = false;

        if (m_Player == null)
            return;

        int lv = Mathf.RoundToInt(curPlayerLevel);
        float curM = m_Player.getCurMana();

        for (int i = 0; i < SkillCount; i++)
        {
            if (lv < GameBalanceHelper.GetSkillUnlockLevel(i))
                continue;

            switch (i)
            {
                case 0:
                    if (m_GeoShot != null)
                        _skillInsufficientMana[i] = curM < m_GeoShot.manacost;
                    break;
                case 1:
                    if (m_GeoBlast != null)
                        _skillInsufficientMana[i] = curM < m_GeoBlast.manacost;
                    break;
                case 2:
                    break;
                case 3:
                    if (m_HealSelf != null)
                        _skillInsufficientMana[i] = curM < m_HealSelf.manacost;
                    break;
                case 4:
                    if (m_Meteor != null)
                        _skillInsufficientMana[i] = curM < m_Meteor.manacost;
                    break;
                case 5:
                    if (m_BloodRitual != null)
                        _skillInsufficientMana[i] = curM < m_BloodRitual.manacost;
                    break;
                case 6:
                    if (m_FreezeTime != null)
                        _skillInsufficientMana[i] = curM < m_FreezeTime.manacost;
                    break;
                case 7:
                    break;
            }
        }
    }

    void BuildSkillStrings()
    {
        int lv = Mathf.RoundToInt(curPlayerLevel);
        for (int i = 0; i < SkillCount; i++)
        {
            if (lv < GameBalanceHelper.GetSkillUnlockLevel(i))
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
                _skillMana[i] = FormatHudWhole(m_GeoShot.manacost);
                _skillDmg[i] = FormatHudWhole(m_GeoShot.getGeoShotDmg());
                _skillHeal[i] = "";
                _skillCdMax[i] = FormatHudCooldownSeconds(m_GeoShot.maxCooldown);
                _skillCdCur[i] = FormatHudCooldownCurrent(m_GeoShot.curCooldown);
            }
            else if (i == 1 && m_GeoBlast != null)
            {
                _skillMana[i] = FormatHudWhole(m_GeoBlast.manacost);
                _skillDmg[i] = FormatHudWhole(m_GeoBlast.getGeoBlastDmg());
                _skillHeal[i] = "";
                _skillCdMax[i] = FormatHudCooldownSeconds(m_GeoBlast.maxCooldown);
                _skillCdCur[i] = FormatHudCooldownCurrent(m_GeoBlast.curCooldown);
            }
            else if (i == 2 && m_GeoPhysics != null)
            {
                _skillMana[i] = "";
                _skillDmg[i] = "";
                _skillHeal[i] = m_GeoPhysics.getGeoPhysicsHealthReg().ToString("F1", CultureInfo.InvariantCulture) + "/s";
                _skillCdMax[i] = "";
                _skillCdCur[i] = "";
            }
            else if (i == 3 && m_HealSelf != null)
            {
                _skillMana[i] = FormatHudWhole(m_HealSelf.manacost);
                _skillDmg[i] = "";
                _skillHeal[i] = FormatHudWhole(m_HealSelf.getHealingAmount());
                _skillCdMax[i] = FormatHudCooldownSeconds(m_HealSelf.maxCooldown);
                _skillCdCur[i] = FormatHudCooldownCurrent(m_HealSelf.curCooldown);
            }
            else if (i == 4 && m_Meteor != null)
            {
                _skillMana[i] = FormatHudWhole(m_Meteor.manacost);
                _skillDmg[i] = FormatHudWhole(m_Meteor.getMeteorDamage());
                _skillHeal[i] = "";
                _skillCdMax[i] = FormatHudCooldownSeconds(m_Meteor.maxCooldown);
                _skillCdCur[i] = FormatHudCooldownCurrent(m_Meteor.curCooldown);
            }
            else if (i == 5 && m_BloodRitual != null)
            {
                _skillMana[i] = FormatHudWhole(m_BloodRitual.manacost);
                _skillDmg[i] = "";
                _skillHeal[i] = "";
                _skillCdMax[i] = FormatHudCooldownSeconds(m_BloodRitual.maxCooldown);
                _skillCdCur[i] = FormatHudCooldownCurrent(m_BloodRitual.curCooldown);
            }
            else if (i == 6 && m_FreezeTime != null)
            {
                _skillMana[i] = FormatHudWhole(m_FreezeTime.manacost);
                _skillDmg[i] = HudNotApplicableStat;
                _skillHeal[i] = HudNotApplicableStat;
                _skillCdMax[i] = FormatHudCooldownSeconds(m_FreezeTime.maxCooldown);
                _skillCdCur[i] = FormatHudCooldownCurrent(m_FreezeTime.curCooldown);
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

    /// <summary>Whole numbers for mana, damage, heal amounts (see class remarks).</summary>
    static string FormatHudWhole(float value)
    {
        return value.ToString("F0", CultureInfo.InvariantCulture);
    }

    /// <summary>Cooldown seconds (max); invariant <c>0.#</c>.</summary>
    static string FormatHudCooldownSeconds(float seconds)
    {
        if (seconds <= 0f)
            return "";
        return seconds.ToString("0.#", CultureInfo.InvariantCulture);
    }

    /// <summary>Remaining cooldown for overlay parsing; empty when ready (no <c>0.00</c> noise). Uses the same ≤0.02s cutoff as cooldown visuals.</summary>
    static string FormatHudCooldownCurrent(float curCooldown)
    {
        if (curCooldown <= 0.02f)
            return "";
        return curCooldown.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
