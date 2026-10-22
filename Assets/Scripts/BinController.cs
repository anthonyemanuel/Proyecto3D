using UnityEngine;

public class BinController : MonoBehaviour
{
    public enum TipoTacho { Papel, Plastico, Organico }
    public TipoTacho tipoAceptado;

    private void OnTriggerEnter(Collider other)
    {
        // Verificamos si lo que entró tiene la etiqueta de basura
        if (other.CompareTag("Basura"))
        {
            // Intentamos obtener el componente TrashItem para saber qué tipo de basura es
            TrashItem itemBasura = other.GetComponent<TrashItem>();

            if (itemBasura != null)
            {
                // Comparamos si el tipo de la basura coincide con el tipo del tacho
                if (itemBasura.tipoResiduo.ToString() == tipoAceptado.ToString())
                {
                    Debug.Log("¡Correcto! Reciclaste " + itemBasura.tipoResiduo + " en el tacho " + tipoAceptado);
                    
                    // Sumamos puntos a través del GameManager
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SumarPuntos(10); // Cada residuo correcto otorga 10 puntos
                    }

                    // Destruimos la basura porque fue depositada en el lugar correcto
                    Destroy(other.gameObject);
                }
                else
                {
                    Debug.Log("¡Error! Este tacho es para " + tipoAceptado + ", no puedes meter " + itemBasura.tipoResiduo);
                }
            }
        }
    }
}