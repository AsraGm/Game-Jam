using System;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    public class ReplacementPart : MonoBehaviour
    {
        [Tooltip("Si esta pieza la colocas a mano en la escena, asígnala aquí")]
        [SerializeField] private ReplacementPartData data;

        [Tooltip("Qué variante EXACTA de data.worldPrefabs le tocó a esta instancia.")]
        [SerializeField] private GameObject chosenPrefab;

        public ReplacementPartData Data => data;

        /// Se dispara cuando el jugador la recoge (PlayerInteractor.TryPickupPart).
        /// PartSpawnCycle se suscribe a esto para saber cuándo volver a llenar ese slot.
        public event Action<ReplacementPart> OnPickedUp;

        /// prefabUsed: la variante puntual que se instanció para esta pieza (la que ve el
        /// jugador en el mundo). Se guarda para que, al reparar con esta pieza, se instale
        /// la MISMA variante en vez de volver a tirar el azar (ver GetPrefabForInstall()).
        public void Initialize(ReplacementPartData newData, GameObject prefabUsed = null)
        {
            data = newData;
            if (prefabUsed != null) chosenPrefab = prefabUsed;
        }

        /// Llamado por PlayerInteractor justo después de guardarla en el inventario.
        public void NotifyPickedUp()
        {
            OnPickedUp?.Invoke(this);
        }

        public bool IsCompatibleWith(PuzzleMechanismData mechanismData)
        {
            return mechanismData != null;
        }

        /// El prefab que debe instalarse en el mecanismo al reparar con ESTA pieza puntual.
        /// Prioriza la variante ya elegida (chosenPrefab) para que coincida con lo que el
        /// jugador vio y recogió; si no hay ninguna asignada, cae a elegir una al azar de
        /// data.worldPrefabs (piezas puestas a mano sin chosenPrefab configurado).
        public GameObject GetPrefabForInstall()
        {
            if (chosenPrefab != null) return chosenPrefab;
            return data != null ? data.GetRandomWorldPrefab() : null;
        }
    }
}