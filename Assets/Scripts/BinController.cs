using UnityEngine;

public class BinController : MonoBehaviour
{
    public enum TipoTacho { Papel, Plastico, Organico }
    public TipoTacho tipoAceptado;

    [Header("Sonidos")]
    public AudioClip sonidoDepositar;

    [Header("Faro Luminoso y Efectos del Tacho")]
    public float intensidadLuzTacho = 2.6f;
    public float rangoLuzTacho = 5.5f;

    private Light luzTacho;
    private ParticleSystem sistemaExplosion;
    private ParticleSystem sistemaOndasTacho;
    private Color colorTacho;
    private float destelloExtraLuz = 0f;
    private float tiempoSiguienteOnda = 0f;

    // Aviso en pantalla compartido al entregar basura en un tacho
    private static string mensajeFeedback = "";
    private static Color colorFeedback = Color.white;
    private static float tiempoFinFeedback = 0f;
    private static BinController instanciaActivaFeedback;
    private static Texture2D texFondoAviso;

    void Start()
    {
        colorTacho = ObtenerColorTacho(tipoAceptado);
        CrearEfectosVisualesTacho();
    }

    void Update()
    {
        if (destelloExtraLuz > 0f)
        {
            destelloExtraLuz = Mathf.MoveTowards(destelloExtraLuz, 0f, Time.deltaTime * 8f);
        }

        if (luzTacho != null)
        {
            float pulso = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.8f + GetInstanceID());
            luzTacho.intensity = intensidadLuzTacho + pulso * 0.6f + destelloExtraLuz;
        }

