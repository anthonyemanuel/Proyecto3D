using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MonsterSonido : MonoBehaviour
{
    [Header("Sonidos")]
    public AudioClip sonidoRugido;    // Cuando patrulla
    public AudioClip sonidoGrito;     // Cuando te persigue
    public AudioClip sonidoAtrapar;   // Cuando te atrapa
    [Range(0f, 1f)] public float volumen = 0.8f;
    [Range(0f, 1f)] public float volumenGrito = 1f;
    [Range(0f, 1f)] public float volumenAtrapar = 1f;

    [Header("Rugido al patrullar (segundos, al azar entre estos dos valores)")]
    public float tiempoMinimo = 6f;
    public float tiempoMaximo = 15f;

    [Header("Grito al perseguir (segundos, al azar entre estos dos valores)")]
    public float tiempoMinimoGrito = 2f;
    public float tiempoMaximoGrito = 4f;

    [Header("Cuando te atrapa")]
    [Tooltip("Si el monstruo está a menos de esta distancia mientras te persigue, cuenta como atrapado")]
    public float distanciaAtrapar = 1.5f;

    [Header("Realismo del sonido 3D")]
    public float distanciaMinima = 3f;
    public float distanciaMaxima = 40f;
    [Range(0f, 1f)] public float efectoDoppler = 0f;

    private AudioSource fuente;
    private Transform jugador;
    private DetectorAgua detectorAgua;
    private bool enPersecucion = false;
    private bool yaAtrapo = false;
    private float tiempoSiguiente;

    void Start()
    {
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;

        // Sonido 3D realista: la curva logarítmica imita la vida real
        fuente.spatialBlend = 1f;
        fuente.rolloffMode = AudioRolloffMode.Logarithmic;
        fuente.minDistance = distanciaMinima;
        fuente.maxDistance = distanciaMaxima;
        fuente.dopplerLevel = efectoDoppler;

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
        {
            jugador = objJugador.transform;
            detectorAgua = objJugador.GetComponent<DetectorAgua>();
        }

        ElegirSiguienteSonido();
    }

    void Update()
    {
        if (detectorAgua == null || jugador == null) return;

        bool persiguiendo = detectorAgua.estaEnAgua;

        // Si cambió el estado (empieza o termina la persecución)
        if (persiguiendo != enPersecucion)
        {
            enPersecucion = persiguiendo;
            yaAtrapo = false;
            fuente.Stop(); // Corta el sonido anterior

            if (enPersecucion)
            {
                ReproducirGrito();   // Grita apenas empieza a perseguir
            }
            ElegirSiguienteSonido();
            return;
        }

        // Sonido de atrapar: una sola vez cada vez que te alcanza
        if (enPersecucion)
        {
            float distancia = Vector3.Distance(transform.position, jugador.position);

            if (!yaAtrapo && distancia <= distanciaAtrapar)
            {
                yaAtrapo = true;
                fuente.Stop();
                if (sonidoAtrapar != null)
                {
                    fuente.PlayOneShot(sonidoAtrapar, volumenAtrapar);
                }
                ElegirSiguienteSonido();
                return;
            }
            else if (yaAtrapo && distancia > distanciaAtrapar * 2f)
            {
                yaAtrapo = false; // Se alejó: puede volver a atraparte
            }
        }

        // Si ya pasó el tiempo, suena el que corresponda
        if (Time.time >= tiempoSiguiente)
        {
            if (enPersecucion)
            {
                if (!yaAtrapo) ReproducirGrito();
            }
            else if (sonidoRugido != null)
            {
                fuente.PlayOneShot(sonidoRugido, volumen);
            }
            ElegirSiguienteSonido();
        }
    }

    void ReproducirGrito()
    {
        if (sonidoGrito != null)
        {
            fuente.PlayOneShot(sonidoGrito, volumenGrito);
        }
    }

    void ElegirSiguienteSonido()
    {
        if (enPersecucion)
        {
            tiempoSiguiente = Time.time + Random.Range(tiempoMinimoGrito, tiempoMaximoGrito);
        }
        else
        {
            tiempoSiguiente = Time.time + Random.Range(tiempoMinimo, tiempoMaximo);
        }
    }
}