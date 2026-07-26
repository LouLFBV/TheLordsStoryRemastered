using UnityEngine;

public class FootstepController : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Footstep Clips")]
    public AudioClip[] grassSteps;
    public AudioClip[] stoneSteps;
    public AudioClip[] woodSteps;
    public AudioClip[] defaultSteps;

    [Header("Raycast")]
    public float rayDistance = 1.5f;
    public LayerMask groundMask;

    [Header("Terrain Surfaces")]
    public TerrainSurface[] terrainSurfaces;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // ?? Appelé UNIQUEMENT par Animation Event
    public void PlayFootstep()
    {
        if (!IsMoving() || (PlayerController.Instance.StateMachine.CurrentState == PlayerController.Instance.AttackState && PlayerController.Instance.usingSpecialAttack) || PlayerController.Instance.StateMachine.CurrentState == PlayerController.Instance.RollState || PlayerController.Instance.StateMachine.CurrentState == PlayerController.Instance.StunnedState)
            return;

        SurfaceType surface = GetSurfaceType();
        AudioClip clip = GetRandomClip(surface);

        if (clip == null)
            return;

        //if (animator.GetBool("IsSprinting"))
        //    audioSource.volume = 0.9f;
        //else if (animator.GetBool("IsCrouched"))
        //    audioSource.volume *= 0.4f;
        //else
        //    audioSource.volume = 0.6f;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clip);
        //Debug.Log(clip.name + " played on " + surface.ToString() + " surface.");
    }

    private bool IsMoving()
    {
        return animator.GetFloat("Speed") > 0.1f;
    }

    private SurfaceType GetSurfaceType()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.3f,
                             Vector3.down,
                             out RaycastHit hit,
                             rayDistance,
                             groundMask))
        {
            // 🔹 CAS TERRAIN
            if (hit.collider.TryGetComponent<Terrain>(out var terrain))
                return GetTerrainSurface(terrain, hit.point);

            // 🔹 CAS MESH / SOL CLASSIQUE
            if (hit.collider.TryGetComponent<SurfaceIdentifier>(out var surface))
                return surface.surfaceType;
        }

        return SurfaceType.Default;
    }

    private SurfaceType GetTerrainSurface(Terrain terrain, Vector3 worldPos)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPos = worldPos - terrain.transform.position;

        int mapX = Mathf.FloorToInt(
            terrainPos.x / terrainData.size.x * terrainData.alphamapWidth);

        int mapZ = Mathf.FloorToInt(
            terrainPos.z / terrainData.size.z * terrainData.alphamapHeight);

        float[,,] alphaMaps = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        int dominantLayer = 0;
        float maxMix = 0f;

        for (int i = 0; i < alphaMaps.GetLength(2); i++)
        {
            if (alphaMaps[0, 0, i] > maxMix)
            {
                maxMix = alphaMaps[0, 0, i];
                dominantLayer = i;
            }
        }

        foreach (var surface in terrainSurfaces)
        {
            if (surface.terrainLayerIndex == dominantLayer)
                return surface.surfaceType;
        }

        return SurfaceType.Default;
    }


    private AudioClip GetRandomClip(SurfaceType type)
    {
        AudioClip[] clips = type switch
        {
            SurfaceType.Grass => grassSteps,
            SurfaceType.Stone => stoneSteps,
            SurfaceType.Wood => woodSteps,
            _ => defaultSteps
        };

        if (clips.Length == 0)
            return null;

        return clips[Random.Range(0, clips.Length)];
    }
}

public enum SurfaceType
{
    Default,
    Grass,
    Stone,
    Wood,
    Sand,
    Water
}

[System.Serializable]
public class TerrainSurface
{
    public int terrainLayerIndex;
    public SurfaceType surfaceType;
}
