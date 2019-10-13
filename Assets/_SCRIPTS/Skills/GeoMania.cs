using UnityEngine;
using System.Collections;

public class GeoMania : SkillBasic {

    public bool geoManiaActivated;

    // Use this for initialization
    void Start () {
        geoManiaActivated = false;
        player = GameObject.FindGameObjectWithTag("Player1");
    }
	
	// Update is called once per frame
	void Update () {
        if (player.GetComponent<PlayerCharacter>().skillAvailable(10)) geoManiaActivated = true;
    }
}
