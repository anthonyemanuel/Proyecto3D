using UnityEngine;

public class TrashFlee : MonoBehaviour
{
    [Header("Configuración de Huida")]
    public float distanciaDeteccion = 4.0f; // A qué distancia empieza a huir
    public float velocidadHuida = 3.5f;     // Qué tan rápido corre del jugador

    private Transform jugador;
    private Rigidbody rb;

    void Start()
    {
        // Buscamos al jugador automáticamente por su Tag
        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
        {
            jugador = objJugador.transform;
        }

        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Si no hay jugador, o si el Rigidbody no existe, no hacemos nada
        if (jugador == null || rb == null) return;

        // Si la basura está siendo sostenida (isKinematic está activado al levantarla), no huye
        if (rb.isKinematic) return;

        // Medimos la distancia entre la basura y el jugador
        float distancia = Vector3.Distance(rb.position, jugador.position);

        if (distancia < distanciaDeteccion)
        {
            // Calculamos la dirección opuesta al jugador
            Vector3 direccionHuida = (rb.position - jugador.position).normalized;
            direccionHuida.y = 0; // Evitamos que se hunda en el suelo

            // Movemos la basura usando el Rigidbody en FixedUpdate para cálculo físico correcto
            Vector3 nuevaPosicion = rb.position + direccionHuida * velocidadHuida * Time.fixedDeltaTime;
            rb.MovePosition(nuevaPosicion);
        }
    }
}
