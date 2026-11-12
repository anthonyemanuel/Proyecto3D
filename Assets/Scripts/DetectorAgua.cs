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

    private float alturaSuperficieAgua = 0.85f;
    private Vector3 ultimaPosicionXZ;
    private float tiempoSiguienteSalpicadura = 0f;
    private float tiempoSiguienteOndaReposo = 0f;
    private float ultimoTiempoEnAire = 0f;

    private ParticleSystem sistemaGotas;
    private ParticleSystem sistemaOndas;
    private Material materialGotas;
    private Material materialOndas;

    void Start()
    {
        GameObject objetoAgua = GameObject.FindGameObjectWithTag("Agua");
        if (objetoAgua != null)
        {
            alturaSuperficieAgua = objetoAgua.transform.position.y;
        }

        ultimaPosicionXZ = new Vector3(transform.position.x, 0f, transform.position.z);
        CrearSistemasDeParticulas();
    }

    void Update()
    {
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

                // Salpicadura fuerte al caer o entrar al agua
                EmitirSalpicadura(puntoContacto, 30, 2.9f, 1.25f);
                EmitirOnda(puntoContacto, 0.85f, 1.3f);
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

    // Genera una textura suave circular en memoria para las gotas
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