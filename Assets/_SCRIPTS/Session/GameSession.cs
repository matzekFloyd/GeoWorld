using UnityEngine;

/// <summary>
/// Resolves whether the run is still active and exposes the active <see cref="PlayerCharacter"/>.
/// End-of-run flags are driven by <see cref="GameOver"/> via <see cref="SyncRunState"/>.
/// </summary>
/// <remarks>
/// <para><b>Scene wiring:</b> You do not need to place this manually. <see cref="GameOver"/> calls
/// <see cref="EnsureForScene"/> from <c>Start</c>, which adds <c>GameSession</c> to the <c>Player1</c>
/// object if missing. Systems such as <see cref="BackgroundMusic"/> and <see cref="UserInterface"/>
/// read <see cref="Instance"/> only.</para>
/// <para><b>Ownership:</b> Round flow and score counters remain on <see cref="GameOver"/>; this type
/// is the façade for unrelated consumers (audio, HUD, skills) so they do not subclass <see cref="GameOver"/>.</para>
/// </remarks>
[DisallowMultipleComponent]
public sealed class GameSession : MonoBehaviour, IGameSession
{
    public static GameSession Instance { get; private set; }

    [SerializeField] PlayerCharacter _player;

    bool _playerDefeated;
    bool _roundTimeExpired;

    public bool IsRunActive => !_playerDefeated && !_roundTimeExpired;

    public PlayerCharacter Player => _player;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (_player == null)
            _player = GetComponent<PlayerCharacter>();
    }

    void Start()
    {
        if (_player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player1");
            if (p != null)
                _player = p.GetComponent<PlayerCharacter>();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Creates the session on <c>Player1</c> when the round controller starts, if needed.</summary>
    public static GameSession EnsureForScene()
    {
        if (Instance != null)
            return Instance;

        var p = GameObject.FindGameObjectWithTag("Player1");
        if (p == null)
        {
            Debug.LogWarning("GeoWorld: GameSession could not be created (no GameObject with tag 'Player1').");
            return null;
        }

        var existing = p.GetComponent<GameSession>();
        if (existing != null)
            return existing;

        return p.gameObject.AddComponent<GameSession>();
    }

    /// <summary>Called from <see cref="GameOver"/> after it updates end flags each frame.</summary>
    public void SyncRunState(bool playerDefeated, bool roundTimeExpired)
    {
        _playerDefeated = playerDefeated;
        _roundTimeExpired = roundTimeExpired;
    }
}
