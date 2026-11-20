using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : MonoBehaviour
{
    [Header("Configuración de Velocidades")]
    public float velocidadTranquilo = 1.5f;   // Velocidad lenta cuando patrulla
    public float velocidadCorriendo = 4.5f;   // Velocidad rápida cuando escucha ruido

    [Header("Referencias")]
    public Transform jugador;
    public DetectorAgua detectorAgua;

    [Header("Puntos de Patrulla / Punto de Ruido")]
    public Vector3 puntoRuido;
    public float radioPatrulla = 8.0f;

    private NavMeshAgent agente;
    private bool persiguiendo = false;
    private bool investigandoUltimoPunto = false;
    private float tiempoSiguientePunto = 0f;
    private float proximaActualizacionDestino = 0f;

    void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        agente.speed = velocidadTranquilo;

        // Buscar referencias si no se asignaron en el Inspector
        if (jugador == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                jugador = playerObj.transform;
                detectorAgua = playerObj.GetComponent<DetectorAgua>();
            }
        }

        if (detectorAgua != null)
        {
            detectorAgua.OnEstadoAguaCambiado += AlCambiarEstadoAgua;
        }

        GenerarNuevoPuntoPatrulla();
    }

    void OnDestroy()
    {
        if (detectorAgua != null)
        {
            detectorAgua.OnEstadoAguaCambiado -= AlCambiarEstadoAgua;
        }
    }

    void AlCambiarEstadoAgua(bool enAgua)
    {
        if (!agente.isOnNavMesh) return;

        if (enAgua)
        {
            persiguiendo = true;
            investigandoUltimoPunto = false;
            agente.speed = velocidadCorriendo;
            puntoRuido = jugador.position;
            agente.SetDestination(PuntoValido(puntoRuido));
        }
        else if (persiguiendo)
        {
            // Al salir del agua, no se detiene al instante: corre a investigar el último lugar del ruido
            persiguiendo = false;
            investigandoUltimoPunto = true;
            agente.SetDestination(PuntoValido(puntoRuido));
        }
    }

    void Update()
    {
        if (jugador == null || detectorAgua == null) return;
        if (!agente.isOnNavMesh) return;

        // ESTADO 1: Persiguiendo activamente mientras el jugador sigue en el agua
        if (persiguiendo)
        {
            if (Time.time >= proximaActualizacionDestino)
            {
                proximaActualizacionDestino = Time.time + 0.2f;
                puntoRuido = jugador.position;
                agente.SetDestination(PuntoValido(puntoRuido));
            }
        }
        // ESTADO 2: Investigando el último punto de ruido (el jugador salió del agua)
        else if (investigandoUltimoPunto)
        {
            // Si llegó cerca del punto investigado, reanuda la patrulla tranquila
            if (!agente.pathPending && agente.remainingDistance <= 1.0f)
            {
                investigandoUltimoPunto = false;
                agente.speed = velocidadTranquilo;
                GenerarNuevoPuntoPatrulla();
            }
        }
        // ESTADO 3: Patrullando tranquilamente
        else
        {
            PatrullarLentamente();
        }
    }

    void PatrullarLentamente()
    {
        bool llegoAlPunto = !agente.pathPending && agente.remainingDistance <= 0.5f;

        // Si llegó al punto o pasó el tiempo (por si se atasca), elige otro
        if (llegoAlPunto || Time.time > tiempoSiguientePunto)
        {
            GenerarNuevoPuntoPatrulla();
        }
    }

    void GenerarNuevoPuntoPatrulla()
    {
        if (!agente.isOnNavMesh) return;

        // Probamos varias veces hasta encontrar un punto sobre el mapa de caminos
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radioPatrulla;
            randomDirection.y = 0f; // Evitamos generar puntos flotantes o subterráneos
            Vector3 candidato = transform.position + randomDirection;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidato, out hit, 2.0f, NavMesh.AllAreas))
            {
                agente.SetDestination(hit.position);
                break;
            }
        }

        // Tiempo antes de cambiar de punto si se queda atascado
        tiempoSiguientePunto = Time.time + 5.0f;
    }

    // Ajusta un punto al lugar más cercano donde el monstruo sí puede caminar
    Vector3 PuntoValido(Vector3 punto)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(punto, out hit, 3.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return punto;
    }
}
