using UnityEngine;

public class ControlMusica : MonoBehaviour
{
    [Header("Referencias")]
    public AudioSource fuenteMusica;
    public AudioClip musicaAmbiente;
    public AudioClip musicaTension;
    public DetectorAgua detectorAgua;

    private bool tensionActiva = false;

    void Start()
    {
        if (fuenteMusica == null)
        {
            fuenteMusica = GetComponent<AudioSource>();
        }

        if (detectorAgua == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
            {
                detectorAgua = jugador.GetComponent<DetectorAgua>();
            }
        }
    }

    void Update()
    {
        if (detectorAgua == null || fuenteMusica == null) return;

        if (detectorAgua.estaEnAgua && !tensionActiva)
        {
            CambiarMusica(musicaTension);
            tensionActiva = true;
        }
        else if (!detectorAgua.estaEnAgua && tensionActiva)
        {
            CambiarMusica(musicaAmbiente);
            tensionActiva = false;
        }
    }

    void CambiarMusica(AudioClip nuevaMusica)
    {
        if (nuevaMusica == null) return;

        fuenteMusica.clip = nuevaMusica;
        fuenteMusica.loop = true;
        fuenteMusica.Play();
    }
}