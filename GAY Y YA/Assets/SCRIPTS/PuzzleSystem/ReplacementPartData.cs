using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Configuración de UN TIPO de pieza de repuesto que el jugador puede
    /// encontrar en el mundo (el objeto "X" que busca).
    /// </summary>
    [CreateAssetMenu(fileName = "New Replacement Part", menuName = "TrainMechanic/Replacement Part Data")]
    public class ReplacementPartData : ScriptableObject
    {
        [Header("Identidad (coincidir con requiredPart.partId del mecanismo)")]
        public string partId; // ej: "copper_pipe", "fuse_20a"
        public string displayName;

        [Header("Prefab para el juego")]
        [Tooltip("También es de acá de donde se saca el mesh/material al reparar " +
                 "un mecanismo con esta pieza (ver MaintenancePartBase.SwapVisualMesh).")]
        public GameObject worldPrefab;

        [Header("icono UI/inventario")]
        public Sprite icon;
    }
}