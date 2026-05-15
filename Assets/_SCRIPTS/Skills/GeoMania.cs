using System.Collections;
using UnityEngine;

public class GeoMania : SkillBasic
{
    [Tooltip("True while skill slot 10 (Geo Mania) is available — same condition as SkillBasic.geoManiaActivated().")]
    public bool maniaActive;

    [Header("Visible manic feedback")]
    [SerializeField] Color maniaCrosshairColor = new Color(0.65f, 0.12f, 0.12f, 1f);
    [SerializeField] Color maniaCrosshairFlashColor = new Color(0.78f, 0.14f, 0.12f, 1f);
    [SerializeField] float maniaCelebrateDurationRealtime = 0.22f;
    [SerializeField, Range(0f, 1f)] float maniaCelebrateStrength = 1f;
    [Tooltip("Extra delay after boss telegraph ends before the crosshair flash (level 10).")]
    [SerializeField] float maniaCelebrateDelayAfterBossTelegraph = 0.12f;

    public Color ManiaCrosshairColor => maniaCrosshairColor;

    Coroutine _celebrateRoutine;
    bool _wasActive;

    void OnEnable()
    {
        maniaActive = geoManiaActivated();
        _wasActive = maniaActive;
        PushHudState();
    }

    void Update()
    {
        bool active = geoManiaActivated();
        if (active && !_wasActive)
        {
            maniaActive = true;
            PushHudState();
            QueueCelebratePulse();
        }
        else if (!active && _wasActive)
        {
            maniaActive = false;
            PushHudState();
        }
        else
        {
            maniaActive = active;
        }

        _wasActive = maniaActive;
    }

    void OnDisable()
    {
        if (_celebrateRoutine != null)
        {
            StopCoroutine(_celebrateRoutine);
            _celebrateRoutine = null;
        }
        GameplayHudView.Instance?.SetGeoManiaActive(false, maniaCrosshairColor);
    }

    void PushHudState()
    {
        GameplayHudView.Instance?.SetGeoManiaActive(maniaActive, maniaCrosshairColor);
    }

    void QueueCelebratePulse()
    {
        if (_celebrateRoutine != null)
            StopCoroutine(_celebrateRoutine);
        _celebrateRoutine = StartCoroutine(CelebrateWhenReadyRoutine());
    }

    IEnumerator CelebrateWhenReadyRoutine()
    {
        var hud = GameplayHudView.Instance;
        while (hud == null)
        {
            yield return null;
            hud = GameplayHudView.Instance;
        }

        while (hud.IsBossTelegraphActive)
            yield return null;

        if (maniaCelebrateDelayAfterBossTelegraph > 0f)
            yield return new WaitForSecondsRealtime(maniaCelebrateDelayAfterBossTelegraph);

        if (!maniaActive)
        {
            _celebrateRoutine = null;
            yield break;
        }

        hud.PlayGeoManiaActivatedFeedback(
            maniaCrosshairFlashColor,
            maniaCelebrateDurationRealtime,
            maniaCelebrateStrength);
        _celebrateRoutine = null;
    }
}
