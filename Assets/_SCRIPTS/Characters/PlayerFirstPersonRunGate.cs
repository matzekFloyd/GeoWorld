using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

/// <summary>
/// Disables Standard Assets first-person movement while dead or after the run ends (<see cref="GameSession.IsRunActive"/>).
/// Covers cases where <see cref="Time.timeScale"/> is still 1 (no <see cref="GameOver"/> in scene) or input/movement still advances.
/// </summary>
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class PlayerFirstPersonRunGate : MonoBehaviour
{
    FirstPersonController _characterControllerFpc;
    RigidbodyFirstPersonController _rigidbodyFpc;
    Rigidbody _rb;

    void Awake()
    {
        _characterControllerFpc = GetComponent<FirstPersonController>();
        _rigidbodyFpc = GetComponent<RigidbodyFirstPersonController>();
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        bool allow = ComputeMovementAllowed();
        ApplyFpc(_characterControllerFpc, allow);
        ApplyFpc(_rigidbodyFpc, allow);
        if (!allow && _rb != null && !_rb.isKinematic)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    static void ApplyFpc(MonoBehaviour fpc, bool allow)
    {
        if (fpc == null || fpc.enabled == allow)
            return;
        fpc.enabled = allow;
    }

    static bool ComputeMovementAllowed()
    {
        var session = GameSession.Instance;
        if (session != null && !session.IsRunActive)
            return false;

        var pc = session != null ? session.Player : null;
        if (pc == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player1");
            pc = go != null ? go.GetComponent<PlayerCharacter>() : null;
        }

        return pc == null || !pc.iAmDead();
    }
}
