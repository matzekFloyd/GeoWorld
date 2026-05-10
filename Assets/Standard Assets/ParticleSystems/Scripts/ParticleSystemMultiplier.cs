using UnityEngine;

namespace UnityStandardAssets.Effects
{
    public class ParticleSystemMultiplier : MonoBehaviour
    {
        // a simple script to scale the size, speed and lifetime of a particle system

        public float multiplier = 1;


        private void Start()
        {
            var systems = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem system in systems)
            {
                var main = system.main;
                float lifetimeScale = Mathf.Lerp(multiplier, 1, 0.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(main.startSize.constant * multiplier);
                main.startSpeed = new ParticleSystem.MinMaxCurve(main.startSpeed.constant * multiplier);
                main.startLifetime = new ParticleSystem.MinMaxCurve(main.startLifetime.constant * lifetimeScale);
                system.Clear();
                system.Play();
            }
        }
    }
}
