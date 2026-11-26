using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Anima proceduralmente todas las partes del cuerpo generadas por ProceduralBodyRig:
/// oscilación de pelvis/cuerpo, contra-torsión y respiración del torso, mirada orgánica de la cabeza,
/// balanceo pendular de brazos con desfase natural, zancada de piernas y movimiento de alas/cola.
/// </summary>
[RequireComponent(typeof(ProceduralBodyRig))]
public class ProceduralBodyAnimator : MonoBehaviour
{
    [Header("Control de Movimiento")]
    [Tooltip("Mantiene la animación de caminata activa aunque el objeto no tenga NavMeshAgent ni Rigidbody.")]
    public bool caminarEnElSitio = true;

    [Tooltip("Si existe NavMeshAgent, CharacterController o Rigidbody, adapta la animación a su velocidad real.")]
    public bool usarVelocidadRealSiExiste = true;

    [Tooltip("Velocidad de referencia para alcanzar la amplitud máxima de caminata.")]
    public float velocidadReferencia = 2.0f;

    [Range(0f, 2f)]
    [Tooltip("Multiplicador global de intensidad de la animación.")]
    public float intensidadGlobal = 1.0f;

    [Header("Respiración y Vida Orgánica (Idle)")]
    public float frecuenciaRespiracion = 1.6f;
    public float amplitudRespiracion = 2.2f;
    public float velocidadMiradaOrganica = 0.45f;

    private ProceduralBodyRig rig;
    private NavMeshAgent navAgent;
    private CharacterController charController;
    private Rigidbody rb;

    private Vector3 pelvisBaseLocalPos;
    private float walkPhase;
    private float breathPhase;
    private float noiseSeed;
    private float intensidadActual;
    private bool baseCapturada;

    private void Start()
    {
        rig = GetComponent<ProceduralBodyRig>();
        navAgent = GetComponent<NavMeshAgent>();
        charController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        // Desfase único por instancia para evitar que todos caminen sincronizados como robots
        float seed = Mathf.Abs(GetInstanceID() * 0.137f + transform.position.x * 0.71f + transform.position.z * 0.37f);
        walkPhase = seed % (Mathf.PI * 2f);
        breathPhase = (seed * 1.7f) % (Mathf.PI * 2f);
        noiseSeed = seed % 100f;

        CapturarPosesBase();
    }

    private void CapturarPosesBase()
    {
        if (rig != null && rig.rigPelvis != null)
        {
            pelvisBaseLocalPos = rig.rigPelvis.localPosition;
            baseCapturada = true;
        }
    }

    private void LateUpdate()
    {
        if (rig == null || !rig.estaInicializado || rig.perfil == null) return;
        if (!baseCapturada) CapturarPosesBase();

        ProceduralRigProfile p = rig.perfil;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float factorMovimiento = CalcularFactorMovimiento();
        intensidadActual = Mathf.Lerp(intensidadActual, factorMovimiento * intensidadGlobal, 1f - Mathf.Exp(-8f * dt));

        // Modulación orgánica sutil para que el ritmo nunca sea completamente mecánico
        float microVar = 0.94f + 0.12f * Mathf.PerlinNoise(noiseSeed, Time.time * 0.35f);
        float freqActual = p.frecuenciaCaminata * Mathf.Lerp(0.45f, 1.0f, intensidadActual) * microVar;

        walkPhase += dt * freqActual;
        breathPhase += dt * frecuenciaRespiracion;

        AplicarAnimacionCuerpo(p, intensidadActual);
    }

    private float CalcularFactorMovimiento()
    {
        if (usarVelocidadRealSiExiste)
        {
            if (navAgent != null && navAgent.enabled)
            {
                float speed = navAgent.velocity.magnitude;
                return Mathf.Clamp01(speed / Mathf.Max(0.1f, velocidadReferencia));
            }
            if (charController != null && charController.enabled)
            {
                Vector3 vel = charController.velocity;
                vel.y = 0f;
                return Mathf.Clamp01(vel.magnitude / Mathf.Max(0.1f, velocidadReferencia));
            }
            if (rb != null && !rb.isKinematic)
            {
                Vector3 vel = rb.linearVelocity;
                vel.y = 0f;
                return Mathf.Clamp01(vel.magnitude / Mathf.Max(0.1f, velocidadReferencia));
            }
        }

        return caminarEnElSitio ? 1.0f : 0.15f;
    }

