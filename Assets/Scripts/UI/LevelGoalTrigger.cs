using UnityEngine;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Componente opcional listo para conectar al final de cualquier nivel.
    /// Permite disparar la ventana de Nivel Completado al entrar en un Trigger o al reciclar toda la basura.
    /// </summary>
    public class LevelGoalTrigger : MonoBehaviour
    {
        [Header("Modos de Activación")]
        [Tooltip("Abre la ventana de Nivel Completado cuando el Jugador entra en este Collider Trigger.")]
        [SerializeField] private bool activarAlEntrarJugador = true;

        [Tooltip("Si se activa, completa el nivel automáticamente cuando no queden objetos con tag 'Basura' en la escena.")]
        [SerializeField] private bool activarAlReciclarTodaLaBasura = false;
        [SerializeField] private float retrasoTrasUltimaBasura = 0.8f;

        [Header("Puntaje Opcional")]
        [SerializeField] private int puntajeOpcional = -1;

        private bool yaActivado;
        private bool tuvoBasurasAlInicio;
        private float tiempoSinBasura;

        private void Start()
        {
            if (activarAlReciclarTodaLaBasura)
            {
                TrashItem[] basuras = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
                tuvoBasurasAlInicio = basuras != null && basuras.Length > 0;
            }
        }

        private void Update()
        {
            if (yaActivado || !activarAlReciclarTodaLaBasura || !tuvoBasurasAlInicio) return;

            TrashItem[] basuras = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
            if (basuras == null || basuras.Length == 0)
            {
                tiempoSinBasura += Time.deltaTime;
                if (tiempoSinBasura >= retrasoTrasUltimaBasura)
                {
                    CompletarNivel();
                }
            }
            else
            {
                tiempoSinBasura = 0f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (yaActivado || !activarAlEntrarJugador) return;
            if (other.CompareTag("Player"))
            {
                CompletarNivel();
            }
        }

        public void CompletarNivel()
        {
            if (yaActivado) return;
            yaActivado = true;
            MenuPausaJuegoController.MostrarNivelCompletado(puntajeOpcional);
        }
    }
}
