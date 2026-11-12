using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

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

    private NavMeshAgent agente;
    private bool persiguiendo = false;
    private bool investigando = false;
    private float tiempoFinInvestigacion = 0f;
    private float tiempoSiguientePunto = 0f;
    private float proximaActualizacionDestino = 0f;
    private float tiempoSiguienteSalpicadura = 0f;

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

    // Texturas generadas para la interfaz y efectos de pantalla
    private Texture2D texVinetaRoja;
    private Texture2D texFondoHUD;
    private Texture2D texBordeHUD;

    void Start()
    {
        Time.timeScale = 1f;
        vidasActuales = vidasMaximas;
        posicionInicialMonstruo = transform.position;

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
        GenerarNuevoPuntoPatrulla();
    }

    void Update()
    {
        // Si perdió todas las vidas, permitir reiniciar con R o Espacio
        if (juegoTerminado)
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space))
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

        // Comprobar si el monstruo alcanza al jugador
        float distanciaAlJugador = Vector3.Distance(transform.position, jugador.position);
        bool tieneInmunidad = Time.time < tiempoFinInmunidad;

        if (!tieneInmunidad && (persiguiendo || investigando) && distanciaAlJugador <= distanciaAtrapar)
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

            // Actualizamos el destino 5 veces por segundo, no en cada fotograma
            if (Time.time >= proximaActualizacionDestino)
            {
                proximaActualizacionDestino = Time.time + 0.2f;
                agente.SetDestination(PuntoValido(puntoRuido));
            }
        }
        // ESTADO 2: El jugador está a salvo
        else
        {
            if (persiguiendo)
            {
                // Dejó de oír ruido: va al último punto donde lo oyó a investigar
                persiguiendo = false;
                investigando = true;
                agente.speed = velocidadTranquilo;
                tiempoFinInvestigacion = 0f;
                agente.SetDestination(PuntoValido(puntoRuido));
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

        // 2. Panel Superior Derecho: Vidas y Estado de Alerta del Monstruo
        DibujarPanelHUD();

        // 3. Aviso Central cuando el monstruo te atrapa
        if (Time.time < tiempoCartelAtrapar && !juegoTerminado)
        {
            DibujarCartelAtrapar();
        }

        // 4. Pantalla de Game Over si pierde las 3 vidas
        if (juegoTerminado)
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
}