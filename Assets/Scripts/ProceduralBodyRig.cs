using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera una jerarquía real de huesos hijos (Pelvis, Torso, Cabeza, Brazos, Piernas y opcionalmente Alas/Cola)
/// y convierte la malla estática en un SkinnedMeshRenderer con pesos suavizados en tiempo de ejecución,
/// permitiendo animar cada parte del cuerpo de forma independiente sin modificar los FBX originales.
/// </summary>
[DisallowMultipleComponent]
public class ProceduralBodyRig : MonoBehaviour
{
    public ProceduralRigProfile perfil = new ProceduralRigProfile();

    [Header("Jerarquía de Huesos Generada")]
    public Transform rigPelvis;
    public Transform rigTorso;
    public Transform rigHead;
    public Transform rigArmL;
    public Transform rigArmR;
    public Transform rigLegL;
    public Transform rigLegR;
    public Transform rigWingL;
    public Transform rigWingR;
    public Transform rigTail;

    [HideInInspector] public Bounds meshBounds;
    [HideInInspector] public bool estaInicializado;

    private Mesh runtimeSkinnedMesh;
    private SkinnedMeshRenderer skinnedRenderer;
    private MeshRenderer staticRenderer;

    private void Reset()
    {
        InicializarPerfilPorNombre();
        EnsureBoneHierarchy();
    }

    private void Awake()
    {
        if (perfil == null || perfil.tipoCriatura == RigCreatureType.AutoDetect)
        {
            InicializarPerfilPorNombre();
        }

        EnsureBoneHierarchy();
        BuildRuntimeSkinnedMesh();
    }

    private void OnDestroy()
    {
        if (runtimeSkinnedMesh != null)
        {
            Destroy(runtimeSkinnedMesh);
            runtimeSkinnedMesh = null;
        }
    }

    /// <summary>
    /// Selecciona automáticamente el perfil anatómico calibrado para el GameObject actual.
    /// </summary>
    public void InicializarPerfilPorNombre()
    {
        RigCreatureType detectado = ProceduralRigProfile.DetectarTipoPorNombre(gameObject.name);
        perfil = ProceduralRigProfile.CrearPreset(detectado);
    }

    /// <summary>
    /// Crea o actualiza los Transforms de los huesos hijos según los pivotes anatómicos del perfil.
    /// Seguro para ejecutarse tanto en Editor como en Play Mode.
    /// </summary>
    public void EnsureBoneHierarchy()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return;

        if (perfil == null || perfil.tipoCriatura == RigCreatureType.AutoDetect)
        {
            InicializarPerfilPorNombre();
        }

        meshBounds = mf.sharedMesh.bounds;
        Vector3 min = meshBounds.min;
        Vector3 size = meshBounds.size;

        Vector3 pPelvis = min + Vector3.Scale(size, perfil.pivotePelvis);
        Vector3 pTorso = min + Vector3.Scale(size, perfil.pivoteTorso);
        Vector3 pHead = min + Vector3.Scale(size, perfil.pivoteCabeza);
        Vector3 pArmL = min + Vector3.Scale(size, perfil.pivoteHombroIzq);
        Vector3 pArmR = min + Vector3.Scale(size, perfil.pivoteHombroDer);
        Vector3 pLegL = min + Vector3.Scale(size, perfil.pivoteCaderaIzq);
        Vector3 pLegR = min + Vector3.Scale(size, perfil.pivoteCaderaDer);

        rigPelvis = GetOrCreateBone("Rig_Pelvis", transform, pPelvis);
        rigTorso = GetOrCreateBone("Rig_Torso", rigPelvis, pTorso - pPelvis);
        rigHead = GetOrCreateBone("Rig_Head", rigTorso, pHead - pTorso);
        rigArmL = GetOrCreateBone("Rig_Arm_L", rigTorso, pArmL - pTorso);
        rigArmR = GetOrCreateBone("Rig_Arm_R", rigTorso, pArmR - pTorso);
        rigLegL = GetOrCreateBone("Rig_Leg_L", rigPelvis, pLegL - pPelvis);
        rigLegR = GetOrCreateBone("Rig_Leg_R", rigPelvis, pLegR - pPelvis);

