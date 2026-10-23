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

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Bloquear y ocultar el cursor para control de cámara estilo FPS
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

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        float velocidadActual = Input.GetKey(KeyCode.LeftShift) ? velocidadCorrer : velocidadCaminar;

        Vector3 mover = transform.right * x + transform.forward * z;
        controller.Move(mover * velocidadActual * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && estaEnSuelo)
        {
            velocidadVertical.y = Mathf.Sqrt(fuerzaSalto * -2f * gravedad);
        }

        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);
    }
}