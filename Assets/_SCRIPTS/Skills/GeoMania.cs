using UnityEngine;

public class GeoMania : SkillBasic
{
    [Tooltip("True while skill slot 10 (Geo Mania) is available — same condition as SkillBasic.geoManiaActivated().")]
    public bool maniaActive;

    void Update()
    {
        maniaActive = geoManiaActivated();
    }
}
