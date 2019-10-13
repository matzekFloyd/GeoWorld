using UnityEngine;
using System.Collections;

public class MeteorExplosionDestroy : MonoBehaviour {

	// Use this for initialization
	void Start () {
        Destroy(this.gameObject, 0.5f);
    }

    // Update is called once per frame
    void Update () {
	}
}
