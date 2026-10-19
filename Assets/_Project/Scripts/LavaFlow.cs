using UnityEngine;

/// <summary>
/// Smoothly scrolls texture UV offsets at runtime to simulate viscous flowing molten lava.
/// </summary>
[DisallowMultipleComponent]
public class LavaFlow : MonoBehaviour
{
    [Header("UV Scrolling Speed")]
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0.008f, 0.016f);

    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
    private static readonly int BumpMapId = Shader.PropertyToID("_BumpMap");

    private Material runtimeMat;
    private Vector2 currentOffset;
    private bool hasBaseMap;
    private bool hasMainTex;
    private bool hasEmissionMap;
    private bool hasBumpMap;

    private void Start()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create a runtime instance of the material so we don't modify the asset file on disk
            runtimeMat = renderer.material;
            if (runtimeMat != null)
            {
                hasBaseMap = runtimeMat.HasProperty(BaseMapId);
                hasMainTex = runtimeMat.HasProperty(MainTexId);
                hasEmissionMap = runtimeMat.HasProperty(EmissionMapId);
                hasBumpMap = runtimeMat.HasProperty(BumpMapId);
            }
        }
    }

    private void Update()
    {
        if (runtimeMat == null) return;

        currentOffset += scrollSpeed * Time.deltaTime;
        currentOffset.x %= 1.0f;
        currentOffset.y %= 1.0f;

        if (hasBaseMap)
            runtimeMat.SetTextureOffset(BaseMapId, currentOffset);

        if (hasMainTex)
            runtimeMat.SetTextureOffset(MainTexId, currentOffset);

        if (hasEmissionMap)
            runtimeMat.SetTextureOffset(EmissionMapId, currentOffset);

        if (hasBumpMap)
            runtimeMat.SetTextureOffset(BumpMapId, currentOffset);
    }

    private void OnDestroy()
    {
        if (runtimeMat != null)
        {
            Destroy(runtimeMat);
        }
    }
}
