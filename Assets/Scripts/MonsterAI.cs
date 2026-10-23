using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    [Header("Configuración de Velocidades")]
    public float velocidadTranquilo = 1.5f;   // Velocidad lenta cuando patrulla
    public float velocidadCorriendo = 6.0f;   // Velocidad rápida cuando escucha ruido

    [Header("Referencias")]
    public Transform jugador;
    public DetectorAgua detectorAgua;

    [Header("Puntos de Patrulla / Punto de Ruido")]
    public Vector3 puntoRuido;
    private Vector3 puntoPatrullaAleatorio;
    private float tiempoSiguientePunto = 0f;

    void Start()
    {
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

        // ESTADO 1: El jugador pisó el AGUA (Hace ruido -> ¡EL MONSTRUO CORRE!)
        if (detectorAgua.estaEnAgua)
        {
            puntoRuido = jugador.position;
            Vector3 destinoRuido = new Vector3(puntoRuido.x, transform.position.y, puntoRuido.z);

            // Se mueve RÁPIDO hacia el ruido
            transform.position = Vector3.MoveTowards(transform.position, destinoRuido, velocidadCorriendo * Time.deltaTime);
            
            // Mira hacia donde corre
            if (destinoRuido != transform.position)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(destinoRuido - transform.position), Time.deltaTime * 10f);
            }

            Debug.Log("¡RUIDO DETECTADO! El monstruo va CORRIENDO al punto.");
        }
        // ESTADO 2: El jugador está a salvo en SUELO SEGURO (El monstruo camina LENTO)
        else
        {
            PatrullarLentamente();
        }
    }

    void PatrullarLentamente()
    {
        Vector3 destinoPatrulla = new Vector3(puntoPatrullaAleatorio.x, transform.position.y, puntoPatrullaAleatorio.z);

        // Se mueve LENTO entre puntos
        transform.position = Vector3.MoveTowards(transform.position, destinoPatrulla, velocidadTranquilo * Time.deltaTime);

        // Si llega cerca del punto o pasa el tiempo, elige otro punto aleatorio
        if (Vector3.Distance(transform.position, destinoPatrulla) < 0.5f || Time.time > tiempoSiguientePunto)
        {
            GenerarNuevoPuntoPatrulla();
        }
        else if (destinoPatrulla != transform.position)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(destinoPatrulla - transform.position), Time.deltaTime * 2f);
        }
    }

    void GenerarNuevoPuntoPatrulla()
    {
        // Genera un punto aleatorio cerca de su posición actual
        float radio = 8.0f;
        Vector3 puntoAleatorio = transform.position + Random.insideUnitSphere * radio;
        puntoPatrullaAleatorio = new Vector3(puntoAleatorio.x, transform.position.y, puntoAleatorio.z);
        
        // Tiempo antes de cambiar de punto si se queda atascado
        tiempoSiguientePunto = Time.time + 5.0f; 
    }
}