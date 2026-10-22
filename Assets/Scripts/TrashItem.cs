using UnityEngine;

public class TrashItem : MonoBehaviour
{
    // Define qué tipo de residuo es este objeto específico
    public enum TipoBasura { Papel, Plastico, Organico }
    public TipoBasura tipoResiduo;
}