using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Catálogo central de piezas de repuesto que pueden aparecer por el tren.
    /// Igual que PuzzleCatalog, pero para ReplacementPartData en vez de mecanismos.
    ///
    /// Agregar una pieza nueva al pool de aparición = crear su ReplacementPartData
    /// y arrastrarla acá con un peso. Cero cambios de código.
    /// </summary>
    [CreateAssetMenu(fileName = "PartCatalog", menuName = "TrainMechanic/Part Catalog")]
    public class PartCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public ReplacementPartData part;

            [Tooltip("Mayor = más probable que aparezca al reponer piezas.")]
            [Min(0.01f)] public float spawnWeight = 1f;
        }

        public List<Entry> parts = new List<Entry>();

        /// Elige una pieza al azar respetando los pesos de cada entrada.
        public ReplacementPartData GetRandomWeighted()
        {
            if (parts.Count == 0) return null;

            float totalWeight = 0f;
            foreach (var entry in parts) totalWeight += entry.spawnWeight;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var entry in parts)
            {
                cumulative += entry.spawnWeight;
                if (roll <= cumulative) return entry.part;
            }

            return parts[parts.Count - 1].part; // fallback por seguridad
        }
    }
}
