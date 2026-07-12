using System;
using System.Collections;
using UnityEngine;

namespace TrainMechanic.Puzzles
{
    [DisallowMultipleComponent]
    public abstract class MaintenancePartBase : MonoBehaviour, IPuzzleMechanism
    {
        [Header("Outline (shader properties _Scale y _Color)")]
        [SerializeField] private Renderer fallbackOutlineRenderer;

        // Renderer ACTUAL sobre el que se dibuja el outline. Se recalcula en cada
        // InstallPart() buscando dentro de la pieza recién instalada; si no se
        // encuentra ninguno compatible, cae a fallbackOutlineRenderer.
        private Renderer outlineRenderer;

        [Header("Colores del outline por estado")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color warningColor = Color.red;

        [Header("Aviso antes de romperse")]
        [Tooltip("Segundos antes de la falla en los que el outline pasa de verde a rojo")]
        [SerializeField] private float warningLeadTime = 5f;

        [Header("Visual intercambiable (\"pon cualquier cosa\")")]
        [Tooltip("Punto (Transform vacío) donde vive la pieza actualmente instalada")]
        [SerializeField] private Transform partAttachPoint;
        [Tooltip("Opcional: prefab que se instala en 'Part Attach Point' al inicializarse")]
        [SerializeField] private GameObject initialPartPrefab;

        [Header("Mecanismo fijo (puesto a mano en el tren)")]
        [Tooltip("Asigna acá el PuzzleMechanismData de ESTE mecanismo si está puesto a mano")]
        [SerializeField] private PuzzleMechanismData fixedData;

        private GameObject _installedPartVisual;

        private static readonly int ScalePropertyId = Shader.PropertyToID("_Scale");
        private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

        [Header("Shader")]
        [Tooltip("Valor de _Scale cuando el outline está oculto/inactivo (sin renderer válido).")]
        [SerializeField] private float scaleHidden = 0.9f;
        [Tooltip("Valor mínimo de _Scale durante el pulso (sano/aviso/roto).")]
        [SerializeField] private float scaleMin = 1.04f;
        [Tooltip("Valor máximo de _Scale durante el pulso (sano/aviso/roto).")]
        [SerializeField] private float scaleMax = 1.1f;
        [SerializeField] private float pulseSpeed = 7f; // ciclos por segundo aprox, ajusta a gusto

        private MaterialPropertyBlock _propBlock;
        private Coroutine _outlineRoutine;

        public bool IsBroken { get; private set; }
        public PuzzleMechanismData Data { get; private set; }

        public event Action<IPuzzleMechanism> OnBroken;
        public event Action<IPuzzleMechanism> OnRepaired;

        public virtual void Initialize(PuzzleMechanismData data)
        {
            Data = data;
            IsBroken = false;

            CancelInvoke(nameof(BreakDown));
            CancelInvoke(nameof(EnterWarningState));

            // Vuelve a la pieza "original" (rota) para este nuevo ciclo. Si venía
            // reciclada del pool con otra pieza instalada de un ciclo anterior, esto
            // la destruye y pone la inicial en su lugar (o queda vacío si no hay una).
            InstallPart(initialPartPrefab);

            SetOutlineColor(healthyColor);
            StartOutline(); // ahora corre SIEMPRE (verde sano / rojo aviso o roto), no solo al romperse.

            ScheduleNextFailure();
        }

        private void ScheduleNextFailure()
        {
            float durability = Data.GetRandomDurability();
            float warningAt = durability - warningLeadTime;

            if (warningAt > 0f)
                Invoke(nameof(EnterWarningState), warningAt);
            else
                EnterWarningState(); // dura menos que el aviso: avisa de una

            Invoke(nameof(BreakDown), durability);
        }

        /// Aviso visual de que el mecanismo está por romperse (todavía no rompió).
        private void EnterWarningState()
        {
            if (IsBroken) return; // ya se rompió, no hace falta el aviso
            SetOutlineColor(warningColor);
        }

        public void BreakDown()
        {
            if (IsBroken) return;
            IsBroken = true;

            CancelInvoke(nameof(EnterWarningState)); // por si se rompe antes del aviso programado
            SetOutlineColor(warningColor); // se queda en rojo mientras está rota

            OnBrokenVisual();
            OnBroken?.Invoke(this);
        }

        public bool TryRepair(ReplacementPart part)
        {
            if (!IsBroken) return false;
            if (part == null || !part.IsCompatibleWith(Data)) return false;

            IsBroken = false;

            SwapInstalledPart(part);
            OnRepairedVisual();
            SetOutlineColor(healthyColor);

            OnRepaired?.Invoke(this);
            ScheduleNextFailure(); // vuelve a degradarse: así se mantiene el loop de mantenimiento
            return true;
        }

        /// Instala físicamente la pieza usada para reparar: destruye lo que había en
        /// partAttachPoint y pone el prefab COMPLETO de la nueva pieza en su lugar.
        /// Esto es lo que permite el chiste de "pon cualquier cosa": el mismo prefab
        /// que el jugador recogió en el mundo (worldPrefab) queda instalado tal cual,
        /// con todos sus materiales/hijos/lo que sea, no solo un mesh suelto.
        private void SwapInstalledPart(ReplacementPart part)
        {
            // GetPrefabForInstall() (no GetRandomWorldPrefab() directo) para instalar la
            // MISMA variante que el jugador vio y recogió, en vez de tirar el azar de nuevo.
            GameObject prefab = part != null ? part.GetPrefabForInstall() : null;
            if (part == null || part.Data == null || prefab == null)
            {
                Debug.LogWarning($"[{name}] SwapInstalledPart: la pieza usada no tiene ningún 'World Prefab' asignado en su ReplacementPartData.");
                return;
            }

            InstallPart(prefab);
            Debug.Log($"[{name}] Pieza instalada: {part.Data.displayName}");
        }

        /// Destruye la pieza actualmente instalada (si hay) e instancia el prefab dado
        /// como hijo de partAttachPoint. prefab puede ser null (deja el punto vacío).
        private void InstallPart(GameObject prefab)
        {
            if (partAttachPoint == null)
            {
                if (prefab != null)
                    Debug.LogWarning($"[{name}] InstallPart: falta asignar 'Part Attach Point' en el Inspector de este mecanismo.");
                return;
            }

            if (_installedPartVisual != null)
            {
                Destroy(_installedPartVisual);
                _installedPartVisual = null;
            }

            if (prefab == null)
            {
                UpdateOutlineRenderer(null); // vuelve al fallback (o sin outline si tampoco hay)
                return;
            }

            _installedPartVisual = Instantiate(prefab, partAttachPoint);
            _installedPartVisual.transform.localPosition = Vector3.zero;
            _installedPartVisual.transform.localRotation = Quaternion.identity;

            // El prefab puede traer Collider/Rigidbody propios (porque también sirve
            // como pieza recogible suelta en el mundo). Ya instalado, no queremos que
            // estorbe físicamente ni que la zona de interacción lo detecte de nuevo.
            var col = _installedPartVisual.GetComponentInChildren<Collider>();
            if (col != null) col.enabled = false;

            var rb = _installedPartVisual.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);

            UpdateOutlineRenderer(_installedPartVisual);
        }

