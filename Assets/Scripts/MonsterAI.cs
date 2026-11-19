using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

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
    public float tiempoInvestigando = 3.0f;   // Segundos que investiga el último ruido

    [Header("Sistema de Vidas y Atrapado")]
    public int vidasMaximas = 3;
    public float distanciaAtrapar = 1.5f;
    public float duracionSusto = 0.65f;       // Duración de la sacudida antes de reaparecer
    public float tiempoInmunidadRespawn = 2.5f;
    public float intensidadSacudidaCamara = 0.18f;

    [Header("Aura Visual de Pesadilla")]
    public Color colorAuraPatrulla = new Color(0.18f, 0.78f, 1.0f, 1f);
    public Color colorAuraInvestigando = new Color(1.0f, 0.58f, 0.10f, 1f);
    public Color colorAuraPersecucion = new Color(1.0f, 0.08f, 0.12f, 1f);
    public float intensidadLuzPatrulla = 2.6f;
    public float intensidadLuzPersecucion = 7.5f;

    private NavMeshAgent agente;
    private bool persiguiendo = false;
    private bool investigando = false;
    private float tiempoFinInvestigacion = 0f;
    private float tiempoSiguientePunto = 0f;
    private float proximaActualizacionDestino = 0f;
    private float tiempoSiguienteSalpicadura = 0f;
    private float tiempoAtascado = 0f;
    private Vector3 ultimaPosicionMonstruo;
    private Vector3 puntoDesvioTemporal;
    private float tiempoFinDesvio = 0f;

    // Estado de Vidas, Respawn y Efectos Visuales
    private int vidasActuales;
    private Vector3 posicionInicialJugador;
    private Quaternion rotacionInicialJugador;
    private Vector3 posicionInicialMonstruo;
    private PlayerPickup playerPickup;
    private CharacterController playerController;
    private Transform camaraJugador;
    private Vector3 posLocalOriginalCamara;
    private bool camaraInicializada = false;

    private bool enSecuenciaAtrapar = false;
    private float tiempoFinSecuenciaAtrapar = 0f;
    private float tiempoFinInmunidad = 0f;
    private float tiempoCartelAtrapar = 0f;
    private float intensidadFlashRojo = 0f;
    private bool juegoTerminado = false;

    public static bool UsarGameOverExterno = false;
    public bool JuegoTerminado => juegoTerminado;
    public int VidasActuales => vidasActuales;

    // Texturas generadas para la interfaz y efectos de pantalla
    private Texture2D texVinetaRoja;
    private Texture2D texFondoHUD;
    private Texture2D texBordeHUD;

    // Componentes del Aura de Pesadilla 3D
    private Light luzAura;
    private ParticleSystem sistemaHumo;
    private ParticleSystem sistemaBrasas;
    private ParticleSystem sistemaPulsoAura;
    private Renderer rendererMonstruo;
    private Color colorBaseOriginalMonstruo = Color.white;
    private float tiempoSiguientePulsoAura = 0f;

    void Start()
    {
        Time.timeScale = 1f;
        vidasActuales = vidasMaximas;
        posicionInicialMonstruo = transform.position;

        agente = GetComponent<NavMeshAgent>();
        agente.speed = velocidadTranquilo;
        agente.radius = 0.28f;
        agente.autoRepath = true;
        agente.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        radioPatrulla = Mathf.Max(radioPatrulla, 18.0f);
        ultimaPosicionMonstruo = transform.position;

        SphereCollider colMonstruo = GetComponent<SphereCollider>();
        if (colMonstruo != null)
        {
            colMonstruo.radius = 0.28f;
        }

        ActualizarNavMeshMapaCompleto();

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

        if (jugador != null)
        {
            posicionInicialJugador = jugador.position;
            rotacionInicialJugador = jugador.rotation;
            playerPickup = jugador.GetComponent<PlayerPickup>();
            playerController = jugador.GetComponent<CharacterController>();

            PlayerController ctrl = jugador.GetComponent<PlayerController>();
            if (ctrl != null && ctrl.camaraTransform != null)
            {
                camaraJugador = ctrl.camaraTransform;
            }
            else if (Camera.main != null)
            {
                camaraJugador = Camera.main.transform;
            }

            if (camaraJugador != null)
            {
                posLocalOriginalCamara = camaraJugador.localPosition;
                camaraInicializada = true;
            }
        }

        CrearTexturasEfectos();
        CrearAuraPesadilla();
        GenerarNuevoPuntoPatrulla();
    }

    void Update()
    {
        // Si perdió todas las vidas, permitir reiniciar con R o Espacio (si no lo maneja una UI externa)
        if (juegoTerminado)
        {
            if (!UsarGameOverExterno && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space)))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (jugador == null || detectorAgua == null) return;
        if (!agente.isOnNavMesh) return;

        // Desvanecer suavemente el flash rojo de susto
        if (intensidadFlashRojo > 0f)
        {
            intensidadFlashRojo = Mathf.MoveTowards(intensidadFlashRojo, 0f, Time.deltaTime * 0.75f);
        }

        // Si está en plena secuencia de susto/atrapado, esperar a que termine para hacer respawn
        if (enSecuenciaAtrapar)
        {
            ActualizarAuraPesadilla();
            Vector3 dirMirar = jugador.position - transform.position;
            dirMirar.y = 0f;
            if (dirMirar.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirMirar), Time.deltaTime * 10f);
            }

            if (Time.time >= tiempoFinSecuenciaAtrapar)
            {
                FinalizarSecuenciaAtrapar();
            }
            return;
        }

        // Comprobar si el monstruo alcanza al jugador (SOLO puede atraparte si estás dentro del agua, nunca en una plataforma)
        float distanciaAlJugador = Vector3.Distance(transform.position, jugador.position);
        bool tieneInmunidad = Time.time < tiempoFinInmunidad;

        if (!tieneInmunidad && detectorAgua.estaEnAgua && persiguiendo && distanciaAlJugador <= distanciaAtrapar)
        {
            IniciarSecuenciaAtrapar();
            return;
        }

        // ESTADO 1: El jugador pisó el AGUA (hace ruido -> el monstruo corre hacia él)
        if (detectorAgua.estaEnAgua && !tieneInmunidad)
        {
            if (!persiguiendo)
            {
                persiguiendo = true;
                investigando = false;
                agente.speed = velocidadCorriendo;
            }

            puntoRuido = jugador.position;

            // Actualizamos el destino buscando siempre la mejor calle o camino completo hacia el jugador
            if (Time.time >= proximaActualizacionDestino)
            {
                proximaActualizacionDestino = Time.time + 0.25f;

                if (Time.time < tiempoFinDesvio)
                {
                    if (Vector3.Distance(transform.position, puntoDesvioTemporal) <= 1.2f)
                    {
                        tiempoFinDesvio = 0f;
                        agente.SetDestination(ObtenerMejorDestinoAlcanzable(puntoRuido));
                    }
                    else
                    {
                        agente.SetDestination(puntoDesvioTemporal);
                    }
                }
                else
                {
                    agente.SetDestination(ObtenerMejorDestinoAlcanzable(puntoRuido));
                }
            }

            // Si una calle está bloqueada o el monstruo se frena contra una esquina/obstáculo, buscar ruta por otra calle
            VerificarYBuscarOtraCalle(puntoRuido);
        }
        // ESTADO 2: El jugador está a salvo
        else
        {
            if (persiguiendo)
            {
                // Dejó de oír ruido: va al último punto donde lo oyó a investigar por la mejor ruta disponible
                persiguiendo = false;
                investigando = true;
                agente.speed = velocidadTranquilo;
                tiempoFinInvestigacion = 0f;
                tiempoFinDesvio = 0f;
                agente.SetDestination(ObtenerMejorDestinoAlcanzable(puntoRuido));
            }

            if (investigando)
            {
                Investigar();
            }
            else
            {
                PatrullarLentamente();
            }
        }

        // Si el monstruo se está moviendo por el agua, genera ondas y salpicaduras
        if (agente.velocity.sqrMagnitude > 0.1f && Time.time >= tiempoSiguienteSalpicadura)
        {
            float intervalo = persiguiendo ? 0.18f : 0.42f;
            tiempoSiguienteSalpicadura = Time.time + intervalo;
            detectorAgua.EmitirSalpicaduraExterna(transform.position, persiguiendo);
        }

        ActualizarAuraPesadilla();
    }

    void LateUpdate()
    {
        if (!camaraInicializada || camaraJugador == null) return;

        // Sacudida de cámara (Camera Shake) cuando el monstruo te atrapa
        if (enSecuenciaAtrapar && !juegoTerminado)
        {
            Vector3 sacudida = Random.insideUnitSphere * intensidadSacudidaCamara;
            camaraJugador.localPosition = posLocalOriginalCamara + sacudida;
        }
        else
        {
            camaraJugador.localPosition = Vector3.Lerp(camaraJugador.localPosition, posLocalOriginalCamara, Time.deltaTime * 15f);
        }
    }

    private void IniciarSecuenciaAtrapar()
    {
        enSecuenciaAtrapar = true;
        tiempoFinSecuenciaAtrapar = Time.time + duracionSusto;
        vidasActuales = Mathf.Max(0, vidasActuales - 1);
        intensidadFlashRojo = 1.0f;
        tiempoCartelAtrapar = Time.time + 3.0f;

        if (agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }

        // Gran salpicadura al momento del ataque
        if (detectorAgua != null)
        {
            detectorAgua.EmitirSalpicaduraExterna(jugador.position, true);
            detectorAgua.EmitirSalpicaduraExterna(transform.position, true);
        }

        // Hacer que el jugador suelte la basura que lleva en la mano
        if (playerPickup != null && playerPickup.basuraEnMano != null)
        {
            playerPickup.SoltarBasura();
        }

        Debug.Log("¡El monstruo te atrapó! Vidas restantes: " + vidasActuales);
    }

    private void FinalizarSecuenciaAtrapar()
    {
        enSecuenciaAtrapar = false;

        if (camaraInicializada && camaraJugador != null)
        {
            camaraJugador.localPosition = posLocalOriginalCamara;
        }

        // Si se quedó sin vidas, activar pantalla de Game Over
        if (vidasActuales <= 0)
        {
            juegoTerminado = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            return;
        }

        // Teletransportar al jugador de regreso a la plataforma segura inicial
        if (playerController != null) playerController.enabled = false;
        jugador.position = posicionInicialJugador + Vector3.up * 0.15f;
        jugador.rotation = rotacionInicialJugador;
        if (playerController != null) playerController.enabled = true;

        detectorAgua.estaEnAgua = false;
        persiguiendo = false;
        investigando = false;
        tiempoFinInmunidad = Time.time + tiempoInmunidadRespawn;

        // Reanudar al monstruo y alejarlo si estaba muy cerca de la zona inicial
        if (agente.isOnNavMesh)
        {
            agente.isStopped = false;
            agente.speed = velocidadTranquilo;

            if (Vector3.Distance(transform.position, posicionInicialJugador) < 6.0f)
            {
                agente.Warp(PuntoValido(posicionInicialMonstruo));
            }

            GenerarNuevoPuntoPatrulla();
        }
    }

    // El monstruo llega al último ruido, espera unos segundos y luego vuelve a patrullar
    void Investigar()
    {
        bool llego = !agente.pathPending && agente.remainingDistance <= 0.5f;

        if (llego && tiempoFinInvestigacion == 0f)
        {
            tiempoFinInvestigacion = Time.time + tiempoInvestigando;
        }

        if (tiempoFinInvestigacion > 0f && Time.time >= tiempoFinInvestigacion)
        {
            investigando = false;
            GenerarNuevoPuntoPatrulla();
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

    // Ajusta un punto al lugar más cercano donde el monstruo sí puede caminar en todo el mapa
    Vector3 PuntoValido(Vector3 punto)
    {
        Vector3 puntoAgua = new Vector3(punto.x, 0.85f, punto.z);
        NavMeshHit hit;
        if (NavMesh.SamplePosition(puntoAgua, out hit, 10.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return puntoAgua;
    }

    // Reconstruye automáticamente el NavMesh sobre todo el plano de agua ampliado y registra los obstáculos del escenario
    private void ActualizarNavMeshMapaCompleto()
    {
        // Asegurar que las columnas, casas circulares y plataformas tengan NavMeshObstacle para que el monstruo las rodee por las calles
        string[] contenedoresObstaculos = { "Columnas", "CasasCirculares", "Plataformas" };
        foreach (string nombre in contenedoresObstaculos)
        {
            GameObject grupo = GameObject.Find(nombre);
            if (grupo != null)
            {
                Collider[] cols = grupo.GetComponentsInChildren<Collider>();
                foreach (Collider c in cols)
                {
                    if (c.isTrigger) continue;
                    if (c.GetComponent<NavMeshObstacle>() == null)
                    {
                        NavMeshObstacle obs = c.gameObject.AddComponent<NavMeshObstacle>();
                        obs.carving = true;
                        obs.carveOnlyStationary = true;
                    }
                }
            }
        }

        NavMeshSurface superficie = FindFirstObjectByType<NavMeshSurface>();
        if (superficie != null)
        {
            superficie.collectObjects = CollectObjects.Children;
            superficie.BuildNavMesh();
        }
    }

    // Evalúa múltiples puntos alrededor del objetivo y elige el camino que tenga una ruta abierta completa (sin cruzar paredes)
    private Vector3 ObtenerMejorDestinoAlcanzable(Vector3 objetivo)
    {
        Vector3 baseAgua = new Vector3(objetivo.x, 0.85f, objetivo.z);
        NavMeshPath mejorCamino = new NavMeshPath();
        Vector3 mejorPunto = PuntoValido(baseAgua);
        float mejorPuntaje = float.MaxValue;
        bool encontroCaminoCompleto = false;

        // Probamos el punto central y 12 puntos radiales alrededor (para no elegir el lado equivocado de una pared delgada)
        int totalCandidatos = 13;
        for (int i = 0; i < totalCandidatos; i++)
        {
            Vector3 offset = Vector3.zero;
            if (i > 0)
            {
                float angulo = (i - 1) * (360f / 12f) * Mathf.Deg2Rad;
                float radio = (i <= 6) ? 1.1f : 2.3f;
                offset = new Vector3(Mathf.Cos(angulo) * radio, 0f, Mathf.Sin(angulo) * radio);
            }

            Vector3 intento = baseAgua + offset;
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(intento, out hit, 4.0f, NavMesh.AllAreas)) continue;

            // Verificar que no haya una pared entre el punto candidato y el jugador
            Vector3 origenRayo = hit.position + Vector3.up * 0.5f;
            Vector3 destinoRayo = new Vector3(objetivo.x, hit.position.y + 0.5f, objetivo.z);
            bool hayParedEnMedio = Physics.Linecast(origenRayo, destinoRayo, out RaycastHit rayHit)
                && rayHit.transform != jugador && !rayHit.collider.CompareTag("Agua") && !rayHit.collider.CompareTag("Basura");

            NavMeshPath camino = new NavMeshPath();
            if (agente.CalculatePath(hit.position, camino) && camino.corners.Length > 0)
            {
                bool esCompleto = (camino.status == NavMeshPathStatus.PathComplete);
                float longitud = CalcularLongitudCamino(camino);
                float distFinalAlObjetivo = Vector3.Distance(camino.corners[camino.corners.Length - 1], baseAgua);

                // Priorizar rutas completas donde el monstruo puede llegar caminando por las calles sin bloqueos
                float penalizacionPared = hayParedEnMedio ? 25f : 0f;
                float penalizacionIncompleto = esCompleto ? 0f : (80f + distFinalAlObjetivo * 10f);
                float puntaje = longitud + distFinalAlObjetivo * 2f + penalizacionPared + penalizacionIncompleto;

                if (esCompleto && !encontroCaminoCompleto)
                {
                    encontroCaminoCompleto = true;
                    mejorPuntaje = puntaje;
                    mejorPunto = hit.position;
                }
                else if ((esCompleto == encontroCaminoCompleto) && puntaje < mejorPuntaje)
                {
                    mejorPuntaje = puntaje;
                    mejorPunto = hit.position;
                }
            }
        }

        return mejorPunto;
    }

    // Si una calle está cerrada o el monstruo se atasca, busca una calle alternativa (flanqueo izquierdo/derecho) para rodear el obstáculo
    private void VerificarYBuscarOtraCalle(Vector3 objetivo)
    {
        float avance = Vector3.Distance(transform.position, ultimaPosicionMonstruo);
        ultimaPosicionMonstruo = transform.position;

        bool avanzaMuyLento = avance < (agente.speed * Time.deltaTime * 0.22f);
        bool caminoIncompleto = !agente.pathPending && agente.pathStatus == NavMeshPathStatus.PathPartial && agente.remainingDistance <= 1.6f;

        if (avanzaMuyLento || caminoIncompleto)
        {
            tiempoAtascado += Time.deltaTime;
        }
        else
        {
            tiempoAtascado = Mathf.Max(0f, tiempoAtascado - Time.deltaTime * 2f);
        }

        if (tiempoAtascado < 0.45f && !caminoIncompleto) return;

        tiempoAtascado = 0f;
        Vector3 dirAlJugador = (objetivo - transform.position);
        dirAlJugador.y = 0f;
        if (dirAlJugador.sqrMagnitude < 0.1f) dirAlJugador = transform.forward;
        dirAlJugador.Normalize();

        // Probar calles laterales en distintos ángulos y distancias para rodear el bloqueo y llegar al jugador
        float[] angulosDesvio = { 45f, -45f, 80f, -80f, 115f, -115f, 150f, -150f };
        float[] distanciasDesvio = { 4.5f, 8.0f, 12.0f };

        Vector3 mejorDesvio = Vector3.zero;
        float mejorCosto = float.MaxValue;
        bool halloDesvio = false;

        foreach (float dist in distanciasDesvio)
        {
            foreach (float ang in angulosDesvio)
            {
                Vector3 dirDesvio = Quaternion.Euler(0f, ang, 0f) * dirAlJugador;
                Vector3 candidato = transform.position + dirDesvio * dist;
                candidato.y = 0.85f;

                NavMeshHit hit;
                if (!NavMesh.SamplePosition(candidato, out hit, 2.5f, NavMesh.AllAreas)) continue;

                NavMeshPath caminoAlDesvio = new NavMeshPath();
                if (!agente.CalculatePath(hit.position, caminoAlDesvio) || caminoAlDesvio.status != NavMeshPathStatus.PathComplete)
                    continue;

                // Comprobar qué tan bien conecta ese desvío con el jugador
                NavMeshPath caminoDesdeDesvio = new NavMeshPath();
                Vector3 destinoJugador = PuntoValido(objetivo);
                bool conecta = NavMesh.CalculatePath(hit.position, destinoJugador, NavMesh.AllAreas, caminoDesdeDesvio);

                float distFinal = (conecta && caminoDesdeDesvio.corners.Length > 0)
                    ? Vector3.Distance(caminoDesdeDesvio.corners[caminoDesdeDesvio.corners.Length - 1], destinoJugador)
                    : Vector3.Distance(hit.position, destinoJugador);

                float costo = CalcularLongitudCamino(caminoAlDesvio)
                    + (conecta ? CalcularLongitudCamino(caminoDesdeDesvio) : Vector3.Distance(hit.position, destinoJugador) * 1.5f)
                    + distFinal * 4f;

                if (costo < mejorCosto)
                {
                    mejorCosto = costo;
                    mejorDesvio = hit.position;
                    halloDesvio = true;
                }
            }
        }

        if (halloDesvio)
        {
            puntoDesvioTemporal = mejorDesvio;
            tiempoFinDesvio = Time.time + 1.8f;
            agente.SetDestination(puntoDesvioTemporal);
        }
    }

    private float CalcularLongitudCamino(NavMeshPath camino)
    {
        if (camino == null || camino.corners.Length < 2) return 0f;
        float total = 0f;
        for (int i = 1; i < camino.corners.Length; i++)
        {
            total += Vector3.Distance(camino.corners[i - 1], camino.corners[i]);
        }
        return total;
    }

    private void OnGUI()
    {
        if (jugador == null) return;

        // 1. Efecto de Viñeta Roja (cuando te persigue de cerca o cuando te atrapa)
        float alphaVineta = intensidadFlashRojo;
        if (persiguiendo && !juegoTerminado)
        {
            float dist = Vector3.Distance(transform.position, jugador.position);
            float factorCercania = Mathf.Clamp01(1f - (dist / 8.0f));
            float latido = 0.5f + 0.5f * Mathf.Sin(Time.time * (5f + factorCercania * 8f));
            alphaVineta = Mathf.Max(alphaVineta, factorCercania * (0.25f + 0.35f * latido));
        }

        if (alphaVineta > 0.01f && texVinetaRoja != null)
        {
            Color prev = GUI.color;
            GUI.color = new Color(1f, 0.08f, 0.08f, Mathf.Clamp01(alphaVineta));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texVinetaRoja, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        // 2. Panel Superior Derecho: Vidas y Estado de Alerta del Monstruo (ocultar mientras está pausado o en pantalla modal)
        if (Time.timeScale > 0f)
        {
            DibujarPanelHUD();
        }

        // 3. Aviso Central cuando el monstruo te atrapa
        if (Time.time < tiempoCartelAtrapar && !juegoTerminado && Time.timeScale > 0f)
        {
            DibujarCartelAtrapar();
        }

        // 4. Pantalla de Game Over si pierde las 3 vidas (cuando no se usa UI externa)
        if (juegoTerminado && !UsarGameOverExterno)
        {
            DibujarPantallaGameOver();
        }
    }

    private void DibujarPanelHUD()
    {
        float ancho = 260f;
        float alto = 76f;
        float x = Screen.width - ancho - 20f;
        float y = 20f;

        // Borde y fondo oscuro semitransparente
        Color colorBorde = persiguiendo
            ? new Color(1f, 0.2f, 0.2f, 0.9f)
            : (investigando ? new Color(1f, 0.75f, 0.2f, 0.85f) : new Color(0.25f, 0.75f, 0.95f, 0.7f));

        GUI.color = colorBorde;
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, ancho + 4f, alto + 4f), texBordeHUD);
        GUI.color = new Color(0.06f, 0.09f, 0.14f, 0.88f);
        GUI.DrawTexture(new Rect(x, y, ancho, alto), texFondoHUD);
        GUI.color = Color.white;

        // Estilo para los corazones de vida
        GUIStyle estiloCorazones = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };

        string textoCorazones = "VIDAS: ";
        for (int i = 0; i < vidasMaximas; i++)
        {
            if (i < vidasActuales)
                textoCorazones += "<color=#FF3344>♥</color> ";
            else
                textoCorazones += "<color=#555566>♡</color> ";
        }

        GUI.Label(new Rect(x, y + 6f, ancho, 34f), textoCorazones, estiloCorazones);

        // Estilo para el estado del monstruo
        GUIStyle estiloEstado = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };

        string estadoTexto;
        if (Time.time < tiempoFinInmunidad)
        {
            estadoTexto = "<color=#44EEFF>★ A SALVO (REAPARECIENDO)</color>";
        }
        else if (persiguiendo)
        {
            bool parpadeo = Mathf.FloorToInt(Time.time * 6f) % 2 == 0;
            estadoTexto = parpadeo
                ? "<color=#FF3333>⚠ ¡PELIGRO: TE PERSIGUE!</color>"
                : "<color=#FFAA33>⚠ ¡SAL DEL AGUA RÁPIDO!</color>";
        }
        else if (investigando)
        {
            estadoTexto = "<color=#FFCC33>▲ INVESTIGANDO ÚLTIMO RUIDO</color>";
        }
        else
        {
            estadoTexto = "<color=#77EE99>● MONSTRUO PATRULLANDO</color>";
        }

        GUI.Label(new Rect(x, y + 42f, ancho, 26f), estadoTexto, estiloEstado);
    }

    private void DibujarCartelAtrapar()
    {
        float factorTiempo = Mathf.Clamp01((tiempoCartelAtrapar - Time.time) / 3.0f);
        float alpha = Mathf.Clamp01(factorTiempo * 1.5f);

        float ancho = 500f;
        float alto = 95f;
        Rect rect = new Rect((Screen.width - ancho) * 0.5f, Screen.height * 0.22f, ancho, alto);

        GUI.color = new Color(0.55f, 0.05f, 0.05f, 0.85f * alpha);
        GUI.DrawTexture(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), texBordeHUD);
        GUI.color = new Color(0.12f, 0.02f, 0.02f, 0.88f * alpha);
        GUI.DrawTexture(rect, texFondoHUD);

        GUIStyle estiloTitulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };

        GUIStyle estiloSub = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(rect.x, rect.y + 10f, rect.width, 38f), "<color=#FF4444>¡EL MONSTRUO TE ATRAPÓ!</color>", estiloTitulo);
        GUI.Label(new Rect(rect.x, rect.y + 50f, rect.width, 32f),
            "Volviste a la zona segura  •  Vidas restantes: <b>" + vidasActuales + "</b>", estiloSub);
        GUI.color = Color.white;
    }

    private void DibujarPantallaGameOver()
    {
        GUI.color = new Color(0.08f, 0.01f, 0.01f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texFondoHUD);

        float ancho = 540f;
        float alto = 210f;
        Rect caja = new Rect((Screen.width - ancho) * 0.5f, (Screen.height - alto) * 0.5f, ancho, alto);

        GUI.color = new Color(0.9f, 0.15f, 0.15f, 0.95f);
        GUI.DrawTexture(new Rect(caja.x - 3f, caja.y - 3f, caja.width + 6f, caja.height + 6f), texBordeHUD);
        GUI.color = new Color(0.12f, 0.03f, 0.04f, 0.96f);
        GUI.DrawTexture(caja, texFondoHUD);
        GUI.color = Color.white;

        GUIStyle estiloTitulo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };

        GUIStyle estiloTexto = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };

        GUI.Label(new Rect(caja.x, caja.y + 26f, caja.width, 48f), "<color=#FF3333>¡HAS SIDO DERROTADO!</color>", estiloTitulo);
        GUI.Label(new Rect(caja.x, caja.y + 85f, caja.width, 36f), "El monstruo del agua agotó todas tus vidas.", estiloTexto);
        GUI.Label(new Rect(caja.x, caja.y + 138f, caja.width, 40f),
            "<color=#FFDD55><b>Presiona [R] o [ESPACIO] para Reiniciar la Partida</b></color>", estiloTexto);
    }

    private void CrearTexturasEfectos()
    {
        texFondoHUD = new Texture2D(1, 1);
        texFondoHUD.SetPixel(0, 0, Color.white);
        texFondoHUD.Apply();

        texBordeHUD = new Texture2D(1, 1);
        texBordeHUD.SetPixel(0, 0, Color.white);
        texBordeHUD.Apply();

        // Textura de viñeta roja (transparente al centro, intensa en los bordes)
        int res = 128;
        texVinetaRoja = new Texture2D(res, res, TextureFormat.RGBA32, false);
        texVinetaRoja.wrapMode = TextureWrapMode.Clamp;

        float centro = (res - 1) * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x - centro) / centro;
                float dy = (y - centro) / centro;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float v = Mathf.Clamp01((d - 0.45f) / 0.65f);
                v = v * v;
                texVinetaRoja.SetPixel(x, y, new Color(1f, 1f, 1f, v));
            }
        }
        texVinetaRoja.Apply();
    }

    // Construye el Aura de Pesadilla 3D del monstruo: Luz dinámica, humo sombrío, brasas flotantes y pulsos de energía
    private void CrearAuraPesadilla()
    {
        rendererMonstruo = GetComponentInChildren<Renderer>();
        if (rendererMonstruo != null && rendererMonstruo.material != null)
        {
            if (rendererMonstruo.material.HasProperty("_BaseColor"))
                colorBaseOriginalMonstruo = rendererMonstruo.material.GetColor("_BaseColor");
            else if (rendererMonstruo.material.HasProperty("_Color"))
                colorBaseOriginalMonstruo = rendererMonstruo.material.color;

            rendererMonstruo.material.EnableKeyword("_EMISSION");
        }

        GameObject contenedorAura = new GameObject("AuraPesadilla_Monstruo");
        contenedorAura.transform.SetParent(transform, false);
        contenedorAura.transform.localPosition = Vector3.zero;

        // 1. Luz 3D Dinámica que tiñe la niebla y el agua alrededor del monstruo
        GameObject objLuz = new GameObject("LuzAuraMonstruo");
        objLuz.transform.SetParent(contenedorAura.transform, false);
        objLuz.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        luzAura = objLuz.AddComponent<Light>();
        luzAura.type = LightType.Point;
        luzAura.range = 10.5f;
        luzAura.intensity = intensidadLuzPatrulla;
        luzAura.color = colorAuraPatrulla;
        luzAura.shadows = LightShadows.Soft;

        Shader shaderParticulas = Shader.Find("Sprites/Default");
        if (shaderParticulas == null)
        {
            shaderParticulas = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        // 2. Sistema de Miasma / Humo Oscuro que se desprende del cuerpo del monstruo
        GameObject objHumo = new GameObject("ParticulasHumoOscuro");
        objHumo.transform.SetParent(contenedorAura.transform, false);
        objHumo.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        sistemaHumo = objHumo.AddComponent<ParticleSystem>();
        sistemaHumo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainHumo = sistemaHumo.main;
        mainHumo.playOnAwake = false;
        mainHumo.loop = true;
        mainHumo.simulationSpace = ParticleSystemSimulationSpace.World;
        mainHumo.scalingMode = ParticleSystemScalingMode.Local;
        mainHumo.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
        mainHumo.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
        mainHumo.startSize = new ParticleSystem.MinMaxCurve(0.65f, 1.35f);
        mainHumo.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        mainHumo.gravityModifier = -0.15f; // Sube como humo denso
        mainHumo.startColor = new Color(0.05f, 0.08f, 0.14f, 0.55f);
        mainHumo.maxParticles = 200;

        var emisionHumo = sistemaHumo.emission;
        emisionHumo.enabled = true;
        emisionHumo.rateOverTime = 16f;

        var formaHumo = sistemaHumo.shape;
        formaHumo.enabled = true;
        formaHumo.shapeType = ParticleSystemShapeType.Sphere;
        formaHumo.radius = 0.45f;

        var ruidoHumo = sistemaHumo.noise;
        ruidoHumo.enabled = true;
        ruidoHumo.strength = 0.35f;
        ruidoHumo.frequency = 0.5f;
        ruidoHumo.scrollSpeed = 0.3f;
        ruidoHumo.damping = true;

        var tamHumo = sistemaHumo.sizeOverLifetime;
        tamHumo.enabled = true;
        AnimationCurve curvaTamHumo = new AnimationCurve();
        curvaTamHumo.AddKey(0.0f, 0.4f);
        curvaTamHumo.AddKey(0.5f, 1.0f);
        curvaTamHumo.AddKey(1.0f, 1.45f);
        tamHumo.size = new ParticleSystem.MinMaxCurve(1f, curvaTamHumo);

        var colorHumo = sistemaHumo.colorOverLifetime;
        colorHumo.enabled = true;
        Gradient gradHumo = new Gradient();
        gradHumo.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.75f, 0.25f),
                new GradientAlphaKey(0.45f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorHumo.color = gradHumo;

        var renderHumo = objHumo.GetComponent<ParticleSystemRenderer>();
        renderHumo.renderMode = ParticleSystemRenderMode.Billboard;
        Material matHumo = new Material(shaderParticulas);
        matHumo.mainTexture = CrearTexturaHumoSuave();
        renderHumo.material = matHumo;
        sistemaHumo.Play();

        // 3. Sistema de Brasas / Chispas Flotantes de Furia
        GameObject objBrasas = new GameObject("ParticulasBrasasPesadilla");
        objBrasas.transform.SetParent(contenedorAura.transform, false);
        objBrasas.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        sistemaBrasas = objBrasas.AddComponent<ParticleSystem>();
        sistemaBrasas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainBrasas = sistemaBrasas.main;
        mainBrasas.playOnAwake = false;
        mainBrasas.loop = true;
        mainBrasas.simulationSpace = ParticleSystemSimulationSpace.World;
        mainBrasas.scalingMode = ParticleSystemScalingMode.Local;
        mainBrasas.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        mainBrasas.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
        mainBrasas.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        mainBrasas.gravityModifier = -0.35f; // Ascienden como chispas ardientes
        mainBrasas.startColor = colorAuraPatrulla;
        mainBrasas.maxParticles = 300;

        var emisionBrasas = sistemaBrasas.emission;
        emisionBrasas.enabled = true;
        emisionBrasas.rateOverTime = 18f;

        var formaBrasas = sistemaBrasas.shape;
        formaBrasas.enabled = true;
        formaBrasas.shapeType = ParticleSystemShapeType.Sphere;
        formaBrasas.radius = 0.55f;

        var ruidoBrasas = sistemaBrasas.noise;
        ruidoBrasas.enabled = true;
        ruidoBrasas.strength = 0.55f;
        ruidoBrasas.frequency = 0.8f;
        ruidoBrasas.scrollSpeed = 0.5f;
        ruidoBrasas.damping = true;

        var tamBrasas = sistemaBrasas.sizeOverLifetime;
        tamBrasas.enabled = true;
        AnimationCurve curvaTamBrasas = new AnimationCurve();
        curvaTamBrasas.AddKey(0.0f, 0.5f);
        curvaTamBrasas.AddKey(0.3f, 1.0f);
        curvaTamBrasas.AddKey(1.0f, 0.0f);
        tamBrasas.size = new ParticleSystem.MinMaxCurve(1f, curvaTamBrasas);

        var colorBrasas = sistemaBrasas.colorOverLifetime;
        colorBrasas.enabled = true;
        Gradient gradBrasas = new Gradient();
        gradBrasas.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0.85f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorBrasas.color = gradBrasas;

        var renderBrasas = objBrasas.GetComponent<ParticleSystemRenderer>();
        renderBrasas.renderMode = ParticleSystemRenderMode.Billboard;
        Material matBrasas = new Material(shaderParticulas);
        matBrasas.mainTexture = CrearTexturaChispaBrillante();
        renderBrasas.material = matBrasas;
        sistemaBrasas.Play();

        // 4. Sistema de Anillo de Pulso en el Suelo/Agua alrededor del monstruo
        GameObject objPulso = new GameObject("ParticulasPulsoTerror");
        objPulso.transform.SetParent(contenedorAura.transform, false);
        objPulso.transform.localPosition = Vector3.zero;
        sistemaPulsoAura = objPulso.AddComponent<ParticleSystem>();
        sistemaPulsoAura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainPulso = sistemaPulsoAura.main;
        mainPulso.playOnAwake = false;
        mainPulso.loop = false;
        mainPulso.simulationSpace = ParticleSystemSimulationSpace.World;
        mainPulso.scalingMode = ParticleSystemScalingMode.Local;
        mainPulso.startLifetime = 0.9f;
        mainPulso.startSpeed = 0f;
        mainPulso.startSize = 2.4f;
        mainPulso.gravityModifier = 0f;
        mainPulso.startColor = colorAuraPatrulla;
        mainPulso.maxParticles = 40;

        var emisionPulso = sistemaPulsoAura.emission;
        emisionPulso.enabled = false;

        var formaPulso = sistemaPulsoAura.shape;
        formaPulso.enabled = false;

        var tamPulso = sistemaPulsoAura.sizeOverLifetime;
        tamPulso.enabled = true;
        AnimationCurve curvaTamPulso = new AnimationCurve();
        curvaTamPulso.AddKey(0.0f, 0.18f);
        curvaTamPulso.AddKey(0.6f, 0.75f);
        curvaTamPulso.AddKey(1.0f, 1.0f);
        tamPulso.size = new ParticleSystem.MinMaxCurve(1f, curvaTamPulso);

        var colorPulso = sistemaPulsoAura.colorOverLifetime;
        colorPulso.enabled = true;
        Gradient gradPulso = new Gradient();
        gradPulso.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.15f),
                new GradientAlphaKey(0.35f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorPulso.color = gradPulso;

        var renderPulso = objPulso.GetComponent<ParticleSystemRenderer>();
        renderPulso.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        Material matPulso = new Material(shaderParticulas);
        matPulso.mainTexture = CrearTexturaAnilloAura();
        renderPulso.material = matPulso;
    }

    // Actualiza en tiempo real la luz, el humo, las brasas y los anillos de energía según el estado del monstruo
    private void ActualizarAuraPesadilla()
    {
        bool modoFuria = persiguiendo || enSecuenciaAtrapar;

        Color colorObjetivo = modoFuria
            ? colorAuraPersecucion
            : (investigando ? colorAuraInvestigando : colorAuraPatrulla);

        // 1. Actualizar Luz 3D con latido dinámico
        if (luzAura != null)
        {
            luzAura.color = Color.Lerp(luzAura.color, colorObjetivo, Time.deltaTime * 6f);

            float frecLatido = modoFuria ? 11f : (investigando ? 5f : 2.2f);
            float onda = 0.5f + 0.5f * Mathf.Sin(Time.time * frecLatido);
            float intBase = modoFuria ? intensidadLuzPersecucion : (investigando ? 4.2f : intensidadLuzPatrulla);
            float intExtra = modoFuria ? 3.2f * onda : 0.8f * onda;

            luzAura.intensity = Mathf.Lerp(luzAura.intensity, intBase + intExtra, Time.deltaTime * 8f);
            luzAura.range = Mathf.Lerp(luzAura.range, modoFuria ? 14f : 10f, Time.deltaTime * 4f);
        }

        // 2. Actualizar Brillo Emisivo en el cuerpo del monstruo
        if (rendererMonstruo != null && rendererMonstruo.material != null)
        {
            Color colorCuerpo = modoFuria
                ? Color.Lerp(colorBaseOriginalMonstruo, new Color(1f, 0.15f, 0.15f), 0.7f)
                : (investigando ? Color.Lerp(colorBaseOriginalMonstruo, colorAuraInvestigando, 0.45f) : colorBaseOriginalMonstruo);

            if (rendererMonstruo.material.HasProperty("_BaseColor"))
                rendererMonstruo.material.SetColor("_BaseColor", colorCuerpo);
            else if (rendererMonstruo.material.HasProperty("_Color"))
                rendererMonstruo.material.color = colorCuerpo;

            if (rendererMonstruo.material.HasProperty("_EmissionColor"))
            {
                float fuerzaEmision = modoFuria ? 2.2f : (investigando ? 1.1f : 0.5f);
                rendererMonstruo.material.SetColor("_EmissionColor", colorObjetivo * fuerzaEmision);
            }
        }

        // 3. Actualizar Humo Oscuro / Miasma
        if (sistemaHumo != null)
        {
            var mainHumo = sistemaHumo.main;
            mainHumo.startColor = modoFuria
                ? new ParticleSystem.MinMaxGradient(new Color(0.18f, 0.02f, 0.03f, 0.75f), new Color(0.45f, 0.04f, 0.06f, 0.65f))
                : (investigando
                    ? new ParticleSystem.MinMaxGradient(new Color(0.22f, 0.12f, 0.03f, 0.55f), new Color(0.12f, 0.08f, 0.04f, 0.5f))
                    : new ParticleSystem.MinMaxGradient(new Color(0.05f, 0.10f, 0.16f, 0.48f), new Color(0.08f, 0.18f, 0.25f, 0.42f)));

            var emisionHumo = sistemaHumo.emission;
            emisionHumo.rateOverTime = modoFuria ? 40f : (investigando ? 24f : 15f);
        }

        // 4. Actualizar Brasas / Chispas de Furia
        if (sistemaBrasas != null)
        {
            var mainBrasas = sistemaBrasas.main;
            if (modoFuria)
            {
                mainBrasas.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.12f, 0.12f, 1f),
                    new Color(1f, 0.65f, 0.15f, 1f)
                );
                mainBrasas.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 3.0f);
            }
            else if (investigando)
            {
                mainBrasas.startColor = new ParticleSystem.MinMaxGradient(
                    colorAuraInvestigando,
                    new Color(1f, 0.85f, 0.3f, 0.95f)
                );
                mainBrasas.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            }
            else
            {
                mainBrasas.startColor = new ParticleSystem.MinMaxGradient(
                    colorAuraPatrulla,
                    new Color(0.65f, 0.92f, 1f, 0.85f)
                );
                mainBrasas.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.3f);
            }

            var emisionBrasas = sistemaBrasas.emission;
            emisionBrasas.rateOverTime = modoFuria ? 70f : (investigando ? 32f : 16f);
        }

        // 5. Emitir Anillos de Pulso en el Suelo/Agua bajo el monstruo
        if (sistemaPulsoAura != null && Time.time >= tiempoSiguientePulsoAura)
        {
            float intervaloPulso = modoFuria ? 0.32f : (investigando ? 0.65f : 1.15f);
            tiempoSiguientePulsoAura = Time.time + intervaloPulso;

            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
            {
                position = new Vector3(transform.position.x, 0.875f, transform.position.z),
                startSize = modoFuria ? 3.2f : (investigando ? 2.4f : 1.9f),
                startLifetime = modoFuria ? 0.75f : 1.05f,
                startColor = new Color(colorObjetivo.r, colorObjetivo.g, colorObjetivo.b, modoFuria ? 0.85f : 0.55f)
            };
            sistemaPulsoAura.Emit(ep, 1);
        }
    }

    private Texture2D CrearTexturaHumoSuave()
    {
        int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float centro = (res - 1) * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x - centro) / centro;
                float dy = (y - centro) / centro;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float f = 1f - (d * d);
                float alpha = f * f * f * 0.75f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private Texture2D CrearTexturaChispaBrillante()
    {
        int res = 64;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float centro = (res - 1) * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x - centro) / centro;
                float dy = (y - centro) / centro;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float nucleo = Mathf.Exp(-d * d * 8f);
                float halo = Mathf.Pow(1f - d, 2.2f);
                float alpha = Mathf.Clamp01(nucleo + halo * 0.65f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private Texture2D CrearTexturaAnilloAura()
    {
        int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float centro = (res - 1) * 0.5f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x - centro) / centro;
                float dy = (y - centro) / centro;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float anillo = Mathf.Exp(-Mathf.Pow((d - 0.80f) / 0.08f, 2f));
                float haloInterno = 0.35f * Mathf.Exp(-Mathf.Pow((d - 0.52f) / 0.12f, 2f));
                float borde = Mathf.Clamp01((1f - d) * 8f);
                float alpha = Mathf.Clamp01((anillo + haloInterno) * borde);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}