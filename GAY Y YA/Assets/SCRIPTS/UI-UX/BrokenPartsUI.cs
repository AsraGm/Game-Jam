using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    public class BrokenPartsUI : MonoBehaviour
    {
        [SerializeField] private MaintenanceManager manager;
        [SerializeField] private TMP_Text listText;
        [SerializeField] private string emptyMessage = "Todo en orden.";
        [SerializeField] private string headerFormat = "Piezas rotas ({0}):";

        private readonly StringBuilder _sb = new StringBuilder();

        private void OnEnable()
        {
            if (manager == null)
            {
                Debug.LogWarning($"[{name}] BrokenPartsUI: falta asignar 'Manager' en el Inspector. " +
                                  "La UI nunca se va a suscribir al evento, así que se queda congelada tal cual la ves ahora.", this);
                return;
            }
            if (listText == null)
            {
                Debug.LogWarning($"[{name}] BrokenPartsUI: falta asignar 'List Text' en el Inspector. " +
                                  "La UI nunca se va a suscribir al evento, así que se queda congelada tal cual la ves ahora.", this);
                return;
            }

            manager.OnBrokenListChanged += HandleBrokenListChanged;

            // Pinta el estado actual al activarse (por si ya había piezas
            // rotas antes de que esta UI existiera, ej. al abrir un menú).
            HandleBrokenListChanged(manager.BrokenMechanisms);
        }

        private void OnDisable()
        {
            if (manager == null) return;
            manager.OnBrokenListChanged -= HandleBrokenListChanged;
        }

        private void HandleBrokenListChanged(IReadOnlyList<IPuzzleMechanism> broken)
        {
            if (listText == null) return;

            if (broken.Count == 0)
            {
                listText.text = emptyMessage;
                return;
            }

            _sb.Clear();
            _sb.AppendLine(string.Format(headerFormat, broken.Count));

            foreach (var mechanism in broken)
            {
                // Data.displayName es el nombre configurado en el
                // PuzzleMechanismData de ese mecanismo (ej. "Válvula #2").
                string name = mechanism.Data != null ? mechanism.Data.displayName : "Mecanismo";
                _sb.Append("• ").AppendLine(name);
            }

            listText.text = _sb.ToString();
        }
    }
}