    private void AplicarAnimacionCuerpo(ProceduralRigProfile p, float walkAmp)
    {
        float sinP = Mathf.Sin(walkPhase);
        float cosP = Mathf.Cos(walkPhase);
        float doubleBob = -Mathf.Cos(walkPhase * 2f); // Rebote vertical doble por cada ciclo completo
        float breath = Mathf.Sin(breathPhase);

        // Desfase secundario para brazos (follow-through natural)
        float armSwing = Mathf.Sin(walkPhase - 0.28f);
        float armTwist = Mathf.Cos(walkPhase - 0.45f);

        float meshH = rig.meshBounds.size.y;
        float meshW = rig.meshBounds.size.x;

        // 1. PELVIS / CUERPO COMPLETO: rebote vertical, vaivén lateral e inclinación de cadera
        if (rig.rigPelvis != null)
        {
            float bobY = (doubleBob * p.amplitudReboteY * walkAmp + breath * 0.005f) * meshH;
            float swayX = sinP * p.amplitudBalanceoX * walkAmp * meshW;
            rig.rigPelvis.localPosition = pelvisBaseLocalPos + new Vector3(swayX, bobY, 0f);

            float hipRollZ = cosP * p.inclinacionCadera * walkAmp;
            float hipYawY = sinP * (p.inclinacionCadera * 0.85f) * walkAmp;
            rig.rigPelvis.localRotation = Quaternion.Euler(0f, hipYawY, hipRollZ);
        }

        // 2. TORSO / PECHO: contra-rotación respecto a la cadera + respiración + inclinación al caminar
        if (rig.rigTorso != null)
        {
            float torsoPitchX = p.inclinacionAdelanteTorso * walkAmp + breath * amplitudRespiracion;
            float torsoCounterYawY = -sinP * p.torsionTorso * walkAmp;
            float torsoCounterRollZ = -cosP * (p.inclinacionCadera * 0.45f) * walkAmp;
            rig.rigTorso.localRotation = Quaternion.Euler(torsoPitchX, torsoCounterYawY, torsoCounterRollZ);
        }

        // 3. CABEZA: estabilización de zancada + mirada orgánica con ruido Perlin
        if (rig.rigHead != null)
        {
            float tNoise = Time.time * velocidadMiradaOrganica;
            float lookYaw = (Mathf.PerlinNoise(noiseSeed + 11f, tNoise) - 0.5f) * 2f * p.movimientoCabeza * 1.4f;
            float lookPitch = (Mathf.PerlinNoise(noiseSeed + 23f, tNoise * 0.8f) - 0.5f) * 2f * p.movimientoCabeza * 0.8f;
            float headNod = doubleBob * (p.movimientoCabeza * 0.45f) * walkAmp - breath * (amplitudRespiracion * 0.5f);
            float headTilt = sinP * (p.movimientoCabeza * 0.3f) * walkAmp;

            rig.rigHead.localRotation = Quaternion.Euler(headNod + lookPitch, lookYaw, headTilt);
        }

        // 4. BRAZOS: balanceo pendular opuesto a las piernas + apertura lateral + torsión de hombro
        if (rig.rigArmL != null)
        {
            float swingX = armSwing * p.balanceoBrazosX * walkAmp + p.offsetBrazoIzqX + breath * 1.5f;
            float flareZ = p.aperturaBrazosZ + Mathf.Abs(armSwing) * (p.aperturaBrazosZ * 0.35f) * walkAmp;
            float twistY = armTwist * 7.5f * walkAmp;
            rig.rigArmL.localRotation = Quaternion.Euler(swingX, twistY, flareZ);
        }

        if (rig.rigArmR != null)
        {
            float swingX = -armSwing * p.balanceoBrazosX * walkAmp + p.offsetBrazoDerX + breath * 1.5f;
            float flareZ = -(p.aperturaBrazosZ + Mathf.Abs(armSwing) * (p.aperturaBrazosZ * 0.35f) * walkAmp);
            float twistY = -armTwist * 7.5f * walkAmp;
            rig.rigArmR.localRotation = Quaternion.Euler(swingX, twistY, flareZ);
        }

        // 5. PIERNAS: zancada alterna sincronizada con el rebote de pelvis
        if (rig.rigLegL != null)
        {
            float strideX = -sinP * p.zancadaPiernasX * walkAmp;
            float stepRollZ = Mathf.Max(0f, cosP) * 4.0f * walkAmp;
            rig.rigLegL.localRotation = Quaternion.Euler(strideX, 0f, stepRollZ);
        }

        if (rig.rigLegR != null)
        {
            float strideX = sinP * p.zancadaPiernasX * walkAmp;
            float stepRollZ = -Mathf.Max(0f, -cosP) * 4.0f * walkAmp;
            rig.rigLegR.localRotation = Quaternion.Euler(strideX, 0f, stepRollZ);
        }

        // 6. ALAS Y COLA (para Monstruo cuadrúpedo)
        if (p.usarAlasYCola)
        {
            float wingFlap = Mathf.Sin(walkPhase - 0.5f) * p.amplitudAlasYCola * Mathf.Max(0.35f, walkAmp) + breath * 3.0f;
            if (rig.rigWingL != null)
            {
                rig.rigWingL.localRotation = Quaternion.Euler(wingFlap * 0.35f, 0f, wingFlap);
            }
            if (rig.rigWingR != null)
            {
                rig.rigWingR.localRotation = Quaternion.Euler(wingFlap * 0.35f, 0f, -wingFlap);
            }
            if (rig.rigTail != null)
            {
                float tailYaw = Mathf.Sin(walkPhase - 0.9f) * (p.amplitudAlasYCola * 1.15f) * Mathf.Max(0.4f, walkAmp);
                float tailPitch = Mathf.Cos(walkPhase * 2f - 0.6f) * (p.amplitudAlasYCola * 0.35f);
                rig.rigTail.localRotation = Quaternion.Euler(tailPitch, tailYaw, 0f);
            }
        }
    }
}
