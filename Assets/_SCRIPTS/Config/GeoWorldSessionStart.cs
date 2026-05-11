/// <summary>
/// Session flag: the player already interacted on the <see cref="GameStart"/> title screen,
/// so WebGL background music can try <see cref="UnityEngine.AudioSource.Play"/> immediately in the next scene.
/// </summary>
public static class GeoWorldSessionStart
{
    public static bool TitleScreenProvidedUserGestureForAudio { get; private set; }

    public static void NotifyGameplayStartingFromTitleScreen()
    {
        TitleScreenProvidedUserGestureForAudio = true;
    }

    public static void ConsumeTitleScreenGestureForAudio()
    {
        TitleScreenProvidedUserGestureForAudio = false;
    }
}
