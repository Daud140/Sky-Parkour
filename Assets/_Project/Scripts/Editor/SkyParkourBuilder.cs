#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        window.minSize = new Vector2(380, 320);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Space(15);
        GUILayout.Label("Volcanic Lava Canyon Generator", EditorStyles.boldLabel);
        GUILayout.Label("Builds a dark, brooding, cinematic volcanic chasm obstacle course:", EditorStyles.wordWrappedLabel);
        GUILayout.Label("• Dark ominous storm skybox & low blood-orange horizon glow\n" +
                        "• Overhead storm cloud canopy with slow turbulent UV drifting\n" +
                        "• Deep red-brown exponential fog seamlessly blending into the haze\n" +
                        "• Low-angle deep red-orange rim sun & subdued ambient lighting\n" +
                        "• Tuned URP post-processing (Bloom threshold 1.15, -0.35 EV, dark red vignette)\n" +
                        "• Superheated emissive lava rivers & high-contrast emerald checkpoints\n" +
                        "• Fully preserved gameplay, checkpoints, hazards, timer & UI", EditorStyles.miniLabel);
        GUILayout.Space(15);

        GUI.backgroundColor = new Color(1.0f, 0.35f, 0.1f);
        if (GUILayout.Button("Build Dark Volcanic Storm Canyon", GUILayout.Height(48)))
        {
            GenerateLevel(true);
        }
        GUI.backgroundColor = Color.white;
    }

    public static void GenerateLevel(bool showDialog = false)
    {
        AssetDatabase.Refresh();
        string targetScenePath = "Assets/Scenes/SampleScene.unity";
        var activeScene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(activeScene.path) || !activeScene.path.Replace('\\', '/').EndsWith(targetScenePath))
        {
            if (System.IO.File.Exists(targetScenePath))
            {
                activeScene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            }
        }
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // 1. Clean existing course root if present
        GameObject oldRoot = GameObject.Find("=== Sky Parkour Course ===");
        if (oldRoot != null)
        {
            Undo.DestroyObjectImmediate(oldRoot);
        }

        // Clean standalone global volume if previously created
        GameObject oldVol = GameObject.Find("Volcanic_GlobalVolume");
        if (oldVol != null)
        {
            Undo.DestroyObjectImmediate(oldVol);
        }

        GameObject root = new GameObject("=== Sky Parkour Course ===");
        Undo.RegisterCreatedObjectUndo(root, "Generate Volcanic Parkour Course");

        // 2. Prepare Materials
        var (matRock, matLava, matFissure, matSpire, matCpInactive, matCpActive, matSkybox, matClouds, matEmber, matSmoke) = GetOrCreateVolcanicMaterials();

        // 3. Audio clips (Procedural tones & sound effects)
        AudioClip chimeClip = CreateToneClip("CheckpointChime", 880f, 0.25f);
        AudioClip winClip = CreateToneClip("VictoryFanfare", 587.3f, 0.6f);
        AudioClip horrorClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Horror.mp3");
        AudioClip deathClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/myinstants.mp3");

        // 3b. Atmospheric Looping Horror Background Music (Horror.mp3)
        SetupBackgroundMusic(root.transform, horrorClip);

        // 4. Setup Dark Storm Skybox, Sun, Reduced Ambient Lighting, and Horizon Fog
        SetupVolcanicAtmosphere(matSkybox);

        // 5. Setup Cinematic URP Post-Processing Volume (Bloom threshold 1.15, -0.35 EV, ACES, Dark Red Vignette)
        SetupGlobalVolume(root.transform);

        // 6. Scenery: Overhanging Dark Storm Cloud Canopy & Horizon Cloud Bank (UV scrolling)
        SetupStormCloudCanopy(root.transform, matClouds);

        // 7. Scenery: Flanking Canyon Walls (Replaces flat back wall with rugged jagged geometry)
        SetupCanyonWalls(root.transform, matSpire, matRock);

        // 8. Scenery: 3 Layers of Towering Basalt Spires & Horizon Silhouettes
        SetupCanyonSpires(root.transform, matSpire);

        // 9. Scenery: Molten Magma Sea, Flowing Fissures, and Broken Basalt Slabs
        SetupLavaSea(root.transform, matLava, matFissure, matRock, matSpire);

        // 10. Scenery: Lava Underglow Point Lights (Lighting rocks from below)
        SetupLavaLights(root.transform);

        // 11. Scenery: Rising Embers & Volcanic Smoke Particle Systems
        SetupVolcanicParticles(root.transform, matEmber, matSmoke);

        // 12. Start Platform (Massive faceted rock slab at Z = 0)
        GameObject startPlatform = CreateRockPlatform("StartPlatform_Rock", root.transform, new Vector3(0, 0, 0), new Vector3(8.5f, 1.4f, 8.5f), matRock, 101);

        GameObject startCheckpointObj = new GameObject("StartCheckpoint");
        startCheckpointObj.transform.parent = root.transform;
        startCheckpointObj.transform.position = new Vector3(0, 1.5f, 0);

        // 13. Setup Player & Camera (Camera background set to dark red-black fallback, death sound assigned)
        GameObject player = SetupPlayer(root.transform, startCheckpointObj.transform, deathClip);

        // 14. Course Containers: Easy Mode, Medium Mode & Hard Mode
        GameObject courseHard = new GameObject("Course_Hard");
        courseHard.transform.parent = root.transform;

        GameObject courseMedium = new GameObject("Course_Medium");
        courseMedium.transform.parent = root.transform;

        GameObject courseEasy = new GameObject("Course_Easy");
        courseEasy.transform.parent = root.transform;

        // --- HARD COURSE (3 Stages, 2 Checkpoints, Hazards, Spanning to Z = 129) ---
        // Stage 1: Precision Rock Jumps
        CreateRockPlatform("Platform_1", courseHard.transform, new Vector3(0f, 0.9f, 8.5f), new Vector3(3.4f, 1.0f, 3.4f), matRock, 102);
        CreateRockPlatform("Platform_2", courseHard.transform, new Vector3(2.8f, 2.2f, 16f), new Vector3(3.0f, 1.0f, 3.0f), matRock, 103);
        CreateRockPlatform("Platform_3", courseHard.transform, new Vector3(-2.4f, 3.6f, 23.5f), new Vector3(3.0f, 1.0f, 3.0f), matRock, 104);
        CreateRockPlatform("Platform_4", courseHard.transform, new Vector3(1.8f, 5.0f, 31f), new Vector3(2.8f, 1.0f, 2.8f), matRock, 105);

        // Hazard 1: Rotating volcanic hazard beam
        GameObject hazard1 = CreateHazardBar("Hazard_RotatingBar1", courseHard.transform, new Vector3(0f, 4.6f, 23.5f), new Vector3(7.5f, 0.5f, 0.5f), matLava);
        var osc1 = hazard1.AddComponent<Oscillator>();
        osc1.rotationSpeed = new Vector3(0, 75f, 0);

        // Checkpoint 1 (Rock Sanctuary at Z = 39)
        CreateRockPlatform("Checkpoint1_Platform", courseHard.transform, new Vector3(0, 6.2f, 39f), new Vector3(6.5f, 1.4f, 6.5f), matRock, 106);
        CreateCheckpoint("Checkpoint_1", courseHard.transform, new Vector3(0, 7.0f, 39f), matCpInactive, matCpActive, chimeClip);

        // Stage 2: Moving Magma Slabs & Propeller Hazards
        GameObject plat5 = CreateRockPlatform("Platform_5_MovingH", courseHard.transform, new Vector3(-2f, 7.8f, 47f), new Vector3(3.4f, 0.9f, 3.4f), matLava, 107, isStatic: false);
        var osc5 = plat5.AddComponent<Oscillator>();
        osc5.moveDirection = Vector3.right;
        osc5.moveDistance = 3.6f;
        osc5.moveSpeed = 1.9f;

        CreateRockPlatform("Platform_6", courseHard.transform, new Vector3(3.2f, 9.4f, 55f), new Vector3(2.8f, 0.9f, 2.8f), matRock, 108);

        GameObject plat7 = CreateRockPlatform("Platform_7_MovingV", courseHard.transform, new Vector3(0f, 11.0f, 63f), new Vector3(3.2f, 0.9f, 3.2f), matLava, 109, isStatic: false);
        var osc7 = plat7.AddComponent<Oscillator>();
        osc7.moveDirection = Vector3.up;
        osc7.moveDistance = 2.6f;
        osc7.moveSpeed = 1.6f;

        // Hazard 2: Propeller above moving elevator
        GameObject hazard2 = CreateHazardBar("Hazard_Propeller2", courseHard.transform, new Vector3(0f, 13.8f, 63f), new Vector3(8.5f, 0.45f, 0.45f), matLava);
        var osc2 = hazard2.AddComponent<Oscillator>();
        osc2.rotationSpeed = new Vector3(0, 110f, 0);

        CreateRockPlatform("Platform_8", courseHard.transform, new Vector3(-2.8f, 13.0f, 71f), new Vector3(2.8f, 0.9f, 2.8f), matRock, 110);

        // Checkpoint 2 (Rock Sanctuary at Z = 80)
        CreateRockPlatform("Checkpoint2_Platform", courseHard.transform, new Vector3(0, 14.5f, 80f), new Vector3(6.5f, 1.4f, 6.5f), matRock, 111);
        CreateCheckpoint("Checkpoint_2", courseHard.transform, new Vector3(0, 15.3f, 80f), matCpInactive, matCpActive, chimeClip);

        // Stage 3: The Sky Dash (Narrow high-altitude rock steps)
        CreateRockPlatform("Platform_9", courseHard.transform, new Vector3(2.6f, 16.2f, 88f), new Vector3(2.4f, 0.8f, 2.4f), matRock, 112);
        CreateRockPlatform("Platform_10", courseHard.transform, new Vector3(-2.4f, 17.8f, 95f), new Vector3(2.4f, 0.8f, 2.4f), matRock, 113);

        GameObject plat11 = CreateRockPlatform("Platform_11_MovingFast", courseHard.transform, new Vector3(0f, 19.5f, 102f), new Vector3(2.8f, 0.8f, 2.8f), matLava, 114, isStatic: false);
        var osc11 = plat11.AddComponent<Oscillator>();
        osc11.moveDirection = Vector3.right;
        osc11.moveDistance = 3.4f;
        osc11.moveSpeed = 2.6f;

        CreateRockPlatform("Platform_12", courseHard.transform, new Vector3(2.0f, 21.2f, 110f), new Vector3(2.2f, 0.8f, 2.2f), matRock, 115);
        CreateRockPlatform("Platform_13", courseHard.transform, new Vector3(0f, 22.8f, 118f), new Vector3(2.6f, 0.8f, 2.6f), matRock, 116);

        // Hard Finish Sanctuary (Grand Volcanic Rock Altar at Z = 129)
        CreateRockPlatform("FinishPlatform_Rock", courseHard.transform, new Vector3(0, 24.2f, 129f), new Vector3(11f, 1.8f, 11f), matRock, 117);
        SetupVolcanicFinishArch(courseHard.transform, new Vector3(0, 25.2f, 129f), matRock, matLava, winClip);

        // --- MEDIUM COURSE (2 Easy Jumps, 1 Checkpoint, 2 Hard Jumps, Finish Altar) ---
        // Jump 1 (Easy): Broad stable rock platform
        CreateRockPlatform("Med_Platform_1_Easy", courseMedium.transform, new Vector3(0f, 0.6f, 8.5f), new Vector3(4.8f, 1.1f, 4.8f), matRock, 601);
        // Jump 2 (Easy): Broad stable rock platform with slight elevation
        CreateRockPlatform("Med_Platform_2_Easy", courseMedium.transform, new Vector3(1.6f, 1.4f, 17.5f), new Vector3(4.4f, 1.1f, 4.4f), matRock, 602);
        // Checkpoint Sanctuary (Z = 26.5): Safe haven platform with single Checkpoint trigger
        CreateRockPlatform("Med_CheckpointPlatform", courseMedium.transform, new Vector3(0f, 2.4f, 26.5f), new Vector3(6.5f, 1.3f, 6.5f), matRock, 603);
        CreateCheckpoint("Checkpoint_Medium", courseMedium.transform, new Vector3(0f, 3.2f, 26.5f), matCpInactive, matCpActive, chimeClip);
        // Jump 3 (Hard Jump 1 after Checkpoint): Moving magma slab with precision gap
        GameObject medPlat3 = CreateRockPlatform("Med_Platform_3_HardMoving", courseMedium.transform, new Vector3(-2.6f, 4.2f, 36.0f), new Vector3(2.8f, 0.9f, 2.8f), matLava, 604, isStatic: false);
        var oscMed = medPlat3.AddComponent<Oscillator>();
        oscMed.moveDirection = Vector3.right;
        oscMed.moveDistance = 2.4f;
        oscMed.moveSpeed = 1.6f;
        // Jump 4 (Hard Jump 2 after Checkpoint): Narrow elevated rock stepping stone
        CreateRockPlatform("Med_Platform_4_Hard", courseMedium.transform, new Vector3(2.4f, 6.0f, 45.0f), new Vector3(2.4f, 0.9f, 2.4f), matRock, 605);
        // Medium Finish Sanctuary Altar (Z = 54.0)
        CreateRockPlatform("Med_FinishPlatform", courseMedium.transform, new Vector3(0f, 7.5f, 54.0f), new Vector3(9.5f, 1.5f, 9.5f), matRock, 606);
        SetupVolcanicFinishArch(courseMedium.transform, new Vector3(0f, 8.5f, 54.0f), matRock, matLava, winClip);

        // --- EASY COURSE (Exactly 2 Jumps, Broad Platforms, No Checkpoints, No Hazards) ---
        // Jump 1: Broad stable rock platform
        CreateRockPlatform("Easy_Platform_1", courseEasy.transform, new Vector3(0f, 0.4f, 8.5f), new Vector3(5.2f, 1.1f, 5.2f), matRock, 501);
        // Jump 2: Broad stable rock platform
        CreateRockPlatform("Easy_Platform_2", courseEasy.transform, new Vector3(0f, 0.9f, 17.0f), new Vector3(5.2f, 1.1f, 5.2f), matRock, 502);
        // Easy Finish Sanctuary Altar (Z = 25.5)
        CreateRockPlatform("Easy_FinishPlatform", courseEasy.transform, new Vector3(0f, 1.4f, 25.5f), new Vector3(8.5f, 1.4f, 8.5f), matRock, 503);
        SetupVolcanicFinishArch(courseEasy.transform, new Vector3(0f, 2.4f, 25.5f), matRock, matLava, winClip);

        // All courses ready; Difficulty Menu controls visibility
        courseEasy.SetActive(false);
        courseMedium.SetActive(false);
        courseHard.SetActive(false);

        // 20. UI Canvas, Difficulty Selection Menu & EventSystem
        SetupGameUI(root.transform, courseEasy, courseMedium, courseHard);

        // 21. Tag registration & scene in build settings
        RegisterPlayerTag();
        EnsureSceneInBuildSettings(activeScene.path);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(activeScene);
        if (!string.IsNullOrEmpty(activeScene.path))
        {
            EditorSceneManager.SaveScene(activeScene, activeScene.path);
        }
        else
        {
            EditorSceneManager.SaveScene(activeScene, targetScenePath);
        }

        // Focus Scene View on Course
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0, 10, 45), new Vector3(35, 30, 80)), false);
        }

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Volcanic Storm Canyon Generated!",
                "The volcanic canyon course now has a dark, ominous storm sky matching an erupting volcano!\n\n" +
                "• Dark Procedural Skybox (Exposure 0.38, Atmosphere Thickness 2.0, no blue/white tones)\n" +
                "• Overhead Dark Storm Cloud Canopy with turbulent UV scrolling\n" +
                "• Matching Blood-Orange Horizon Fog (Density 0.019)\n" +
                "• Low-Angle Deep Red-Orange Rim Sun & Subdued Ambient Light\n" +
                "• ACES Tonemapping, Raised Bloom Threshold (1.15), -0.35 EV, Dark Red Vignette\n" +
                "• Dark Red-Black Camera Background Fallback\n" +
                "• Easy Level (2 Jumps) & Hard Level (3 Stages) with Difficulty Selection Screen\n" +
                "• Blazing Lava Rivers, Glowing Rock Platforms & Emerald Checkpoints\n\n" +
                "Press Play (▶) at the top to test your run!", "Awesome!");
        }
    }

    private static (Material rock, Material lava, Material fissure, Material spire, Material cpInactive, Material cpActive, Material skybox, Material clouds, Material ember, Material smoke) GetOrCreateVolcanicMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Materials");
        }

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null) unlitShader = Shader.Find("Unlit/Color");

        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader == null) skyShader = Shader.Find("Skybox/6 Sided");

        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
        if (particleShader == null) particleShader = Shader.Find("Legacy Shaders/Particles/Additive");

        Texture2D texVolcanic = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/Volcanic_Rock_Crag.jpg");
        Texture2D texLava = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/Lava_Molten_Sea.jpg");
        Texture2D texSky = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Textures/Volcanic_Sky_Dusk.jpg");

        // 1. Volcanic Rock Material (Dark Basalt with Glowing Magma Veins in Crevices)
        Material matRock = LoadOrCreateMat("Assets/_Project/Materials/M_Volcanic_Rock.mat", litShader);
        if (texVolcanic != null)
        {
            SetTextureAndTiling(matRock, texVolcanic, 2.5f, 2.5f);
            matRock.EnableKeyword("_EMISSION");
            if (matRock.HasProperty("_EmissionColor"))
                matRock.SetColor("_EmissionColor", new Color(2.6f, 0.80f, 0.15f)); // High-contrast magma cracks
            if (matRock.HasProperty("_EmissionMap"))
                matRock.SetTexture("_EmissionMap", texVolcanic);
        }
        matRock.color = new Color(0.16f, 0.16f, 0.18f);
        SetSmoothness(matRock, 0.38f);

        // 2. Molten Magma Lava Sea Material (Blinding HDR Incandescent Flow)
        Material matLava = LoadOrCreateMat("Assets/_Project/Materials/M_Molten_Lava.mat", litShader);
        if (texLava != null)
        {
            SetTextureAndTiling(matLava, texLava, 4.0f, 4.0f);
            matLava.EnableKeyword("_EMISSION");
            if (matLava.HasProperty("_EmissionColor"))
                matLava.SetColor("_EmissionColor", new Color(5.8f, 2.3f, 0.45f)); // Vibrant HDR Orange-Gold
            if (matLava.HasProperty("_EmissionMap"))
                matLava.SetTexture("_EmissionMap", texLava);
        }
        matLava.color = new Color(1.0f, 0.65f, 0.25f);
        SetSmoothness(matLava, 0.85f);

        // 3. Glowing Lava Fissures / River Veins Material (Superheated Liquid Gold Core)
        Material matFissure = LoadOrCreateMat("Assets/_Project/Materials/M_Lava_Fissure.mat", unlitShader);
        if (texLava != null)
        {
            SetTextureAndTiling(matFissure, texLava, 1.0f, 6.0f);
        }
        matFissure.color = new Color(7.2f, 3.0f, 0.6f);

        // 4. Basalt Spire Material (Towering Dark Charcoal Monoliths with Wet Basalt Sheen)
        Material matSpire = LoadOrCreateMat("Assets/_Project/Materials/M_Basalt_Spire.mat", litShader);
        if (texVolcanic != null)
        {
            SetTextureAndTiling(matSpire, texVolcanic, 1.8f, 5.0f);
            matSpire.EnableKeyword("_EMISSION");
            if (matSpire.HasProperty("_EmissionColor"))
                matSpire.SetColor("_EmissionColor", new Color(0.9f, 0.25f, 0.05f));
            if (matSpire.HasProperty("_EmissionMap"))
                matSpire.SetTexture("_EmissionMap", texVolcanic);
        }
        matSpire.color = new Color(0.10f, 0.10f, 0.12f);
        SetSmoothness(matSpire, 0.36f);

        // 5. Checkpoint Inactive Pad
        Material matCpInactive = LoadOrCreateMat("Assets/_Project/Materials/M_Checkpoint_Inactive.mat", litShader);
        if (texVolcanic != null) SetTextureAndTiling(matCpInactive, texVolcanic, 1f, 1f);
        matCpInactive.color = new Color(0.24f, 0.26f, 0.30f);
        SetSmoothness(matCpInactive, 0.3f);

        // 6. Checkpoint Active Pad (Blazing Emerald Rune Glow - High Visual Contrast)
        Material matCpActive = LoadOrCreateMat("Assets/_Project/Materials/M_Checkpoint_Active.mat", litShader);
        if (texVolcanic != null) SetTextureAndTiling(matCpActive, texVolcanic, 1f, 1f);
        matCpActive.color = new Color(0.1f, 1.0f, 0.45f);
        matCpActive.EnableKeyword("_EMISSION");
        if (matCpActive.HasProperty("_EmissionColor"))
            matCpActive.SetColor("_EmissionColor", new Color(0.2f, 3.2f, 0.7f));
        SetSmoothness(matCpActive, 0.8f);

        // 7. Dark Volcanic Storm Skybox Material (No blue or lavender tones)
        // Top: Near-Black (#1A0603) | Middle: Dark Crimson (#4D0E07) | Horizon: Deep Orange (#B83808)
        Material matSkybox = LoadOrCreateMat("Assets/_Project/Materials/M_Volcanic_Skybox.mat", skyShader);
        if (matSkybox.HasProperty("_SunDisk")) matSkybox.SetInt("_SunDisk", 1); // Simple small disk
        if (matSkybox.HasProperty("_SunSize")) matSkybox.SetFloat("_SunSize", 0.015f); // Small sun size to prevent blinding halo
        if (matSkybox.HasProperty("_SunSizeConvergence")) matSkybox.SetFloat("_SunSizeConvergence", 10.0f);
        if (matSkybox.HasProperty("_AtmosphereThickness")) matSkybox.SetFloat("_AtmosphereThickness", 2.0f); // Max thickness filters out all blue/white
        if (matSkybox.HasProperty("_SkyTint")) matSkybox.SetColor("_SkyTint", new Color(0.10f, 0.022f, 0.012f, 1.0f)); // Top/Zenith: Near-black crimson
        if (matSkybox.HasProperty("_GroundColor")) matSkybox.SetColor("_GroundColor", new Color(0.035f, 0.010f, 0.005f, 1.0f)); // Bottom: Charred black
        if (matSkybox.HasProperty("_Exposure")) matSkybox.SetFloat("_Exposure", 0.38f); // Low exposure prevents lavender/white wash

        // 8. Overhead Storm Cloud Canopy Material (Heavy dark roiling storm cloud layer)
        Material matClouds = LoadOrCreateMat("Assets/_Project/Materials/M_Storm_Clouds.mat", litShader);
        if (texSky != null)
        {
            SetTextureAndTiling(matClouds, texSky, 2.5f, 4.0f);
            matClouds.EnableKeyword("_EMISSION");
            if (matClouds.HasProperty("_EmissionColor"))
                matClouds.SetColor("_EmissionColor", new Color(0.40f, 0.10f, 0.03f)); // Blood-orange cloud underglow
            if (matClouds.HasProperty("_EmissionMap"))
                matClouds.SetTexture("_EmissionMap", texSky);
        }
        else if (texVolcanic != null)
        {
            SetTextureAndTiling(matClouds, texVolcanic, 2.5f, 4.0f);
        }
        matClouds.color = new Color(0.10f, 0.035f, 0.03f, 1.0f);
        SetSmoothness(matClouds, 0.15f);

        // 9. Rising Ember Particle Material (Additive Glow)
        Material matEmber = LoadOrCreateMat("Assets/_Project/Materials/M_Volcanic_Ember.mat", particleShader);
        matEmber.SetFloat("_Surface", 1f); // Transparent
        matEmber.SetFloat("_Blend", 1f);   // Additive
        matEmber.color = new Color(1.0f, 0.65f, 0.15f, 1.0f);
        matEmber.EnableKeyword("_EMISSION");
        if (matEmber.HasProperty("_EmissionColor"))
            matEmber.SetColor("_EmissionColor", new Color(3.5f, 1.8f, 0.4f));

        // 10. Volcanic Smoke Particle Material (Alpha Blended Veil)
        Material matSmoke = LoadOrCreateMat("Assets/_Project/Materials/M_Volcanic_Smoke.mat", particleShader);
        matSmoke.SetFloat("_Surface", 1f); // Transparent
        matSmoke.SetFloat("_Blend", 0f);   // Alpha
        matSmoke.color = new Color(0.10f, 0.05f, 0.04f, 0.5f);

        AssetDatabase.SaveAssets();

        return (matRock, matLava, matFissure, matSpire, matCpInactive, matCpActive, matSkybox, matClouds, matEmber, matSmoke);
    }

    private static void SetupVolcanicAtmosphere(Material matSkybox)
    {
        // 1. Procedural Dark Volcanic Skybox
        if (matSkybox != null)
        {
            RenderSettings.skybox = matSkybox;
        }

        // 2. Volcanic Fog (Exponential Squared Dark Red-Brown Horizon Haze)
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.019f; // Raised slightly to blend distant spires & sky smoothly into haze
        RenderSettings.fogColor = new Color(0.14f, 0.035f, 0.018f); // Matches the dark blood-orange/red-brown horizon

        // 3. Environment Ambient Lighting (Subdued, dark, dramatic)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.06f, 0.015f, 0.01f);      // Near-black dark crimson overhead
        RenderSettings.ambientEquatorColor = new Color(0.16f, 0.04f, 0.02f);  // Deep blood-red canyon haze
        RenderSettings.ambientGroundColor = new Color(0.48f, 0.14f, 0.03f);   // Molten magma floor bounce
        RenderSettings.ambientIntensity = 0.6f;

        // 4. Directional Sun Light (Low-angle, deep red-orange, moderate intensity)
        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight == null || dirLight.type != LightType.Directional)
        {
            GameObject sunObj = new GameObject("Directional Light");
            dirLight = sunObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }

        dirLight.color = new Color(1.0f, 0.28f, 0.06f); // Deep red-orange sun
        dirLight.intensity = 0.60f; // Lowered intensity prevents bleaching the sky
        dirLight.transform.rotation = Quaternion.Euler(9f, 20f, 0f); // Low angle near horizon
        dirLight.shadows = LightShadows.Soft;
        RenderSettings.sun = dirLight;

        DynamicGI.UpdateEnvironment();
    }

    private static void SetupGlobalVolume(Transform parent)
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Settings"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Settings");
        }

        string profilePath = "Assets/_Project/Settings/Volcanic_VolumeProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }

        // 1. Bloom (High threshold prevents sky bloom; focused on incandescent lava & runes)
        if (!profile.TryGet<Bloom>(out var bloom))
        {
            bloom = profile.Add<Bloom>(true);
            if (!AssetDatabase.Contains(bloom) && AssetDatabase.Contains(profile))
                AssetDatabase.AddObjectToAsset(bloom, profile);
        }
        bloom.active = true;
        bloom.threshold.Override(1.15f); // Raised so sky and storm clouds never bloom into white
        bloom.intensity.Override(0.85f); // Controlled bloom for superheated lava
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(new Color(1.0f, 0.65f, 0.35f));
        bloom.highQualityFiltering.Override(true);

        // 2. Color Adjustments (Slight negative post-exposure & rich warm contrast)
        if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
        {
            colorAdj = profile.Add<ColorAdjustments>(true);
            if (!AssetDatabase.Contains(colorAdj) && AssetDatabase.Contains(profile))
                AssetDatabase.AddObjectToAsset(colorAdj, profile);
        }
        colorAdj.active = true;
        colorAdj.postExposure.Override(-0.35f); // Slight negative exposure deepens shadows and sky
        colorAdj.contrast.Override(28f);
        colorAdj.colorFilter.Override(new Color(1.0f, 0.78f, 0.70f));
        colorAdj.saturation.Override(22f);

        // 3. Tonemapping (ACES Filmic Curve)
        if (!profile.TryGet<Tonemapping>(out var tone))
        {
            tone = profile.Add<Tonemapping>(true);
            if (!AssetDatabase.Contains(tone) && AssetDatabase.Contains(profile))
                AssetDatabase.AddObjectToAsset(tone, profile);
        }
        tone.active = true;
        tone.mode.Override(TonemappingMode.ACES);

        // 4. Vignette (Dark Red Volcanic Vignette)
        if (!profile.TryGet<Vignette>(out var vig))
        {
            vig = profile.Add<Vignette>(true);
            if (!AssetDatabase.Contains(vig) && AssetDatabase.Contains(profile))
                AssetDatabase.AddObjectToAsset(vig, profile);
        }
        vig.active = true;
        vig.color.Override(new Color(0.18f, 0.02f, 0.01f)); // Dark blood-red vignette
        vig.intensity.Override(0.44f);
        vig.smoothness.Override(0.40f);
        vig.rounded.Override(false);

        // 5. Film Grain (Fine Volcanic Ash Texture)
        if (!profile.TryGet<FilmGrain>(out var grain))
        {
            grain = profile.Add<FilmGrain>(true);
            if (!AssetDatabase.Contains(grain) && AssetDatabase.Contains(profile))
                AssetDatabase.AddObjectToAsset(grain, profile);
        }
        grain.active = true;
        grain.type.Override(FilmGrainLookup.Medium1);
        grain.intensity.Override(0.20f);
        grain.response.Override(0.70f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // Scene Global Volume Object
        GameObject volumeObj = GameObject.Find("Volcanic_GlobalVolume");
        if (volumeObj == null)
        {
            volumeObj = new GameObject("Volcanic_GlobalVolume");
        }
        volumeObj.transform.parent = parent;

        var volComp = volumeObj.GetComponent<Volume>();
        if (volComp == null) volComp = volumeObj.AddComponent<Volume>();
        volComp.isGlobal = true;
        volComp.priority = 10f;
        volComp.sharedProfile = profile;
    }

    private static void SetupStormCloudCanopy(Transform parent, Material matClouds)
    {
        GameObject cloudsRoot = new GameObject("Canyon_StormCloudCanopy");
        cloudsRoot.transform.parent = parent;

        // 1. Inverted Curved Storm Cloud Canopy spanning over the canyon vault
        GameObject canopyObj = new GameObject("StormCloud_OverheadVault");
        canopyObj.transform.parent = cloudsRoot.transform;
        canopyObj.transform.position = new Vector3(0, 48f, 0);

        var mf = canopyObj.AddComponent<MeshFilter>();
        mf.sharedMesh = CreateInvertedCanopyMesh(120f, 250f, 16, 24, 18f);

        var mr = canopyObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = matClouds;

        var flow = canopyObj.AddComponent<LavaFlow>();
        var so = new SerializedObject(flow);
        so.FindProperty("scrollSpeed").vector2Value = new Vector2(0.002f, 0.005f); // Slow ominous storm drift
        so.ApplyModifiedProperties();

        // 2. Far Horizon Cloud Bank covering the canyon notch gap so the horizon is never blank
        GameObject farCloudBank = GameObject.CreatePrimitive(PrimitiveType.Quad);
        farCloudBank.name = "StormCloud_FarHorizonBank";
        farCloudBank.transform.parent = cloudsRoot.transform;
        farCloudBank.transform.position = new Vector3(0, 32f, 240f);
        farCloudBank.transform.localScale = new Vector3(160f, 65f, 1f);
        farCloudBank.transform.rotation = Quaternion.Euler(15f, 0f, 0f); // Tilted facing player
        farCloudBank.GetComponent<MeshRenderer>().sharedMaterial = matClouds;
        DestroyImmediate(farCloudBank.GetComponent<Collider>());

        var flowBank = farCloudBank.AddComponent<LavaFlow>();
        var soBank = new SerializedObject(flowBank);
        soBank.FindProperty("scrollSpeed").vector2Value = new Vector2(-0.003f, 0.002f);
        soBank.ApplyModifiedProperties();
    }

    private static Mesh CreateInvertedCanopyMesh(float width, float length, int resX, int resZ, float archHeight)
    {
        Mesh mesh = new Mesh();
        mesh.name = "InvertedCanopyMesh";

        var vertices = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();

        for (int z = 0; z < resZ; z++)
        {
            float tz = (float)z / (resZ - 1);
            float posZ = Mathf.Lerp(-40f, length, tz);

            for (int x = 0; x < resX; x++)
            {
                float tx = (float)x / (resX - 1);
                float posX = Mathf.Lerp(-width * 0.5f, width * 0.5f, tx);

                // Inverted vault: highest at center (overhead), dipping at the sides and far horizon
                float arch = (1f - Mathf.Pow((tx - 0.5f) * 2f, 2f)) * archHeight;
                float horizonDip = Mathf.Pow(tz, 2.5f) * (archHeight * 0.75f);
                float posY = arch - horizonDip;

                vertices.Add(new Vector3(posX, posY, posZ));
                uvs.Add(new Vector2(tx * 3.5f, tz * 7.0f));
            }
        }

        for (int z = 0; z < resZ - 1; z++)
        {
            for (int x = 0; x < resX - 1; x++)
            {
                int i0 = z * resX + x;
                int i1 = z * resX + (x + 1);
                int i2 = (z + 1) * resX + x;
                int i3 = (z + 1) * resX + (x + 1);

                // Face normals pointing DOWNWARD into the canyon toward the player
                triangles.Add(i0);
                triangles.Add(i1);
                triangles.Add(i2);

                triangles.Add(i1);
                triangles.Add(i3);
                triangles.Add(i2);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SetupCanyonWalls(Transform parent, Material matSpire, Material matRock)
    {
        GameObject wallsRoot = new GameObject("Canyon_RockWalls");
        wallsRoot.transform.parent = parent;

        Random.State oldState = Random.state;
        Random.InitState(777);

        // 1. Left Canyon Wall & Ridges (X: -24 to -42, Z: -20 to 180)
        for (int i = 0; i < 22; i++)
        {
            float z = -20f + i * 9.2f;
            float x = -26f - Mathf.Sin(i * 0.4f) * 6.0f - Random.Range(0f, 6f);
            float h = 42f + Random.Range(0f, 32f);
            float w = 14f + Random.Range(0f, 8f);
            float d = 16f + Random.Range(0f, 8f);

            GameObject wallChunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallChunk.name = $"WallL_Chunk_{i}";
            wallChunk.transform.parent = wallsRoot.transform;
            wallChunk.transform.position = new Vector3(x, h * 0.45f - 12f, z);
            wallChunk.transform.localScale = new Vector3(w, h, d);
            wallChunk.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(10f, 45f), Random.Range(8f, 22f));
            wallChunk.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? matSpire : matRock;
            MarkStatic(wallChunk);
        }

        // 2. Right Canyon Wall & Ridges (X: +24 to +42, Z: -20 to 180)
        for (int i = 0; i < 22; i++)
        {
            float z = -20f + i * 9.2f;
            float x = 26f + Mathf.Cos(i * 0.4f) * 6.0f + Random.Range(0f, 6f);
            float h = 42f + Random.Range(0f, 32f);
            float w = 14f + Random.Range(0f, 8f);
            float d = 16f + Random.Range(0f, 8f);

            GameObject wallChunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallChunk.name = $"WallR_Chunk_{i}";
            wallChunk.transform.parent = wallsRoot.transform;
            wallChunk.transform.position = new Vector3(x, h * 0.45f - 12f, z);
            wallChunk.transform.localScale = new Vector3(w, h, d);
            wallChunk.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(-45f, -10f), Random.Range(-22f, -8f));
            wallChunk.GetComponent<MeshRenderer>().sharedMaterial = (i % 2 == 0) ? matSpire : matRock;
            MarkStatic(wallChunk);
        }

        // 3. Far Canyon Gateway: Bluffs flanking the sides, framing the horizon
        for (int i = 0; i < 8; i++)
        {
            float xL = -32f - i * 5.0f;
            float zL = 180f + (i % 3) * 12f;
            float hL = 55f + i * 4.0f;
            GameObject chunkL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunkL.name = $"FarBluffL_{i}";
            chunkL.transform.parent = wallsRoot.transform;
            chunkL.transform.position = new Vector3(xL, hL * 0.45f - 12f, zL);
            chunkL.transform.localScale = new Vector3(20f, hL, 22f);
            chunkL.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(20f, 60f), Random.Range(5f, 18f));
            chunkL.GetComponent<MeshRenderer>().sharedMaterial = matSpire;
            MarkStatic(chunkL);

            float xR = 32f + i * 5.0f;
            float zR = 180f + (i % 3) * 12f;
            float hR = 55f + i * 4.0f;
            GameObject chunkR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunkR.name = $"FarBluffR_{i}";
            chunkR.transform.parent = wallsRoot.transform;
            chunkR.transform.position = new Vector3(xR, hR * 0.45f - 12f, zR);
            chunkR.transform.localScale = new Vector3(20f, hR, 22f);
            chunkR.transform.rotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(-60f, -20f), Random.Range(-18f, -5f));
            chunkR.GetComponent<MeshRenderer>().sharedMaterial = matSpire;
            MarkStatic(chunkR);
        }

        Random.state = oldState;
    }

    private static void SetupCanyonSpires(Transform parent, Material matSpire)
    {
        GameObject spiresGroup = new GameObject("Canyon_BasaltSpires");
        spiresGroup.transform.parent = parent;

        Random.State oldState = Random.state;
        Random.InitState(4242);

        // LAYER 1: Inner Canyon Basalt Spires flanking the parkour route (Z: -10 to 145)
        float[] leftZ = { -10f, 10f, 25f, 45f, 65f, 85f, 105f, 125f, 142f };
        for (int i = 0; i < leftZ.Length; i++)
        {
            float x = -19f - Random.Range(0f, 5.0f);
            float z = leftZ[i] + Random.Range(-3f, 3f);
            float h = 38f + Random.Range(0f, 24f);
            float radius = 3.6f + Random.Range(0f, 1.8f);
            CreateSpireWithVariation($"SpireL1_{i}", spiresGroup.transform, new Vector3(x, -12f, z), h, radius, matSpire, 1000 + i);
        }

        float[] rightZ = { -5f, 15f, 35f, 55f, 75f, 95f, 115f, 135f, 148f };
        for (int i = 0; i < rightZ.Length; i++)
        {
            float x = 19f + Random.Range(0f, 5.0f);
            float z = rightZ[i] + Random.Range(-3f, 3f);
            float h = 40f + Random.Range(0f, 24f);
            float radius = 3.6f + Random.Range(0f, 1.8f);
            CreateSpireWithVariation($"SpireR1_{i}", spiresGroup.transform, new Vector3(x, -12f, z), h, radius, matSpire, 2000 + i);
        }

        // LAYER 2: Mid-Distant Spires (Z: 155 to 210, H: 65 to 110m)
        for (int i = 0; i < 10; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float x = side * (16f + (i * 2.8f) + Random.Range(0f, 6f));
            float z = 155f + i * 6.5f + Random.Range(-4f, 4f);
            float h = 65f + Random.Range(0f, 45f);
            float radius = 5.2f + Random.Range(0f, 2.8f);
            CreateSpireWithVariation($"Spire_MidDepth_{i}", spiresGroup.transform, new Vector3(x, -12f, z), h, radius, matSpire, 3000 + i);
        }

        // LAYER 3: Colossal Horizon Silhouette Spires (Z: 230 to 300, H: 130 to 180m)
        float[] horizonX = { -14f, -7f, 0f, 8f, 15f, -22f, 23f };
        for (int i = 0; i < horizonX.Length; i++)
        {
            float x = horizonX[i] + Random.Range(-2.5f, 2.5f);
            float z = 235f + (i % 3) * 25f + Random.Range(0f, 15f);
            float h = 135f + Random.Range(0f, 55f);
            float radius = 9.5f + Random.Range(0f, 5.0f);
            CreateSpireWithVariation($"Spire_HorizonSilhouette_{i}", spiresGroup.transform, new Vector3(x, -14f, z), h, radius, matSpire, 4000 + i);
        }

        Random.state = oldState;
    }

    private static void CreateSpireWithVariation(string name, Transform parent, Vector3 pos, float height, float baseRadius, Material mat, int seed)
    {
        GameObject spireObj = new GameObject(name);
        spireObj.transform.parent = parent;
        spireObj.transform.position = pos;

        Random.State oldState = Random.state;
        Random.InitState(seed);
        float tiltX = Random.Range(-6f, 6f);
        float tiltZ = Random.Range(-6f, 6f);
        float rotY = Random.Range(0f, 360f);
        spireObj.transform.rotation = Quaternion.Euler(tiltX, rotY, tiltZ);
        Random.state = oldState;

        var mf = spireObj.AddComponent<MeshFilter>();
        mf.sharedMesh = CreateSpireMesh(height, baseRadius, seed);

        var mr = spireObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        var col = spireObj.AddComponent<MeshCollider>();
        col.sharedMesh = mf.sharedMesh;

        MarkStatic(spireObj);
    }

    private static void SetupLavaSea(Transform parent, Material matLava, Material matFissure, Material matRock, Material matSpire)
    {
        // 1. Vast Molten Lava Sea Floor (Kill Zone with smooth UV flow)
        GameObject lavaSea = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lavaSea.name = "LavaSea_KillZone";
        lavaSea.transform.parent = parent;
        lavaSea.transform.position = new Vector3(0, -12f, 65f);
        lavaSea.transform.localScale = new Vector3(320f, 2.0f, 320f);
        lavaSea.GetComponent<MeshRenderer>().sharedMaterial = matLava;

        var col = lavaSea.GetComponent<BoxCollider>();
        col.isTrigger = true;
        lavaSea.AddComponent<HazardTrigger>();
        lavaSea.AddComponent<LavaFlow>();

        // 2. Central Meandering Lava River Ribbon & Glowing Veins
        GameObject fissuresRoot = new GameObject("LavaRiver_Fissures");
        fissuresRoot.transform.parent = parent;

        int numSteps = 32;
        float startZ = -20f;
        float endZ = 160f;
        float stepLength = (endZ - startZ) / numSteps;

        for (int i = 0; i < numSteps; i++)
        {
            float z = startZ + i * stepLength;
            float x = Mathf.Sin(z * 0.07f) * 3.8f + Mathf.Cos(z * 0.15f) * 1.5f;
            float angle = Mathf.Cos(z * 0.07f) * 18f;

            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = $"LavaFissure_{i}";
            strip.transform.parent = fissuresRoot.transform;
            strip.transform.position = new Vector3(x, -10.95f, z);
            strip.transform.localScale = new Vector3(2.6f + Mathf.Sin(i * 0.5f) * 0.8f, 0.25f, stepLength * 1.25f);
            strip.transform.rotation = Quaternion.Euler(0, angle, 0);
            strip.GetComponent<MeshRenderer>().sharedMaterial = matFissure;
            DestroyImmediate(strip.GetComponent<Collider>());
            strip.AddComponent<LavaFlow>();
            MarkStatic(strip);
        }

        // 3. Scattered Broken Basalt Slabs & Boulders
        GameObject slabsRoot = new GameObject("Lava_BrokenSlabs");
        slabsRoot.transform.parent = parent;

        Random.State oldState = Random.state;
        Random.InitState(999);

        for (int i = 0; i < 35; i++)
        {
            float z = Random.Range(-15f, 150f);
            float side = (i % 2 == 0) ? -1f : 1f;
            float x = side * Random.Range(6f, 18f) + Random.Range(-2f, 2f);
            float y = Random.Range(-10.8f, -8.0f);

            Vector3 slabSize = new Vector3(Random.Range(3.5f, 7.5f), Random.Range(1.2f, 2.5f), Random.Range(3.5f, 8.0f));

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = $"BasaltSlab_{i}";
            slab.transform.parent = slabsRoot.transform;
            slab.transform.position = new Vector3(x, y, z);
            slab.transform.localScale = slabSize;
            slab.transform.rotation = Quaternion.Euler(Random.Range(15f, 40f), Random.Range(0f, 360f), Random.Range(10f, 35f));
            slab.GetComponent<MeshRenderer>().sharedMaterial = (i % 3 == 0) ? matRock : matSpire;
            MarkStatic(slab);
        }

        Random.state = oldState;
    }

    private static void SetupLavaLights(Transform parent)
    {
        GameObject lightsRoot = new GameObject("Lava_UnderglowLights");
        lightsRoot.transform.parent = parent;

        float[] lightZ = { -10f, 15f, 38f, 62f, 85f, 108f, 130f, 155f };
        for (int i = 0; i < lightZ.Length; i++)
        {
            float xOffset = ((i % 2 == 0) ? -1f : 1f) * 3.5f;
            CreateLavaPointLight($"LavaGlow_{i + 1}", lightsRoot.transform, new Vector3(xOffset, -9.2f, lightZ[i]), 36f, 3.8f);
        }
    }

    private static void CreateLavaPointLight(string name, Transform parent, Vector3 pos, float range, float intensity)
    {
        GameObject lightObj = new GameObject(name);
        lightObj.transform.parent = parent;
        lightObj.transform.position = pos;
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1.0f, 0.42f, 0.10f);
        light.range = range;
        light.intensity = intensity;
        light.lightmapBakeType = LightmapBakeType.Mixed;
        MarkStatic(lightObj);
    }

    private static void SetupVolcanicParticles(Transform parent, Material matEmber, Material matSmoke)
    {
        GameObject particlesRoot = new GameObject("Volcanic_Particles");
        particlesRoot.transform.parent = parent;

        // 1. Rising Embers Particle System
        GameObject emberObj = new GameObject("Particle_RisingEmbers");
        emberObj.transform.parent = particlesRoot.transform;
        emberObj.transform.position = new Vector3(0, -9.0f, 65f);

        var psEmber = emberObj.AddComponent<ParticleSystem>();
        var mainE = psEmber.main;
        mainE.loop = true;
        mainE.playOnAwake = true;
        mainE.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 7.5f);
        mainE.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
        mainE.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        mainE.maxParticles = 500;
        mainE.simulationSpace = ParticleSystemSimulationSpace.World;

        var emissionE = psEmber.emission;
        emissionE.rateOverTime = 75f;

        var shapeE = psEmber.shape;
        shapeE.shapeType = ParticleSystemShapeType.Box;
        shapeE.position = Vector3.zero;
        shapeE.scale = new Vector3(42f, 2f, 165f);

        var velE = psEmber.velocityOverLifetime;
        velE.enabled = true;
        velE.space = ParticleSystemSimulationSpace.World;
        velE.x = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        velE.y = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
        velE.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.8f);

        var colE = psEmber.colorOverLifetime;
        colE.enabled = true;
        Gradient gradE = new Gradient();
        gradE.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(2.6f, 1.9f, 0.5f), 0.0f),
                new GradientColorKey(new Color(1.8f, 0.5f, 0.08f), 0.45f),
                new GradientColorKey(new Color(0.4f, 0.08f, 0.02f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.85f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colE.color = gradE;

        var psRendererE = emberObj.GetComponent<ParticleSystemRenderer>();
        psRendererE.sharedMaterial = matEmber;

        // 2. Volcanic Smoke / Ash Layer
        GameObject smokeObj = new GameObject("Particle_VolcanicSmoke");
        smokeObj.transform.parent = particlesRoot.transform;
        smokeObj.transform.position = new Vector3(0, -8.5f, 65f);

        var psSmoke = smokeObj.AddComponent<ParticleSystem>();
        var mainS = psSmoke.main;
        mainS.loop = true;
        mainS.playOnAwake = true;
        mainS.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        mainS.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        mainS.startSize = new ParticleSystem.MinMaxCurve(4.5f, 8.5f);
        mainS.maxParticles = 300;
        mainS.simulationSpace = ParticleSystemSimulationSpace.World;

        var emissionS = psSmoke.emission;
        emissionS.rateOverTime = 28f;

        var shapeS = psSmoke.shape;
        shapeS.shapeType = ParticleSystemShapeType.Box;
        shapeS.position = Vector3.zero;
        shapeS.scale = new Vector3(45f, 1.5f, 165f);

        var sizeS = psSmoke.sizeOverLifetime;
        sizeS.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.6f);
        sizeCurve.AddKey(0.5f, 1.3f);
        sizeCurve.AddKey(1f, 2.2f);
        sizeS.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colS = psSmoke.colorOverLifetime;
        colS.enabled = true;
        Gradient gradS = new Gradient();
        gradS.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.12f, 0.07f, 0.05f), 0.0f),
                new GradientColorKey(new Color(0.08f, 0.04f, 0.03f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.16f, 0.25f),
                new GradientAlphaKey(0.12f, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colS.color = gradS;

        var rotS = psSmoke.rotationOverLifetime;
        rotS.enabled = true;
        rotS.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

        var psRendererS = smokeObj.GetComponent<ParticleSystemRenderer>();
        psRendererS.sharedMaterial = matSmoke;
    }

    private static GameObject CreateRockPlatform(string name, Transform parent, Vector3 pos, Vector3 size, Material mat, int seed, bool isStatic = true)
    {
        GameObject rockObj = new GameObject(name);
        rockObj.transform.parent = parent;
        rockObj.transform.position = pos;

        var meshFilter = rockObj.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateRockMesh(size, seed, 0.26f, true);

        var meshRenderer = rockObj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mat;

        var col = rockObj.AddComponent<BoxCollider>();
        col.size = new Vector3(size.x * 0.92f, size.y, size.z * 0.92f);
        col.center = Vector3.zero;

        if (isStatic)
        {
            MarkStatic(rockObj);
        }

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
                        p.y += deform * 0.08f;
                        p.x += deform * 0.22f;
                        p.z += deform * 0.22f;
                    }
                    else
                    {
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

    private static Mesh CreateSpireMesh(float height, float baseRadius, int seed)
    {
        Mesh mesh = new Mesh();
        mesh.name = "SpireMesh_" + seed;

        Random.State oldState = Random.state;
        Random.InitState(seed);

        int sides = 7;
        int segments = 6;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var uvs = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();

        Vector3[,] grid = new Vector3[segments + 1, sides];

        for (int s = 0; s <= segments; s++)
        {
            float t = (float)s / segments;
            float y = t * height;
            float radius = baseRadius * Mathf.Lerp(1f, 0.06f, Mathf.Pow(t, 0.85f));

            for (int i = 0; i < sides; i++)
            {
                float angle = (float)i / sides * Mathf.PI * 2f;
                float ribNoise = (Mathf.PerlinNoise(i * 1.8f + seed, s * 0.9f) - 0.5f) * 0.40f;
                float r = radius * (1f + ribNoise);
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

        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "PadVisual";
        pad.transform.parent = cpObj.transform;
        pad.transform.position = pos;
        pad.transform.localScale = new Vector3(2.6f, 0.18f, 2.6f);
        var padRenderer = pad.GetComponent<MeshRenderer>();
        padRenderer.sharedMaterial = inactiveMat;
        DestroyImmediate(pad.GetComponent<Collider>());

        var boxCol = cpObj.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;
        boxCol.center = new Vector3(0, 1.0f, 0);
        boxCol.size = new Vector3(3.2f, 2.5f, 3.2f);

        var audio = cpObj.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = chime;

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

        var pL = CreateBox("PillarL", arch.transform, pos + new Vector3(-3.0f, 2.5f, 0), new Vector3(0.9f, 5.0f, 0.9f), stoneMat);
        MarkStatic(pL);

        var pR = CreateBox("PillarR", arch.transform, pos + new Vector3(3.0f, 2.5f, 0), new Vector3(0.9f, 5.0f, 0.9f), stoneMat);
        MarkStatic(pR);

        var cross = CreateBox("Crossbar", arch.transform, pos + new Vector3(0f, 5.0f, 0), new Vector3(7.0f, 0.9f, 0.9f), lavaMat);
        MarkStatic(cross);

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

    private static void SetupBackgroundMusic(Transform parent, AudioClip horrorClip)
    {
        GameObject musicObj = new GameObject("Background_Music");
        musicObj.transform.parent = parent;
        var bgm = musicObj.AddComponent<BackgroundMusic>();
        var soBgm = new SerializedObject(bgm);
        if (horrorClip != null)
        {
            soBgm.FindProperty("musicClip").objectReferenceValue = horrorClip;
        }
        soBgm.FindProperty("volume").floatValue = 0.45f;
        soBgm.ApplyModifiedProperties();

        var audioSource = musicObj.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.clip = horrorClip;
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            audioSource.volume = 0.45f;
            audioSource.spatialBlend = 0f;
        }
    }

    private static GameObject SetupPlayer(Transform parent, Transform startCheckpoint, AudioClip deathClip = null)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            player = new GameObject("Player");
        }
        player.tag = "Player";
        player.transform.parent = parent;

        var cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();

        cc.enabled = false;
        player.transform.position = startCheckpoint.position;
        player.transform.rotation = Quaternion.identity;
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 1f, 0);
        cc.enabled = true;

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

        // Dark red-black background fallback in case skybox is ever missing
        mainCam.clearFlags = CameraClearFlags.Skybox;
        mainCam.backgroundColor = new Color(0.05f, 0.012f, 0.008f, 1.0f);

        var pm = player.GetComponent<PlayerMovement>();
        if (pm == null) pm = player.AddComponent<PlayerMovement>();
        var soPm = new SerializedObject(pm);
        soPm.FindProperty("cameraTransform").objectReferenceValue = mainCam.transform;
        soPm.ApplyModifiedProperties();

        var pr = player.GetComponent<PlayerRespawn>();
        if (pr == null) pr = player.AddComponent<PlayerRespawn>();
        var soPr = new SerializedObject(pr);
        soPr.FindProperty("currentCheckpoint").objectReferenceValue = startCheckpoint;
        if (deathClip != null)
        {
            soPr.FindProperty("deathSound").objectReferenceValue = deathClip;
        }
        soPr.ApplyModifiedProperties();

        return player;
    }

    private static void SetupGameUI(Transform root, GameObject courseEasy = null, GameObject courseMedium = null, GameObject courseHard = null)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
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
        canvas.transform.SetParent(root, false);

        var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        GameObject esObj = (eventSystem != null) ? eventSystem.gameObject : new GameObject("EventSystem");
        esObj.transform.SetParent(root, false);

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

        // 1. Difficulty Selection Panel (Shown First Before Playing)
        GameObject diffPanel = new GameObject("DifficultySelectionPanel");
        diffPanel.transform.SetParent(canvas.transform, false);
        var diffRect = diffPanel.AddComponent<RectTransform>();
        diffRect.anchorMin = Vector2.zero;
        diffRect.anchorMax = Vector2.one;
        diffRect.sizeDelta = Vector2.zero;
        var diffBg = diffPanel.AddComponent<Image>();
        diffBg.color = new Color(0.04f, 0.02f, 0.02f, 0.96f);

        // Title
        GameObject menuTitle = new GameObject("Title");
        menuTitle.transform.SetParent(diffPanel.transform, false);
        var menuTitleRect = menuTitle.AddComponent<RectTransform>();
        menuTitleRect.anchoredPosition = new Vector2(0, 180);
        menuTitleRect.sizeDelta = new Vector2(700, 80);
        var menuTitleTmp = menuTitle.AddComponent<TextMeshProUGUI>();
        menuTitleTmp.text = "🌋 SKY PARKOUR";
        menuTitleTmp.fontSize = 58;
        menuTitleTmp.fontStyle = FontStyles.Bold;
        menuTitleTmp.alignment = TextAlignmentOptions.Center;
        menuTitleTmp.color = new Color(1.0f, 0.65f, 0.15f);

        GameObject menuSub = new GameObject("Subtitle");
        menuSub.transform.SetParent(diffPanel.transform, false);
        var menuSubRect = menuSub.AddComponent<RectTransform>();
        menuSubRect.anchoredPosition = new Vector2(0, 120);
        menuSubRect.sizeDelta = new Vector2(600, 40);
        var menuSubTmp = menuSub.AddComponent<TextMeshProUGUI>();
        menuSubTmp.text = "SELECT DIFFICULTY TO PLAY";
        menuSubTmp.fontSize = 24;
        menuSubTmp.alignment = TextAlignmentOptions.Center;
        menuSubTmp.color = new Color(0.85f, 0.85f, 0.9f);

        // 2. HUD Root
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
        cpRect.sizeDelta = new Vector2(400, 60);
        var cpTmp = cpObj.AddComponent<TextMeshProUGUI>();
        cpTmp.text = "Checkpoints: 0 / 2";
        cpTmp.fontSize = 30;
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

        // 3. Victory Panel (Centered)
        GameObject winPanel = new GameObject("VictoryPanel");
        winPanel.transform.SetParent(canvas.transform, false);
        var winRect = winPanel.AddComponent<RectTransform>();
        winRect.anchorMin = Vector2.zero;
        winRect.anchorMax = Vector2.one;
        winRect.sizeDelta = Vector2.zero;
        var winBg = winPanel.AddComponent<Image>();
        winBg.color = new Color(0.08f, 0.02f, 0.02f, 0.94f);
        winBg.raycastTarget = true;

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

        GameObject finalTimeObj = new GameObject("FinalTimeText");
        finalTimeObj.transform.SetParent(winPanel.transform, false);
        var finalRect = finalTimeObj.AddComponent<RectTransform>();
        finalRect.anchoredPosition = new Vector2(0, 10);
        finalRect.sizeDelta = new Vector2(600, 60);
        var finalTmp = finalTimeObj.AddComponent<TextMeshProUGUI>();
        finalTmp.text = "Completed in: 00:00";
        finalTmp.fontSize = 30;
        finalTmp.alignment = TextAlignmentOptions.Center;
        finalTmp.color = Color.white;
        finalTmp.raycastTarget = false;

        var gameUI = canvas.GetComponent<GameUI>();
        if (gameUI == null) gameUI = canvas.gameObject.AddComponent<GameUI>();

        var soUI = new SerializedObject(gameUI);
        soUI.FindProperty("courseEasy").objectReferenceValue = courseEasy;
        soUI.FindProperty("courseMedium").objectReferenceValue = courseMedium;
        soUI.FindProperty("courseHard").objectReferenceValue = courseHard;
        soUI.FindProperty("difficultyPanel").objectReferenceValue = diffPanel;
        soUI.FindProperty("hudPanel").objectReferenceValue = hud;
        soUI.FindProperty("timerText").objectReferenceValue = timerTmp;
        soUI.FindProperty("checkpointText").objectReferenceValue = cpTmp;
        soUI.FindProperty("winPanel").objectReferenceValue = winPanel;
        soUI.FindProperty("finalTimeText").objectReferenceValue = finalTmp;
        soUI.ApplyModifiedProperties();

        winPanel.SetActive(false);
    }

    private static void MarkStatic(GameObject obj)
    {
        StaticEditorFlags flags = StaticEditorFlags.BatchingStatic |
                                  StaticEditorFlags.ContributeGI |
                                  StaticEditorFlags.ReflectionProbeStatic |
                                  StaticEditorFlags.OccluderStatic |
                                  StaticEditorFlags.OccludeeStatic;
        GameObjectUtility.SetStaticEditorFlags(obj, flags);
    }

    private static Material LoadOrCreateMat(string path, Shader shader)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else if (m.shader != shader && shader != null)
        {
            m.shader = shader;
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
