using System;
using UnityEngine;

/// <summary>
/// Tipos de anatomía soportados para el auto-rigging y la animación procedural.
/// </summary>
public enum RigCreatureType
{
    AutoDetect,
    CajaMonstruo,
    BotellaMonstruo,
    ManzanaMonstruo,
    Anthony,
    MonstruoCuadrupedo
}

/// <summary>
/// Define las regiones anatómicas normalizadas [0..1] en el espacio local de la malla,
/// los pivotes de las articulaciones y los parámetros de movimiento procedural.
/// </summary>
[Serializable]
public class ProceduralRigProfile
{
    [Header("Identidad Anatómica")]
    public RigCreatureType tipoCriatura = RigCreatureType.AutoDetect;

    [Header("Umbrales Normalizados de Extremidades [0..1]")]
    [Range(0.05f, 0.45f)] public float umbralBrazoIzqX = 0.27f;
    [Range(0.55f, 0.95f)] public float umbralBrazoDerX = 0.73f;
    [Range(0.10f, 0.60f)] public float alturaMinBrazoY = 0.28f;
    [Range(0.50f, 0.95f)] public float alturaMaxBrazoY = 0.80f;

    [Range(0.10f, 0.50f)] public float umbralPiernasY = 0.30f;
    [Range(0.50f, 0.90f)] public float umbralCabezaY = 0.72f;
    [Range(0.02f, 0.15f)] public float anchoMezclaArticulacion = 0.06f;

    [Tooltip("Si está activo, divide las piernas a lo largo del eje Z (ideal para modelos en pose de zancada como Anthony).")]
    public bool dividirPiernasPorZ = false;
    [Range(0.2f, 0.8f)] public float lineaDivisionPiernas = 0.50f;

    [Header("Extremidades Especiales (Monstruo Cuadrúpedo)")]
    public bool usarAlasYCola = false;
    [Range(0.0f, 0.5f)] public float umbralColaZ = 0.28f;
    [Range(0.5f, 0.95f)] public float umbralCabezaZ = 0.68f;
    [Range(0.45f, 0.85f)] public float umbralAlasY = 0.62f;

    [Header("Pivotes Normalizados de Articulaciones [0..1]")]
    public Vector3 pivotePelvis = new Vector3(0.50f, 0.32f, 0.50f);
    public Vector3 pivoteTorso = new Vector3(0.50f, 0.45f, 0.50f);
    public Vector3 pivoteCabeza = new Vector3(0.50f, 0.73f, 0.52f);
    public Vector3 pivoteHombroIzq = new Vector3(0.28f, 0.58f, 0.52f);
    public Vector3 pivoteHombroDer = new Vector3(0.72f, 0.58f, 0.52f);
    public Vector3 pivoteCaderaIzq = new Vector3(0.38f, 0.30f, 0.50f);
    public Vector3 pivoteCaderaDer = new Vector3(0.62f, 0.30f, 0.50f);
    public Vector3 pivoteAlaIzq = new Vector3(0.40f, 0.66f, 0.48f);
    public Vector3 pivoteAlaDer = new Vector3(0.60f, 0.66f, 0.48f);
    public Vector3 pivoteCola = new Vector3(0.50f, 0.22f, 0.30f);

    [Header("Parámetros de Animación Procedural")]
    [Tooltip("Velocidad del ciclo de caminata en radianes/segundo.")]
    public float frecuenciaCaminata = 5.2f;
    [Tooltip("Rebote vertical del cuerpo (fracción de la altura de la malla).")]
    public float amplitudReboteY = 0.032f;
    [Tooltip("Balanceo lateral del cuerpo (fracción del ancho de la malla).")]
    public float amplitudBalanceoX = 0.024f;
    [Tooltip("Inclinación lateral de cadera en grados.")]
    public float inclinacionCadera = 5.5f;
    [Tooltip("Rotación/torsión del torso en grados.")]
    public float torsionTorso = 8.0f;
    [Tooltip("Inclinación hacia adelante del torso al caminar en grados.")]
    public float inclinacionAdelanteTorso = 4.5f;
    [Tooltip("Ángulo máximo de balanceo hacia adelante/atrás de los brazos en grados.")]
    public float balanceoBrazosX = 28.0f;
    [Tooltip("Ángulo de apertura lateral de los brazos en grados.")]
    public float aperturaBrazosZ = 7.0f;
    [Tooltip("Ángulo de zancada de las piernas en grados.")]
    public float zancadaPiernasX = 22.0f;
    [Tooltip("Movimiento de cabeza en grados.")]
    public float movimientoCabeza = 6.5f;
    [Tooltip("Elevación base adicional del brazo izquierdo (útil para armas o poses asimétricas).")]
    public float offsetBrazoIzqX = 0f;
    [Tooltip("Elevación base adicional del brazo derecho.")]
    public float offsetBrazoDerX = 0f;
    [Tooltip("Amplitud de movimiento para alas y cola (si aplica).")]
    public float amplitudAlasYCola = 16.0f;

