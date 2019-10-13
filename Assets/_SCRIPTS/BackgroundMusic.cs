using UnityEngine;
using System.Collections;

public class BackgroundMusic : GameOver{

    public AudioClip backGroundMusic;
    AudioSource a;

    // Use this for initialization
    void Start () {
        a = GetComponent<AudioSource>();
        a.PlayOneShot(backGroundMusic, 1F);

    }

    // Update is called once per frame
    void Update () {
        if (playerDied || gameTimeIsOver) a.Stop();
    }
}
