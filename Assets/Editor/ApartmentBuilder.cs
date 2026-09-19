using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class ApartmentBuilder : EditorWindow
{
    [MenuItem("ApartmentTour/Build Complete Scene")]
    public static void BuildScene()
    {
        ApartmentBuilder builder = ScriptableObject.CreateInstance<ApartmentBuilder>();
        builder.ExecuteBuild();
        DestroyImmediate(builder);
    }

    private Dictionary<string, Material> materials = new Dictionary<string, Material>();

    public void ExecuteBuild()
    {
        Debug.Log("<color=cyan><b>[ApartmentBuilder] Building optimized 3D Apartment Complex scene...</b></color>");

        string[] oldNames = new string[] { "Main Camera", "Directional Light", "Global Volume", "--- APARTMENT COMPLEX WORLD ---", "EventSystem" };
        foreach (string n in oldNames)
        {
            GameObject obj = GameObject.Find(n);
            if (obj != null) DestroyImmediate(obj);
        }

        CreateMaterials();

        GameObject root = new GameObject("--- APARTMENT COMPLEX WORLD ---");

        // 1. Managers
        GameObject mgmtGroup = new GameObject("--- MANAGERS ---");
        mgmtGroup.transform.SetParent(root.transform);

        GameObject gmObj = new GameObject("GameManager");
        gmObj.transform.SetParent(mgmtGroup.transform);
        GameManager gm = gmObj.AddComponent<GameManager>();

        GameObject soundObj = new GameObject("SoundManager");
        soundObj.transform.SetParent(mgmtGroup.transform);
        SoundManager sm = soundObj.AddComponent<SoundManager>();

        // 2. Lighting & Post-Processing
        GameObject lightGroup = new GameObject("--- LIGHTING & FX ---");
        lightGroup.transform.SetParent(root.transform);

        GameObject sunObj = new GameObject("Directional Light (Sun)");
        sunObj.transform.SetParent(lightGroup.transform);
        Light sunLight = sunObj.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = new Color(1f, 0.96f, 0.88f);
        sunLight.intensity = 1.25f;
        sunLight.shadows = LightShadows.Soft; // Single soft-shadow casting light for max performance!
        sunObj.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        GameObject dnObj = new GameObject("DayNightCycle");
        dnObj.transform.SetParent(mgmtGroup.transform);
        DayNightCycle dnc = dnObj.AddComponent<DayNightCycle>();
        dnc.sunLight = sunLight;

        GameObject volObj = new GameObject("Global PostProcessing Volume");
        volObj.transform.SetParent(lightGroup.transform);
        Volume volume = volObj.AddComponent<Volume>();
        volume.isGlobal = true;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "ApartmentTour_VolumeProfile";

        Bloom bloom;
        if (!profile.TryGet(out bloom)) bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = 0.35f;
        bloom.threshold.value = 1.05f;

        Tonemapping tonemapping;
        if (!profile.TryGet(out tonemapping)) tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.value = TonemappingMode.ACES;

        Vignette vignette;
        if (!profile.TryGet(out vignette)) vignette = profile.Add<Vignette>(true);
        vignette.intensity.value = 0.2f;
        vignette.smoothness.value = 0.35f;

        ColorAdjustments colorAdj;
        if (!profile.TryGet(out colorAdj)) colorAdj = profile.Add<ColorAdjustments>(true);
        colorAdj.contrast.value = 10f;
        colorAdj.saturation.value = 8f;

        volume.profile = profile;

        // 3. Surroundings & Grounds
        GameObject envGroup = new GameObject("--- SURROUNDINGS & GROUNDS ---");
        envGroup.transform.SetParent(root.transform);
        BuildGroundAndSurroundings(envGroup, dnc);

        // 4. 4-Floor Apartment Building Architecture
        GameObject bldgGroup = new GameObject("--- APARTMENT BUILDING ---");
        bldgGroup.transform.SetParent(root.transform);
        BuildBuildingStructure(bldgGroup, dnc);

        // 5. 3 Interactive Apartment Interiors
        GameObject interiorsGroup = new GameObject("--- INTERACTIVE APARTMENTS ---");
        interiorsGroup.transform.SetParent(root.transform);

        InspectableApartment apt1 = BuildApartment1_LivingKitchen(interiorsGroup);
        InspectableApartment apt2 = BuildApartment2_MasterSuite(interiorsGroup);
        InspectableApartment apt3 = BuildApartment3_PenthouseLoft(interiorsGroup);

        gm.apartments.Add(apt1);
        gm.apartments.Add(apt2);
        gm.apartments.Add(apt3);

        // 6. Player Character & Camera (Positioned for immediate building framing!)
        GameObject playerGroup = new GameObject("--- PLAYER & CAMERA ---");
        playerGroup.transform.SetParent(root.transform);
        PlayerController player = BuildPlayer(playerGroup);
        CameraController camController = player.GetComponentInChildren<CameraController>();

        gm.playerController = player;
        gm.cameraController = camController;

        // Overview Drone Anchor
        GameObject overviewAnchor = new GameObject("OverviewCameraAnchor");
        overviewAnchor.transform.SetParent(lightGroup.transform);
        overviewAnchor.transform.position = new Vector3(0f, 22f, -38f);
        overviewAnchor.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
        camController.overviewAnchor = overviewAnchor.transform;
        gm.overviewCameraAnchor = overviewAnchor.transform;

        // Ground Spawn Point (Framing building immediately!)
        GameObject spawnPt = new GameObject("GroundSpawnPoint");
        spawnPt.transform.SetParent(playerGroup.transform);
        spawnPt.transform.position = new Vector3(0f, 1.6f, -30f);
        spawnPt.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        gm.groundSpawnPoint = spawnPt.transform;

        // 7. Modern Minimal UI Canvas
        GameObject uiGroup = new GameObject("--- UI CANVAS ---");
        uiGroup.transform.SetParent(root.transform);
        UIManager uim = BuildUI(uiGroup, gm, dnc);

        dnc.SetTimeOfDay(TimeOfDay.Day, true);

        EditorUtility.SetDirty(root);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=green><b>[ApartmentBuilder] Optimized 3D Apartment Complex scene built successfully!</b></color>");
    }

    private void CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        Material MakeMat(string name, Color color, float smoothness = 0.5f, float metallic = 0f, bool isTransparent = false, Color? emission = null)
        {
            string path = "Assets/Materials/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(urpLit);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = urpLit;
            m.color = color;
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            m.enableInstancing = true; // Enables GPU Instancing / SRP Batching for high performance!

            if (isTransparent)
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 0);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.renderQueue = 3000;
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                m.SetFloat("_Surface", 0);
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                m.SetInt("_ZWrite", 1);
                m.renderQueue = -1;
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            else
            {
                m.DisableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.black);
            }

            EditorUtility.SetDirty(m);
            materials[name] = m;
            return m;
        }

        MakeMat("Mat_Asphalt", new Color(0.2f, 0.21f, 0.23f), 0.2f, 0.05f);
        MakeMat("Mat_RoadStripe", new Color(0.95f, 0.95f, 0.92f), 0.3f, 0f);
        MakeMat("Mat_Sidewalk", new Color(0.76f, 0.76f, 0.77f), 0.35f, 0.02f);
        MakeMat("Mat_Grass", new Color(0.28f, 0.54f, 0.24f), 0.1f, 0f);
        MakeMat("Mat_BuildingConcrete", new Color(0.92f, 0.93f, 0.94f), 0.35f, 0.02f);
        MakeMat("Mat_BuildingConcreteDark", new Color(0.28f, 0.3f, 0.33f), 0.4f, 0.05f);
        MakeMat("Mat_WoodSlat", new Color(0.68f, 0.44f, 0.24f), 0.45f, 0.05f);
        MakeMat("Mat_DarkMetal", new Color(0.12f, 0.13f, 0.15f), 0.65f, 0.85f);
        MakeMat("Mat_Glass", new Color(0.4f, 0.55f, 0.65f, 0.4f), 0.95f, 0.2f, true);
        MakeMat("Mat_BalconyGlass", new Color(0.3f, 0.45f, 0.55f, 0.55f), 0.9f, 0.1f, true);
        MakeMat("Mat_WoodFloor", new Color(0.72f, 0.53f, 0.34f), 0.55f, 0.05f);
        MakeMat("Mat_MarbleFloor", new Color(0.94f, 0.94f, 0.95f), 0.85f, 0.08f);
        MakeMat("Mat_FabricSofa", new Color(0.32f, 0.35f, 0.4f), 0.2f, 0f);
        MakeMat("Mat_FabricTeal", new Color(0.14f, 0.55f, 0.58f), 0.2f, 0f);
        MakeMat("Mat_FabricCream", new Color(0.88f, 0.86f, 0.82f), 0.2f, 0f);
        MakeMat("Mat_LightWoodTable", new Color(0.78f, 0.63f, 0.46f), 0.45f, 0.05f);
        MakeMat("Mat_KitchenCabinet", new Color(0.2f, 0.25f, 0.32f), 0.5f, 0.1f);
        MakeMat("Mat_KitchenCounter", new Color(0.96f, 0.96f, 0.97f), 0.85f, 0.05f);
        MakeMat("Mat_EmissiveWarmLight", new Color(1f, 0.92f, 0.75f), 0.5f, 0f, false, new Color(1f, 0.88f, 0.65f) * 3.5f);
        MakeMat("Mat_EmissiveNightWindow", new Color(1f, 0.85f, 0.55f), 0.5f, 0f, false, new Color(1f, 0.8f, 0.5f) * 2.2f);
        MakeMat("Mat_Foliage", new Color(0.22f, 0.48f, 0.2f), 0.2f, 0f);
        MakeMat("Mat_TreeBark", new Color(0.35f, 0.26f, 0.19f), 0.15f, 0f);
        MakeMat("Mat_BedSheet", new Color(0.95f, 0.95f, 0.96f), 0.25f, 0f);
        MakeMat("Mat_FireplaceGlow", new Color(1f, 0.45f, 0.1f), 0.5f, 0f, false, new Color(1f, 0.4f, 0.05f) * 4.0f);
        MakeMat("Mat_Water", new Color(0.2f, 0.6f, 0.75f, 0.8f), 0.95f, 0.1f, true);

        AssetDatabase.SaveAssets();
    }

    private Material GetMat(string name)
    {
        if (materials.ContainsKey(name)) return materials[name];
        string path = "Assets/Materials/" + name + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) materials[name] = m;
        return m;
    }

    private GameObject CreateCube(string name, Transform parent, Vector3 pos, Vector3 scale, string matName, bool addCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (GetMat(matName) != null) go.GetComponent<Renderer>().sharedMaterial = GetMat(matName);
        if (!addCollider)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }
        return go;
    }

    private GameObject CreateCylinder(string name, Transform parent, Vector3 pos, Vector3 scale, string matName, bool addCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (GetMat(matName) != null) go.GetComponent<Renderer>().sharedMaterial = GetMat(matName);
        if (!addCollider)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }
        return go;
    }

    private void BuildGroundAndSurroundings(GameObject parent, DayNightCycle dnc)
    {
        // Walkable ground plane
        CreateCube("GrassLandscape", parent.transform, new Vector3(0f, -0.2f, 0f), new Vector3(140f, 0.4f, 140f), "Mat_Grass", true);
        CreateCube("MainRoad", parent.transform, new Vector3(0f, 0.01f, -32f), new Vector3(140f, 0.1f, 12f), "Mat_Asphalt", false);

        for (int x = -60; x <= 60; x += 6)
        {
            CreateCube("RoadDashedLine", parent.transform, new Vector3(x, 0.07f, -32f), new Vector3(3f, 0.02f, 0.3f), "Mat_RoadStripe", false);
        }

        CreateCube("RoadCurbNorth", parent.transform, new Vector3(0f, 0.1f, -25.8f), new Vector3(140f, 0.2f, 0.4f), "Mat_Sidewalk", true);
        CreateCube("RoadCurbSouth", parent.transform, new Vector3(0f, 0.1f, -38.2f), new Vector3(140f, 0.2f, 0.4f), "Mat_Sidewalk", true);

        CreateCube("EntrancePlaza", parent.transform, new Vector3(0f, 0.05f, -16f), new Vector3(36f, 0.1f, 18f), "Mat_Sidewalk", false);
        CreateCube("SidewalkWest", parent.transform, new Vector3(-24f, 0.05f, -20f), new Vector3(12f, 0.1f, 10f), "Mat_Sidewalk", false);
        CreateCube("SidewalkEast", parent.transform, new Vector3(24f, 0.05f, -20f), new Vector3(12f, 0.1f, 10f), "Mat_Sidewalk", false);

        GameObject parkingGroup = new GameObject("ParkingLot");
        parkingGroup.transform.SetParent(parent.transform);
        CreateCube("ParkingAsphalt", parkingGroup.transform, new Vector3(32f, 0.06f, -14f), new Vector3(22f, 0.08f, 22f), "Mat_Asphalt", false);

        for (int i = 0; i < 4; i++)
        {
            float zPos = -22f + i * 5.2f;
            CreateCube("StallLine", parkingGroup.transform, new Vector3(32f, 0.11f, zPos), new Vector3(10f, 0.02f, 0.25f), "Mat_RoadStripe", false);
        }

        BuildModernCar(parkingGroup.transform, new Vector3(32f, 0.6f, -19.5f), new Color(0.85f, 0.85f, 0.87f));
        BuildModernCar(parkingGroup.transform, new Vector3(32f, 0.6f, -14.3f), new Color(0.15f, 0.2f, 0.35f));
        BuildModernCar(parkingGroup.transform, new Vector3(32f, 0.6f, -9.1f), new Color(0.65f, 0.15f, 0.15f));

        GameObject plazaFeatures = new GameObject("PlazaFeatures");
        plazaFeatures.transform.SetParent(parent.transform);

        GameObject fountain = new GameObject("ReflectingPool");
        fountain.transform.SetParent(plazaFeatures.transform);
        CreateCube("PoolRimOuter", fountain.transform, new Vector3(0f, 0.2f, -18f), new Vector3(8f, 0.4f, 5f), "Mat_DarkMetal", false);
        CreateCube("PoolWater", fountain.transform, new Vector3(0f, 0.35f, -18f), new Vector3(7.4f, 0.1f, 4.4f), "Mat_Water", false);
        CreateCube("FountainSculpture", fountain.transform, new Vector3(0f, 1.2f, -18f), new Vector3(1f, 1.8f, 1f), "Mat_BuildingConcreteDark", false);

        CreateCube("PlanterLeft", plazaFeatures.transform, new Vector3(-10f, 0.4f, -16f), new Vector3(4f, 0.8f, 1.5f), "Mat_BuildingConcreteDark", false);
        CreateCube("HedgeLeft", plazaFeatures.transform, new Vector3(-10f, 1.0f, -16f), new Vector3(3.6f, 0.6f, 1.2f), "Mat_Foliage", false);

        CreateCube("PlanterRight", plazaFeatures.transform, new Vector3(10f, 0.4f, -16f), new Vector3(4f, 0.8f, 1.5f), "Mat_BuildingConcreteDark", false);
        CreateCube("HedgeRight", plazaFeatures.transform, new Vector3(10f, 1.0f, -16f), new Vector3(3.6f, 0.6f, 1.2f), "Mat_Foliage", false);

        for (int x = -28; x <= -14; x += 4)
        {
            CreateCube("FrontHedgeW", plazaFeatures.transform, new Vector3(x, 0.6f, -24f), new Vector3(3.6f, 1.2f, 1f), "Mat_Foliage", false);
        }
        for (int x = 14; x <= 20; x += 4)
        {
            CreateCube("FrontHedgeE", plazaFeatures.transform, new Vector3(x, 0.6f, -24f), new Vector3(3.6f, 1.2f, 1f), "Mat_Foliage", false);
        }

        BuildModernBench(plazaFeatures.transform, new Vector3(-6f, 0f, -14f), 0f);
        BuildModernBench(plazaFeatures.transform, new Vector3(6f, 0f, -14f), 0f);

        BuildModernTree(plazaFeatures.transform, new Vector3(-16f, 0f, -20f));
        BuildModernTree(plazaFeatures.transform, new Vector3(-24f, 0f, -14f));
        BuildModernTree(plazaFeatures.transform, new Vector3(-22f, 0f, 4f));
        BuildModernTree(plazaFeatures.transform, new Vector3(22f, 0f, 4f));
        BuildModernTree(plazaFeatures.transform, new Vector3(-16f, 0f, 16f));
        BuildModernTree(plazaFeatures.transform, new Vector3(16f, 0f, 16f));

        GameObject lampsGroup = new GameObject("StreetLamps");
        lampsGroup.transform.SetParent(parent.transform);

        BuildStreetLamp(lampsGroup.transform, new Vector3(-12f, 0f, -24.5f), dnc);
        BuildStreetLamp(lampsGroup.transform, new Vector3(12f, 0f, -24.5f), dnc);
        BuildStreetLamp(lampsGroup.transform, new Vector3(-24f, 0f, -24.5f), dnc);
        BuildStreetLamp(lampsGroup.transform, new Vector3(24f, 0f, -24.5f), dnc);
        BuildStreetLamp(lampsGroup.transform, new Vector3(40f, 0f, -24.5f), dnc);
        BuildStreetLamp(lampsGroup.transform, new Vector3(40f, 0f, -4f), dnc);
    }

    private void BuildModernCar(Transform parent, Vector3 pos, Color bodyColor)
    {
        GameObject car = new GameObject("ModernCar");
        car.transform.SetParent(parent);
        car.transform.position = pos;

        Material carMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        carMat.color = bodyColor;
        carMat.SetFloat("_Smoothness", 0.9f);
        carMat.SetFloat("_Metallic", 0.7f);
        carMat.enableInstancing = true;

        GameObject lower = CreateCube("CarBodyLower", car.transform, pos + Vector3.up * 0.1f, new Vector3(2.2f, 0.6f, 4.4f), "Mat_DarkMetal", false);
        lower.GetComponent<Renderer>().sharedMaterial = carMat;

        CreateCube("CarCabin", car.transform, pos + Vector3.up * 0.65f + Vector3.back * 0.2f, new Vector3(1.9f, 0.65f, 2.4f), "Mat_Glass", false);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                CreateCylinder("Wheel", car.transform, pos + new Vector3(x * 1.1f, -0.2f, z * 1.3f), new Vector3(0.65f, 0.25f, 0.65f), "Mat_DarkMetal", false);
            }
        }
    }

    private void BuildModernBench(Transform parent, Vector3 pos, float rotY)
    {
        GameObject bench = new GameObject("ModernBench");
        bench.transform.SetParent(parent);
        bench.transform.position = pos;
        bench.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        CreateCube("BenchLegL", bench.transform, pos + new Vector3(-1f, 0.25f, 0f), new Vector3(0.2f, 0.5f, 0.6f), "Mat_BuildingConcreteDark", false);
        CreateCube("BenchLegR", bench.transform, pos + new Vector3(1f, 0.25f, 0f), new Vector3(0.2f, 0.5f, 0.6f), "Mat_BuildingConcreteDark", false);
        CreateCube("BenchSeat", bench.transform, pos + new Vector3(0f, 0.52f, 0f), new Vector3(2.4f, 0.08f, 0.6f), "Mat_WoodSlat", false);
        CreateCube("BenchBack", bench.transform, pos + new Vector3(0f, 0.85f, 0.28f), new Vector3(2.4f, 0.45f, 0.08f), "Mat_WoodSlat", false);
    }

    private void BuildModernTree(Transform parent, Vector3 pos)
    {
        GameObject tree = new GameObject("ArchitecturalTree");
        tree.transform.SetParent(parent);
        tree.transform.position = pos;

        CreateCylinder("Trunk", tree.transform, pos + Vector3.up * 2f, new Vector3(0.35f, 2f, 0.35f), "Mat_TreeBark", false);
        CreateCube("Foliage1", tree.transform, pos + Vector3.up * 4.2f, new Vector3(3.2f, 2.2f, 3.2f), "Mat_Foliage", false);
        CreateCube("Foliage2", tree.transform, pos + Vector3.up * 5.6f, new Vector3(2.2f, 1.6f, 2.2f), "Mat_Foliage", false);
        CreateCube("Foliage3", tree.transform, pos + Vector3.up * 6.6f, new Vector3(1.2f, 1.0f, 1.2f), "Mat_Foliage", false);
    }

    private void BuildStreetLamp(Transform parent, Vector3 pos, DayNightCycle dnc)
    {
        GameObject lamp = new GameObject("StreetLamp");
        lamp.transform.SetParent(parent);
        lamp.transform.position = pos;

        CreateCylinder("Pole", lamp.transform, pos + Vector3.up * 2.5f, new Vector3(0.12f, 2.5f, 0.12f), "Mat_DarkMetal", false);
        CreateCube("Arm", lamp.transform, pos + Vector3.up * 5.1f + Vector3.forward * 0.5f, new Vector3(0.1f, 0.1f, 1.2f), "Mat_DarkMetal", false);
        CreateCube("Fixture", lamp.transform, pos + Vector3.up * 5.0f + Vector3.forward * 1.0f, new Vector3(0.35f, 0.1f, 0.6f), "Mat_EmissiveWarmLight", false);

        GameObject lightObj = new GameObject("LampLight");
        lightObj.transform.SetParent(lamp.transform);
        lightObj.transform.position = pos + Vector3.up * 4.8f + Vector3.forward * 1.0f;
        Light pLight = lightObj.AddComponent<Light>();
        pLight.type = LightType.Point;
        pLight.color = new Color(1f, 0.88f, 0.65f);
        pLight.range = 14f;
        pLight.intensity = 2.0f;
        pLight.shadows = LightShadows.None; // Optimized: No shadows for point lights!

        if (dnc != null) dnc.nightLights.Add(pLight);
    }

    private void BuildBuildingStructure(GameObject parent, DayNightCycle dnc)
    {
        float bWidth = 26f;
        float bDepth = 16f;
        float fHeight = 3.8f;

        CreateCube("BuildingPodium", parent.transform, new Vector3(0f, 0.2f, 0f), new Vector3(bWidth + 2f, 0.4f, bDepth + 2f), "Mat_BuildingConcreteDark", true);

        for (int f = 0; f <= 4; f++)
        {
            float y = f * fHeight + 0.3f;
            float slabWidth = (f == 4) ? bWidth - 4f : bWidth;
            float slabDepth = (f == 4) ? bDepth - 2f : bDepth;
            CreateCube($"FloorSlab_Level{f}", parent.transform, new Vector3(0f, y, 0f), new Vector3(slabWidth, 0.4f, slabDepth), "Mat_BuildingConcreteDark", true);
        }

        CreateCube("ExteriorWall_Back", parent.transform, new Vector3(0f, 7.5f, bDepth * 0.5f), new Vector3(bWidth, 15f, 0.4f), "Mat_BuildingConcrete", true);
        CreateCube("ExteriorWall_West", parent.transform, new Vector3(-bWidth * 0.5f, 7.5f, 0f), new Vector3(0.4f, 15f, bDepth), "Mat_BuildingConcrete", true);
        CreateCube("ExteriorWall_East", parent.transform, new Vector3(bWidth * 0.5f, 7.5f, 0f), new Vector3(0.4f, 15f, bDepth), "Mat_BuildingConcrete", true);

        CreateCube("LobbyGlassWest", parent.transform, new Vector3(-8f, 2.0f, -bDepth * 0.5f), new Vector3(9f, 3.4f, 0.15f), "Mat_Glass", false);
        CreateCube("LobbyGlassEast", parent.transform, new Vector3(8f, 2.0f, -bDepth * 0.5f), new Vector3(9f, 3.4f, 0.15f), "Mat_Glass", false);
        CreateCube("LobbyEntranceDoors", parent.transform, new Vector3(0f, 1.8f, -bDepth * 0.5f), new Vector3(4.5f, 3.0f, 0.15f), "Mat_Glass", false);
        CreateCube("LobbyCanopy", parent.transform, new Vector3(0f, 3.8f, -bDepth * 0.5f - 2.5f), new Vector3(8f, 0.25f, 5f), "Mat_DarkMetal", false);
        CreateCube("CanopyColumnL", parent.transform, new Vector3(-3.8f, 1.9f, -bDepth * 0.5f - 4.8f), new Vector3(0.2f, 3.8f, 0.2f), "Mat_DarkMetal", false);
        CreateCube("CanopyColumnR", parent.transform, new Vector3(3.8f, 1.9f, -bDepth * 0.5f - 4.8f), new Vector3(0.2f, 3.8f, 0.2f), "Mat_DarkMetal", false);
        CreateCube("LobbySlatWall", parent.transform, new Vector3(0f, 2.0f, 0f), new Vector3(10f, 3.4f, 0.3f), "Mat_WoodSlat", false);
        CreateCube("ReceptionDesk", parent.transform, new Vector3(0f, 0.6f, -3f), new Vector3(3.5f, 1.1f, 1.2f), "Mat_MarbleFloor", false);

        for (int x = -12; x <= -4; x += 2)
        {
            CreateCube("WoodFinW", parent.transform, new Vector3(x, 9.5f, -bDepth * 0.5f - 0.2f), new Vector3(0.15f, 11f, 0.4f), "Mat_WoodSlat", false);
        }
        for (int x = 4; x <= 12; x += 2)
        {
            CreateCube("WoodFinE", parent.transform, new Vector3(x, 9.5f, -bDepth * 0.5f - 0.2f), new Vector3(0.15f, 11f, 0.4f), "Mat_WoodSlat", false);
        }

        GameObject rooftopGroup = new GameObject("RooftopGarden");
        rooftopGroup.transform.SetParent(parent.transform);
        CreateCube("RoofParapetFront", rooftopGroup.transform, new Vector3(0f, 16.0f, -bDepth * 0.5f + 1f), new Vector3(bWidth - 4f, 1.0f, 0.2f), "Mat_DarkMetal", false);
        CreateCube("RoofParapetBack", rooftopGroup.transform, new Vector3(0f, 16.0f, bDepth * 0.5f - 1f), new Vector3(bWidth - 4f, 1.0f, 0.2f), "Mat_DarkMetal", false);
        for (int x = -8; x <= 8; x += 2)
        {
            CreateCube("PergolaRafter", rooftopGroup.transform, new Vector3(x, 18.2f, 0f), new Vector3(0.15f, 0.25f, 8f), "Mat_WoodSlat", false);
        }
        CreateCube("PergolaBeamL", rooftopGroup.transform, new Vector3(-8f, 18.0f, 0f), new Vector3(0.2f, 0.3f, 8f), "Mat_DarkMetal", false);
        CreateCube("PergolaBeamR", rooftopGroup.transform, new Vector3(8f, 18.0f, 0f), new Vector3(0.2f, 0.3f, 8f), "Mat_DarkMetal", false);
        CreateCube("PergolaPost1", rooftopGroup.transform, new Vector3(-8f, 16.8f, -4f), new Vector3(0.2f, 2.8f, 0.2f), "Mat_DarkMetal", false);
        CreateCube("PergolaPost2", rooftopGroup.transform, new Vector3(-8f, 16.8f, 4f), new Vector3(0.2f, 2.8f, 0.2f), "Mat_DarkMetal", false);
        CreateCube("PergolaPost3", rooftopGroup.transform, new Vector3(8f, 16.8f, -4f), new Vector3(0.2f, 2.8f, 0.2f), "Mat_DarkMetal", false);
        CreateCube("PergolaPost4", rooftopGroup.transform, new Vector3(8f, 16.8f, 4f), new Vector3(0.2f, 2.8f, 0.2f), "Mat_DarkMetal", false);
    }

    private InspectableApartment BuildApartment1_LivingKitchen(GameObject parent)
    {
        GameObject aptObj = new GameObject("Apartment_101_LivingKitchen");
        aptObj.transform.SetParent(parent.transform);
        aptObj.transform.position = new Vector3(-6.5f, 4.0f, -2.5f);

        InspectableApartment apt = aptObj.AddComponent<InspectableApartment>();
        apt.apartmentID = "Unit 101";
        apt.apartmentName = "Scandinavian Living & Kitchen Suite";
        apt.floorNumber = "1st Floor (West)";
        apt.apartmentType = "2-Bedroom Luxury Suite";
        apt.areaSqFt = "1,150 sq ft";
        apt.priceOrRent = "$2,400 / month";
        apt.features = "• Open-concept gourmet kitchen\n• Solid white oak hardwood flooring\n• Private cantilevered balcony with city view\n• Designer furniture & ambient LED fixtures";
        apt.defaultOrbitDistance = 7.5f;
        apt.minOrbitDistance = 3.0f;
        apt.maxOrbitDistance = 14f;

        Vector3 origin = aptObj.transform.position;

        CreateCube("InteriorFloor", aptObj.transform, origin + new Vector3(0f, 0.05f, 0f), new Vector3(11f, 0.1f, 9f), "Mat_WoodFloor", true);
        CreateCube("LivingWallEast", aptObj.transform, origin + new Vector3(5.4f, 1.8f, 0f), new Vector3(0.2f, 3.4f, 9f), "Mat_BuildingConcrete", false);
        CreateCube("LivingWallNorth", aptObj.transform, origin + new Vector3(0f, 1.8f, 4.4f), new Vector3(11f, 3.4f, 0.2f), "Mat_BuildingConcrete", false);
        CreateCube("LivingWallWest", aptObj.transform, origin + new Vector3(-5.4f, 1.8f, 0f), new Vector3(0.2f, 3.4f, 9f), "Mat_BuildingConcrete", false);

        GameObject featureWall = CreateCube("TVFeatureWall", aptObj.transform, origin + new Vector3(0f, 1.8f, 4.2f), new Vector3(5.5f, 3.2f, 0.15f), "Mat_WoodSlat", false);

        CreateCube("TVDisplay", aptObj.transform, origin + new Vector3(0f, 2.0f, 4.05f), new Vector3(2.8f, 1.6f, 0.08f), "Mat_DarkMetal", false);
        CreateCube("TVMediaConsole", aptObj.transform, origin + new Vector3(0f, 0.4f, 3.8f), new Vector3(3.6f, 0.6f, 0.6f), "Mat_LightWoodTable", false);

        GameObject sofa = new GameObject("ModernSofa");
        sofa.transform.SetParent(aptObj.transform);
        CreateCube("SofaBase", sofa.transform, origin + new Vector3(0f, 0.4f, 1.2f), new Vector3(3.2f, 0.45f, 1.1f), "Mat_FabricSofa", false);
        CreateCube("SofaBack", sofa.transform, origin + new Vector3(0f, 0.85f, 0.75f), new Vector3(3.2f, 0.65f, 0.3f), "Mat_FabricSofa", false);
        CreateCube("SofaArmL", sofa.transform, origin + new Vector3(-1.5f, 0.65f, 1.2f), new Vector3(0.3f, 0.45f, 1.1f), "Mat_FabricSofa", false);
        CreateCube("SofaArmR", sofa.transform, origin + new Vector3(1.5f, 0.65f, 1.2f), new Vector3(0.3f, 0.45f, 1.1f), "Mat_FabricSofa", false);
        CreateCube("Pillow1", sofa.transform, origin + new Vector3(-1.1f, 0.65f, 0.95f), new Vector3(0.5f, 0.45f, 0.2f), "Mat_FabricTeal", false);
        CreateCube("Pillow2", sofa.transform, origin + new Vector3(1.1f, 0.65f, 0.95f), new Vector3(0.5f, 0.45f, 0.2f), "Mat_FabricCream", false);

        CreateCube("CoffeeTable", aptObj.transform, origin + new Vector3(0f, 0.35f, 2.5f), new Vector3(1.8f, 0.35f, 0.9f), "Mat_LightWoodTable", false);
        CreateCube("CoffeeCup", aptObj.transform, origin + new Vector3(0.3f, 0.58f, 2.5f), new Vector3(0.12f, 0.12f, 0.12f), "Mat_MarbleFloor", false);

        GameObject kitchen = new GameObject("OpenKitchen");
        kitchen.transform.SetParent(aptObj.transform);
        CreateCube("IslandCounter", kitchen.transform, origin + new Vector3(3.2f, 0.6f, -1.0f), new Vector3(1.4f, 1.1f, 3.6f), "Mat_KitchenCabinet", false);
        CreateCube("IslandTop", kitchen.transform, origin + new Vector3(3.2f, 1.18f, -1.0f), new Vector3(1.7f, 0.08f, 3.8f), "Mat_KitchenCounter", false);
        for (int z = -2; z <= 0; z++)
        {
            CreateCylinder("BarstoolSeat", kitchen.transform, origin + new Vector3(2.1f, 0.78f, z * 1.1f), new Vector3(0.45f, 0.06f, 0.45f), "Mat_DarkMetal", false);
            CreateCylinder("BarstoolLeg", kitchen.transform, origin + new Vector3(2.1f, 0.38f, z * 1.1f), new Vector3(0.08f, 0.75f, 0.08f), "Mat_DarkMetal", false);
        }
        for (int z = -2; z <= 0; z++)
        {
            CreateCylinder("PendantCord", kitchen.transform, origin + new Vector3(3.2f, 2.7f, z * 1.1f), new Vector3(0.02f, 0.8f, 0.02f), "Mat_DarkMetal", false);
            CreateCylinder("PendantLamp", kitchen.transform, origin + new Vector3(3.2f, 2.2f, z * 1.1f), new Vector3(0.25f, 0.2f, 0.25f), "Mat_EmissiveWarmLight", false);
        }

        GameObject balcony = new GameObject("Balcony");
        balcony.transform.SetParent(aptObj.transform);
        CreateCube("BalconySlab", balcony.transform, origin + new Vector3(0f, 0.0f, -5.2f), new Vector3(8f, 0.3f, 2.2f), "Mat_BuildingConcreteDark", true);
        CreateCube("BalconyGlassFront", balcony.transform, origin + new Vector3(0f, 0.65f, -6.25f), new Vector3(8f, 1.1f, 0.1f), "Mat_BalconyGlass", false);
        CreateCube("BalconyGlassWest", balcony.transform, origin + new Vector3(-3.95f, 0.65f, -5.2f), new Vector3(0.1f, 1.1f, 2.1f), "Mat_BalconyGlass", false);
        CreateCube("BalconyGlassEast", balcony.transform, origin + new Vector3(3.95f, 0.65f, -5.2f), new Vector3(0.1f, 1.1f, 2.1f), "Mat_BalconyGlass", false);
        CreateCube("BalconyRailingTop", balcony.transform, origin + new Vector3(0f, 1.22f, -6.25f), new Vector3(8.1f, 0.08f, 0.12f), "Mat_DarkMetal", false);

        CreateCube("SlidingGlassDoor", aptObj.transform, origin + new Vector3(0f, 1.8f, -4.4f), new Vector3(7.8f, 3.4f, 0.15f), "Mat_Glass", false);

        GameObject camAnchor = new GameObject("InspectionCameraAnchor");
        camAnchor.transform.SetParent(aptObj.transform);
        camAnchor.transform.position = origin + new Vector3(0f, 3.0f, -4.8f);

        GameObject targetAnchor = new GameObject("InspectionTargetAnchor");
        targetAnchor.transform.SetParent(aptObj.transform);
        targetAnchor.transform.position = origin + new Vector3(0f, 1.2f, 1.5f);

        GameObject spawnAnchor = new GameObject("PlayerSpawnAnchor");
        spawnAnchor.transform.SetParent(aptObj.transform);
        spawnAnchor.transform.position = origin + new Vector3(0f, 0.5f, -4.8f);

        apt.inspectionCameraAnchor = camAnchor.transform;
        apt.inspectionTargetAnchor = targetAnchor.transform;
        apt.playerSpawnPoint = spawnAnchor.transform;

        // Inspection Hotspot Trigger Box
        GameObject hotspot = CreateCube("InspectionHotspotTrigger", aptObj.transform, origin + new Vector3(0f, 1.5f, -4.2f), new Vector3(4f, 3f, 2f), "Mat_Glass", false);
        BoxCollider boxCol = hotspot.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;
        apt.highlightRenderers.Add(featureWall.GetComponent<Renderer>());

        GameObject lightsGroup = new GameObject("InteriorLights");
        lightsGroup.transform.SetParent(aptObj.transform);
        GameObject spot1 = new GameObject("LivingSpotlight");
        spot1.transform.SetParent(lightsGroup.transform);
        spot1.transform.position = origin + new Vector3(0f, 3.4f, 1.5f);
        Light l1 = spot1.AddComponent<Light>();
        l1.type = LightType.Point;
        l1.color = new Color(1f, 0.92f, 0.78f);
        l1.range = 9f;
        l1.intensity = 1.8f;
        l1.shadows = LightShadows.None; // Optimized: No point light shadows!
        apt.interiorLightingGroup = lightsGroup;

        return apt;
    }

    private InspectableApartment BuildApartment2_MasterSuite(GameObject parent)
    {
        GameObject aptObj = new GameObject("Apartment_201_MasterSuite");
        aptObj.transform.SetParent(parent.transform);
        aptObj.transform.position = new Vector3(6.5f, 7.8f, -2.5f);

        InspectableApartment apt = aptObj.AddComponent<InspectableApartment>();
        apt.apartmentID = "Unit 201";
        apt.apartmentName = "Modern Minimalist Master Suite";
        apt.floorNumber = "2nd Floor (East)";
        apt.apartmentType = "1-Bedroom Executive Suite";
        apt.areaSqFt = "920 sq ft";
        apt.priceOrRent = "$1,950 / month";
        apt.features = "• Custom king-size platform bed with upholstered headboard\n• Dedicated home office workstation & laptop desk\n• Floor-to-ceiling panoramic glass windows\n• Herringbone oak floors & bedside ambient lighting";
        apt.defaultOrbitDistance = 6.8f;
        apt.minOrbitDistance = 2.5f;
        apt.maxOrbitDistance = 13f;

        Vector3 origin = aptObj.transform.position;

        CreateCube("InteriorFloor", aptObj.transform, origin + new Vector3(0f, 0.05f, 0f), new Vector3(11f, 0.1f, 9f), "Mat_WoodFloor", true);
        CreateCube("SuiteWallWest", aptObj.transform, origin + new Vector3(-5.4f, 1.8f, 0f), new Vector3(0.2f, 3.4f, 9f), "Mat_BuildingConcrete", false);
        CreateCube("SuiteWallNorth", aptObj.transform, origin + new Vector3(0f, 1.8f, 4.4f), new Vector3(11f, 3.4f, 0.2f), "Mat_BuildingConcrete", false);
        CreateCube("SuiteWallEast", aptObj.transform, origin + new Vector3(5.4f, 1.8f, 0f), new Vector3(0.2f, 3.4f, 9f), "Mat_BuildingConcrete", false);

        GameObject bedHeadboardWall = CreateCube("BedAccentWall", aptObj.transform, origin + new Vector3(-1.5f, 1.8f, 4.2f), new Vector3(4.8f, 3.2f, 0.15f), "Mat_FabricTeal", false);

        GameObject bed = new GameObject("KingPlatformBed");
        bed.transform.SetParent(aptObj.transform);
        CreateCube("BedFrame", bed.transform, origin + new Vector3(-1.5f, 0.25f, 2.4f), new Vector3(2.6f, 0.4f, 2.6f), "Mat_DarkMetal", false);
        CreateCube("Mattress", bed.transform, origin + new Vector3(-1.5f, 0.6f, 2.4f), new Vector3(2.4f, 0.45f, 2.4f), "Mat_BedSheet", false);
        CreateCube("DuvetCover", bed.transform, origin + new Vector3(-1.5f, 0.7f, 2.0f), new Vector3(2.45f, 0.3f, 1.6f), "Mat_FabricCream", false);
        CreateCube("PillowL", bed.transform, origin + new Vector3(-2.1f, 0.9f, 3.3f), new Vector3(0.7f, 0.25f, 0.5f), "Mat_BedSheet", false);
        CreateCube("PillowR", bed.transform, origin + new Vector3(-0.9f, 0.9f, 3.3f), new Vector3(0.7f, 0.25f, 0.5f), "Mat_BedSheet", false);

        CreateCube("NightstandL", aptObj.transform, origin + new Vector3(-3.2f, 0.35f, 3.3f), new Vector3(0.6f, 0.6f, 0.6f), "Mat_LightWoodTable", false);
        CreateCylinder("LampL", aptObj.transform, origin + new Vector3(-3.2f, 0.85f, 3.3f), new Vector3(0.2f, 0.25f, 0.2f), "Mat_EmissiveWarmLight", false);

        CreateCube("NightstandR", aptObj.transform, origin + new Vector3(0.2f, 0.35f, 3.3f), new Vector3(0.6f, 0.6f, 0.6f), "Mat_LightWoodTable", false);
        CreateCylinder("LampR", aptObj.transform, origin + new Vector3(0.2f, 0.85f, 3.3f), new Vector3(0.2f, 0.25f, 0.2f), "Mat_EmissiveWarmLight", false);

        GameObject workstation = new GameObject("Workstation");
        workstation.transform.SetParent(aptObj.transform);
        CreateCube("StudyDesk", workstation.transform, origin + new Vector3(3.6f, 0.75f, 2.2f), new Vector3(2.4f, 0.08f, 1.1f), "Mat_LightWoodTable", false);
        CreateCube("DeskLegL", workstation.transform, origin + new Vector3(2.6f, 0.37f, 2.2f), new Vector3(0.08f, 0.74f, 0.9f), "Mat_DarkMetal", false);
        CreateCube("DeskLegR", workstation.transform, origin + new Vector3(4.6f, 0.37f, 2.2f), new Vector3(0.08f, 0.74f, 0.9f), "Mat_DarkMetal", false);
        CreateCube("LaptopBase", workstation.transform, origin + new Vector3(3.6f, 0.82f, 2.2f), new Vector3(0.4f, 0.02f, 0.3f), "Mat_DarkMetal", false);
        CreateCube("LaptopScreen", workstation.transform, origin + new Vector3(3.6f, 0.98f, 2.36f), new Vector3(0.4f, 0.28f, 0.02f), "Mat_Glass", false);
        CreateCube("OfficeChair", workstation.transform, origin + new Vector3(3.6f, 0.5f, 1.3f), new Vector3(0.55f, 0.7f, 0.55f), "Mat_FabricSofa", false);

        CreateCube("Wardrobe", aptObj.transform, origin + new Vector3(3.5f, 1.6f, -1.8f), new Vector3(2.6f, 3.0f, 1.0f), "Mat_KitchenCabinet", false);

        CreateCube("PanoramicWindow", aptObj.transform, origin + new Vector3(0f, 1.8f, -4.4f), new Vector3(8.0f, 3.4f, 0.15f), "Mat_Glass", false);
        CreateCube("BalconyGlassFront2", aptObj.transform, origin + new Vector3(0f, 0.65f, -6.25f), new Vector3(8f, 1.1f, 0.1f), "Mat_BalconyGlass", false);
        CreateCube("BalconyRailingTop2", aptObj.transform, origin + new Vector3(0f, 1.22f, -6.25f), new Vector3(8.1f, 0.08f, 0.12f), "Mat_DarkMetal", false);

        GameObject camAnchor = new GameObject("InspectionCameraAnchor");
        camAnchor.transform.SetParent(aptObj.transform);
        camAnchor.transform.position = origin + new Vector3(0f, 2.8f, -4.6f);

        GameObject targetAnchor = new GameObject("InspectionTargetAnchor");
        targetAnchor.transform.SetParent(aptObj.transform);
        targetAnchor.transform.position = origin + new Vector3(-0.5f, 1.0f, 1.8f);

        GameObject spawnAnchor = new GameObject("PlayerSpawnAnchor");
        spawnAnchor.transform.SetParent(aptObj.transform);
        spawnAnchor.transform.position = origin + new Vector3(0f, 0.5f, -4.6f);

        apt.inspectionCameraAnchor = camAnchor.transform;
        apt.inspectionTargetAnchor = targetAnchor.transform;
        apt.playerSpawnPoint = spawnAnchor.transform;

        GameObject hotspot = CreateCube("InspectionHotspotTrigger", aptObj.transform, origin + new Vector3(0f, 1.5f, -4.2f), new Vector3(4f, 3f, 2f), "Mat_Glass", false);
        BoxCollider boxCol = hotspot.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;
        apt.highlightRenderers.Add(bedHeadboardWall.GetComponent<Renderer>());

        GameObject lightsGroup = new GameObject("InteriorLights");
        lightsGroup.transform.SetParent(aptObj.transform);
        GameObject spot1 = new GameObject("MasterSpotlight");
        spot1.transform.SetParent(lightsGroup.transform);
        spot1.transform.position = origin + new Vector3(-0.5f, 3.4f, 1.8f);
        Light l1 = spot1.AddComponent<Light>();
        l1.type = LightType.Point;
        l1.color = new Color(1f, 0.92f, 0.82f);
        l1.range = 8.5f;
        l1.intensity = 1.8f;
        l1.shadows = LightShadows.None; // Optimized: No point light shadows!
        apt.interiorLightingGroup = lightsGroup;

        return apt;
    }

    private InspectableApartment BuildApartment3_PenthouseLoft(GameObject parent)
    {
        GameObject aptObj = new GameObject("Apartment_301_PenthouseLoft");
        aptObj.transform.SetParent(parent.transform);
        aptObj.transform.position = new Vector3(0f, 11.6f, 0f);

        InspectableApartment apt = aptObj.AddComponent<InspectableApartment>();
        apt.apartmentID = "Unit 301";
        apt.apartmentName = "Luxury Sky Penthouse Loft";
        apt.floorNumber = "3rd Floor / Penthouse";
        apt.apartmentType = "3-Bedroom Panoramic Loft";
        apt.areaSqFt = "2,450 sq ft";
        apt.priceOrRent = "$4,800 / month";
        apt.features = "• Private wrap-around sky-terrace with pergola\n• Architectural modern fireplace with ambient glow\n• Luxury polished Carrara marble flooring\n• Double-height ceilings & premium bar lounge";
        apt.defaultOrbitDistance = 8.5f;
        apt.minOrbitDistance = 3.5f;
        apt.maxOrbitDistance = 16f;

        Vector3 origin = aptObj.transform.position;

        CreateCube("InteriorFloor", aptObj.transform, origin + new Vector3(0f, 0.05f, 0f), new Vector3(18f, 0.1f, 12f), "Mat_MarbleFloor", true);
        CreateCube("LoftWallBack", aptObj.transform, origin + new Vector3(0f, 2.0f, 5.8f), new Vector3(18f, 3.8f, 0.3f), "Mat_BuildingConcrete", false);
        CreateCube("LoftWallWest", aptObj.transform, origin + new Vector3(-8.8f, 2.0f, 0f), new Vector3(0.3f, 3.8f, 12f), "Mat_BuildingConcrete", false);

        GameObject fireplace = new GameObject("ModernFireplace");
        fireplace.transform.SetParent(aptObj.transform);
        CreateCube("FireplaceColumn", fireplace.transform, origin + new Vector3(0f, 2.0f, 5.5f), new Vector3(4.0f, 3.8f, 0.5f), "Mat_BuildingConcreteDark", false);
        CreateCube("FireplaceHearth", fireplace.transform, origin + new Vector3(0f, 0.5f, 5.3f), new Vector3(2.6f, 0.6f, 0.4f), "Mat_DarkMetal", false);
        CreateCube("FireGlowEmbers", fireplace.transform, origin + new Vector3(0f, 0.5f, 5.25f), new Vector3(2.2f, 0.25f, 0.1f), "Mat_FireplaceGlow", false);

        GameObject sectional = new GameObject("LSectionalSofa");
        sectional.transform.SetParent(aptObj.transform);
        CreateCube("SofaMain", sectional.transform, origin + new Vector3(0f, 0.4f, 2.5f), new Vector3(4.0f, 0.5f, 1.1f), "Mat_FabricCream", false);
        CreateCube("SofaBack", sectional.transform, origin + new Vector3(0f, 0.9f, 2.0f), new Vector3(4.0f, 0.65f, 0.3f), "Mat_FabricCream", false);
        CreateCube("SofaLounge", sectional.transform, origin + new Vector3(-1.45f, 0.4f, 3.6f), new Vector3(1.1f, 0.5f, 1.8f), "Mat_FabricCream", false);
        CreateCube("PillowA", sectional.transform, origin + new Vector3(1.4f, 0.7f, 2.3f), new Vector3(0.5f, 0.45f, 0.2f), "Mat_FabricTeal", false);
        CreateCube("PillowB", sectional.transform, origin + new Vector3(0.2f, 0.7f, 2.3f), new Vector3(0.5f, 0.45f, 0.2f), "Mat_FabricTeal", false);

        CreateCube("GlassCoffeeTable", aptObj.transform, origin + new Vector3(0.3f, 0.3f, 3.8f), new Vector3(2.0f, 0.35f, 1.0f), "Mat_Glass", false);
        CreateCube("TableMetalBase", aptObj.transform, origin + new Vector3(0.3f, 0.15f, 3.8f), new Vector3(1.8f, 0.25f, 0.8f), "Mat_DarkMetal", false);

        GameObject bar = new GameObject("BarLounge");
        bar.transform.SetParent(aptObj.transform);
        CreateCube("BarCabinet", bar.transform, origin + new Vector3(5.5f, 0.6f, 3.0f), new Vector3(1.2f, 1.2f, 3.6f), "Mat_KitchenCabinet", false);
        CreateCube("BarTop", bar.transform, origin + new Vector3(5.5f, 1.25f, 3.0f), new Vector3(1.5f, 0.08f, 3.8f), "Mat_MarbleFloor", false);

        CreateCube("GlassFront", aptObj.transform, origin + new Vector3(0f, 2.0f, -3.0f), new Vector3(17.6f, 3.8f, 0.15f), "Mat_Glass", false);
        CreateCube("GlassEast", aptObj.transform, origin + new Vector3(8.8f, 2.0f, 0f), new Vector3(0.15f, 3.8f, 12f), "Mat_Glass", false);

        GameObject terrace = new GameObject("SkyTerrace");
        terrace.transform.SetParent(aptObj.transform);
        CreateCube("TerraceSlab", terrace.transform, origin + new Vector3(0f, 0.0f, -5.5f), new Vector3(18f, 0.3f, 5.0f), "Mat_WoodFloor", true);
        CreateCube("TerraceRailingFront", terrace.transform, origin + new Vector3(0f, 0.65f, -7.9f), new Vector3(18f, 1.1f, 0.1f), "Mat_BalconyGlass", false);
        CreateCube("TerraceRailingTop", terrace.transform, origin + new Vector3(0f, 1.22f, -7.9f), new Vector3(18.1f, 0.08f, 0.12f), "Mat_DarkMetal", false);

        CreateCube("SunLounger1", terrace.transform, origin + new Vector3(-4f, 0.35f, -5.5f), new Vector3(1.0f, 0.35f, 2.2f), "Mat_FabricTeal", false);
        CreateCube("SunLounger2", terrace.transform, origin + new Vector3(-2f, 0.35f, -5.5f), new Vector3(1.0f, 0.35f, 2.2f), "Mat_FabricTeal", false);

        GameObject camAnchor = new GameObject("InspectionCameraAnchor");
        camAnchor.transform.SetParent(aptObj.transform);
        camAnchor.transform.position = origin + new Vector3(0f, 3.5f, -5.5f);

        GameObject targetAnchor = new GameObject("InspectionTargetAnchor");
        targetAnchor.transform.SetParent(aptObj.transform);
        targetAnchor.transform.position = origin + new Vector3(0f, 1.2f, 2.0f);

        GameObject spawnAnchor = new GameObject("PlayerSpawnAnchor");
        spawnAnchor.transform.SetParent(aptObj.transform);
        spawnAnchor.transform.position = origin + new Vector3(0f, 0.5f, -5.5f);

        apt.inspectionCameraAnchor = camAnchor.transform;
        apt.inspectionTargetAnchor = targetAnchor.transform;
        apt.playerSpawnPoint = spawnAnchor.transform;

        GameObject hotspot = CreateCube("InspectionHotspotTrigger", aptObj.transform, origin + new Vector3(0f, 1.5f, -4.5f), new Vector3(5f, 3f, 3f), "Mat_Glass", false);
        BoxCollider boxCol = hotspot.AddComponent<BoxCollider>();
        boxCol.isTrigger = true;
        apt.highlightRenderers.Add(fireplace.GetComponentInChildren<Renderer>());

        GameObject lightsGroup = new GameObject("InteriorLights");
        lightsGroup.transform.SetParent(aptObj.transform);
        GameObject spot1 = new GameObject("PenthouseSpotlight");
        spot1.transform.SetParent(lightsGroup.transform);
        spot1.transform.position = origin + new Vector3(0f, 3.6f, 2.0f);
        Light l1 = spot1.AddComponent<Light>();
        l1.type = LightType.Point;
        l1.color = new Color(1f, 0.94f, 0.85f);
        l1.range = 14f;
        l1.intensity = 2.2f;
        l1.shadows = LightShadows.None; // Optimized: No point light shadows!
        apt.interiorLightingGroup = lightsGroup;

        return apt;
    }

    private PlayerController BuildPlayer(GameObject parent)
    {
        GameObject playerObj = new GameObject("PlayerCharacter");
        playerObj.transform.SetParent(parent.transform);
        // Fixed initial spawn position framing the apartment complex immediately!
        playerObj.transform.position = new Vector3(0f, 1.6f, -30f);
        playerObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        playerObj.tag = "Player";

        CharacterController cc = playerObj.AddComponent<CharacterController>();
        cc.height = 1.9f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.95f, 0f);

        PlayerController pc = playerObj.AddComponent<PlayerController>();
        pc.walkSpeed = 5.5f;
        pc.sprintSpeed = 9.5f;
        pc.jumpHeight = 1.5f;

        GameObject gcObj = new GameObject("GroundCheck");
        gcObj.transform.SetParent(playerObj.transform);
        gcObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        pc.groundCheck = gcObj.transform;

        GameObject camHolder = new GameObject("CameraHolder");
        camHolder.transform.SetParent(playerObj.transform);
        camHolder.transform.localPosition = new Vector3(0f, 1.7f, 0f);
        pc.cameraHolder = camHolder.transform;

        GameObject camObj = new GameObject("Main Camera");
        camObj.transform.SetParent(camHolder.transform);
        camObj.transform.localPosition = Vector3.zero;
        camObj.transform.localRotation = Quaternion.identity;
        camObj.tag = "MainCamera";

        Camera cam = camObj.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 350f;
        cam.fieldOfView = 65f;

        camObj.AddComponent<AudioListener>();

        CameraController ccCam = camObj.AddComponent<CameraController>();
        ccCam.playerBody = playerObj.transform;
        ccCam.playerCameraHolder = camHolder.transform;

        InteractionRaycaster raycaster = camObj.AddComponent<InteractionRaycaster>();
        raycaster.playerCamera = cam;

        return pc;
    }

    private UIManager BuildUI(GameObject parent, GameManager gm, DayNightCycle dnc)
    {
        GameObject canvasObj = new GameObject("UICanvas");
        canvasObj.transform.SetParent(parent.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject es = GameObject.Find("EventSystem");
        if (es == null)
        {
            es = new GameObject("EventSystem");
            es.transform.SetParent(parent.transform);
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        UIManager uim = canvasObj.AddComponent<UIManager>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject CreateUIPanel(string name, Transform p, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(p, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Image img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        Text CreateUIText(string name, Transform p, string text, int fontSize, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(p, false);
            RectTransform rt = txtObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Text t = txtObj.AddComponent<Text>();
            t.font = defaultFont;
            t.fontSize = fontSize;
            t.text = text;
            t.color = color;
            t.alignment = alignment;
            t.raycastTarget = false;
            return t;
        }

        Button CreateUIButton(string name, Transform p, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta, Color bgColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(p, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = bgColor * 1.25f;
            cb.pressedColor = bgColor * 0.8f;
            btn.colors = cb;

            CreateUIText("Label", btnObj.transform, label, 15, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return btn;
        }

        // ==================== MAIN MENU PANEL ====================
        GameObject mainMenuPanel = CreateUIPanel("MainMenuPanel", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.07f, 0.1f, 0.95f));
        uim.mainMenuPanel = mainMenuPanel;

        CreateUIText("MenuLogo", mainMenuPanel.transform, "🏢 THE METROPOLITAN", 38, new Color(0.3f, 0.8f, 1f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(800f, 60f));
        CreateUIText("MenuSub", mainMenuPanel.transform, "3D INTERACTIVE ARCHITECTURAL SHOWCASE", 18, new Color(0.85f, 0.88f, 0.95f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.73f), new Vector2(0.5f, 0.73f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(800f, 30f));

        Button startTourBtn = CreateUIButton("StartTourBtn", mainMenuPanel.transform, "✨ START EXPLORATION TOUR", new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 56f), new Color(0.2f, 0.55f, 0.85f, 0.95f));
        startTourBtn.onClick.AddListener(() => { gm.StartTour(); });

        Button menuOverviewBtn = CreateUIButton("MenuOverviewBtn", mainMenuPanel.transform, "🚁 CINEMATIC OVERVIEW", new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 52f), new Color(0.18f, 0.45f, 0.35f, 0.95f));
        menuOverviewBtn.onClick.AddListener(() => { gm.EnterOverviewMode(); });

        CreateUIText("DirectoryHeader", mainMenuPanel.transform, "DIRECT ROOM INSPECTION", 14, new Color(0.65f, 0.7f, 0.8f), TextAnchor.MiddleCenter, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 25f));

        Button mUnit1Btn = CreateUIButton("MUnit1Btn", mainMenuPanel.transform, "Unit 101 (Floor 1 Living & Kitchen)", new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.31f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 44f), new Color(0.15f, 0.2f, 0.28f, 0.9f));
        mUnit1Btn.onClick.AddListener(() => { gm.TeleportToApartment(0); });

        Button mUnit2Btn = CreateUIButton("MUnit2Btn", mainMenuPanel.transform, "Unit 201 (Floor 2 Master Suite)", new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 44f), new Color(0.15f, 0.2f, 0.28f, 0.9f));
        mUnit2Btn.onClick.AddListener(() => { gm.TeleportToApartment(1); });

        Button mUnit3Btn = CreateUIButton("MUnit3Btn", mainMenuPanel.transform, "Unit 301 (Floor 3 Sky Penthouse)", new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.17f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 44f), new Color(0.15f, 0.2f, 0.28f, 0.9f));
        mUnit3Btn.onClick.AddListener(() => { gm.TeleportToApartment(2); });

        mainMenuPanel.SetActive(false);

        // ==================== HUD PANEL ====================
        GameObject hudPanel = CreateUIPanel("HUDPanel", canvasObj.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.clear);
        uim.hudPanel = hudPanel;

        GameObject topBar = CreateUIPanel("TopBar", hudPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(-40f, 52f), new Color(0.06f, 0.08f, 0.12f, 0.85f));
        CreateUIText("LogoTitle", topBar.transform, "🏢 THE METROPOLITAN • LUXURY RESIDENCES", 18, new Color(0.95f, 0.95f, 0.98f), TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(450f, 40f));

        Button dnBtn = CreateUIButton("DayNightButton", topBar.transform, "☀️ Day / Night [T]", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-210f, 0f), new Vector2(170f, 38f), new Color(0.2f, 0.45f, 0.65f, 0.9f));
        uim.timeOfDayToggleButton = dnBtn;
        uim.timeOfDayLabelText = dnBtn.GetComponentInChildren<Text>();

        Button ovBtn = CreateUIButton("OverviewButton", topBar.transform, "🚁 Overview [O]", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(170f, 38f), new Color(0.25f, 0.6f, 0.45f, 0.9f));
        uim.overviewToggleButton = ovBtn;

        Button f1Btn = CreateUIButton("Floor1Btn", topBar.transform, "Unit 101 [1]", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-580f, 0f), new Vector2(110f, 34f), new Color(0.18f, 0.22f, 0.28f, 0.9f));
        f1Btn.onClick.AddListener(() => { gm.TeleportToApartment(0); });

        Button f2Btn = CreateUIButton("Floor2Btn", topBar.transform, "Unit 201 [2]", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-460f, 0f), new Vector2(110f, 34f), new Color(0.18f, 0.22f, 0.28f, 0.9f));
        f2Btn.onClick.AddListener(() => { gm.TeleportToApartment(1); });

        Button f3Btn = CreateUIButton("Floor3Btn", topBar.transform, "Unit 301 [3]", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-340f, 0f), new Vector2(110f, 34f), new Color(0.18f, 0.22f, 0.28f, 0.9f));
        f3Btn.onClick.AddListener(() => { gm.TeleportToApartment(2); });

        GameObject crosshair = CreateUIPanel("CrosshairDot", hudPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f), new Color(1f, 1f, 1f, 0.8f));
        uim.crosshair = crosshair;

        GameObject promptBadge = CreateUIPanel("PromptBadge", hudPanel.transform, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 50f), new Color(0.06f, 0.08f, 0.12f, 0.92f));
        Text promptText = CreateUIText("PromptText", promptBadge.transform, "[E] Inspect Apartment", 17, new Color(1f, 0.9f, 0.4f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        uim.interactionPromptBadge = promptBadge;
        uim.interactionPromptText = promptText;
        promptBadge.SetActive(false);

        GameObject legend = CreateUIPanel("ControlsLegend", hudPanel.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(25f, 25f), new Vector2(330f, 185f), new Color(0.06f, 0.08f, 0.12f, 0.88f));
        uim.controlsLegendPanel = legend;
        CreateUIText("LegendHeader", legend.transform, "🎮 CONTROLS & SHORTCUTS", 14, new Color(0.3f, 0.8f, 1f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(15f, -12f), new Vector2(-30f, 25f));
        string legendStr = "• <b>W A S D</b> : Walk / Explore\n• <b>Left Shift</b> : Sprint\n• <b>Space</b> : Jump\n• <b>E</b> : Inspect Room Hotspot\n• <b>1 / 2 / 3</b> : Direct Jump to Apartment\n• <b>T</b> : Toggle Day / Sunset / Night\n• <b>O</b> : Cinematic Overview\n• <b>Esc</b> : Unlock Mouse Cursor";
        CreateUIText("LegendBody", legend.transform, legendStr, 13, new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(15f, -38f), new Vector2(-30f, -48f));

        // ==================== APARTMENT INSPECT CARD PANEL ====================
        GameObject cardPanel = CreateUIPanel("InspectCardPanel", canvasObj.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(460f, 550f), new Color(0.06f, 0.08f, 0.12f, 0.92f));
        uim.inspectCardPanel = cardPanel;

        uim.cardApartmentIDText = CreateUIText("CardID", cardPanel.transform, "UNIT 101", 14, new Color(0.3f, 0.8f, 1f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(25f, -20f), new Vector2(-50f, 25f));
        uim.cardTitleText = CreateUIText("CardTitle", cardPanel.transform, "Scandinavian Living & Kitchen Suite", 21, Color.white, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(25f, -45f), new Vector2(-50f, 40f));
        uim.cardFloorTypeText = CreateUIText("CardFloorType", cardPanel.transform, "1st Floor • 2-Bedroom Luxury Suite", 14, new Color(1f, 0.85f, 0.4f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(25f, -90f), new Vector2(-50f, 25f));
        uim.cardPriceAreaText = CreateUIText("CardPriceArea", cardPanel.transform, "1,150 sq ft | $2,400 / month", 16, new Color(0.4f, 0.95f, 0.6f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(25f, -120f), new Vector2(-50f, 30f));

        CreateUIPanel("Divider", cardPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(25f, -155f), new Vector2(-50f, 2f), new Color(1f, 1f, 1f, 0.15f));
        CreateUIText("FeaturesHeader", cardPanel.transform, "KEY HIGHLIGHTS & AMENITIES", 13, new Color(0.7f, 0.75f, 0.82f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(25f, -170f), new Vector2(-50f, 25f));
        uim.cardFeaturesText = CreateUIText("CardFeatures", cardPanel.transform, "• Open kitchen\n• Balcony", 14, new Color(0.9f, 0.92f, 0.95f), TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(25f, -200f), new Vector2(-50f, 180f));

        CreateUIText("InspectTip", cardPanel.transform, "💡 <i>Left/Right Click Drag to Orbit • Scroll to Zoom</i>", 13, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 85f), new Vector2(-50f, 30f));

        Button returnBtn = CreateUIButton("ReturnButton", cardPanel.transform, "⬅️ Return to Exploration [Esc]", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(-50f, 48f), new Color(0.2f, 0.5f, 0.85f, 0.9f));
        uim.returnInspectButton = returnBtn;

        cardPanel.SetActive(false);

        // ==================== OVERVIEW UI PANEL ====================
        GameObject overviewPanel = CreateUIPanel("OverviewPanel", canvasObj.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(580f, 85f), new Color(0.06f, 0.08f, 0.12f, 0.88f));
        uim.overviewPanel = overviewPanel;
        CreateUIText("OverviewHeader", overviewPanel.transform, "🚁 CINEMATIC AERIAL DRONE VIEW", 21, new Color(0.3f, 0.85f, 1f), TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(-30f, 35f));

        Button returnOvBtn = CreateUIButton("ReturnOverviewBtn", overviewPanel.transform, "Exit Overview [Esc / O]", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -38f), new Vector2(230f, 36f), new Color(0.85f, 0.35f, 0.35f, 0.9f));
        uim.returnOverviewButton = returnOvBtn;
        overviewPanel.SetActive(false);

        return uim;
    }
}