    /// <summary>
    /// Detecta automáticamente el perfil anatómico más adecuado según el nombre del GameObject.
    /// </summary>
    public static RigCreatureType DetectarTipoPorNombre(string nombreObjeto)
    {
        if (string.IsNullOrEmpty(nombreObjeto)) return RigCreatureType.CajaMonstruo;

        string lower = nombreObjeto.ToLowerInvariant();
        if (lower.Contains("anthony")) return RigCreatureType.Anthony;
        if (lower.Contains("caja")) return RigCreatureType.CajaMonstruo;
        if (lower.Contains("botella")) return RigCreatureType.BotellaMonstruo;
        if (lower.Contains("manzana")) return RigCreatureType.ManzanaMonstruo;
        if (lower.Contains("monstruo")) return RigCreatureType.MonstruoCuadrupedo;
        return RigCreatureType.CajaMonstruo;
    }

    /// <summary>
    /// Crea un perfil preconfigurado y calibrado para cada modelo de la escena.
    /// </summary>
    public static ProceduralRigProfile CrearPreset(RigCreatureType tipo)
    {
        switch (tipo)
        {
            case RigCreatureType.Anthony:
                return new ProceduralRigProfile
                {
                    tipoCriatura = RigCreatureType.Anthony,
                    umbralBrazoIzqX = 0.25f,
                    umbralBrazoDerX = 0.75f,
                    alturaMinBrazoY = 0.34f,
                    alturaMaxBrazoY = 0.68f,
                    umbralPiernasY = 0.35f,
                    umbralCabezaY = 0.66f,
                    anchoMezclaArticulacion = 0.055f,
                    dividirPiernasPorZ = true,
                    lineaDivisionPiernas = 0.49f,
                    pivotePelvis = new Vector3(0.54f, 0.36f, 0.48f),
                    pivoteTorso = new Vector3(0.54f, 0.48f, 0.48f),
                    pivoteCabeza = new Vector3(0.54f, 0.66f, 0.49f),
                    pivoteHombroIzq = new Vector3(0.28f, 0.58f, 0.48f),
                    pivoteHombroDer = new Vector3(0.74f, 0.58f, 0.50f),
                    pivoteCaderaIzq = new Vector3(0.48f, 0.34f, 0.36f),
                    pivoteCaderaDer = new Vector3(0.60f, 0.34f, 0.64f),
                    frecuenciaCaminata = 6.2f,
                    amplitudReboteY = 0.032f,
                    amplitudBalanceoX = 0.022f,
                    inclinacionCadera = 5.0f,
                    torsionTorso = 9.0f,
                    inclinacionAdelanteTorso = 5.0f,
                    balanceoBrazosX = 32.0f,
                    aperturaBrazosZ = 6.0f,
                    zancadaPiernasX = 20.0f,
                    movimientoCabeza = 5.5f
                };

            case RigCreatureType.CajaMonstruo:
                return new ProceduralRigProfile
                {
                    tipoCriatura = RigCreatureType.CajaMonstruo,
                    umbralBrazoIzqX = 0.27f,
                    umbralBrazoDerX = 0.73f,
                    alturaMinBrazoY = 0.28f,
                    alturaMaxBrazoY = 0.76f,
                    umbralPiernasY = 0.29f,
                    umbralCabezaY = 0.74f,
                    anchoMezclaArticulacion = 0.06f,
                    dividirPiernasPorZ = false,
                    lineaDivisionPiernas = 0.50f,
                    pivotePelvis = new Vector3(0.51f, 0.31f, 0.53f),
                    pivoteTorso = new Vector3(0.51f, 0.46f, 0.53f),
                    pivoteCabeza = new Vector3(0.51f, 0.75f, 0.53f),
                    pivoteHombroIzq = new Vector3(0.28f, 0.57f, 0.52f),
                    pivoteHombroDer = new Vector3(0.72f, 0.57f, 0.52f),
                    pivoteCaderaIzq = new Vector3(0.36f, 0.28f, 0.53f),
                    pivoteCaderaDer = new Vector3(0.66f, 0.28f, 0.53f),
                    frecuenciaCaminata = 5.4f,
                    amplitudReboteY = 0.038f,
                    amplitudBalanceoX = 0.028f,
                    inclinacionCadera = 6.5f,
                    torsionTorso = 8.0f,
                    inclinacionAdelanteTorso = 4.0f,
                    balanceoBrazosX = 30.0f,
                    aperturaBrazosZ = 9.0f,
                    zancadaPiernasX = 24.0f,
                    movimientoCabeza = 6.0f
                };

            case RigCreatureType.BotellaMonstruo:
                return new ProceduralRigProfile
                {
                    tipoCriatura = RigCreatureType.BotellaMonstruo,
                    umbralBrazoIzqX = 0.24f,
                    umbralBrazoDerX = 0.74f,
                    alturaMinBrazoY = 0.26f,
                    alturaMaxBrazoY = 0.85f,
                    umbralPiernasY = 0.27f,
                    umbralCabezaY = 0.76f,
                    anchoMezclaArticulacion = 0.055f,
                    dividirPiernasPorZ = false,
                    lineaDivisionPiernas = 0.50f,
                    pivotePelvis = new Vector3(0.50f, 0.30f, 0.55f),
                    pivoteTorso = new Vector3(0.50f, 0.48f, 0.56f),
                    pivoteCabeza = new Vector3(0.50f, 0.77f, 0.60f),
                    pivoteHombroIzq = new Vector3(0.26f, 0.64f, 0.56f),
                    pivoteHombroDer = new Vector3(0.73f, 0.64f, 0.56f),
                    pivoteCaderaIzq = new Vector3(0.36f, 0.26f, 0.54f),
                    pivoteCaderaDer = new Vector3(0.64f, 0.26f, 0.54f),
                    frecuenciaCaminata = 5.8f,
                    amplitudReboteY = 0.035f,
                    amplitudBalanceoX = 0.026f,
                    inclinacionCadera = 6.0f,
                    torsionTorso = 10.0f,
                    inclinacionAdelanteTorso = 4.5f,
                    balanceoBrazosX = 34.0f,
                    aperturaBrazosZ = 10.0f,
                    zancadaPiernasX = 24.0f,
                    movimientoCabeza = 7.5f
                };

            case RigCreatureType.ManzanaMonstruo:
                return new ProceduralRigProfile
                {
                    tipoCriatura = RigCreatureType.ManzanaMonstruo,
                    umbralBrazoIzqX = 0.25f,
                    umbralBrazoDerX = 0.75f,
                    alturaMinBrazoY = 0.24f,
                    alturaMaxBrazoY = 0.78f,
                    umbralPiernasY = 0.24f,
                    umbralCabezaY = 0.74f,
                    anchoMezclaArticulacion = 0.05f,
                    dividirPiernasPorZ = false,
                    lineaDivisionPiernas = 0.52f,
                    pivotePelvis = new Vector3(0.52f, 0.27f, 0.60f),
                    pivoteTorso = new Vector3(0.52f, 0.45f, 0.62f),
                    pivoteCabeza = new Vector3(0.52f, 0.74f, 0.64f),
                    pivoteHombroIzq = new Vector3(0.27f, 0.56f, 0.62f),
                    pivoteHombroDer = new Vector3(0.73f, 0.54f, 0.62f),
                    pivoteCaderaIzq = new Vector3(0.38f, 0.23f, 0.58f),
                    pivoteCaderaDer = new Vector3(0.66f, 0.23f, 0.58f),
                    frecuenciaCaminata = 5.0f,
                    amplitudReboteY = 0.036f,
                    amplitudBalanceoX = 0.030f,
                    inclinacionCadera = 7.0f,
                    torsionTorso = 9.0f,
                    inclinacionAdelanteTorso = 5.0f,
                    balanceoBrazosX = 26.0f,
                    aperturaBrazosZ = 8.0f,
                    zancadaPiernasX = 22.0f,
                    movimientoCabeza = 6.5f,
                    offsetBrazoIzqX = -8.0f // Sostiene el garrote ligeramente alzado
                };

            case RigCreatureType.MonstruoCuadrupedo:
            default:
                return new ProceduralRigProfile
                {
                    tipoCriatura = RigCreatureType.MonstruoCuadrupedo,
                    umbralBrazoIzqX = 0.28f,
                    umbralBrazoDerX = 0.76f,
                    alturaMinBrazoY = 0.10f,
                    alturaMaxBrazoY = 0.65f,
                    umbralPiernasY = 0.22f,
                    umbralCabezaY = 0.72f,
                    anchoMezclaArticulacion = 0.07f,
                    dividirPiernasPorZ = false,
                    lineaDivisionPiernas = 0.54f,
                    usarAlasYCola = true,
                    umbralColaZ = 0.30f,
                    umbralCabezaZ = 0.66f,
                    umbralAlasY = 0.62f,
                    pivotePelvis = new Vector3(0.54f, 0.28f, 0.50f),
                    pivoteTorso = new Vector3(0.54f, 0.40f, 0.56f),
                    pivoteCabeza = new Vector3(0.54f, 0.46f, 0.68f),
                    pivoteHombroIzq = new Vector3(0.30f, 0.38f, 0.64f),
                    pivoteHombroDer = new Vector3(0.75f, 0.38f, 0.62f),
                    pivoteCaderaIzq = new Vector3(0.40f, 0.22f, 0.42f),
                    pivoteCaderaDer = new Vector3(0.68f, 0.22f, 0.42f),
                    pivoteAlaIzq = new Vector3(0.42f, 0.64f, 0.52f),
                    pivoteAlaDer = new Vector3(0.66f, 0.64f, 0.52f),
                    pivoteCola = new Vector3(0.54f, 0.20f, 0.30f),
                    frecuenciaCaminata = 3.8f,
                    amplitudReboteY = 0.032f,
                    amplitudBalanceoX = 0.025f,
                    inclinacionCadera = 4.5f,
                    torsionTorso = 7.0f,
                    inclinacionAdelanteTorso = 3.5f,
                    balanceoBrazosX = 24.0f,
                    aperturaBrazosZ = 10.0f,
                    zancadaPiernasX = 16.0f,
                    movimientoCabeza = 8.0f,
                    amplitudAlasYCola = 15.0f
                };
        }
    }
}
