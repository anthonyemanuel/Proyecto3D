using UnityEngine;

public class PlataformaMovil : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public Transform puntoA;
    public Transform puntoB;
    public float velocidad = 3f;
    public float tiempoEspera = 1f;

    private Transform destinoActual;
    private float temporizadorEspera;
    private bool estaEsperando = false;

    void Start()
    {
        destinoActual = puntoB;

        if (puntoA == null)
        {
            GameObject tempGO = new GameObject(gameObject.name + "_PuntoA");
            tempGO.transform.position = transform.position;
            puntoA = tempGO.transform;
        }
    }

    void Update()
    {
        if (puntoA == null || puntoB == null) return;

        if (estaEsperando)
        {
            temporizadorEspera += Time.deltaTime;
            if (temporizadorEspera >= tiempoEspera)
            {
                estaEsperando = false;
                temporizadorEspera = 0f;
            }
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, destinoActual.position, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, destinoActual.position) < 0.05f)
        {
            destinoActual = (destinoActual == puntoA) ? puntoB : puntoA;
            estaEsperando = true;
        }
    }
}