using UnityEngine;

public class DetectorAgua : MonoBehaviour
{
    [Header("Estado actual")]
    public bool estaEnAgua = false;

    [Header("Configuración de Salpicaduras y Partículas")]
    [Tooltip("Color principal de las gotas y espuma de agua")]
    public Color colorGotas = new Color(0.82f, 0.94f, 1.0f, 0.9f);
    [Tooltip("Color de las ondas circulares en la superficie del agua")]
    public Color colorOndas = new Color(0.75f, 0.90f, 1.0f, 0.65f);
    [Tooltip("Intervalo entre salpicaduras al caminar en el agua")]
    public float intervaloPasoCaminando = 0.26f;
    [Tooltip("Intervalo entre salpicaduras al correr en el agua")]
    public float intervaloPasoCorriendo = 0.13f;

    [Header("Atmósfera: Niebla y Partículas Flotantes")]
    [Tooltip("Densidad de la niebla cuando estás a salvo en las plataformas")]
    public float densidadNieblaBase = 0.028f;
    [Tooltip("Densidad de la niebla cuando pisas el agua y hay peligro")]
    public float densidadNieblaPeligro = 0.042f;
    [Tooltip("Color ambiental de la niebla en noche de tormenta")]
    public Color colorNieblaNormal = new Color(0.14f, 0.20f, 0.30f, 1f);
    [Tooltip("Color ambiental de la niebla cuando pisas el agua")]
    public Color colorNieblaPeligro = new Color(0.10f, 0.14f, 0.23f, 1f);

    [Header("Noche de Tormenta, Lluvia y Relámpagos")]
    public Color colorLuzLuna = new Color(0.48f, 0.62f, 0.84f, 1f);
    public Color colorRelampago = new Color(0.82f, 0.92f, 1.0f, 1f);
    public float intensidadLuzLuna = 0.78f;
    public float intensidadMaxRelampago = 5.8f;
    public float tiempoMinRelampago = 7.0f;
    public float tiempoMaxRelampago = 14.0f;

    private float alturaSuperficieAgua = 0.85f;
    private Vector3 ultimaPosicionXZ;
    private float tiempoSiguienteSalpicadura = 0f;
    private float tiempoSiguienteOndaReposo = 0f;
    private float ultimoTiempoEnAire = 0f;

    private ParticleSystem sistemaGotas;
    private ParticleSystem sistemaOndas;
    private ParticleSystem sistemaMotasNiebla;
    private ParticleSystem sistemaBrumaBaja;
    private ParticleSystem sistemaLluvia;
    private Material materialGotas;
    private Material materialOndas;
    private Material materialBruma;

    // Estado de la Tormenta y Relámpagos
    private Light luzDireccional;
    private Camera camaraPrincipal;
    private LineRenderer rayoPrincipal;
    private LineRenderer rayoRama;
    private AudioSource fuenteTrueno;
    private AudioClip clipTrueno;
    private float tiempoSiguienteRelampago = 0f;
    private float tiempoInicioRelampago = -10f;
    private float duracionRelampagoActual = 0.45f;
    private float tiempoSiguienteGotaLluviaOnda = 0f;

    void Start()
    {
        GameObject objetoAgua = GameObject.FindGameObjectWithTag("Agua");
        if (objetoAgua != null)
        {
            alturaSuperficieAgua = objetoAgua.transform.position.y;
        }

        // Configurar la niebla ambiental inicial
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = densidadNieblaBase;
        RenderSettings.fogColor = colorNieblaNormal;

        ultimaPosicionXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        CrearSistemasDeParticulas();
        CrearSistemasNieblaFlotante();
        InicializarTormentaNocturna();
    }

