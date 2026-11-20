using UnityEngine;

public class BinController : MonoBehaviour
{
    public TipoResiduo tipoAceptado;

    [Header("Configuración de Puntuación")]
    public int puntosPorBasura = 10;

    [Header("Sonidos")]
    public AudioClip sonidoDepositar;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos si el que entró al tacho es el Jugador
        if (other.CompareTag("Player"))
        {
            PlayerPickup pickup = other.GetComponent<PlayerPickup>();

            // Verificamos si el jugador efectivamente trae basura en la mano
            if (pickup != null && pickup.TieneBasura())
            {
                TrashItem itemBasura = pickup.ObtenerBasuraActual().GetComponent<TrashItem>();

                if (itemBasura != null)
                {
                    // Comparamos directamente por enum (cero asignación de memoria)
                    if (itemBasura.tipoResiduo == tipoAceptado)
                    {
                        Debug.Log("¡Correcto! Entregaste " + itemBasura.tipoResiduo + " en el tacho " + tipoAceptado);

                        // Sonido al depositar
                        if (sonidoDepositar != null)
                        {
                            if (audioSource != null)
                            {
                                audioSource.PlayOneShot(sonidoDepositar);
                            }
                            else
                            {
                                AudioSource.PlayClipAtPoint(sonidoDepositar, transform.position);
                            }
                        }

                        // Sumamos los puntos
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.SumarPuntos(puntosPorBasura);
                        }

                        // Delegamos la entrega y limpieza al jugador de forma limpia
                        pickup.EntregarBasura();
                    }
                    else
                    {
                        Debug.Log("¡Error! Este tacho es para " + tipoAceptado + ", no para " + itemBasura.tipoResiduo);
                    }
                }
            }
        }
    }
}
