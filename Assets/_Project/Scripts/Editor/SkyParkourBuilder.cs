#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class SkyParkourBuilder : EditorWindow
{
    [MenuItem("Tools/Sky Parkour/Build Volcanic Lava Canyon Course")]
    public static void BuildCourseDirect()
    {
        GenerateLevel();
    }

    [MenuItem("Tools/Sky Parkour/Level Builder Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<SkyParkourBuilder>("Volcanic Parkour Builder");
        window.minSize = new Vector2(380, 300);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(15);
        GUILayout.Label("Volcanic Lava Canyon Generator", EditorStyles.boldLabel);
        GUILayout.Label("Builds a cinematic fiery/lava dungeon obstacle course:", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Procedural fractured volcanic rock slabs & boulders\n• Towering basalt spires & volcanic chasm walls\n• Searing molten magma sea floor & incandescent glow\n• Dark, mysterious, cinematic lighting & smoky embers\n• 3 challenging parkour stages + 2 checkpoints + finish arch\n• Working fail-safe UI with [R] key restart", EditorStyles.miniLabel);
        GUILayout.Space(15);

        GUI.backgroundColor = new Color(1.0f, 0.35f, 0.1f);
        if (GUILayout.Button("Build Fiery Volcanic Canyon Course", GUILayout.Height(48)))
        {
            GenerateLevel();
        }
        GUI.backgroundColor = Color.white;
    }

    public static void GenerateLevel()
    {
        AssetDatabase.Refresh();
        var activeScene = EditorSceneManager.GetActiveScene();
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // 1. Clean existing course root if present
        GameObject oldRoot = GameObject.Find("=== Sky Parkour Course ===");
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }

        GameObject root = new GameObject("=== Sky Parkour Course ===");
        Undo.RegisterCreatedObjectUndo(root, "Generate Volcanic Parkour Course");

        // 2. Prepare Materials
        var (matRock, matLava, matSpire, matCpInactive, matCpActive, matBackdrop) = GetOrCreateVolcanicMaterials();

        // 3. Audio clips (Procedural tones)
        AudioClip chimeClip = CreateToneClip("CheckpointChime", 880f, 0.25f);
        AudioClip winClip = CreateToneClip("VictoryFanfare", 587.3f, 0.6f);

        // 4. Setup Dark & Mysterious Volcanic Lighting & Atmosphere
        SetupVolcanicAtmosphere(root.transform);

        // 5. Scenery: Towering Basalt Spires flanking the chasm
        SetupCanyonSpires(root.transform, matSpire);

        // 6. Scenery: Volcanic Sky Backdrop
        SetupSkyBackdrop(root.transform, matBackdrop);

        // 7. Scenery: Molten Lava Sea (Kill Zone)
        SetupLavaSea(root.transform, matLava);

        // 8. Start Platform (Massive faceted rock slab at Z = 0)
        GameObject startPlatform = CreateRockPlatform("StartPlatform_Rock", root.transform, new Vector3(0, 0, 0), new Vector3(8.5f, 1.4f, 8.5f), matRock, 101);
        
        GameObject startCheckpointObj = new GameObject("StartCheckpoint");
        startCheckpointObj.transform.parent = root.transform;
        startCheckpointObj.transform.position = new Vector3(0, 1.5f, 0);

        // 9. Setup Player & Camera at start
        GameObject player = SetupPlayer(root.transform, startCheckpointObj.transform);

        // 10. Stage 1: Precision Rock Jumps
        CreateRockPlatform("Platform_1", root.transform, new Vector3(0f, 0.9f, 8.5f), new Vector3(3.4f, 1.0f, 3.4f), matRock, 102);
        CreateRockPlatform("Platform_2", root.transform, new Vector3(2.8f, 2.2f, 16f), new Vector3(3.0f, 1.0f, 3.0f), matRock, 103);
        CreateRockPlatform("Platform_3", root.transform, new Vector3(-2.4f, 3.6f, 23.5f), new Vector3(3.0f, 1.0f, 3.0f), matRock, 104);
        CreateRockPlatform("Platform_4", root.transform, new Vector3(1.8f, 5.0f, 31f), new Vector3(2.8f, 1.0f, 2.8f), matRock, 105);

        // Hazard 1: Rotating volcanic hazard beam
        GameObject hazard1 = CreateHazardBar("Hazard_RotatingBar1", root.transform, new Vector3(0f, 4.6f, 23.5f), new Vector3(7.5f, 0.5f, 0.5f), matLava);
        var osc1 = hazard1.AddComponent<Oscillator>();
        osc1.rotationSpeed = new Vector3(0, 75f, 0);

        // 11. Checkpoint 1 (Rock Sanctuary at Z = 39)
        CreateRockPlatform("Checkpoint1_Platform", root.transform, new Vector3(0, 6.2f, 39f), new Vector3(6.5f, 1.4f, 6.5f), matRock, 106);
        CreateCheckpoint("Checkpoint_1", root.transform, new Vector3(0, 7.0f, 39f), matCpInactive, matCpActive, chimeClip);

        // 12. Stage 2: Moving Magma Slabs & Propeller Hazards
        GameObject plat5 = CreateRockPlatform("Platform_5_MovingH", root.transform, new Vector3(-2f, 7.8f, 47f), new Vector3(3.4f, 0.9f, 3.4f), matLava, 107);
        var osc5 = plat5.AddComponent<Oscillator>();
        osc5.moveDirection = Vector3.right;
        osc5.moveDistance = 3.6f;
        osc5.moveSpeed = 1.9f;

        CreateRockPlatform("Platform_6", root.transform, new Vector3(3.2f, 9.4f, 55f), new Vector3(2.8f, 0.9f, 2.8f), matRock, 108);

        GameObject plat7 = CreateRockPlatform("Platform_7_MovingV", root.transform, new Vector3(0f, 11.0f, 63f), new Vector3(3.2f, 0.9f, 3.2f), matLava, 109);
        var osc7 = plat7.AddComponent<Oscillator>();
        osc7.moveDirection = Vector3.up;
        osc7.moveDistance = 2.6f;
        osc7.moveSpeed = 1.6f;

        // Hazard 2: Propeller above moving elevator
        GameObject hazard2 = CreateHazardBar("Hazard_Propeller2", root.transform, new Vector3(0f, 13.8f, 63f), new Vector3(8.5f, 0.45f, 0.45f), matLava);
        var osc2 = hazard2.AddComponent<Oscillator>();
        osc2.rotationSpeed = new Vector3(0, 110f, 0);

        CreateRockPlatform("Platform_8", root.transform, new Vector3(-2.8f, 13.0f, 71f), new Vector3(2.8f, 0.9f, 2.8f), matRock, 110);

        // 13. Checkpoint 2 (Rock Sanctuary at Z = 80)
        CreateRockPlatform("Checkpoint2_Platform", root.transform, new Vector3(0, 14.5f, 80f), new Vector3(6.5f, 1.4f, 6.5f), matRock, 111);
        CreateCheckpoint("Checkpoint_2", root.transform, new Vector3(0, 15.3f, 80f), matCpInactive, matCpActive, chimeClip);

        // 14. Stage 3: The Sky Dash (Narrow high-altitude rock steps)
        CreateRockPlatform("Platform_9", root.transform, new Vector3(2.6f, 16.2f, 88f), new Vector3(2.4f, 0.8f, 2.4f), matRock, 112);
        CreateRockPlatform("Platform_10", root.transform, new Vector3(-2.4f, 17.8f, 95f), new Vector3(2.4f, 0.8f, 2.4f), matRock, 113);

        GameObject plat11 = CreateRockPlatform("Platform_11_MovingFast", root.transform, new Vector3(0f, 19.5f, 102f), new Vector3(2.8f, 0.8f, 2.8f), matLava, 114);
        var osc11 = plat11.AddComponent<Oscillator>();
        osc11.moveDirection = Vector3.right;
        osc11.moveDistance = 3.4f;
        osc11.moveSpeed = 2.6f;

        CreateRockPlatform("Platform_12", root.transform, new Vector3(2.0f, 21.2f, 110f), new Vector3(2.2f, 0.8f, 2.2f), matRock, 115);
        CreateRockPlatform("Platform_13", root.transform, new Vector3(0f, 22.8f, 118f), new Vector3(2.6f, 0.8f, 2.6f), matRock, 116);

        // 15. Finish Sanctuary (Grand Volcanic Rock Altar at Z = 129)
        CreateRockPlatform("FinishPlatform_Rock", root.transform, new Vector3(0, 24.2f, 129f), new Vector3(11f, 1.8f, 11f), matRock, 117);
        SetupVolcanicFinishArch(root.transform, new Vector3(0, 25.2f, 129f), matRock, matLava, winClip);

        // 16. UI Canvas & EventSystem
        SetupGameUI(root.transform);

        // 17. Tag registration & scene in build settings
        RegisterPlayerTag();
        EnsureSceneInBuildSettings(activeScene.path);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(activeScene);

        // Focus Scene View on Start Platform
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0, 6, 25), new Vector3(25, 25, 50)), false);
        }

        Debug.Log("<b>[Sky Parkour]</b> Volcanic Lava Canyon Course generated successfully! Press <b>Play</b> to test.");
        EditorUtility.DisplayDialog("Volcanic Lava Canyon Generated!", 
            "The fiery volcanic canyon course has been created!\n\n" +
            "• Procedural jagged volcanic rock platforms\n" +
            "• Searing molten lava sea & incandescent glow\n" +
            "• Towering basalt spires & volcanic dusk backdrop\n" +
            "• Mysterious, dark, high-contrast atmospheric lighting\n" +
            "• Collect both checkpoints and reach the Finish Arch!\n\n" +
            "Press Play (▶) at the top to experience the lava parkour run!", "Awesome!");
    }

    private static (Material rock, Material lava, Material spire, Material cpInactive, Material cpActive, Material backdrop) GetOrCreateVolcanicMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Materials");
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Texture");

        Texture2D texVolcanic = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/Volcanic_Rock_Crag.jpg");
        Texture2D texLava = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/Lava_Molten_Sea.jpg");
        Texture2D texSky = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/Volcanic_Sky_Dusk.jpg");

        // 1. Volcanic Rock Material (Vibrant Glowing Crag)
        Material matRock = LoadOrCreateMat("Assets/_Project/Materials/M_Volcanic_Rock.mat", litShader);
        if (texVolcanic != null)
        {
            SetTextureAndTiling(matRock, texVolcanic, 2.0f, 2.0f);
            matRock.EnableKeyword("_EMISSION");
            if (matRock.HasProperty("_EmissionColor"))
                matRock.SetColor("_EmissionColor", new Color(2.4f, 0.7f, 0.12f));
            if (matRock.HasProperty("_EmissionMap"))
                matRock.SetTexture("_EmissionMap", texVolcanic);
        }
        matRock.color = new Color(0.95f, 0.95f, 0.95f);
        SetSmoothness(matRock, 0.35f);

        // 2. Molten Magma Lava Material (Blinding Incandescent Flow)
        Material matLava = LoadOrCreateMat("Assets/_Project/Materials/M_Molten_Lava.mat", litShader);
        if (texLava != null)
        {
            SetTextureAndTiling(matLava, texLava, 3.0f, 3.0f);
            matLava.EnableKeyword("_EMISSION");
            if (matLava.HasProperty("_EmissionColor"))
                matLava.SetColor("_EmissionColor", new Color(3.5f, 1.5f, 0.3f));
            if (matLava.HasProperty("_EmissionMap"))
                matLava.SetTexture("_EmissionMap", texLava);
        }
        matLava.color = Color.white;
        SetSmoothness(matLava, 0.85f);

        // 3. Basalt Spire Material (Dark Charcoal Volcanic Needles)
        Material matSpire = LoadOrCreateMat("Assets/_Project/Materials/M_Basalt_Spire.mat", litShader);
        if (texVolcanic != null)
        {
            SetTextureAndTiling(matSpire, texVolcanic, 1.5f, 4.0f);
            matSpire.EnableKeyword("_EMISSION");
            if (matSpire.HasProperty("_EmissionColor"))
                matSpire.SetColor("_EmissionColor", new Color(1.0f, 0.28f, 0.05f));
            if (matSpire.HasProperty("_EmissionMap"))
                matSpire.SetTexture("_EmissionMap", texVolcanic);
        }
        matSpire.color = new Color(0.45f, 0.45f, 0.48f);
        SetSmoothness(matSpire, 0.2f);

        // 4. Checkpoint Inactive
        Material matCpInactive = LoadOrCreateMat("Assets/_Project/Materials/M_Checkpoint_Inactive.mat", litShader);
        if (texVolcanic != null) SetTextureAndTiling(matCpInactive, texVolcanic, 1f, 1f);
        matCpInactive.color = new Color(0.35f, 0.38f, 0.42f);
        SetSmoothness(matCpInactive, 0.3f);

        // 5. Checkpoint Active (Blazing Emerald Rune Glow)
        Material matCpActive = LoadOrCreateMat("Assets/_Project/Materials/M_Checkpoint_Active.mat", litShader);
        if (texVolcanic != null) SetTextureAndTiling(matCpActive, texVolcanic, 1f, 1f);
        matCpActive.color = new Color(0.1f, 1.0f, 0.45f);
        matCpActive.EnableKeyword("_EMISSION");
        if (matCpActive.HasProperty("_EmissionColor"))
            matCpActive.SetColor("_EmissionColor", new Color(0.2f, 2.5f, 0.6f));
        SetSmoothness(matCpActive, 0.8f);

        // 6. Volcanic Sky Backdrop
        Material matBackdrop = LoadOrCreateMat("Assets/_Project/Materials/M_Volcanic_Backdrop.mat", unlitShader);
        if (texSky != null)
        {
            if (matBackdrop.HasProperty("_BaseMap")) matBackdrop.SetTexture("_BaseMap", texSky);
            if (matBackdrop.HasProperty("_MainTex")) matBackdrop.SetTexture("_MainTex", texSky);
        }
        matBackdrop.color = Color.white;

        AssetDatabase.SaveAssets();

        return (matRock, matLava, matSpire, matCpInactive, matCpActive, matBackdrop);
    }

    private static void SetupVolcanicAtmosphere(Transform root)
    {
        // Atmospheric Volcanic Fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.fogColor = new Color(0.12f, 0.04f, 0.02f); // Dark smoky ember haze

        // Ambient Lighting: Dark sky + Warm fiery lava bounce from below
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.10f, 0.05f, 0.07f);      // Dusky smoke sky
        RenderSettings.ambientEquatorColor = new Color(0.25f, 0.08f, 0.03f);  // Volcanic dusk haze
        RenderSettings.ambientGroundColor = new Color(0.65f, 0.20f, 0.04f);   // Radiant magma floor bounce

        // Directional Sun Light (Low-angle, moody, dramatic crimson-orange)
        Light dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            dirLight.color = new Color(1.0f, 0.38f, 0.12f);
            dirLight.intensity = 0.65f;
            dirLight.transform.rotation = Quaternion.Euler(20f, 42f, 0f);
            dirLight.shadows = LightShadows.Soft;
        }

        // Fiery underglow lights along the chasm
        CreateLavaUnderlight("LavaGlow_1", root, new Vector3(0, -6f, 15f), 35f, 2.0f);
        CreateLavaUnderlight("LavaGlow_2", root, new Vector3(0, -2f, 55f), 38f, 2.2f);
        CreateLavaUnderlight("LavaGlow_3", root, new Vector3(0, 5f, 95f), 40f, 2.5f);
        CreateLavaUnderlight("LavaGlow_4", root, new Vector3(0, 10f, 130f), 42f, 2.5f);
    }

    private static void CreateLavaUnderlight(string name, Transform parent, Vector3 pos, float range, float intensity)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.parent = parent;
        lightObj.transform.position = pos;
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1.0f, 0.35f, 0.05f);
        light.range = range;
        light.intensity = intensity;
    }

    private static void SetupCanyonSpires(Transform parent, Material matSpire)
    {
        GameObject spiresGroup = new GameObject("Canyon_BasaltSpires");
        spiresGroup.transform.parent = parent;

        // Left canyon spires (X: -16 to -28)
        float[] leftZ = { -10f, 10f, 25f, 45f, 65f, 85f, 105f, 125f, 145f };
        for (int i = 0; i < leftZ.Length; i++)
        {
            float x = -19f - (i % 3) * 3.5f;
            float z = leftZ[i];
            float h = 32f + (i * 3.5f);
            CreateSpire($"SpireL_{i}", spiresGroup.transform, new Vector3(x, -10f, z), h, 3.5f + (i % 2), matSpire, 200 + i);
        }

        // Right canyon spires (X: +16 to +28)
        float[] rightZ = { -5f, 15f, 35f, 55f, 75f, 95f, 115f, 135f, 150f };
        for (int i = 0; i < rightZ.Length; i++)
        {
            float x = 19f + (i % 3) * 3.5f;
            float z = rightZ[i];
            float h = 34f + (i * 3.2f);
            CreateSpire($"SpireR_{i}", spiresGroup.transform, new Vector3(x, -10f, z), h, 3.5f + (i % 2), matSpire, 300 + i);
        }
    }

    private static void SetupSkyBackdrop(Transform parent, Material matBackdrop)
    {
        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        backdrop.name = "Volcanic_Sky_Backdrop";
        backdrop.transform.parent = parent;
        backdrop.transform.position = new Vector3(0, 35f, 175f);
        backdrop.transform.localScale = new Vector3(180f, 95f, 1f);
        backdrop.transform.rotation = Quaternion.Euler(0, 0, 0);

        DestroyImmediate(backdrop.GetComponent<Collider>());
        if (matBackdrop != null)
        {
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = matBackdrop;
        }
    }

    private static void SetupLavaSea(Transform parent, Material matLava)
    {
        // The vast molten magma river floor (serves as the fiery kill zone)
        GameObject lavaLake = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lavaLake.name = "LavaSea_KillZone";
        lavaLake.transform.parent = parent;
        lavaLake.transform.position = new Vector3(0, -12f, 65f);
        lavaLake.transform.localScale = new Vector3(280f, 1.5f, 280f);
        lavaLake.GetComponent<MeshRenderer>().sharedMaterial = matLava;

        var col = lavaLake.GetComponent<BoxCollider>();
        col.isTrigger = true;
        lavaLake.AddComponent<HazardTrigger>();
    }

    private static GameObject CreateRockPlatform(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, int seed)
    {
        GameObject rockObj = new GameObject(name);
        rockObj.transform.parent = parent;
        rockObj.transform.position = pos;

        // Procedural Faceted Rock Mesh
        var meshFilter = rockObj.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateRockMesh(size, seed, 0.26f, true);

        var meshRenderer = rockObj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mat;

        // Reliable box collider matching rock platform footprint
        var col = rockObj.AddComponent<BoxCollider>();
        col.size = new Vector3(size.x * 0.92f, size.y, size.z * 0.92f);
        col.center = Vector3.zero;

        return rockObj;
    }

    private static Mesh CreateRockMesh(Vector3 size, int seed, float roughness = 0.25f, bool flatTop = true)
    {
        Mesh mesh = new Mesh();
        mesh.name = "RockMesh_" + seed;

        var vertices = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();

        Vector3[] faceNormals = {
            Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
        };
        Vector3[] faceUp = {
            Vector3.forward, Vector3.back, Vector3.up, Vector3.up, Vector3.up, Vector3.up
        };

        int res = 4;
        Random.State oldState = Random.state;
        Random.InitState(seed);

        for (int f = 0; f < 6; f++)
        {
            Vector3 normal = faceNormals[f];
            Vector3 up = faceUp[f];
            Vector3 right = Vector3.Cross(normal, up);

            Vector3[,] grid = new Vector3[res, res];
            Vector2[,] uvGrid = new Vector2[res, res];

            for (int y = 0; y < res; y++)
            {
                float ty = (float)y / (res - 1) - 0.5f;
                for (int x = 0; x < res; x++)
                {
                    float tx = (float)x / (res - 1) - 0.5f;
                    Vector3 p = (normal * 0.5f + right * tx + up * ty);

                    p.x *= size.x;
                    p.y *= size.y;
                    p.z *= size.z;

                    float noise = (Mathf.PerlinNoise((p.x + seed) * 0.35f, (p.z + seed) * 0.35f) - 0.5f) * 2f;
                    float deform = noise * roughness * Mathf.Min(size.x, size.z);

                    if (flatTop && normal == Vector3.up)
                    {
                        // Keep the top walking surface relatively level for responsive jumps
                        p.y += deform * 0.08f;
                        p.x += deform * 0.22f;
                        p.z += deform * 0.22f;
                    }
                    else
                    {
                        // Strong jagged craggy rock facets on sides and bottom
                        p += normal * deform;
                        p.x += (Mathf.PerlinNoise(p.y * 0.5f + seed, p.z * 0.5f) - 0.5f) * roughness * size.x;
                        p.z += (Mathf.PerlinNoise(p.x * 0.5f + seed, p.y * 0.5f) - 0.5f) * roughness * size.z;
                    }

                    grid[x, y] = p;
                    uvGrid[x, y] = new Vector2((float)x / (res - 1), (float)y / (res - 1));
                }
            }

            for (int y = 0; y < res - 1; y++)
            {
                for (int x = 0; x < res - 1; x++)
                {
                    Vector3 v00 = grid[x, y];
                    Vector3 v10 = grid[x + 1, y];
                    Vector3 v01 = grid[x, y + 1];
                    Vector3 v11 = grid[x + 1, y + 1];

                    Vector2 uv00 = uvGrid[x, y];
                    Vector2 uv10 = uvGrid[x + 1, y];
                    Vector2 uv01 = uvGrid[x, y + 1];
                    Vector2 uv11 = uvGrid[x + 1, y + 1];

                    int idx = vertices.Count;
                    vertices.Add(v00); vertices.Add(v01); vertices.Add(v10);
                    uvs.Add(uv00); uvs.Add(uv01); uvs.Add(uv10);
                    triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);

                    idx = vertices.Count;
                    vertices.Add(v10); vertices.Add(v01); vertices.Add(v11);
                    uvs.Add(uv10); uvs.Add(uv01); uvs.Add(uv11);
                    triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);
                }
            }
        }

        Random.state = oldState;

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void CreateSpire(string name, Transform parent, Vector3 pos, float height, float baseRadius, Material mat, int seed)
    {
        GameObject spireObj = new GameObject(name);
        spireObj.transform.parent = parent;
        spireObj.transform.position = pos;

        var mf = spireObj.AddComponent<MeshFilter>();
        mf.sharedMesh = CreateSpireMesh(height, baseRadius, seed);

        var mr = spireObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        // Light mesh collider for spires
        var col = spireObj.AddComponent<MeshCollider>();
        col.sharedMesh = mf.sharedMesh;
    }

    private static Mesh CreateSpireMesh(float height, float baseRadius, int seed)
    {
        Mesh mesh = new Mesh();
        mesh.name = "SpireMesh_" + seed;

        Random.State oldState = Random.state;
        Random.InitState(seed);

        int sides = 6;
        int segments = 5;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();

        Vector3[,] grid = new Vector3[segments + 1, sides];

        for (int s = 0; s <= segments; s++)
        {
            float t = (float)s / segments;
            float y = t * height;
            float radius = baseRadius * Mathf.Lerp(1f, 0.08f, t);

            for (int i = 0; i < sides; i++)
            {
                float angle = (float)i / sides * Mathf.PI * 2f;
                float r = radius * (1f + (Mathf.PerlinNoise(i * 1.5f + seed, s * 1.2f) - 0.5f) * 0.45f);
                float x = Mathf.Cos(angle) * r;
                float z = Mathf.Sin(angle) * r;
                grid[s, i] = new Vector3(x, y, z);
            }
        }

        for (int s = 0; s < segments; s++)
        {
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                Vector3 v0 = grid[s, i];
                Vector3 v1 = grid[s + 1, i];
                Vector3 v2 = grid[s, next];
                Vector3 v3 = grid[s + 1, next];

                int idx = vertices.Count;
                vertices.Add(v0); vertices.Add(v1); vertices.Add(v2);
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 0));
                triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);

                idx = vertices.Count;
                vertices.Add(v2); vertices.Add(v1); vertices.Add(v3);
                uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1));
                triangles.Add(idx); triangles.Add(idx + 1); triangles.Add(idx + 2);
            }
        }

        Random.state = oldState;

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static GameObject CreateHazardBar(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        bar.transform.parent = parent;
        bar.transform.position = pos;
        bar.transform.localScale = scale;
        if (mat != null) bar.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var col = bar.GetComponent<BoxCollider>();
        col.isTrigger = true;
        bar.AddComponent<HazardTrigger>();
        return bar;
    }

    private static GameObject CreateCheckpoint(string name, Transform parent, Vector3 pos, Material inactiveMat, Material activeMat, AudioClip chime)
    {
        GameObject cpObj = new GameObject(name);
        cpObj.transform.parent = parent;
        cpObj.transform.position = pos;
        cpObj.transform.localScale = Vector3.one;

        // Visual Rune Crystal Pad (Cylinder)
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "PadVisual";
        pad.transform.parent = cpObj.transform;
        pad.transform.position = pos;
        pad.transform.localScale = new Vector3(2.6f, 0.18f, 2.6f);
        var padRenderer = pad.GetComponent<MeshRenderer>();
        padRenderer.sharedMaterial = inactiveMat;
        DestroyImmediate(pad.GetComponent<Collider>());

        // Trigger Collider on parent
        var boxCol = cpObj.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;
        boxCol.center = new Vector3(0, 1.0f, 0);
        boxCol.size = new Vector3(3.2f, 2.5f, 3.2f);

        // Audio Source
        var audio = cpObj.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = chime;

        // Checkpoint Component
        var cpComp = cpObj.AddComponent<Checkpoint>();
        var so = new SerializedObject(cpComp);
        so.FindProperty("activeMaterial").objectReferenceValue = activeMat;
        so.FindProperty("visualRenderer").objectReferenceValue = padRenderer;
        so.FindProperty("audioSource").objectReferenceValue = audio;
        so.ApplyModifiedProperties();

        return cpObj;
    }

    private static void SetupVolcanicFinishArch(Transform parent, Vector3 pos, Material stoneMat, Material lavaMat, AudioClip winClip)
    {
        GameObject arch = new GameObject("FinishArch_Volcanic");
        arch.transform.parent = parent;
        arch.transform.position = pos;
        arch.transform.localScale = Vector3.one;

        // Left Obsidian Pillar
        CreateBox("PillarL", arch.transform, pos + new Vector3(-3.0f, 2.5f, 0), new Vector3(0.9f, 5.0f, 0.9f), stoneMat);
        // Right Obsidian Pillar
        CreateBox("PillarR", arch.transform, pos + new Vector3(3.0f, 2.5f, 0), new Vector3(0.9f, 5.0f, 0.9f), stoneMat);
        // Blazing Magma Crossbar
        CreateBox("Crossbar", arch.transform, pos + new Vector3(0f, 5.0f, 0), new Vector3(7.0f, 0.9f, 0.9f), lavaMat);

        // Trigger Box - Exact unscaled volume
        GameObject triggerObj = new GameObject("FinishTrigger");
        triggerObj.transform.parent = arch.transform;
        triggerObj.transform.position = pos + new Vector3(0, 2.5f, 0);
        triggerObj.transform.localScale = Vector3.one;

        var col = triggerObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(5.0f, 4.5f, 2.0f);

        var audio = triggerObj.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = winClip;

        var finishComp = triggerObj.AddComponent<FinishLine>();
        var so = new SerializedObject(finishComp);
        so.FindProperty("winAudio").objectReferenceValue = audio;
        so.ApplyModifiedProperties();
    }

    private static GameObject SetupPlayer(Transform parent, Transform startCheckpoint)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = new GameObject("Player");
        }
        player.tag = "Player";
        player.transform.parent = parent;

        // CharacterController
        var cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        
        cc.enabled = false;
        player.transform.position = startCheckpoint.position;
        player.transform.rotation = Quaternion.identity;
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1f, 0);
        cc.enabled = true;

        // Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            mainCam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }
        mainCam.transform.parent = player.transform;
        mainCam.transform.localPosition = new Vector3(0, 1.65f, 0);
        mainCam.transform.localRotation = Quaternion.identity;

        // PlayerMovement
        var pm = player.GetComponent<PlayerMovement>();
        if (pm == null) pm = player.AddComponent<PlayerMovement>();
        var soPm = new SerializedObject(pm);
        soPm.FindProperty("cameraTransform").objectReferenceValue = mainCam.transform;
        soPm.ApplyModifiedProperties();

        // PlayerRespawn
        var pr = player.GetComponent<PlayerRespawn>();
        if (pr == null) pr = player.AddComponent<PlayerRespawn>();
        var soPr = new SerializedObject(pr);
        soPr.FindProperty("currentCheckpoint").objectReferenceValue = startCheckpoint;
        soPr.ApplyModifiedProperties();

        return player;
    }

    private static void SetupGameUI(Transform root)
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("GameCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        canvas.transform.parent = root;

        var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        GameObject esObj = (eventSystem != null) ? eventSystem.gameObject : new GameObject("EventSystem");
        esObj.transform.parent = root;

        if (eventSystem == null)
        {
            eventSystem = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

        var standalone = esObj.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone != null)
        {
            DestroyImmediate(standalone);
        }

#if ENABLE_INPUT_SYSTEM
        var inputModule = esObj.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
        {
            inputModule = esObj.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }
#else
        if (esObj.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
        {
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
#endif

        for (int i = canvas.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(canvas.transform.GetChild(i).gameObject);
        }

        // HUD Root
        GameObject hud = new GameObject("HUD");
        hud.transform.SetParent(canvas.transform, false);
        var hudRect = hud.AddComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.sizeDelta = Vector2.zero;

        // Timer Text (Top Left)
        GameObject timerObj = new GameObject("TimerText");
        timerObj.transform.SetParent(hud.transform, false);
        var timerRect = timerObj.AddComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0, 1);
        timerRect.anchorMax = new Vector2(0, 1);
        timerRect.pivot = new Vector2(0, 1);
        timerRect.anchoredPosition = new Vector2(35, -30);
        timerRect.sizeDelta = new Vector2(300, 60);
        var timerTmp = timerObj.AddComponent<TextMeshProUGUI>();
        timerTmp.text = "Time: 00:00";
        timerTmp.fontSize = 32;
        timerTmp.fontStyle = FontStyles.Bold;
        timerTmp.color = new Color(1.0f, 0.85f, 0.7f);

        // Checkpoint Text (Top Right)
        GameObject cpObj = new GameObject("CheckpointText");
        cpObj.transform.SetParent(hud.transform, false);
        var cpRect = cpObj.AddComponent<RectTransform>();
        cpRect.anchorMin = new Vector2(1, 1);
        cpRect.anchorMax = new Vector2(1, 1);
        cpRect.pivot = new Vector2(1, 1);
        cpRect.anchoredPosition = new Vector2(-35, -30);
        cpRect.sizeDelta = new Vector2(350, 60);
        var cpTmp = cpObj.AddComponent<TextMeshProUGUI>();
        cpTmp.text = "Checkpoints: 0 / 2";
        cpTmp.fontSize = 32;
        cpTmp.fontStyle = FontStyles.Bold;
        cpTmp.alignment = TextAlignmentOptions.TopRight;
        cpTmp.color = new Color(1.0f, 0.55f, 0.15f);

        // Center Crosshair Dot
        GameObject crosshair = new GameObject("CrosshairDot");
        crosshair.transform.SetParent(hud.transform, false);
        var crosshairRect = crosshair.AddComponent<RectTransform>();
        crosshairRect.sizeDelta = new Vector2(6, 6);
        var crossImg = crosshair.AddComponent<Image>();
        crossImg.color = new Color(1, 1, 1, 0.75f);
        crossImg.raycastTarget = false;

        // Victory Panel (Centered)
        GameObject winPanel = new GameObject("VictoryPanel");
        winPanel.transform.SetParent(canvas.transform, false);
        var winRect = winPanel.AddComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero;
        winRect.anchorMax = Vector2.one;
        winRect.sizeDelta = Vector2.zero;
        var winBg = winPanel.AddComponent<Image>();
        winBg.color = new Color(0.08f, 0.02f, 0.02f, 0.94f);
        winBg.raycastTarget = true;

        // Title
        GameObject winTitle = new GameObject("Title");
        winTitle.transform.SetParent(winPanel.transform, false);
        var titleRect = winTitle.AddComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 90);
        titleRect.sizeDelta = new Vector2(600, 80);
        var titleTmp = winTitle.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "CHASM CONQUERED!";
        titleTmp.fontSize = 54;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color(1.0f, 0.65f, 0.1f);
        titleTmp.raycastTarget = false;

        // Final Time Text
        GameObject finalTimeObj = new GameObject("FinalTimeText");
        finalTimeObj.transform.SetParent(winPanel.transform, false);
        var finalRect = finalTimeObj.AddComponent<RectTransform>();
        finalRect.anchoredPosition = new Vector2(0, 10);
        finalRect.sizeDelta = new Vector2(500, 50);
        var finalTmp = finalTimeObj.AddComponent<TextMeshProUGUI>();
        finalTmp.text = "Completed in: 00:00";
        finalTmp.fontSize = 32;
        finalTmp.alignment = TextAlignmentOptions.Center;
        finalTmp.color = Color.white;
        finalTmp.raycastTarget = false;

        // Restart Button
        GameObject btnObj = new GameObject("RestartButton");
        btnObj.transform.SetParent(winPanel.transform, false);
        var btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchoredPosition = new Vector2(0, -90);
        btnRect.sizeDelta = new Vector2(250, 60);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.95f, 0.40f, 0.10f);
        btnImg.raycastTarget = true;
        var btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        GameObject btnTxt = new GameObject("Text");
        btnTxt.transform.SetParent(btnObj.transform, false);
        var btnTxtRect = btnTxt.AddComponent<RectTransform>();
        btnTxtRect.anchorMin = Vector2.zero;
        btnTxtRect.anchorMax = Vector2.one;
        btnTxtRect.sizeDelta = Vector2.zero;
        var btnTmp = btnTxt.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "PLAY AGAIN (or [R])";
        btnTmp.fontSize = 22;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;
        btnTmp.raycastTarget = false;

        // Wire GameUI script
        var gameUI = canvas.GetComponent<GameUI>();
        if (gameUI == null) gameUI = canvas.gameObject.AddComponent<GameUI>();

        var soUI = new SerializedObject(gameUI);
        soUI.FindProperty("timerText").objectReferenceValue = timerTmp;
        soUI.FindProperty("checkpointText").objectReferenceValue = cpTmp;
        soUI.FindProperty("winPanel").objectReferenceValue = winPanel;
        soUI.FindProperty("finalTimeText").objectReferenceValue = finalTmp;
        soUI.ApplyModifiedProperties();

        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, gameUI.RestartGame);

        winPanel.SetActive(false);
    }

    private static Material LoadOrCreateMat(string path, Shader shader)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        return m;
    }

    private static void SetTextureAndTiling(Material mat, Texture2D tex, float tileX, float tileY)
    {
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        mat.mainTexture = tex;
        mat.mainTextureScale = new Vector2(tileX, tileY);
    }

    private static void SetSmoothness(Material mat, float val)
    {
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", val);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", val);
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.parent = parent;
        box.transform.position = pos;
        box.transform.localScale = scale;
        if (mat != null) box.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return box;
    }

    private static AudioClip CreateToneClip(string name, float frequency, float duration)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (t / duration);
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static void RegisterPlayerTag()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals("Player"))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Player";
            tagManager.ApplyModifiedProperties();
        }
    }

    private static void EnsureSceneInBuildSettings(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath)) return;
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes)
        {
            if (s.path == scenePath) return;
        }
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
