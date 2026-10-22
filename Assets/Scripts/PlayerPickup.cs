using UnityEngine;

public class PlayerPickup : MonoBehaviour
{
    [Header("Configuración de Agarrado")]
    public Transform holdPoint;       
    public float distanciaInteraccion = 3.0f; 
    
    private GameObject objetoSostenido;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (objetoSostenido == null)
            {
                IntentarAgarrarBasura();
            }
            else
            {
                SoltarBasura();
            }
        }
    }

    void IntentarAgarrarBasura()
    {
        RaycastHit hit;
        Vector3 origenRaycast = Camera.main.transform.position;
        Vector3 direccionRaycast = Camera.main.transform.forward;

        if (Physics.Raycast(origenRaycast, direccionRaycast, out hit, distanciaInteraccion))
        {
            if (hit.collider.CompareTag("Basura"))
            {
                objetoSostenido = hit.collider.gameObject;

                Rigidbody rb = objetoSostenido.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }

                objetoSostenido.transform.SetParent(holdPoint);
                objetoSostenido.transform.localPosition = Vector3.zero;
                objetoSostenido.transform.localRotation = Quaternion.identity;

                Debug.Log("¡Basura recogida!");
            }
        }
    }

    void SoltarBasura()
    {
        if (objetoSostenido != null)
        {
            Rigidbody rb = objetoSostenido.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            objetoSostenido.transform.SetParent(null);
            objetoSostenido = null;

            Debug.Log("Basura soltada.");
        }
    }
}