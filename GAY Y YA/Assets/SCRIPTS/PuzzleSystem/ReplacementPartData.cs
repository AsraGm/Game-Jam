using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    [CreateAssetMenu(fileName = "New Replacement Part", menuName = "TrainMechanic/Replacement Part Data")]
    public class ReplacementPartData : ScriptableObject
    {
        [Header("Identidad (coincidir con requiredPart.partId del mecanismo)")]
        public string partId; // ej: "copper_pipe", "fuse_20a"
        public string displayName;

        [Header("Prefabs para el juego (variantes visuales)")]
        [Tooltip("Una o más variantes válidas para esta pieza. Si hay más de una, cada vez " +
                 "que se necesita instanciar un prefab (al spawnear en el mundo con " +
                 "PartSpawnCycle, o al instalarse en un mecanismo reparado con " +
                 "MaintenancePartBase.SwapInstalledPart) se elige una al azar. Todas cuentan " +
                 "como la MISMA pieza a efectos de partId/matching, solo cambia el aspecto.")]
        [SerializeField] private List<GameObject> worldPrefabs = new List<GameObject>();

        [Header("icono UI/inventario")]
        public Sprite icon;

        /// Devuelve una variante al azar de worldPrefabs, o null si la lista está vacía
        /// (todavía no se asignó ningún prefab en el Inspector).
        public GameObject GetRandomWorldPrefab()
        {
            if (worldPrefabs == null || worldPrefabs.Count == 0) return null;
            if (worldPrefabs.Count == 1) return worldPrefabs[0];
            return worldPrefabs[Random.Range(0, worldPrefabs.Count)];
        }
    }
}