        // Emitir halo constante alrededor del tacho como faro guía en la tormenta
        if (sistemaOndasTacho != null && Time.time >= tiempoSiguienteOnda)
        {
            tiempoSiguienteOnda = Time.time + 1.2f;
            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
            {
                position = transform.position + Vector3.up * 0.1f,
                startSize = 1.6f,
                startLifetime = 1.15f,
                startColor = new Color(colorTacho.r, colorTacho.g, colorTacho.b, 0.65f)
            };
            sistemaOndasTacho.Emit(ep, 1);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos si el que entró al tacho es el Jugador
        if (other.CompareTag("Player"))
        {
            PlayerPickup pickup = other.GetComponent<PlayerPickup>();

            // Verificamos si el jugador efectivamente trae basura en la mano
            if (pickup != null && pickup.basuraEnMano != null)
            {
                TrashItem itemBasura = pickup.basuraEnMano.GetComponent<TrashItem>();

                if (itemBasura != null)
                {
                    // Comparamos si el tipo de la basura coincide con el tacho
                    if (itemBasura.tipoResiduo.ToString() == tipoAceptado.ToString())
                    {
                        Debug.Log("¡Correcto! Entregaste " + itemBasura.tipoResiduo + " en el tacho " + tipoAceptado);

                        // Sonido al depositar
                        if (sonidoDepositar != null)
                        {
                            AudioSource.PlayClipAtPoint(sonidoDepositar, transform.position);
                        }

                        // Sumamos los puntos
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.SumarPuntos(10);
                        }

                        // Explosión de partículas brillantes de celebración
                        DispararEfectoDeposito(true);
                        MostrarFeedback("✓ ¡RECICLAJE CORRECTO! (+10 PUNTOS) — Tacho " + tipoAceptado, new Color(0.25f, 1f, 0.55f));

                        // Destruimos la basura que llevábamos en la mano
                        Destroy(pickup.basuraEnMano);

                        // Limpiamos la mano del jugador
                        pickup.basuraEnMano = null;
                    }
                    else
                    {
                        Debug.Log("¡Error! Este tacho es para " + tipoAceptado + ", no para " + itemBasura.tipoResiduo);
                        DispararEfectoDeposito(false);
                        MostrarFeedback("✗ TACHO INCORRECTO: Este tacho es para " + tipoAceptado + " (llevas " + itemBasura.tipoResiduo + ")", new Color(1f, 0.35f, 0.35f));
                    }
                }
            }
        }
    }

    private Color ObtenerColorTacho(TipoTacho tipo)
    {
        switch (tipo)
        {
            case TipoTacho.Papel:
                return new Color(0.20f, 0.70f, 1.0f, 1f);
            case TipoTacho.Plastico:
                return new Color(1.0f, 0.85f, 0.15f, 1f);
            case TipoTacho.Organico:
                return new Color(0.22f, 1.0f, 0.45f, 1f);
            default:
                return Color.white;
        }
    }

    private void DispararEfectoDeposito(bool correcto)
    {
        destelloExtraLuz = correcto ? 6.5f : 3.5f;

        if (sistemaExplosion != null)
        {
            var main = sistemaExplosion.main;
            if (correcto)
            {
                main.startColor = new ParticleSystem.MinMaxGradient(
                    colorTacho,
                    new Color(1f, 1f, 0.85f, 1f)
                );
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
                sistemaExplosion.Emit(48);
            }
            else
            {
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.18f, 0.18f, 1f),
                    new Color(1f, 0.55f, 0.15f, 1f)
                );
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
                sistemaExplosion.Emit(24);
            }
        }

        if (sistemaOndasTacho != null)
        {
            ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams
            {
                position = transform.position + Vector3.up * 0.25f,
                startSize = correcto ? 2.8f : 2.0f,
                startLifetime = 0.85f,
                startColor = correcto ? new Color(colorTacho.r, colorTacho.g, colorTacho.b, 0.95f) : new Color(1f, 0.2f, 0.2f, 0.9f)
            };
            sistemaOndasTacho.Emit(ep, 1);
        }
    }

    private void MostrarFeedback(string texto, Color color)
    {
        mensajeFeedback = texto;
        colorFeedback = color;
        tiempoFinFeedback = Time.time + 2.8f;
        instanciaActivaFeedback = this;
    }

    private void OnGUI()
    {
        if (instanciaActivaFeedback != this || Time.time >= tiempoFinFeedback) return;

        if (texFondoAviso == null)
        {
            texFondoAviso = new Texture2D(1, 1);
            texFondoAviso.SetPixel(0, 0, Color.white);
            texFondoAviso.Apply();
        }

        float alpha = Mathf.Clamp01((tiempoFinFeedback - Time.time) * 1.2f);
        float ancho = 540f;
        float alto = 46f;
        Rect caja = new Rect((Screen.width - ancho) * 0.5f, Screen.height - 95f, ancho, alto);

        GUI.color = new Color(colorFeedback.r, colorFeedback.g, colorFeedback.b, 0.85f * alpha);
        GUI.DrawTexture(new Rect(caja.x - 2f, caja.y - 2f, caja.width + 4f, caja.height + 4f), texFondoAviso);
        GUI.color = new Color(0.06f, 0.09f, 0.14f, 0.90f * alpha);
        GUI.DrawTexture(caja, texFondoAviso);

        GUIStyle estilo = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        estilo.normal.textColor = new Color(colorFeedback.r, colorFeedback.g, colorFeedback.b, alpha);

        GUI.color = Color.white;
        GUI.Label(caja, mensajeFeedback, estilo);
    }

    private void CrearEfectosVisualesTacho()
    {
        GameObject contenedor = new GameObject("EfectosLuminosos_Tacho");
        contenedor.transform.SetParent(transform, false);
        contenedor.transform.localPosition = Vector3.zero;

        Vector3 s = transform.lossyScale;
        contenedor.transform.localScale = new Vector3(
            Mathf.Abs(s.x) > 0.0001f ? 1f / s.x : 1f,
            Mathf.Abs(s.y) > 0.0001f ? 1f / s.y : 1f,
            Mathf.Abs(s.z) > 0.0001f ? 1f / s.z : 1f
        );

        // 1. Luz Faro sobre el Tacho
        GameObject objLuz = new GameObject("LuzFaroTacho");
        objLuz.transform.SetParent(contenedor.transform, false);
        objLuz.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        luzTacho = objLuz.AddComponent<Light>();
        luzTacho.type = LightType.Point;
        luzTacho.color = colorTacho;
        luzTacho.range = rangoLuzTacho;
        luzTacho.intensity = intensidadLuzTacho;
        luzTacho.shadows = LightShadows.None;

        Shader shaderParticulas = Shader.Find("Sprites/Default");
        if (shaderParticulas == null)
        {
            shaderParticulas = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        // 2. Sistema de Explosión de Chispas al Reciclar
        GameObject objExp = new GameObject("ParticulasExplosionTacho");
        objExp.transform.SetParent(contenedor.transform, false);
        objExp.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        sistemaExplosion = objExp.AddComponent<ParticleSystem>();
        sistemaExplosion.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainExp = sistemaExplosion.main;
        mainExp.playOnAwake = false;
        mainExp.loop = false;
        mainExp.simulationSpace = ParticleSystemSimulationSpace.World;
        mainExp.scalingMode = ParticleSystemScalingMode.Local;
        mainExp.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        mainExp.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 4.2f);
        mainExp.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        mainExp.gravityModifier = 0.85f;
        mainExp.startColor = colorTacho;
        mainExp.maxParticles = 150;

        var emisionExp = sistemaExplosion.emission;
        emisionExp.enabled = false;

        var formaExp = sistemaExplosion.shape;
        formaExp.enabled = true;
        formaExp.shapeType = ParticleSystemShapeType.Cone;
        formaExp.angle = 42f;
        formaExp.radius = 0.25f;
        formaExp.rotation = new Vector3(-90f, 0f, 0f);

        var tamExp = sistemaExplosion.sizeOverLifetime;
        tamExp.enabled = true;
        AnimationCurve curvaExp = new AnimationCurve();
        curvaExp.AddKey(0.0f, 1.0f);
        curvaExp.AddKey(1.0f, 0.0f);
        tamExp.size = new ParticleSystem.MinMaxCurve(1f, curvaExp);

        var renderExp = objExp.GetComponent<ParticleSystemRenderer>();
        renderExp.renderMode = ParticleSystemRenderMode.Billboard;
        Material matExp = new Material(shaderParticulas);
        matExp.mainTexture = TrashItem.CrearTexturaChispa();
        renderExp.material = matExp;

        // 3. Sistema de Halo / Ondas del Tacho
        GameObject objOndas = new GameObject("ParticulasHaloTacho");
        objOndas.transform.SetParent(contenedor.transform, false);
        objOndas.transform.localPosition = Vector3.zero;
        sistemaOndasTacho = objOndas.AddComponent<ParticleSystem>();
        sistemaOndasTacho.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainOndas = sistemaOndasTacho.main;
        mainOndas.playOnAwake = false;
        mainOndas.loop = false;
        mainOndas.simulationSpace = ParticleSystemSimulationSpace.World;
        mainOndas.scalingMode = ParticleSystemScalingMode.Local;
        mainOndas.startLifetime = 1.1f;
        mainOndas.startSpeed = 0f;
        mainOndas.startSize = 1.6f;
        mainOndas.gravityModifier = 0f;
        mainOndas.startColor = colorTacho;
        mainOndas.maxParticles = 25;

        var emisionOndas = sistemaOndasTacho.emission;
        emisionOndas.enabled = false;

        var formaOndas = sistemaOndasTacho.shape;
        formaOndas.enabled = false;

        var tamOndas = sistemaOndasTacho.sizeOverLifetime;
        tamOndas.enabled = true;
        AnimationCurve curvaOndas = new AnimationCurve();
        curvaOndas.AddKey(0.0f, 0.35f);
        curvaOndas.AddKey(1.0f, 1.0f);
        tamOndas.size = new ParticleSystem.MinMaxCurve(1f, curvaOndas);

        var colOndas = sistemaOndasTacho.colorOverLifetime;
        colOndas.enabled = true;
        Gradient gradOndas = new Gradient();
        gradOndas.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.2f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colOndas.color = gradOndas;

        var renderOndas = objOndas.GetComponent<ParticleSystemRenderer>();
        renderOndas.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        Material matOndas = new Material(shaderParticulas);
        matOndas.mainTexture = TrashItem.CrearTexturaHalo();
        renderOndas.material = matOndas;
    }
}