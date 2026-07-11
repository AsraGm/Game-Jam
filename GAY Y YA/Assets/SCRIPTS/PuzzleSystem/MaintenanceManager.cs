using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    public class MaintenanceManager : MonoBehaviour
    {
        private readonly List<IPuzzleMechanism> _brokenMechanisms = new();
        public IReadOnlyList<IPuzzleMechanism> BrokenMechanisms => _brokenMechanisms;

        /// Se dispara cada vez que la lista de rotos cambia (se rompió algo
        /// o se reparó algo). La UI se suscribe a esto, no a cada mecanismo.
        public event Action<IReadOnlyList<IPuzzleMechanism>> OnBrokenListChanged;

        private void Awake()
        {
            // Los mecanismos son fijos en la escena (no pooleados), así que basta
            // con encontrarlos UNA vez al arrancar y suscribirse a sus eventos.
            var mechanisms = FindObjectsByType<MaintenancePartBase>(FindObjectsSortMode.None);
            foreach (var mechanism in mechanisms)
            {
                mechanism.OnBroken += HandleAnyMechanismBroken;
                mechanism.OnRepaired += HandleAnyMechanismRepaired;
            }
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
    }
}