    void Update()
    {
        ActualizarAtmosferaNiebla();
        ActualizarTormentaYRelampagos();

        Vector3 posActualXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        float distanciaXZ = Vector3.Distance(posActualXZ, ultimaPosicionXZ);
        float velocidadHorizontal = Time.deltaTime > 0.0001f ? distanciaXZ / Time.deltaTime : 0f;
        ultimaPosicionXZ = posActualXZ;

        if (!estaEnAgua) return;

        Vector3 puntoPiesAgua = new Vector3(transform.position.x, alturaSuperficieAgua, transform.position.z);

        // Si el jugador se está moviendo dentro del agua, genera salpicaduras y ondas según su velocidad
        if (velocidadHorizontal > 0.15f)
        {
            bool corriendo = Input.GetKey(KeyCode.LeftShift) || velocidadHorizontal > 2.6f;
            float intervalo = corriendo ? intervaloPasoCorriendo : intervaloPasoCaminando;

            if (Time.time >= tiempoSiguienteSalpicadura)
            {
                tiempoSiguienteSalpicadura = Time.time + intervalo;
                int cantidadGotas = corriendo ? 16 : 8;
                float tamanoOnda = corriendo ? 0.95f : 0.65f;
                float velocidadGotas = corriendo ? 2.4f : 1.6f;

                EmitirSalpicadura(puntoPiesAgua, cantidadGotas, velocidadGotas, tamanoOnda);
            }
        }
        else
        {
            // Si está quieto en el agua, emite ondas suaves de reposo alrededor
            if (Time.time >= tiempoSiguienteOndaReposo)
            {
                tiempoSiguienteOndaReposo = Time.time + 1.1f;
                EmitirOnda(puntoPiesAgua, 0.45f, 1.1f);
            }
        }
    }

    // Mantiene las partículas flotantes alrededor del jugador y ajusta suavemente la densidad de la niebla
    private void ActualizarAtmosferaNiebla()
    {
        float objetivoDensidad = estaEnAgua ? densidadNieblaPeligro : densidadNieblaBase;
        float respiracionNiebla = Mathf.Sin(Time.time * 0.45f) * 0.004f;
        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, objetivoDensidad + respiracionNiebla, Time.deltaTime * 1.8f);

