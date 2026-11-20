using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI y Puntuación")]
    public TextMeshProUGUI textoPuntajeUI;
    private int puntajeActual = 0;
    public int PuntajeActual => puntajeActual;

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
            textoPuntajeUI.SetText("Puntaje: {0}", puntajeActual);
        }
    }
}