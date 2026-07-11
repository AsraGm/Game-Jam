using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Ejemplo de mecanismo concreto: una válvula que hay que reemplazar.
    /// Así se ve un mecanismo NUEVO: hereda de MaintenancePartBase
    /// e implementa únicamente su parte visual.
    /// </summary>
    public class ValveMechanism : MaintenancePartBase
    {
        [SerializeField] private Renderer valveRenderer;
        [SerializeField] private Color brokenColor = Color.red;
        [SerializeField] private Color okColor = Color.green;
        [SerializeField] private ParticleSystem steamLeakFX;

        protected override void OnBrokenVisual()
        {
            if (valveRenderer) valveRenderer.material.color = brokenColor;
            if (steamLeakFX) steamLeakFX.Play();
        }

        protected override void OnRepairedVisual()
        {
            if (valveRenderer) valveRenderer.material.color = okColor;
            if (steamLeakFX) steamLeakFX.Stop();
        }
    }
}
