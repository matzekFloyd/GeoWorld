/// <summary>
/// Read-only view of the active single-player run (alive player, round timer not expired).
/// Implemented by <see cref="GameSession"/>.
/// </summary>
public interface IGameSession
{
    bool IsRunActive { get; }
    PlayerCharacter Player { get; }
}
