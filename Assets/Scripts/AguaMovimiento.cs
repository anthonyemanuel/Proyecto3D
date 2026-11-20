using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class AguaMovimiento : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

    [Tooltip("Qué tan rápido se desplazan las ondas (X e Y)")]
    public Vector2 velocidad = new Vector2(0.02f, 0.01f);

    private Renderer rend;
    private Material materialAgua;
    private Vector2 desplazamiento;

    void Start()
    {
        rend = GetComponent<Renderer>();
        materialAgua = rend.material;
    }

    void Update()
    {
        desplazamiento += velocidad * Time.deltaTime;
        desplazamiento.x %= 1f;
        desplazamiento.y %= 1f;

        // Acceso optimizado mediante ID numérico y material cacheado
        if (materialAgua != null)
        {
            materialAgua.SetTextureOffset(BaseMapId, desplazamiento);
        }
    }

    void OnDestroy()
    {
        // Evitamos fugas de memoria del material instanciado en tiempo de ejecución
        if (materialAgua != null)
        {
            Destroy(materialAgua);
        }
    }
}
