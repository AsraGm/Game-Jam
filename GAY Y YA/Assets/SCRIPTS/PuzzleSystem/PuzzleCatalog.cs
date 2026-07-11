using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// EL catálogo central: la lista de todos los tipos de puzzle/mecanismo
    /// disponibles para la generación procedural.
    ///
    /// Agregar un mecanismo nuevo al juego = crear su PuzzleMechanismData
    /// y arrastrarlo a esta lista. Cero cambios de código en el resto del sistema.
    /// </summary>
    [CreateAssetMenu(fileName = "PuzzleCatalog", menuName = "TrainMechanic/Puzzle Catalog")]
    public class PuzzleCatalog : ScriptableObject
    {
        public List<PuzzleMechanismData> mechanisms = new List<PuzzleMechanismData>();

        /// Elige un mecanismo al azar respetando los pesos (spawnWeight) de cada uno.
        public PuzzleMechanismData GetRandomWeighted()
        {
            if (mechanisms.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var m in mechanisms) totalWeight += m.spawnWeight;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var m in mechanisms)
            {
                cumulative += m.spawnWeight;
                if (roll <= cumulative) return m;
            }

            return mechanisms[mechanisms.Count - 1]; // fallback por seguridad
        }
    }
}