        if (perfil.usarAlasYCola)
        {
            Vector3 pWingL = min + Vector3.Scale(size, perfil.pivoteAlaIzq);
            Vector3 pWingR = min + Vector3.Scale(size, perfil.pivoteAlaDer);
            Vector3 pTail = min + Vector3.Scale(size, perfil.pivoteCola);

            rigWingL = GetOrCreateBone("Rig_Wing_L", rigTorso, pWingL - pTorso);
            rigWingR = GetOrCreateBone("Rig_Wing_R", rigTorso, pWingR - pTorso);
            rigTail = GetOrCreateBone("Rig_Tail", rigPelvis, pTail - pPelvis);
        }
    }

    private Transform GetOrCreateBone(string boneName, Transform parent, Vector3 localPos)
    {
        Transform existing = parent.Find(boneName);
        if (existing == null)
        {
            // Por si estaba en la raíz temporalmente
            existing = transform.Find(boneName);
        }

        GameObject go;
        if (existing == null)
        {
            go = new GameObject(boneName);
            existing = go.transform;
        }

        if (existing.parent != parent)
        {
            existing.SetParent(parent, false);
        }

        existing.localPosition = localPos;
        existing.localRotation = Quaternion.identity;
        existing.localScale = Vector3.one;
        return existing;
    }

    private void BuildRuntimeSkinnedMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        staticRenderer = GetComponent<MeshRenderer>();
        if (mf == null || mf.sharedMesh == null || staticRenderer == null)
        {
            Debug.LogWarning($"[ProceduralBodyRig] '{name}' necesita MeshFilter y MeshRenderer.", this);
            return;
        }

        Mesh sourceMesh = mf.sharedMesh;
        if (!sourceMesh.isReadable)
        {
            Debug.LogError($"[ProceduralBodyRig] La malla '{sourceMesh.name}' de '{name}' no tiene Read/Write Enabled en el importador FBX.", this);
            return;
        }

        List<Transform> boneList = new List<Transform>(10)
        {
            rigPelvis, // 0
            rigTorso,  // 1
            rigHead,   // 2
            rigArmL,   // 3
            rigArmR,   // 4
            rigLegL,   // 5
            rigLegR    // 6
        };

        if (perfil.usarAlasYCola)
        {
            boneList.Add(rigWingL); // 7
            boneList.Add(rigWingR); // 8
            boneList.Add(rigTail);  // 9
        }

        int boneCount = boneList.Count;
        Matrix4x4[] bindposes = new Matrix4x4[boneCount];
        Matrix4x4 rootLocalToWorld = transform.localToWorldMatrix;
        for (int i = 0; i < boneCount; i++)
        {
            bindposes[i] = boneList[i].worldToLocalMatrix * rootLocalToWorld;
        }

        runtimeSkinnedMesh = Instantiate(sourceMesh);
        runtimeSkinnedMesh.name = sourceMesh.name + "_ProceduralRig";

        Vector3[] verts = runtimeSkinnedMesh.vertices;
        int vertCount = verts.Length;
        BoneWeight[] weights = new BoneWeight[vertCount];

        Vector3 min = meshBounds.min;
        Vector3 size = meshBounds.size;
        float invX = size.x > 1e-6f ? 1f / size.x : 0f;
        float invY = size.y > 1e-6f ? 1f / size.y : 0f;
        float invZ = size.z > 1e-6f ? 1f / size.z : 0f;
        float blend = Mathf.Max(0.01f, perfil.anchoMezclaArticulacion);

        float[] raw = new float[10];

        for (int i = 0; i < vertCount; i++)
        {
            Vector3 v = verts[i];
            float nx = (v.x - min.x) * invX;
            float ny = (v.y - min.y) * invY;
            float nz = (v.z - min.z) * invZ;

            for (int k = 0; k < boneCount; k++) raw[k] = 0f;

            if (!perfil.usarAlasYCola)
            {
                float armL = SmoothLess(nx, perfil.umbralBrazoIzqX, blend)
                           * SmoothRange(ny, perfil.alturaMinBrazoY, perfil.alturaMaxBrazoY, blend);
                float armR = SmoothGreater(nx, perfil.umbralBrazoDerX, blend)
                           * SmoothRange(ny, perfil.alturaMinBrazoY, perfil.alturaMaxBrazoY, blend);
                float armTotal = Mathf.Clamp01(armL + armR);

                float bodyMask = 1f - armTotal;
                float legTotal = bodyMask * SmoothLess(ny, perfil.umbralPiernasY, blend);
                float legCoord = perfil.dividirPiernasPorZ ? nz : nx;
                float rightLegFrac = SmoothGreater(legCoord, perfil.lineaDivisionPiernas, blend);
                float legL = legTotal * (1f - rightLegFrac);
                float legR = legTotal * rightLegFrac;

                float upperMask = bodyMask - legTotal;
                float head = upperMask * SmoothGreater(ny, perfil.umbralCabezaY, blend);
                float torsoTotal = upperMask - head;
                float torsoFrac = SmoothGreater(ny, perfil.pivoteTorso.y, blend * 1.5f);

                raw[0] = torsoTotal * (1f - torsoFrac); // Pelvis
                raw[1] = torsoTotal * torsoFrac;        // Torso
                raw[2] = head;                          // Head
                raw[3] = armL;                          // Arm_L
                raw[4] = armR;                          // Arm_R
                raw[5] = legL;                          // Leg_L
                raw[6] = legR;                          // Leg_R
            }
            else
            {
                float armL = SmoothLess(nx, perfil.umbralBrazoIzqX, blend)
                           * SmoothRange(ny, perfil.alturaMinBrazoY, perfil.alturaMaxBrazoY, blend)
                           * SmoothGreater(nz, 0.30f, blend);
                float armR = SmoothGreater(nx, perfil.umbralBrazoDerX, blend)
                           * SmoothRange(ny, perfil.alturaMinBrazoY, perfil.alturaMaxBrazoY, blend)
                           * SmoothGreater(nz, 0.30f, blend);
                float armTotal = Mathf.Clamp01(armL + armR);

                float rem1 = 1f - armTotal;
                float wingTotal = rem1 * SmoothGreater(ny, perfil.umbralAlasY, blend)
                                       * SmoothRange(nz, 0.25f, 0.72f, blend);
                float wingRightFrac = SmoothGreater(nx, 0.54f, blend);
                float wingL = wingTotal * (1f - wingRightFrac);
                float wingR = wingTotal * wingRightFrac;

                float rem2 = rem1 - wingTotal;
                float tail = rem2 * SmoothLess(nz, perfil.umbralColaZ, blend);

                float rem3 = rem2 - tail;
                float head = rem3 * SmoothGreater(nz, perfil.umbralCabezaZ, blend);

                float rem4 = rem3 - head;
                float legTotal = rem4 * SmoothLess(ny, perfil.umbralPiernasY, blend);
                float legRightFrac = SmoothGreater(nx, perfil.lineaDivisionPiernas, blend);
                float legL = legTotal * (1f - legRightFrac);
                float legR = legTotal * legRightFrac;

                float torsoTotal = rem4 - legTotal;
                float torsoFrac = SmoothGreater(ny, perfil.pivoteTorso.y, blend);

                raw[0] = torsoTotal * (1f - torsoFrac);
                raw[1] = torsoTotal * torsoFrac;
                raw[2] = head;
                raw[3] = armL;
                raw[4] = armR;
                raw[5] = legL;
                raw[6] = legR;
                raw[7] = wingL;
                raw[8] = wingR;
                raw[9] = tail;
            }

            weights[i] = PackTopFourWeights(raw, boneCount);
        }

        runtimeSkinnedMesh.boneWeights = weights;
        runtimeSkinnedMesh.bindposes = bindposes;

        Transform visualChild = GetOrCreateBone("Rig_Visual", transform, Vector3.zero);
        skinnedRenderer = visualChild.GetComponent<SkinnedMeshRenderer>();
        if (skinnedRenderer == null)
        {
            skinnedRenderer = visualChild.gameObject.AddComponent<SkinnedMeshRenderer>();
        }

        skinnedRenderer.sharedMaterials = staticRenderer.sharedMaterials;
        skinnedRenderer.shadowCastingMode = staticRenderer.shadowCastingMode;
        skinnedRenderer.receiveShadows = staticRenderer.receiveShadows;
        skinnedRenderer.lightProbeUsage = staticRenderer.lightProbeUsage;
        skinnedRenderer.reflectionProbeUsage = staticRenderer.reflectionProbeUsage;
        skinnedRenderer.bones = boneList.ToArray();
        skinnedRenderer.rootBone = rigPelvis;
        skinnedRenderer.sharedMesh = runtimeSkinnedMesh;
        skinnedRenderer.localBounds = new Bounds(meshBounds.center, meshBounds.size * 1.6f);
        skinnedRenderer.enabled = true;

        staticRenderer.enabled = false;
        estaInicializado = true;
    }

    private static float SmoothLess(float val, float edge, float halfWidth)
    {
        return 1f - Mathf.SmoothStep(edge - halfWidth, edge + halfWidth, val);
    }

    private static float SmoothGreater(float val, float edge, float halfWidth)
    {
        return Mathf.SmoothStep(edge - halfWidth, edge + halfWidth, val);
    }

    private static float SmoothRange(float val, float minEdge, float maxEdge, float halfWidth)
    {
        return SmoothGreater(val, minEdge, halfWidth) * SmoothLess(val, maxEdge, halfWidth);
    }

    private static BoneWeight PackTopFourWeights(float[] raw, int count)
    {
        int i0 = 0, i1 = 0, i2 = 0, i3 = 0;
        float w0 = 0f, w1 = 0f, w2 = 0f, w3 = 0f;

        for (int b = 0; b < count; b++)
        {
            float w = raw[b];
            if (w <= 0.0001f) continue;

            if (w > w0)
            {
                w3 = w2; i3 = i2;
                w2 = w1; i2 = i1;
                w1 = w0; i1 = i0;
                w0 = w; i0 = b;
            }
            else if (w > w1)
            {
                w3 = w2; i3 = i2;
                w2 = w1; i2 = i1;
                w1 = w; i1 = b;
            }
            else if (w > w2)
            {
                w3 = w2; i3 = i2;
                w2 = w; i2 = b;
            }
            else if (w > w3)
            {
                w3 = w; i3 = b;
            }
        }

        float sum = w0 + w1 + w2 + w3;
        if (sum <= 1e-6f)
        {
            return new BoneWeight { boneIndex0 = 0, weight0 = 1f };
        }

        float invSum = 1f / sum;
        return new BoneWeight
        {
            boneIndex0 = i0,
            weight0 = w0 * invSum,
            boneIndex1 = i1,
            weight1 = w1 * invSum,
            boneIndex2 = i2,
            weight2 = w2 * invSum,
            boneIndex3 = i3,
            weight3 = w3 * invSum
        };
    }
}
