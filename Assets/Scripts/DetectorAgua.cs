using UnityEngine;

public class DetectorAgua : MonoBehaviour
{
    [Header("Estado actual")]
    public bool estaEnAgua = false;

    // Se ejecuta cuando los pies/cuerpo del jugador entran en contacto con el agua
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Agua"))
        {
            if (!estaEnAgua)
            {
                estaEnAgua = true;
                Debug.Log("¡CUIDADO! Has pisado el agua. El monstruo escuchó el ruido.");
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
    }
}