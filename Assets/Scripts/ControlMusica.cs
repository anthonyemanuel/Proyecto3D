using UnityEngine;
using System.Collections;

public class ControlMusica : MonoBehaviour
{
    [Header("Referencias")]
    public AudioSource fuenteMusica;
    public AudioClip musicaAmbiente;
    public AudioClip musicaTension;
    public DetectorAgua detectorAgua;

    [Header("Ajustes de Transición")]
    [Range(0.2f, 2f)] public float duracionTransicion = 0.8f;
    private float volumenMaximo = 1f;
    private Coroutine corrutinaTransicion;

    void Start()
    {
        if (fuenteMusica == null)
        {
            fuenteMusica = GetComponent<AudioSource>();
        }

        if (fuenteMusica != null)
        {
            volumenMaximo = fuenteMusica.volume > 0 ? fuenteMusica.volume : 1f;
        }

        if (detectorAgua == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                detectorAgua = jugador.GetComponent<DetectorAgua>();
            }
        }

        if (detectorAgua != null)
        {
            detectorAgua.OnEstadoAguaCambiado += AlCambiarEstadoAgua;
        }

        // Reproducir música ambiente por defecto si no está sonando
        if (fuenteMusica != null && musicaAmbiente != null && !fuenteMusica.isPlaying)
        {
            fuenteMusica.clip = musicaAmbiente;
            fuenteMusica.loop = true;
            fuenteMusica.Play();
        }
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
        AudioClip proximoClip = enAgua ? musicaTension : musicaAmbiente;
        if (corrutinaTransicion != null) StopCoroutine(corrutinaTransicion);
        corrutinaTransicion = StartCoroutine(TransicionSuave(proximoClip));
    }

    IEnumerator TransicionSuave(AudioClip nuevoClip)
    {
        if (fuenteMusica == null || nuevoClip == null || fuenteMusica.clip == nuevoClip) yield break;

        float mitadTiempo = duracionTransicion * 0.5f;
        float volumenInicial = fuenteMusica.volume;

        // Fade Out
        float tiempo = 0f;
        while (tiempo < mitadTiempo)
        {
            tiempo += Time.deltaTime;
            fuenteMusica.volume = Mathf.Lerp(volumenInicial, 0f, tiempo / mitadTiempo);
            yield return null;
        }

        fuenteMusica.clip = nuevoClip;
        fuenteMusica.Play();

        // Fade In
        tiempo = 0f;
        while (tiempo < mitadTiempo)
        {
            tiempo += Time.deltaTime;
            fuenteMusica.volume = Mathf.Lerp(0f, volumenMaximo, tiempo / mitadTiempo);
            yield return null;
        }
        fuenteMusica.volume = volumenMaximo;
    }
}
