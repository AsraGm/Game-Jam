using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    [CreateAssetMenu(fileName = "New Replacement Part", menuName = "TrainMechanic/Replacement Part Data")]
    public class ReplacementPartData : ScriptableObject
    {
        [Header("Identidad (coincidir con requiredPart.partId del mecanismo)")]
        public string[] acceptedPartIds; // ej: "copper_pipe", "fuse_20a"
        public string displayName;

        [Header("Prefabs para el juego (variantes visuales)")]
        [Tooltip("Una o más variantes válidas para esta pieza. Si hay más de una, cada vez")]
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