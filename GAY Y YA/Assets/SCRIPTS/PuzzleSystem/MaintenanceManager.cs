using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace TrainMechanic.Puzzles
{
    public class MaintenanceManager : MonoBehaviour
    {
        [SerializeField] private PuzzleCatalog catalog;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private int initialPoolSizePerType = 3;

        [Header("Auto-arranque (útil mientras no tengas un GameManager)")]
        [Tooltip("Si está activo, genera todos los puzzles automáticamente en Start(). " +
                 "Desactívalo si prefieres llamar SpawnAllInitialPuzzles() tú mismo desde otro script.")]
        [SerializeField] private bool spawnOnStart = true;

        // Un pool por cada tipo de mecanismo (por prefab), así reciclamos
        // instancias en vez de crear/destruir GameObjects constantemente.
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

        // Lista central de mecanismos rotos AHORA MISMO. La UI lee esto,
        // no tiene que llevar su propia cuenta.
        private readonly List<IPuzzleMechanism> _brokenMechanisms = new();
        public IReadOnlyList<IPuzzleMechanism> BrokenMechanisms => _brokenMechanisms;

        /// Se dispara cada vez que la lista de rotos cambia (se rompió algo
        /// o se reparó algo). La UI se suscribe a esto, no a cada mecanismo.
        public event Action<IReadOnlyList<IPuzzleMechanism>> OnBrokenListChanged;

        private void Awake()
        {
            PrewarmPools();
        }

        private void Start()
        {
            if (spawnOnStart)
                SpawnAllInitialPuzzles();
        }

        private void PrewarmPools()
        {
            foreach (var data in catalog.mechanisms)
            {
                var prefab = data.mechanismPrefab;
                if (_pools.ContainsKey(prefab)) continue;

                _pools[prefab] = new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(prefab),
                    actionOnGet: obj => obj.SetActive(true),
                    actionOnRelease: obj => obj.SetActive(false),
                    actionOnDestroy: Destroy,
                    collectionCheck: false,
                    defaultCapacity: initialPoolSizePerType,
                    maxSize: initialPoolSizePerType * 4
                );
            }
        }

        /// Llamar esto para generar un puzzle nuevo en un punto de spawn dado.
        public void SpawnPuzzleAt(Transform point)
        {
            PuzzleMechanismData data = catalog.GetRandomWeighted();
            if (data == null) return;

            var pool = _pools[data.mechanismPrefab];
            GameObject instance = pool.Get();
            instance.transform.SetPositionAndRotation(point.position, point.rotation);

            var mechanism = instance.GetComponent<IPuzzleMechanism>();
            mechanism.Initialize(data);

            // Nos suscribimos UNA vez por instancia spawneada. El manager
            // centraliza todo, así la UI (u otros sistemas) solo necesitan
            // escuchar al manager, no a cada mecanismo suelto.
            mechanism.OnBroken += HandleAnyMechanismBroken;
            mechanism.OnRepaired += HandleAnyMechanismRepaired;
        }

        private void HandleAnyMechanismBroken(IPuzzleMechanism mechanism)
        {
            if (!_brokenMechanisms.Contains(mechanism))
                _brokenMechanisms.Add(mechanism);

            OnBrokenListChanged?.Invoke(_brokenMechanisms);
        }

        private void HandleAnyMechanismRepaired(IPuzzleMechanism mechanism)
        {
            _brokenMechanisms.Remove(mechanism);
            OnBrokenListChanged?.Invoke(_brokenMechanisms);
        }

        /// Devuelve una instancia al pool en vez de destruirla.
        public void ReleaseMechanism(PuzzleMechanismData data, GameObject instance)
        {
            var mechanism = instance.GetComponent<IPuzzleMechanism>();
            if (mechanism != null)
            {
                // Evita fugas de suscripción y "fantasmas" en la lista de rotos
                // si una instancia vuelve al pool estando rota.
                mechanism.OnBroken -= HandleAnyMechanismBroken;
                mechanism.OnRepaired -= HandleAnyMechanismRepaired;
                if (_brokenMechanisms.Remove(mechanism))
                    OnBrokenListChanged?.Invoke(_brokenMechanisms);
            }

            _pools[data.mechanismPrefab].Release(instance);
        }

        public void SpawnAllInitialPuzzles()
        {
            foreach (var point in spawnPoints)
                SpawnPuzzleAt(point);
        }
    }
}