using UnityEngine;

public class BinController : MonoBehaviour
{
    public enum TipoTacho { Papel, Plastico, Organico }
    public TipoTacho tipoAceptado;

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
                        
                        // Sumamos los puntos
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.SumarPuntos(10);
                        }

                        // Destruimos la basura que llevábamos en la mano
                        Destroy(pickup.basuraEnMano);
                        
                        // Limpiamos la mano del jugador
                        pickup.basuraEnMano = null;
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