using UnityEngine;

public class TrashItem : MonoBehaviour
{
    // Define qué tipo de residuo es este objeto específico
    public enum TipoBasura { Papel, Plastico, Organico }
    public TipoBasura tipoResiduo;

    [Header("Aura Luminosa del Residuo")]
    public float intensidadLuzBase = 2.0f;
    public float rangoLuz = 3.5f;

    private Light luzAura;
    private ParticleSystem sistemaChispas;
    private ParticleSystem sistemaHaloSuelo;
    private Renderer rendBasura;
    private Color colorAura;
    private float tiempoSiguienteHalo = 0f;
    private static Texture2D texChispaCompartida;
    private static Texture2D texHaloCompartido;

    void Start()
    {
        colorAura = ObtenerColorPorTipo(tipoResiduo);
        CrearAuraLuminosa();
    }

    void Update()
    {
        bool enManoDelJugador = transform.parent != null && !transform.parent.CompareTag("Untagged");
        if (transform.parent != null && transform.parent.name.Contains("Mano"))
        {
            enManoDelJugador = true;
        }

        // Pulso suave de la luz 3D para que resalte en la noche de tormenta
        if (luzAura != null)
        {
            float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.0f + GetInstanceID());
            float intensidadObjetivo = enManoDelJugador ? 0.65f : (intensidadLuzBase + pulso * 0.75f);
            luzAura.intensity = Mathf.Lerp(luzAura.intensity, intensidadObjetivo, Time.deltaTime * 6f);
            luzAura.range = enManoDelJugador ? 1.8f : rangoLuz;
        }

