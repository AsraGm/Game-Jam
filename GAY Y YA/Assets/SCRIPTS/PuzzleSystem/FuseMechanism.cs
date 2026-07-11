using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Otro ejemplo de mecanismo: caja de fusibles.
    /// Demuestra que cada mecanismo tiene su propia lógica visual
    /// sin que el resto del sistema (manager, catálogo) se entere.
    /// </summary>
    public class FuseMechanism : MaintenancePartBase
    {
        [SerializeField] private Light indicatorLight;
        [SerializeField] private GameObject sparksFX;

        protected override void OnBrokenVisual()
        {
            if (indicatorLight) indicatorLight.color = Color.red;
            if (sparksFX) sparksFX.SetActive(true);
        }

        protected override void OnRepairedVisual()
        {
            if (indicatorLight) indicatorLight.color = Color.green;
            if (sparksFX) sparksFX.SetActive(false);
        }
    }
}
