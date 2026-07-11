using System.Collections.Generic;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    /// <summary>
    /// Reemplaza el raycast de interacción por un volumen trigger (normalmente
    /// hijo de la cámara o del jugador). Todo lo que entra/sale del trigger se
    /// cachea en listas vía OnTriggerEnter/Exit (cero trabajo por frame);
    /// PlayerInteractor solo pregunta "¿qué tengo más cerca ahora mismo?" al
    /// presionar la tecla de interacción.
    ///
    /// Requisito de física de Unity: para que OnTrigger dispare, AL MENOS uno
    /// de los dos objetos involucrados necesita un Rigidbody en su jerarquía.
    /// Si este objeto es hijo del jugador (que ya tiene Rigidbody por el
    /// MovementController), no hace falta agregarle uno acá.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class InteractionZone : MonoBehaviour
    {
        private readonly List<IPuzzleMechanism> _mechanismsInRange = new();
        private readonly List<ReplacementPart> _partsInRange = new();

        private void Reset()
        {
            // Conveniencia al agregar el componente: ya lo deja en modo Trigger.
            GetComponent<SphereCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var mechanism = other.GetComponentInParent<IPuzzleMechanism>();
            if (mechanism != null)
            {
                if (!_mechanismsInRange.Contains(mechanism))
                    _mechanismsInRange.Add(mechanism);
                return; // un objeto no debería ser mecanismo Y pieza a la vez
            }

            var part = other.GetComponentInParent<ReplacementPart>();
            if (part != null && !_partsInRange.Contains(part))
                _partsInRange.Add(part);
        }

        private void OnTriggerExit(Collider other)
        {
            var mechanism = other.GetComponentInParent<IPuzzleMechanism>();
            if (mechanism != null)
            {
                _mechanismsInRange.Remove(mechanism);
                return;
            }

            var part = other.GetComponentInParent<ReplacementPart>();
            if (part != null)
                _partsInRange.Remove(part);
        }

        /// El mecanismo ROTO más cercano al punto dado, o null si no hay ninguno en rango.
        public IPuzzleMechanism GetClosestBrokenMechanism(Vector3 fromPosition)
        {
            // Mismo seguro que en GetClosestPart. Los mecanismos hoy son fijos y no se
            // destruyen, pero si en algún momento se pooléan de nuevo, esto evita el
            // mismo tipo de crash sin tener que acordarse de tocar acá.
            _mechanismsInRange.RemoveAll(m => m is Component comp && comp == null);

            IPuzzleMechanism closest = null;
            float closestSqrDist = float.MaxValue;

            foreach (var m in _mechanismsInRange)
            {
                if (!m.IsBroken) continue;

                // IPuzzleMechanism no expone posición; como en la práctica todo
                // mecanismo es un MonoBehaviour, volvemos a Component para leerla.
                if (m is not Component comp) continue;

                float sqrDist = (comp.transform.position - fromPosition).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closest = m;
                }
            }
            return closest;
        }

        /// La pieza de repuesto más cercana al punto dado, o null si no hay ninguna en rango.
        public ReplacementPart GetClosestPart(Vector3 fromPosition)
        {
            // Seguro defensivo: si por lo que sea una pieza se destruyó sin pasar por
            // OnTriggerExit (ej. Destroy() directo mientras seguía "adentro" del trigger),
            // Unity la deja como referencia fantasma acá. La purgamos antes de leer nada
            // de ella para no repetir la MissingReferenceException.
            _partsInRange.RemoveAll(p => p == null);

            ReplacementPart closest = null;
            float closestSqrDist = float.MaxValue;

            foreach (var p in _partsInRange)
            {
                float sqrDist = (p.transform.position - fromPosition).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closest = p;
                }
            }
            return closest;
        }

        private void OnDisable()
        {
            // Si el objeto se desactiva con algo "adentro" del trigger, Unity no
            // dispara OnTriggerExit: limpiamos a mano para no dejar fantasmas.
            _mechanismsInRange.Clear();
            _partsInRange.Clear();
        }
    }
}