        // Emitir halo circular en el suelo mientras la basura no haya sido recogida
        if (!enManoDelJugador && sistemaHaloSuelo != null && Time.time >= tiempoSiguienteHalo)
        {
            tiempoSiguienteHalo = Time.time + 0.85f;
            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
            {
                position = transform.position + Vector3.up * 0.04f,
                startSize = 0.95f,
                startLifetime = 0.95f,
                startColor = new Color(colorAura.r, colorAura.g, colorAura.b, 0.70f)
            };
            sistemaHaloSuelo.Emit(ep, 1);
        }
    }

    public static Color ObtenerColorPorTipo(TipoBasura tipo)
    {
        switch (tipo)
        {
            case TipoBasura.Papel:
                return new Color(0.20f, 0.70f, 1.0f, 1f);    // Azul brillante (Papel/Caja)
            case TipoBasura.Plastico:
                return new Color(1.0f, 0.85f, 0.15f, 1f);    // Amarillo dorado (Plástico/Botella)
            case TipoBasura.Organico:
                return new Color(0.22f, 1.0f, 0.45f, 1f);    // Verde esmeralda (Orgánico/Manzana)
            default:
                return Color.white;
        }
    }

    private void CrearAuraLuminosa()
    {
        // Iluminar ligeramente el material de la basura
        rendBasura = GetComponentInChildren<Renderer>();
        if (rendBasura != null && rendBasura.material != null)
        {
            rendBasura.material.EnableKeyword("_EMISSION");
            if (rendBasura.material.HasProperty("_EmissionColor"))
            {
                rendBasura.material.SetColor("_EmissionColor", colorAura * 0.45f);
            }
        }

        GameObject contenedor = new GameObject("AuraLuminosa_Basura");
        contenedor.transform.SetParent(transform, false);
        contenedor.transform.localPosition = Vector3.zero;

        // Compensar la escala del prefab para que el aura mida siempre en metros reales (1:1)
        Vector3 s = transform.lossyScale;
        contenedor.transform.localScale = new Vector3(
            Mathf.Abs(s.x) > 0.0001f ? 1f / s.x : 1f,
            Mathf.Abs(s.y) > 0.0001f ? 1f / s.y : 1f,
            Mathf.Abs(s.z) > 0.0001f ? 1f / s.z : 1f
        );

        // 1. Luz Puntual 3D del color del residuo
        GameObject objLuz = new GameObject("LuzBasura");
        objLuz.transform.SetParent(contenedor.transform, false);
        objLuz.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        luzAura = objLuz.AddComponent<Light>();
        luzAura.type = LightType.Point;
        luzAura.color = colorAura;
        luzAura.range = rangoLuz;
        luzAura.intensity = intensidadLuzBase;
        luzAura.shadows = LightShadows.None;

        Shader shaderParticulas = Shader.Find("Sprites/Default");
        if (shaderParticulas == null)
        {
            shaderParticulas = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (texChispaCompartida == null) texChispaCompartida = CrearTexturaChispa();
        if (texHaloCompartido == null) texHaloCompartido = CrearTexturaHalo();

        // 2. Chispas / Destellos Flotantes Ascendentes
        GameObject objChispas = new GameObject("ParticulasDestellosBasura");
        objChispas.transform.SetParent(contenedor.transform, false);
        objChispas.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        sistemaChispas = objChispas.AddComponent<ParticleSystem>();
        sistemaChispas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainChispas = sistemaChispas.main;
        mainChispas.playOnAwake = false;
        mainChispas.loop = true;
        mainChispas.simulationSpace = ParticleSystemSimulationSpace.World;
        mainChispas.scalingMode = ParticleSystemScalingMode.Local;
        mainChispas.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        mainChispas.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        mainChispas.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        mainChispas.gravityModifier = -0.18f; // Suben suavemente
        mainChispas.startColor = new ParticleSystem.MinMaxGradient(
            colorAura,
            Color.Lerp(colorAura, Color.white, 0.55f)
        );
        mainChispas.maxParticles = 60;

        var emisionChispas = sistemaChispas.emission;
        emisionChispas.enabled = true;
        emisionChispas.rateOverTime = 14f;

        var formaChispas = sistemaChispas.shape;
        formaChispas.enabled = true;
        formaChispas.shapeType = ParticleSystemShapeType.Sphere;
        formaChispas.radius = 0.28f;

        var ruidoChispas = sistemaChispas.noise;
        ruidoChispas.enabled = true;
        ruidoChispas.strength = 0.25f;
        ruidoChispas.frequency = 0.6f;
        ruidoChispas.scrollSpeed = 0.3f;
        ruidoChispas.damping = true;

        var tamChispas = sistemaChispas.sizeOverLifetime;
        tamChispas.enabled = true;
        AnimationCurve curvaTam = new AnimationCurve();
        curvaTam.AddKey(0.0f, 0.3f);
        curvaTam.AddKey(0.4f, 1.0f);
        curvaTam.AddKey(1.0f, 0.0f);
        tamChispas.size = new ParticleSystem.MinMaxCurve(1f, curvaTam);

        var colChispas = sistemaChispas.colorOverLifetime;
        colChispas.enabled = true;
        Gradient gradChispas = new Gradient();
        gradChispas.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(colorAura, 0.5f),
                new GradientColorKey(colorAura, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.95f, 0.2f),
                new GradientAlphaKey(0.85f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colChispas.color = gradChispas;

        var renderChispas = objChispas.GetComponent<ParticleSystemRenderer>();
        renderChispas.renderMode = ParticleSystemRenderMode.Billboard;
        Material matChispas = new Material(shaderParticulas);
        matChispas.mainTexture = texChispasCompartida();
        renderChispas.material = matChispas;
        sistemaChispas.Play();

        // 3. Halo Circular en el Suelo bajo la Basura
        GameObject objHalo = new GameObject("ParticulasHaloBasura");
        objHalo.transform.SetParent(contenedor.transform, false);
        objHalo.transform.localPosition = Vector3.zero;
        sistemaHaloSuelo = objHalo.AddComponent<ParticleSystem>();
        sistemaHaloSuelo.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainHalo = sistemaHaloSuelo.main;
        mainHalo.playOnAwake = false;
        mainHalo.loop = false;
        mainHalo.simulationSpace = ParticleSystemSimulationSpace.World;
        mainHalo.scalingMode = ParticleSystemScalingMode.Local;
        mainHalo.startLifetime = 0.95f;
        mainHalo.startSpeed = 0f;
        mainHalo.startSize = 0.95f;
        mainHalo.gravityModifier = 0f;
        mainHalo.startColor = colorAura;
        mainHalo.maxParticles = 15;

        var emisionHalo = sistemaHaloSuelo.emission;
        emisionHalo.enabled = false;

        var formaHalo = sistemaHaloSuelo.shape;
        formaHalo.enabled = false;

        var tamHalo = sistemaHaloSuelo.sizeOverLifetime;
        tamHalo.enabled = true;
        AnimationCurve curvaHalo = new AnimationCurve();
        curvaHalo.AddKey(0.0f, 0.30f);
        curvaHalo.AddKey(0.7f, 0.85f);
        curvaHalo.AddKey(1.0f, 1.0f);
        tamHalo.size = new ParticleSystem.MinMaxCurve(1f, curvaHalo);

        var colHalo = sistemaHaloSuelo.colorOverLifetime;
        colHalo.enabled = true;
        Gradient gradHalo = new Gradient();
        gradHalo.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.75f, 0.2f),
                new GradientAlphaKey(0.35f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colHalo.color = gradHalo;

        var renderHalo = objHalo.GetComponent<ParticleSystemRenderer>();
        renderHalo.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        Material matHalo = new Material(shaderParticulas);
        matHalo.mainTexture = texHaloCompartido;
        renderHalo.material = matHalo;
    }

    private static Texture2D texChispasCompartida()
    {
        if (texChispaCompartida == null) texChispaCompartida = CrearTexturaChispa();
        return texChispaCompartida;
    }

    public static Texture2D CrearTexturaChispa()
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
                float nucleo = Mathf.Exp(-d * d * 9f);
                float halo = Mathf.Pow(1f - d, 2.4f);
                float alpha = Mathf.Clamp01(nucleo + halo * 0.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    public static Texture2D CrearTexturaHalo()
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

                float anillo = Mathf.Exp(-Mathf.Pow((d - 0.78f) / 0.08f, 2f));
                float centroSuave = 0.25f * Mathf.Pow(1f - d, 2f);
                float borde = Mathf.Clamp01((1f - d) * 8f);
                float alpha = Mathf.Clamp01((anillo + centroSuave) * borde);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }
}