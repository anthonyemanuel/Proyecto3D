using UnityEngine;

namespace SneakyRecycling.UI
{
    /// <summary>
    /// Movimiento flotante y rotación suave exclusiva para los modelos decorativos de la escena 3D del Menú de Inicio.
    /// </summary>
    public class MenuIdleFloat : MonoBehaviour
    {
        [SerializeField] private float floatAmplitude = 0.08f;
        [SerializeField] private float floatFrequency = 1.1f;
        [SerializeField] private float phaseOffset = 0f;
        [SerializeField] private Vector3 rotateSpeed = new Vector3(0f, 12f, 0f);

        private Vector3 startLocalPos;

        private void Start()
        {
            startLocalPos = transform.localPosition;
        }

        private void Update()
        {
            float y = Mathf.Sin(Time.time * floatFrequency + phaseOffset) * floatAmplitude;
            transform.localPosition = startLocalPos + new Vector3(0f, y, 0f);
            if (rotateSpeed.sqrMagnitude > 0.0001f)
            {
                transform.Rotate(rotateSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
