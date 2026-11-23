using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidadCaminar = 3.0f;
    public float velocidadCorrer = 8.0f;
    public float gravedad = -9.81f;
    public float fuerzaSalto = 1.5f;

    [Header("Sensibilidad de Cámara (1ra Persona)")]
    public Transform camaraTransform;
    public float sensibilidadMouse = 180.0f;

    private CharacterController controller;
    private Vector3 velocidadVertical;
    private bool estaEnSuelo;
    private float rotacionXCamara = 0f;

    // Variables para plataforma móvil
    private Transform plataformaActual;
    private Vector3 ultimaPosicionPlataforma;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        ManejarRotacionCamara();
        ManejarMovimiento();
    }

    void ManejarRotacionCamara()
    {
        if (camaraTransform == null) return;

        float mouseX = Input.GetAxis("Mouse X") * sensibilidadMouse * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadMouse * Time.deltaTime;

        rotacionXCamara -= mouseY;
        rotacionXCamara = Mathf.Clamp(rotacionXCamara, -80f, 80f);
        camaraTransform.localRotation = Quaternion.Euler(rotacionXCamara, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }

    void ManejarMovimiento()
    {
        estaEnSuelo = controller.isGrounded;

        if (estaEnSuelo && velocidadVertical.y < 0)
        {
            velocidadVertical.y = -2f;
        }

        // Si estamos parados sobre una plataforma móvil, nos movemos con ella
        if (plataformaActual != null)
        {
            Vector3 movimientoPlataforma = plataformaActual.position - ultimaPosicionPlataforma;
            controller.Move(movimientoPlataforma);
            ultimaPosicionPlataforma = plataformaActual.position;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float velocidadActual = Input.GetKey(KeyCode.LeftShift) ? velocidadCorrer : velocidadCaminar;

        Vector3 mover = transform.right * x + transform.forward * z;
        controller.Move(mover * velocidadActual * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && estaEnSuelo)
        {
            velocidadVertical.y = Mathf.Sqrt(fuerzaSalto * -2f * gravedad);
            plataformaActual = null; // Al saltar nos soltamos de la plataforma
        }

        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);

        // Si dejamos de tocar el suelo, nos soltamos de la plataforma
        if (!estaEnSuelo)
        {
            plataformaActual = null;
        }
    }

    // Detecta automáticamente qué objeto pisa el CharacterController
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Si lo que pisamos tiene el script PlataformaMovil (y está debajo de los pies)
        if (hit.normal.y > 0.5f && hit.gameObject.GetComponent<PlataformaMovil>() != null)
        {
            if (plataformaActual != hit.transform)
            {
                plataformaActual = hit.transform;
                ultimaPosicionPlataforma = plataformaActual.position;
            }
        }
        else if (hit.normal.y > 0.5f)
        {
            // Si pisamos cualquier otro suelo normal, soltamos la plataforma
            plataformaActual = null;
        }
    }
}