        /// Busca, dentro de la pieza recién instalada, un Renderer cuyo material
        /// tenga las properties _Scale y _Color (las que usa el outline). Así, cada
        /// vez que se instala una pieza nueva (con su propio mesh/material/shader),
        /// el outline "sigue" a esa pieza en vez de quedar pegado a un Renderer fijo
        /// que se rompería (o quedaría como referencia fantasma) apenas cambia la
        /// variante visual instalada.
        ///
        /// installedRoot puede ser null (InstallPart(null), punto vacío): en ese
        /// caso cae directo al fallback.
        private void UpdateOutlineRenderer(GameObject installedRoot)
        {
            Renderer found = null;

            if (installedRoot != null)
            {
                foreach (var candidate in installedRoot.GetComponentsInChildren<Renderer>())
                {
                    // sharedMaterial (no .material): solo estamos LEYENDO si la property
                    // existe. Usar .material acá crearía una instancia de material por
                    // pieza instalada, gasto de memoria innecesario para algo que ni
                    // siquiera vamos a modificar por este renderer en particular.
                    if (candidate.sharedMaterial != null &&
                        candidate.sharedMaterial.HasProperty(ScalePropertyId) &&
                        candidate.sharedMaterial.HasProperty(ColorPropertyId))
                    {
                        found = candidate;
                        break;
                    }
                }

                if (found == null)
                {
                    Debug.LogWarning($"[{name}] UpdateOutlineRenderer: la pieza instalada " +
                                      $"('{installedRoot.name}') no tiene ningún Renderer con las " +
                                      $"properties _Scale/_Color del shader de outline. Revisa que " +
                                      $"su material use el shader correcto (o asigná un " +
                                      $"'Fallback Outline Renderer' en el Inspector como respaldo).", this);
                }
            }

            outlineRenderer = found != null ? found : fallbackOutlineRenderer;

            // El MaterialPropertyBlock queda "cacheado" leyendo/escribiendo sobre el
            // renderer que tenía asignado antes; como el renderer activo puede haber
            // cambiado, se descarta para que el próximo SetScale/SetOutlineColor pida
            // el property block del renderer NUEVO (GetPropertyBlock/SetPropertyBlock
            // ya lo manejan bien recreándolo, esto solo evita arrastrar valores viejos
            // de un renderer que ya no es el que se está usando).
            _propBlock = null;

            // Caso borde: si al Initialize() inicial no había NINGÚN renderer válido
            // (ni en la pieza inicial ni fallback), StartOutline() no arrancó la
            // corrutina de pulso. Si ahora (con esta pieza nueva) sí hay uno, la
            // arrancamos recién acá; si ya estaba corriendo, StartOutline es un no-op
            // seguro (reinicia la misma corrutina, no la duplica).
            if (outlineRenderer != null)
                StartOutline();
        }

