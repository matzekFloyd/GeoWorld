using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;


public class GameStart : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKeyUp(KeyCode.G)) SceneManager.LoadScene("GeoWorldMain");

    }
}
