using UnityEngine;
using System.Collections.Generic;

public class PlayerPickup : MonoBehaviour
{
    private List<GameObject> basurasEnRango = new List<GameObject>();
    
    [Header("Referencias de Carga")]
    public GameObject basuraEnMano = null; // La basura que estamos cargando actualmente
    public Transform puntoMano;           // El objeto PuntoMano que creamos frente al jugador

    void Update()
    {
        // Si presionamos la E y no tenemos nada en las manos, recogemos
        if (Input.GetKeyDown(KeyCode.E) && basuraEnMano == null)
        {
            IntentarRecogerBasura();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Basura"))
        {
            if (!basurasEnRango.Contains(other.gameObject) && basuraEnMano != other.gameObject)
            {
                basurasEnRango.Add(other.gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Basura"))
        {
            if (basurasEnRango.Contains(other.gameObject))
            {
                basurasEnRango.Remove(other.gameObject);
            }
        }
    }

    void IntentarRecogerBasura()
    {
        basurasEnRango.RemoveAll(item => item == null);

        if (basurasEnRango.Count > 0)
        {
            // Tomamos la basura más cercana
            GameObject basuraAAtrapar = basurasEnRango[0];
            basurasEnRango.Remove(basuraAAtrapar);

            basuraEnMano = basuraAAtrapar;

            // 1. Desactivamos las físicas para que no salga volando ni choque con nosotros
            Rigidbody rb = basuraEnMano.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Collider col = basuraEnMano.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // 2. Desactivamos temporariamente su script de huida
            TrashFlee flee = basuraEnMano.GetComponent<TrashFlee>();
            if (flee != null) flee.enabled = false;

            // 3. La ponemos en la mano del jugador
            if (puntoMano != null)
            {
                basuraEnMano.transform.SetParent(puntoMano);
                basuraEnMano.transform.localPosition = Vector3.zero;
                basuraEnMano.transform.localRotation = Quaternion.identity;
            }
            else
            {
                basuraEnMano.transform.SetParent(transform);
                basuraEnMano.transform.localPosition = new Vector3(0, 0.5f, 1.2f);
            }

            Debug.Log("¡Has levantado la basura! Llévala a su tacho.");
        }
    }
}