        /// Hook para efectos visuales/sonido cuando se rompe. Override en cada mecanismo hijo.
        protected abstract void OnBrokenVisual();

        /// Hook para efectos visuales/sonido cuando se repara. Override en cada mecanismo hijo.
        protected abstract void OnRepairedVisual();

        private void StartOutline()
        {
            if (outlineRenderer == null) return;
            if (_outlineRoutine != null) StopCoroutine(_outlineRoutine);
            _outlineRoutine = StartCoroutine(PulseOutline());
        }

        private void StopOutline()
        {
            if (_outlineRoutine != null)
            {
                StopCoroutine(_outlineRoutine);
                _outlineRoutine = null;
            }
            SetScale(scaleHidden);
        }

        // Corre todo el tiempo que el mecanismo está activo (sano, en aviso o roto).
        // Solo cambia el color entre estados; el pulso de escala es siempre el mismo.
        private IEnumerator PulseOutline()
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
                float scale = Mathf.Lerp(scaleMin, scaleMax, t);
                SetScale(scale);
                yield return null;
            }
        }

        private void SetScale(float value)
        {
            if (outlineRenderer == null) return;

            // MaterialPropertyBlock: cambia la property visualmente sin crear
            // una instancia de material nueva por objeto (cero GC extra,
            // no rompe el instancing/batching de Unity).
            _propBlock ??= new MaterialPropertyBlock();
            outlineRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(ScalePropertyId, value);
            outlineRenderer.SetPropertyBlock(_propBlock);
        }

        private void SetOutlineColor(Color color)
        {
            if (outlineRenderer == null) return;

            _propBlock ??= new MaterialPropertyBlock();
            outlineRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ColorPropertyId, color);
            outlineRenderer.SetPropertyBlock(_propBlock);
        }

        protected virtual void OnEnable()
        {
            // Si este mecanismo está puesto a mano en la escena (no generado por
            // un manager), se inicializa solo con su propia data la primera vez.
            if (fixedData != null && Data == null)
                Initialize(fixedData);
        }

        protected virtual void OnDisable()
        {
            // limpieza al volver al pool: evita callbacks fantasma sobre un objeto desactivado
            CancelInvoke(nameof(BreakDown));
            CancelInvoke(nameof(EnterWarningState));
            StopOutline();
        }

#if UNITY_EDITOR
        [ContextMenu("TEST: Forzar ruptura ahora")]
        private void ForceBreakNowForTesting() => BreakDown();
#endif
    }
}