        Color objetivoColor = estaEnAgua ? colorNieblaPeligro : colorNieblaNormal;
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, objetivoColor, Time.deltaTime * 1.8f);

        if (sistemaMotasNiebla != null)
        {
            sistemaMotasNiebla.transform.position = new Vector3(transform.position.x, alturaSuperficieAgua + 2.2f, transform.position.z);
        }

        if (sistemaBrumaBaja != null)
        {
            sistemaBrumaBaja.transform.position = new Vector3(transform.position.x, alturaSuperficieAgua + 0.55f, transform.position.z);
        }
    }

    // Se ejecuta cuando los pies/cuerpo del jugador entran en contacto con el agua o plataformas
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Agua"))
        {
            alturaSuperficieAgua = hit.point.y;
            Vector3 puntoContacto = new Vector3(transform.position.x, alturaSuperficieAgua, transform.position.z);

            if (!estaEnAgua)
            {
                estaEnAgua = true;
                Debug.Log("¡CUIDADO! Has pisado el agua. El monstruo escuchó el ruido.");

                // Salpicadura fuerte al caer o entrar al agua y relámpago dramático
                EmitirSalpicadura(puntoContacto, 30, 2.9f, 1.25f);
                EmitirOnda(puntoContacto, 0.85f, 1.3f);
                DispararRelampago();
                tiempoSiguienteSalpicadura = Time.time + 0.2f;
            }
            else if (Time.time - ultimoTiempoEnAire < 0.15f)
            {
                // Si saltó dentro del agua y volvió a caer
                EmitirSalpicadura(puntoContacto, 26, 2.7f, 1.15f);
                ultimoTiempoEnAire = -1f;
            }
        }
        else if (hit.gameObject.CompareTag("SueloSeguro") || hit.gameObject.CompareTag("Untagged"))
        {
            if (estaEnAgua)
            {
                estaEnAgua = false;
                Debug.Log("¡A salvo! Estás sobre una plataforma segura.");
            }
        }

        if (Input.GetButtonDown("Jump"))
        {
            ultimoTiempoEnAire = Time.time;
        }
    }

    // Permite que otros sistemas (como el Monstruo) generen salpicaduras al caminar por el agua
    public void EmitirSalpicaduraExterna(Vector3 posicionMundo, bool esFuerte)
    {
        Vector3 puntoEnAgua = new Vector3(posicionMundo.x, alturaSuperficieAgua, posicionMundo.z);
        int gotas = esFuerte ? 14 : 6;
        float velGotas = esFuerte ? 2.3f : 1.4f;
        float tamOnda = esFuerte ? 1.1f : 0.7f;

        EmitirSalpicadura(puntoEnAgua, gotas, velGotas, tamOnda);
    }

    private void EmitirSalpicadura(Vector3 posicionAgua, int cantidadGotas, float velocidadMax, float tamanoOnda)
    {
        if (sistemaGotas != null)
        {
            sistemaGotas.transform.position = posicionAgua + Vector3.up * 0.03f;
            var main = sistemaGotas.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(velocidadMax * 0.55f, velocidadMax);
            sistemaGotas.Emit(cantidadGotas);
        }

        EmitirOnda(posicionAgua, tamanoOnda, Random.Range(0.85f, 1.15f));
    }

    private void EmitirOnda(Vector3 posicionAgua, float tamanoFinal, float duracion)
    {
        if (sistemaOndas == null) return;

        ParticleSystem.EmitParams parametros = new ParticleSystem.EmitParams
        {
            position = posicionAgua + Vector3.up * 0.02f,
            startSize = tamanoFinal,
            startLifetime = duracion,
            startColor = colorOndas
        };

        sistemaOndas.Emit(parametros, 1);
    }

    private void CrearSistemasDeParticulas()
    {
        GameObject contenedor = new GameObject("EfectosAgua_Particulas");

        Shader shaderParticulas = Shader.Find("Sprites/Default");
        if (shaderParticulas == null)
        {
            shaderParticulas = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        // 1. Sistema de Gotas y Espuma 3D
        GameObject objGotas = new GameObject("ParticulasGotasAgua");
        objGotas.transform.SetParent(contenedor.transform);
        sistemaGotas = objGotas.AddComponent<ParticleSystem>();

        var mainGotas = sistemaGotas.main;
        mainGotas.playOnAwake = false;
        mainGotas.loop = false;
        mainGotas.simulationSpace = ParticleSystemSimulationSpace.World;
        mainGotas.scalingMode = ParticleSystemScalingMode.Local;
        mainGotas.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        mainGotas.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        mainGotas.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.08f);
        mainGotas.gravityModifier = 1.25f;
        mainGotas.startColor = new ParticleSystem.MinMaxGradient(
            colorGotas,
            new Color(1f, 1f, 1f, 0.95f)
        );
        mainGotas.maxParticles = 400;

        var emisionGotas = sistemaGotas.emission;
        emisionGotas.enabled = false;

        var formaGotas = sistemaGotas.shape;
        formaGotas.enabled = true;
        formaGotas.shapeType = ParticleSystemShapeType.Cone;
        formaGotas.angle = 36f;
        formaGotas.radius = 0.12f;
        formaGotas.rotation = new Vector3(-90f, 0f, 0f); // Apunta hacia arriba

        var tamanoGotas = sistemaGotas.sizeOverLifetime;
        tamanoGotas.enabled = true;
        AnimationCurve curvaTamanoGotas = new AnimationCurve();
        curvaTamanoGotas.AddKey(0.0f, 0.9f);
        curvaTamanoGotas.AddKey(0.4f, 1.0f);
        curvaTamanoGotas.AddKey(1.0f, 0.15f);
        tamanoGotas.size = new ParticleSystem.MinMaxCurve(1f, curvaTamanoGotas);

        var colorTiempoGotas = sistemaGotas.colorOverLifetime;
        colorTiempoGotas.enabled = true;
        Gradient gradienteGotas = new Gradient();
        gradienteGotas.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(colorGotas, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.75f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorTiempoGotas.color = gradienteGotas;

        var renderGotas = objGotas.GetComponent<ParticleSystemRenderer>();
        renderGotas.renderMode = ParticleSystemRenderMode.Billboard;
        materialGotas = new Material(shaderParticulas);
        materialGotas.mainTexture = CrearTexturaGota();
        renderGotas.material = materialGotas;

        // 2. Sistema de Ondas Circulares en la Superficie
        GameObject objOndas = new GameObject("ParticulasOndasAgua");
        objOndas.transform.SetParent(contenedor.transform);
        sistemaOndas = objOndas.AddComponent<ParticleSystem>();

        var mainOndas = sistemaOndas.main;
        mainOndas.playOnAwake = false;
        mainOndas.loop = false;
        mainOndas.simulationSpace = ParticleSystemSimulationSpace.World;
        mainOndas.scalingMode = ParticleSystemScalingMode.Local;
        mainOndas.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.2f);
        mainOndas.startSpeed = 0f;
        mainOndas.startSize = 0.8f;
        mainOndas.gravityModifier = 0f;
        mainOndas.startColor = colorOndas;
        mainOndas.maxParticles = 150;

        var emisionOndas = sistemaOndas.emission;
        emisionOndas.enabled = false;

        var formaOndas = sistemaOndas.shape;
        formaOndas.enabled = false;

        var tamanoOndas = sistemaOndas.sizeOverLifetime;
        tamanoOndas.enabled = true;
        AnimationCurve curvaTamanoOndas = new AnimationCurve();
        curvaTamanoOndas.AddKey(0.0f, 0.20f);
        curvaTamanoOndas.AddKey(0.5f, 0.72f);
        curvaTamanoOndas.AddKey(1.0f, 1.00f);
        tamanoOndas.size = new ParticleSystem.MinMaxCurve(1f, curvaTamanoOndas);

        var colorTiempoOndas = sistemaOndas.colorOverLifetime;
        colorTiempoOndas.enabled = true;
        Gradient gradienteOndas = new Gradient();
        gradienteOndas.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.15f),
                new GradientAlphaKey(0.4f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorTiempoOndas.color = gradienteOndas;

        var renderOndas = objOndas.GetComponent<ParticleSystemRenderer>();
        renderOndas.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        materialOndas = new Material(shaderParticulas);
        materialOndas.mainTexture = CrearTexturaOnda();
        renderOndas.material = materialOndas;
    }

    // Crea motas etéreas suspendidas en el aire y bancos de bruma suave a ras del agua
    private void CrearSistemasNieblaFlotante()
    {
        GameObject contenedorNiebla = new GameObject("EfectosNiebla_Atmosfera");

        Shader shaderParticulas = Shader.Find("Sprites/Default");
        if (shaderParticulas == null)
        {
            shaderParticulas = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        // 1. Motas / Partículas Etéreas Flotando en la Niebla
        GameObject objMotas = new GameObject("ParticulasMotasNiebla");
        objMotas.transform.SetParent(contenedorNiebla.transform);
        objMotas.transform.position = new Vector3(transform.position.x, alturaSuperficieAgua + 2.2f, transform.position.z);
        sistemaMotasNiebla = objMotas.AddComponent<ParticleSystem>();
        sistemaMotasNiebla.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainMotas = sistemaMotasNiebla.main;
        mainMotas.playOnAwake = false;
        mainMotas.loop = true;
        mainMotas.simulationSpace = ParticleSystemSimulationSpace.World;
        mainMotas.scalingMode = ParticleSystemScalingMode.Local;
        mainMotas.startLifetime = new ParticleSystem.MinMaxCurve(5.5f, 9.0f);
        mainMotas.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
        mainMotas.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.085f);
        mainMotas.gravityModifier = -0.01f; // Flotan suavemente
        mainMotas.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.85f, 0.94f, 1.0f, 0.75f),
            new Color(0.68f, 0.84f, 0.96f, 0.55f)
        );
        mainMotas.maxParticles = 350;

        var emisionMotas = sistemaMotasNiebla.emission;
        emisionMotas.enabled = true;
        emisionMotas.rateOverTime = 42f;

        var formaMotas = sistemaMotasNiebla.shape;
        formaMotas.enabled = true;
        formaMotas.shapeType = ParticleSystemShapeType.Box;
        formaMotas.scale = new Vector3(22f, 5.5f, 22f);

        var velMotas = sistemaMotasNiebla.velocityOverLifetime;
        velMotas.enabled = true;
        velMotas.space = ParticleSystemSimulationSpace.World;
        velMotas.x = new ParticleSystem.MinMaxCurve(-0.20f, 0.20f);
        velMotas.y = new ParticleSystem.MinMaxCurve(-0.04f, 0.12f);
        velMotas.z = new ParticleSystem.MinMaxCurve(-0.20f, 0.20f);

        var ruidoMotas = sistemaMotasNiebla.noise;
        ruidoMotas.enabled = true;
        ruidoMotas.strength = 0.24f;
        ruidoMotas.frequency = 0.35f;
        ruidoMotas.scrollSpeed = 0.15f;
        ruidoMotas.damping = true;

        var tamanoMotas = sistemaMotasNiebla.sizeOverLifetime;
        tamanoMotas.enabled = true;
        AnimationCurve curvaTamMotas = new AnimationCurve();
        curvaTamMotas.AddKey(0.0f, 0.3f);
        curvaTamMotas.AddKey(0.5f, 1.0f);
        curvaTamMotas.AddKey(1.0f, 0.3f);
        tamanoMotas.size = new ParticleSystem.MinMaxCurve(1f, curvaTamMotas);

        var colorMotas = sistemaMotasNiebla.colorOverLifetime;
        colorMotas.enabled = true;
        Gradient gradMotas = new Gradient();
        gradMotas.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.85f, 0.95f, 1f), 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(new Color(0.75f, 0.88f, 1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.75f, 0.22f),
                new GradientAlphaKey(0.75f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorMotas.color = gradMotas;

        var renderMotas = objMotas.GetComponent<ParticleSystemRenderer>();
        renderMotas.renderMode = ParticleSystemRenderMode.Billboard;
        renderMotas.material = materialGotas;

        sistemaMotasNiebla.Simulate(5.0f, true, true);
        sistemaMotasNiebla.Play();

        // 2. Bruma Volumétrica Baja sobre la Superficie del Agua
        GameObject objBruma = new GameObject("ParticulasBrumaBaja");
        objBruma.transform.SetParent(contenedorNiebla.transform);
        objBruma.transform.position = new Vector3(transform.position.x, alturaSuperficieAgua + 0.55f, transform.position.z);
        sistemaBrumaBaja = objBruma.AddComponent<ParticleSystem>();
        sistemaBrumaBaja.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainBruma = sistemaBrumaBaja.main;
        mainBruma.playOnAwake = false;
        mainBruma.loop = true;
        mainBruma.simulationSpace = ParticleSystemSimulationSpace.World;
        mainBruma.scalingMode = ParticleSystemScalingMode.Local;
        mainBruma.startLifetime = new ParticleSystem.MinMaxCurve(7.0f, 11.0f);
        mainBruma.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        mainBruma.startSize = new ParticleSystem.MinMaxCurve(2.8f, 4.8f);
        mainBruma.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        mainBruma.gravityModifier = 0f;
        mainBruma.startColor = new Color(0.76f, 0.86f, 0.94f, 0.065f);
        mainBruma.maxParticles = 60;

        var emisionBruma = sistemaBrumaBaja.emission;
        emisionBruma.enabled = true;
        emisionBruma.rateOverTime = 6f;

        var formaBruma = sistemaBrumaBaja.shape;
        formaBruma.enabled = true;
        formaBruma.shapeType = ParticleSystemShapeType.Box;
        formaBruma.scale = new Vector3(24f, 0.9f, 24f);

        var rotBruma = sistemaBrumaBaja.rotationOverLifetime;
        rotBruma.enabled = true;
        rotBruma.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

        var colorBruma = sistemaBrumaBaja.colorOverLifetime;
        colorBruma.enabled = true;
        Gradient gradBruma = new Gradient();
        gradBruma.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.78f, 0.88f, 0.95f), 0f),
                new GradientColorKey(new Color(0.78f, 0.88f, 0.95f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.3f),
                new GradientAlphaKey(0.85f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorBruma.color = gradBruma;

        var renderBruma = objBruma.GetComponent<ParticleSystemRenderer>();
        renderBruma.renderMode = ParticleSystemRenderMode.Billboard;
        materialBruma = new Material(shaderParticulas);
        materialBruma.mainTexture = CrearTexturaBruma();
        renderBruma.material = materialBruma;

        sistemaBrumaBaja.Simulate(6.0f, true, true);
        sistemaBrumaBaja.Play();
    }

    // Configura la iluminación nocturna de tormenta, la lluvia 3D, los rayos en el cielo y el trueno
    private void InicializarTormentaNocturna()
    {
        camaraPrincipal = Camera.main;
        if (camaraPrincipal != null)
        {
            camaraPrincipal.clearFlags = CameraClearFlags.SolidColor;
            camaraPrincipal.backgroundColor = colorNieblaNormal;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f, 0.32f, 0.46f, 1f);

        Light[] luces = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light l in luces)
        {
            if (l.type == LightType.Directional)
            {
                luzDireccional = l;
                luzDireccional.color = colorLuzLuna;
                luzDireccional.intensity = intensidadLuzLuna;
                break;
            }
        }

        GameObject contenedorTormenta = new GameObject("EfectosTormenta_Nocturna");

        Shader shaderParticulas = Shader.Find("Sprites/Default");
        if (shaderParticulas == null)
        {
            shaderParticulas = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        // 1. Cortina de Lluvia 3D alrededor del jugador
        GameObject objLluvia = new GameObject("ParticulasLluviaTormenta");
        objLluvia.transform.SetParent(contenedorTormenta.transform);
        objLluvia.transform.position = transform.position + Vector3.up * 8f;
        sistemaLluvia = objLluvia.AddComponent<ParticleSystem>();
        sistemaLluvia.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainLluvia = sistemaLluvia.main;
        mainLluvia.playOnAwake = false;
        mainLluvia.loop = true;
        mainLluvia.simulationSpace = ParticleSystemSimulationSpace.World;
        mainLluvia.scalingMode = ParticleSystemScalingMode.Local;
        mainLluvia.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.85f);
        mainLluvia.startSpeed = new ParticleSystem.MinMaxCurve(14f, 18f);
        mainLluvia.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.045f);
        mainLluvia.gravityModifier = 0.5f;
        mainLluvia.startColor = new Color(0.72f, 0.86f, 1.0f, 0.42f);
        mainLluvia.maxParticles = 600;

        var emisionLluvia = sistemaLluvia.emission;
        emisionLluvia.enabled = true;
        emisionLluvia.rateOverTime = 260f;

        var formaLluvia = sistemaLluvia.shape;
        formaLluvia.enabled = true;
        formaLluvia.shapeType = ParticleSystemShapeType.Box;
        formaLluvia.scale = new Vector3(24f, 0.5f, 24f);

        var velLluvia = sistemaLluvia.velocityOverLifetime;
        velLluvia.enabled = true;
        velLluvia.space = ParticleSystemSimulationSpace.World;
        velLluvia.x = new ParticleSystem.MinMaxCurve(-2.0f, -1.2f);
        velLluvia.y = new ParticleSystem.MinMaxCurve(-16.0f, -13.0f);
        velLluvia.z = new ParticleSystem.MinMaxCurve(-1.0f, -0.4f);

        var renderLluvia = objLluvia.GetComponent<ParticleSystemRenderer>();
        renderLluvia.renderMode = ParticleSystemRenderMode.Stretch;
        renderLluvia.lengthScale = 5.8f;
        renderLluvia.velocityScale = 0.06f;
        renderLluvia.material = materialGotas;

        sistemaLluvia.Simulate(1.5f, true, true);
        sistemaLluvia.Play();

        // 2. Rayo Eléctrico Ramificado en el Cielo (LineRenderer)
        Material matRayo = new Material(shaderParticulas);

        GameObject objRayo = new GameObject("RayoCieloPrincipal");
        objRayo.transform.SetParent(contenedorTormenta.transform);
        rayoPrincipal = objRayo.AddComponent<LineRenderer>();
        rayoPrincipal.material = matRayo;
        rayoPrincipal.positionCount = 14;
        rayoPrincipal.startWidth = 0.42f;
        rayoPrincipal.endWidth = 0.08f;
        rayoPrincipal.useWorldSpace = true;
        rayoPrincipal.enabled = false;

        GameObject objRama = new GameObject("RayoCieloRama");
        objRama.transform.SetParent(contenedorTormenta.transform);
        rayoRama = objRama.AddComponent<LineRenderer>();
        rayoRama.material = matRayo;
        rayoRama.positionCount = 7;
        rayoRama.startWidth = 0.22f;
        rayoRama.endWidth = 0.03f;
        rayoRama.useWorldSpace = true;
        rayoRama.enabled = false;

        // 3. Fuente de Audio y Trueno Sintetizado
        GameObject objTrueno = new GameObject("AudioTruenoTormenta");
        objTrueno.transform.SetParent(contenedorTormenta.transform);
        fuenteTrueno = objTrueno.AddComponent<AudioSource>();
        fuenteTrueno.playOnAwake = false;
        fuenteTrueno.spatialBlend = 0f;
        fuenteTrueno.volume = 0.65f;
        clipTrueno = CrearClipTrueno();

        // Primer relámpago a los pocos segundos de iniciar para lucir el efecto
        tiempoSiguienteRelampago = Time.time + 3.0f;
    }

    private void ActualizarTormentaYRelampagos()
    {
        if (sistemaLluvia != null)
        {
            sistemaLluvia.transform.position = new Vector3(transform.position.x + 1.0f, alturaSuperficieAgua + 8.5f, transform.position.z + 0.5f);
        }

        // Pequeñas ondas de gotas de lluvia cayendo al azar sobre el agua alrededor del jugador
        if (Time.time >= tiempoSiguienteGotaLluviaOnda)
        {
            tiempoSiguienteGotaLluviaOnda = Time.time + 0.09f;
            Vector2 offset = Random.insideUnitCircle * 9.5f;
            Vector3 puntoGota = new Vector3(transform.position.x + offset.x, alturaSuperficieAgua, transform.position.z + offset.y);
            EmitirOnda(puntoGota, Random.Range(0.16f, 0.30f), Random.Range(0.45f, 0.70f));
        }

        // Disparar relámpagos periódicos
        if (Time.time >= tiempoSiguienteRelampago)
        {
            DispararRelampago();
            float minT = estaEnAgua ? tiempoMinRelampago * 0.6f : tiempoMinRelampago;
            float maxT = estaEnAgua ? tiempoMaxRelampago * 0.65f : tiempoMaxRelampago;
            tiempoSiguienteRelampago = Time.time + Random.Range(minT, maxT);
        }

        // Animar el destello del relámpago (doble parpadeo realista y desvanecimiento)
        float t = Time.time - tiempoInicioRelampago;
        float factorRelampago = 0f;

        if (t >= 0f && t <= duracionRelampagoActual)
        {
            // Doble pulso eléctrico: pico inicial, breve caída y segundo destello fuerte
            if (t < 0.07f)
                factorRelampago = 1.0f;
            else if (t < 0.12f)
                factorRelampago = 0.25f;
            else if (t < 0.20f)
                factorRelampago = 0.95f;
            else
                factorRelampago = Mathf.Clamp01(1f - ((t - 0.20f) / (duracionRelampagoActual - 0.20f)));

            if (rayoPrincipal != null)
            {
                Color cRayo = new Color(0.85f, 0.95f, 1f, factorRelampago);
                rayoPrincipal.startColor = cRayo;
                rayoPrincipal.endColor = cRayo;
            }
            if (rayoRama != null)
            {
                Color cRama = new Color(0.75f, 0.90f, 1f, factorRelampago * 0.85f);
                rayoRama.startColor = cRama;
                rayoRama.endColor = cRama;
            }
        }
        else
        {
            if (rayoPrincipal != null && rayoPrincipal.enabled) rayoPrincipal.enabled = false;
            if (rayoRama != null && rayoRama.enabled) rayoRama.enabled = false;
        }

        // Aplicar iluminación del relámpago al sol/luna, ambiente, niebla y cielo
        if (luzDireccional != null)
        {
            luzDireccional.intensity = Mathf.Lerp(intensidadLuzLuna, intensidadMaxRelampago, factorRelampago);
            luzDireccional.color = Color.Lerp(colorLuzLuna, colorRelampago, factorRelampago);
        }

        Color ambBase = new Color(0.24f, 0.32f, 0.46f, 1f);
        RenderSettings.ambientLight = Color.Lerp(ambBase, colorRelampago * 0.75f, factorRelampago);

        if (factorRelampago > 0.01f)
        {
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, colorRelampago * 0.65f, factorRelampago * 0.8f);
        }

        if (camaraPrincipal != null)
        {
            camaraPrincipal.backgroundColor = RenderSettings.fogColor;
        }
    }

    // Dispara un relámpago visual en el cielo frente al jugador con sonido de trueno
    public void DispararRelampago()
    {
        tiempoInicioRelampago = Time.time;
        duracionRelampagoActual = Random.Range(0.42f, 0.62f);

        // Posicionar el rayo en el cielo dentro del campo de visión o alrededores
        Vector3 dirVista = camaraPrincipal != null ? camaraPrincipal.transform.forward : transform.forward;
        dirVista.y = 0f;
        if (dirVista.sqrMagnitude < 0.01f) dirVista = Vector3.forward;
        dirVista.Normalize();

        Vector3 derecha = Vector3.Cross(Vector3.up, dirVista);
        Vector3 baseCielo = transform.position + dirVista * Random.Range(22f, 34f) + derecha * Random.Range(-12f, 12f);
        Vector3 inicioAlto = new Vector3(baseCielo.x, alturaSuperficieAgua + 26f, baseCielo.z);
        Vector3 finBajo = new Vector3(baseCielo.x + Random.Range(-3f, 3f), alturaSuperficieAgua + 2f, baseCielo.z + Random.Range(-3f, 3f));

        if (rayoPrincipal != null)
        {
            int n = rayoPrincipal.positionCount;
            Vector3 nodoRama = inicioAlto;
            for (int i = 0; i < n; i++)
            {
                float f = (float)i / (n - 1);
                Vector3 p = Vector3.Lerp(inicioAlto, finBajo, f);
                if (i > 0 && i < n - 1)
                {
                    p += new Vector3(Random.Range(-1.4f, 1.4f), Random.Range(-0.4f, 0.4f), Random.Range(-1.4f, 1.4f));
                }
                rayoPrincipal.SetPosition(i, p);
                if (i == 5) nodoRama = p;
            }
            rayoPrincipal.enabled = true;

            if (rayoRama != null)
            {
                int m = rayoRama.positionCount;
                Vector3 finRama = nodoRama + derecha * Random.Range(4f, 7f) * (Random.value > 0.5f ? 1f : -1f) + Vector3.down * 8f;
                for (int j = 0; j < m; j++)
                {
                    float fj = (float)j / (m - 1);
                    Vector3 pr = Vector3.Lerp(nodoRama, finRama, fj);
                    if (j > 0)
                    {
                        pr += new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.3f, 0.3f), Random.Range(-0.8f, 0.8f));
                    }
                    rayoRama.SetPosition(j, pr);
                }
                rayoRama.enabled = true;
            }
        }

        if (fuenteTrueno != null && clipTrueno != null)
        {
            fuenteTrueno.pitch = Random.Range(0.82f, 1.12f);
            fuenteTrueno.PlayOneShot(clipTrueno, Random.Range(0.55f, 0.80f));
        }
    }

    // Sintetiza un sonido de trueno grave y envolvente en memoria
    private AudioClip CrearClipTrueno()
    {
        int frec = 22050;
        float duracion = 2.8f;
        int muestras = Mathf.FloorToInt(frec * duracion);
        float[] datos = new float[muestras];

        float filtro1 = 0f;
        float filtro2 = 0f;
        System.Random rng = new System.Random(42);

        for (int i = 0; i < muestras; i++)
        {
            float t = (float)i / frec;
            float ruido = (float)(rng.NextDouble() * 2.0 - 1.0);

            // Filtro paso-bajo doble para obtener el retumbo profundo de un trueno real
            filtro1 += 0.045f * (ruido - filtro1);
            filtro2 += 0.025f * (filtro1 - filtro2);

            // Envolvente con estallido inicial y dos ecos de retumbo
            float env1 = Mathf.Exp(-t * 2.2f);
            float env2 = 0.65f * Mathf.Exp(-Mathf.Pow((t - 0.55f) / 0.35f, 2f));
            float env3 = 0.40f * Mathf.Exp(-Mathf.Pow((t - 1.25f) / 0.55f, 2f));
            float envolvente = Mathf.Clamp01(env1 + env2 + env3);

            datos[i] = Mathf.Clamp(filtro2 * envolvente * 3.8f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("TruenoSintetizado", muestras, 1, frec, false);
        clip.SetData(datos, 0);
        return clip;
    }

    // Genera una textura suave circular en memoria para las gotas y motas
    private Texture2D CrearTexturaGota()
    {
        int res = 64;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float centro = (res - 1) * 0.5f;
        float radioMax = centro;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x - centro) / radioMax;
                float dy = (y - centro) / radioMax;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    // Genera una textura ultra suave para los bancos de bruma flotante
    private Texture2D CrearTexturaBruma()
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

                // Caída cúbica muy suave para que nunca se vean bordes duros
                float falloff = 1f - (d * d);
                float alpha = falloff * falloff * falloff;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }

    // Genera una textura de doble anillo concéntrico para las ondas del agua
    private Texture2D CrearTexturaOnda()
    {
        int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float centro = (res - 1) * 0.5f;
        float radioMax = centro;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dx = (x - centro) / radioMax;
                float dy = (y - centro) / radioMax;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d >= 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float anilloExterno = Mathf.Exp(-Mathf.Pow((d - 0.78f) / 0.08f, 2f));
                float anilloInterno = 0.55f * Mathf.Exp(-Mathf.Pow((d - 0.46f) / 0.07f, 2f));
                float bordeSuave = Mathf.Clamp01((1f - d) * 8f);

                float alpha = Mathf.Clamp01((anilloExterno + anilloInterno) * bordeSuave);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return tex;
    }
}