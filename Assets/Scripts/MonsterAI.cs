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

        GenerarNuevoPuntoPatrulla();
    }

    void Update()
    {
        if (jugador == null || detectorAgua == null) return;
        if (!agente.isOnNavMesh) return;

        // ESTADO 1: El jugador pisó el AGUA (hace ruido -> el monstruo corre hacia él)
        if (detectorAgua.estaEnAgua)
        {
            if (!persiguiendo)
            {
                persiguiendo = true;
                agente.speed = velocidadCorriendo;
            }

            puntoRuido = jugador.position;

            // Actualizamos el destino 5 veces por segundo, no en cada fotograma
            if (Time.time >= proximaActualizacionDestino)
            {
                proximaActualizacionDestino = Time.time + 0.2f;
                agente.SetDestination(PuntoValido(puntoRuido));
            }
        }
        // ESTADO 2: El jugador está a salvo (el monstruo patrulla lento)
        else
        {
            if (persiguiendo)
            {
                persiguiendo = false;
                agente.speed = velocidadTranquilo;
                GenerarNuevoPuntoPatrulla();
            }

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
            Vector3 candidato = transform.position + Random.insideUnitSphere * radioPatrulla;
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