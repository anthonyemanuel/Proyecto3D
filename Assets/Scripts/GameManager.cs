using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("UI y Puntuación")]
    public TextMeshProUGUI textoPuntajeUI;
    private int puntajeActual = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        ActualizarTextoUI();
    }

    public void SumarPuntos(int cantidad)
    {
        puntajeActual += cantidad;
        ActualizarTextoUI();
        Debug.Log("¡Puntaje actual: " + puntajeActual + "!");
    }

    void ActualizarTextoUI()
    {
        if (textoPuntajeUI != null)
        {
            textoPuntajeUI.text = "Puntaje: " + puntajeActual;
        }
    }
}