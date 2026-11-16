using UnityEngine;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Movimiento cinematográfico sutil de cámara para el diorama 3D del Menú de Inicio.
    /// </summary>
    public class MenuCameraDrift : MonoBehaviour
    {
        [Header("Oscilación Atmosférica")]
        [SerializeField] private Vector3 positionAmplitude = new Vector3(0.32f, 0.14f, 0.22f);
        [SerializeField] private Vector2 driftFrequency = new Vector2(0.22f, 0.16f);

        [Header("Paralaje por Cursor")]
        [SerializeField] private float mouseYawRange = 2.2f;
        [SerializeField] private float mousePitchRange = 1.4f;
        [SerializeField] private float smoothSpeed = 3.0f;

        private Vector3 baseLocalPos;
        private Quaternion baseLocalRot;
        private Vector2 smoothedOffset;

        private void Start()
        {
            baseLocalPos = transform.localPosition;
            baseLocalRot = transform.localRotation;
        }

        private void Update()
        {
            float t = Time.time;

            Vector3 drift = new Vector3(
                Mathf.Sin(t * driftFrequency.x) * positionAmplitude.x,
                Mathf.Sin(t * driftFrequency.y * 1.3f) * positionAmplitude.y,
                Mathf.Cos(t * driftFrequency.y) * positionAmplitude.z
            );
            transform.localPosition = baseLocalPos + drift;

            Vector2 normalizedMouse = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null && Screen.width > 0 && Screen.height > 0)
            {
                Vector2 mPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                normalizedMouse = new Vector2(
                    Mathf.Clamp(mPos.x / Screen.width - 0.5f, -0.5f, 0.5f) * 2f,
                    Mathf.Clamp(mPos.y / Screen.height - 0.5f, -0.5f, 0.5f) * 2f
                );
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.InputSystem.Mouse.current == null && Screen.width > 0 && Screen.height > 0)
            {
                Vector3 mPos = Input.mousePosition;
                normalizedMouse = new Vector2(
                    Mathf.Clamp(mPos.x / Screen.width - 0.5f, -0.5f, 0.5f) * 2f,
                    Mathf.Clamp(mPos.y / Screen.height - 0.5f, -0.5f, 0.5f) * 2f
                );
            }
#endif

            smoothedOffset = Vector2.Lerp(smoothedOffset, normalizedMouse, Time.deltaTime * smoothSpeed);

            float swayYaw = Mathf.Sin(t * 0.18f) * 1.2f + smoothedOffset.x * mouseYawRange;
            float swayPitch = Mathf.Cos(t * 0.14f) * 0.7f - smoothedOffset.y * mousePitchRange;
            transform.localRotation = baseLocalRot * Quaternion.Euler(swayPitch, swayYaw, 0f);
        }
    }
}
