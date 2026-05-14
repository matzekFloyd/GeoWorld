using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class EventSystemChecker : MonoBehaviour
{
    //public GameObject eventSystem;

	// Use this for initialization
	void Awake ()
	{
	    if (FindAnyObjectByType<EventSystem>() == null)
        {
           //Instantiate(eventSystem);
            GameObject obj = new GameObject("EventSystem");
            obj.AddComponent<EventSystem>();
            obj.AddComponent<InputSystemUIInputModule>();
        }
	}
}
