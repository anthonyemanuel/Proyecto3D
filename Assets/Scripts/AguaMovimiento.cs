using UnityEngine;

public class AguaMovimiento : MonoBehaviour
{
    [Tooltip("Qué tan rápido se desplazan las ondas (X e Y)")]
    public Vector2 velocidad = new Vector2(0.02f, 0.01f);

    private Renderer rend;
    private Vector2 desplazamiento;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        desplazamiento += velocidad * Time.deltaTime;
        desplazamiento.x %= 1f;
        desplazamiento.y %= 1f;

        rend.material.SetTextureOffset("_BaseMap", desplazamiento);
    }
}