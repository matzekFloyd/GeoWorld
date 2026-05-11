using UnityEngine;

public class MeteorExplosionDestroy : MonoBehaviour {

	void OnEnable () {
        CancelInvoke(nameof(ReturnToPoolOrDestroy));
        Invoke(nameof(ReturnToPoolOrDestroy), 0.5f);
    }

    void ReturnToPoolOrDestroy()
    {
        GeoWorldObjectPools.Release(gameObject);
    }
}
