using System;
using UnityEngine;

public class DetectorAgua : MonoBehaviour
{
    [Header("Estado actual")]
    public bool estaEnAgua = false;

    public event Action<bool> OnEstadoAguaCambiado;

    // Se ejecuta cuando los pies/cuerpo del jugador entran en contacto con una superficie
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Solo considerar colisiones con el suelo (evitar que rozar paredes con tag Untagged cambie el estado)
        if (hit.normal.y < 0.5f) return;

        if (hit.gameObject.CompareTag("Agua"))
        {
            CambiarEstado(true, "¡CUIDADO! Has pisado el agua. El monstruo escuchó el ruido.");
        }
        else if (hit.gameObject.CompareTag("SueloSeguro") || hit.gameObject.CompareTag("Untagged"))
        {
            CambiarEstado(false, "¡A salvo! Estás sobre una plataforma segura.");
        }
    }

    private void CambiarEstado(bool enAgua, string mensaje)
    {
        if (estaEnAgua != enAgua)
        {
            estaEnAgua = enAgua;
            Debug.Log(mensaje);
            OnEstadoAguaCambiado?.Invoke(estaEnAgua);
        }
    }
}
