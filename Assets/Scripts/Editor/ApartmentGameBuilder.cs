#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using ApartmentAfterDark;
using ApartmentAfterDark.Audio;
using ApartmentAfterDark.Camera;
using ApartmentAfterDark.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ApartmentAfterDark.EditorTools
{
    /// <summary>
    /// One-click builder for a complete, organized apartment assembled from the real
    /// interior FBX models in Assets/Environment/Apartment/Models/Interior.
    /// Output scene: Assets/Scenes/ApartmentGame.unity
    ///
    /// Access: Tools > Game > Build Apartment Game
    ///
    /// The structural shell (Floor / Walls / Ceiling) is generated with primitives
    /// because no wall/floor/ceiling models exist in the pack. Every piece of
    /// furniture, door, window and light is a real imported FBX model from the pack.
    /// All objects are measured at build time, rescaled to a coherent size, snapped
    /// onto the floor (no floating/overlap), and given a fitted collider so the
    /// player can walk every room but cannot pass walls, closed doors, or fall
    /// through the floor.
    /// </summary>
    public static class ApartmentGameBuilder
    {
        private const string InteriorDir = "Assets/Environment/Apartment/Models/Interior";
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = SceneDir + "/ApartmentGame.unity";
        private const string MatDir = "Assets/Environment/Apartment/Materials";

        // ---------- Layout constants (meters) ----------
        private const float WallThickness = 0.32f;
        private const float WallHeight = 2.8f;
        private const float FloorY = 0f;          // top surface of the floor
        private const float CeilingY = WallHeight; // bottom of the ceiling
        private const float ExtX = 16.6f;          // outer footprint X
        private const float ExtZ = 16.6f;          // outer footprint Z

        private static readonly Dictionary<string, GameObject> allModels
            = new Dictionary<string, GameObject>();

        [MenuItem("Tools/Game/Build Apartment Game")]
        public static void BuildApartmentGame()
        {
            EnsureMaterials();
            IndexModels();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("ApartmentGame");

            // --- Environment ---
            var env = Child(root, "Environment");

            // Floor / walls / ceiling primitives (no wall/ceiling/floor models exist in pack)
            MeshRenderer floor = CreateSolidBox("Floor", env, new Vector3(ExtX / 2f, -0.1f, ExtZ / 2f),
                new Vector3(ExtX, 0.2f, ExtZ), MatPath("Floor"));
            CreateSolidBox("Ceiling", env, new Vector3(ExtX / 2f, CeilingY + 0.1f, ExtZ / 2f),
                new Vector3(ExtX, 0.2f, ExtZ), MatPath("Wall"));

            BuildOuterWalls(env);
            BuildPartitionWalls(env);

            // --- Rooms containers ---
            var rooms = Child(root, "Rooms");
            Child(rooms, "LivingRoom");
            Child(rooms, "Kitchen");
            Child(rooms, "Hallway");
            Child(rooms, "Bedroom");
            Child(rooms, "Bathroom");

            // --- Furniture containers ---
            var furniture = Child(root, "Furniture");
            var bedFurn = Child(furniture, "BedroomFurniture");
            var livFurn = Child(furniture, "LivingRoomFurniture");
            var kitFurn = Child(furniture, "KitchenFurniture");
            var bathFurn = Child(furniture, "BathroomFurniture");

            // --- Doors & windows default to Environment + need parent for Doors group too ---
            var doors = Child(root, "Doors");
            var windows = Child(env, "Windows");

            // ---- Windows on outer walls (real models) ----
            PlaceModel("Window_Large1", windows, new Vector3(0.35f, 1.4f, 1.2f), 0f);
            PlaceModel("Window_Large2", windows, new Vector3(0.35f, 1.4f, 4.8f), 0f);
            PlaceModel("Window_Large1", windows, new Vector3(15.65f, 1.4f, 1.2f), 180f);
            PlaceModel("Window_Round1", windows, new Vector3(1.2f, 1.4f, 0.35f), 270f);
            PlaceModel("Window_Small1", windows, new Vector3(5.0f, 1.4f, 15.65f), 0f);

            // ---- Living Room furniture ----
            PlaceModel("Couch_Medium1", livFurn, new Vector3(2.6f, 0f, 4.7f), 180f, fit: 2.0f);
            PlaceModel("Couch_Small2", livFurn, new Vector3(2.6f, 0f, 3.0f), 0f, fit: 1.4f);
            PlaceModel("Table_RoundLarge", livFurn, new Vector3(2.6f, 0f, 3.9f), 0f, fit: 1.0f);
            PlaceModel("Chair_1", livFurn, new Vector3(1.6f, 0f, 4.1f), 90f, fit: 0.6f);
            PlaceModel("Chair_2", livFurn, new Vector3(3.6f, 0f, 4.1f), 270f, fit: 0.6f);
            PlaceModel("Bookshelf", livFurn, new Vector3(0.5f, 0f, 3.0f), 90f, fit: 0.8f);
            PlaceModel("Fireplace", livFurn, new Vector3(3.7f, 0f, 0.55f), 0f, fit: 1.2f);
            PlaceModel("Carpet_1", livFurn, new Vector3(2.6f, 0f, 3.9f), 0f, fit: 2.2f);
            PlaceModel("Houseplant_2", livFurn, new Vector3(6.2f, 0f, 5.4f), 0f, fit: 0.4f);
            PlaceModel("Houseplant_5", livFurn, new Vector3(0.8f, 0f, 5.5f), 0f, fit: 0.4f);
            PlaceModel("Stool", livFurn, new Vector3(5.8f, 0f, 1.5f), 0f, fit: 0.45f);
            PlaceModel("Trashcan_Small1", livFurn, new Vector3(6.3f, 0f, 5.1f), 0f, fit: 0.3f);
            PlaceModel("Light_Stand1", livFurn, new Vector3(0.6f, 0f, 5.6f), 0f, fit: 0.4f);

            // ---- Kitchen furniture ----
            PlaceModel("Kitchen_Fridge", kitFurn, new Vector3(10.7f, 0f, 0.7f), 0f, fit: 0.9f);
            PlaceModel("Kitchen_Oven", kitFurn, new Vector3(12.2f, 0f, 0.7f), 0f, fit: 0.8f);
            PlaceModel("Kitchen_Sink", kitFurn, new Vector3(13.7f, 0f, 0.7f), 0f, fit: 0.8f);
            PlaceModel("Kitchen_Cabinet1", kitFurn, new Vector3(15.3f, 0f, 1.6f), 90f, fit: 0.9f);
            PlaceModel("Kitchen_CabinetSmall", kitFurn, new Vector3(15.3f, 0f, 3.2f), 90f, fit: 0.6f);
            PlaceModel("Kitchen_2Drawers", kitFurn, new Vector3(15.3f, 0f, 4.6f), 90f, fit: 0.8f);
            PlaceModel("Table_RoundSmall", kitFurn, new Vector3(12.5f, 0f, 4.6f), 0f, fit: 0.9f);
            PlaceModel("Chair_3", kitFurn, new Vector3(12.5f, 0f, 3.6f), 0f, fit: 0.5f);
            PlaceModel("Chair_4", kitFurn, new Vector3(13.4f, 0f, 4.6f), 180f, fit: 0.5f);
            PlaceModel("Shelf_1", kitFurn, new Vector3(9.7f, 0f, 5.4f), 0f, fit: 0.8f);
            PlaceModel("Trashcan_Green", kitFurn, new Vector3(15.2f, 0f, 5.2f), 0f, fit: 0.3f);
            PlaceModel("Plate_1", kitFurn, new Vector3(12.5f, 0.75f, 4.9f), 0f, fit: 0.25f);

            // ---- Bedroom furniture ----
            PlaceModel("Bed_King", bedFurn, new Vector3(3.5f, 0f, 9.9f), 0f, fit: 2.0f);
            PlaceModel("NightStand_1", bedFurn, new Vector3(2.1f, 0f, 9.9f), 0f, fit: 0.5f);
            PlaceModel("NightStand_2", bedFurn, new Vector3(4.9f, 0f, 9.9f), 0f, fit: 0.5f);
            PlaceModel("Drawer_2", bedFurn, new Vector3(2.0f, 0f, 12.2f), 0f, fit: 0.8f);
            PlaceModel("Drawer_4", bedFurn, new Vector3(2.0f, 0f, 13.2f), 0f, fit: 0.8f);
            PlaceModel("Bed_Single", bedFurn, new Vector3(5.6f, 0f, 13.5f), 180f, fit: 1.5f);
            PlaceModel("Carpet_2", bedFurn, new Vector3(3.5f, 0f, 11.0f), 0f, fit: 2.0f);
            PlaceModel("Carpet_Round", bedFurn, new Vector3(5.6f, 0f, 13.5f), 0f, fit: 1.0f);
            PlaceModel("Houseplant_1", bedFurn, new Vector3(6.4f, 0f, 9.6f), 90f, fit: 0.4f);
            PlaceModel("Light_Desk", bedFurn, new Vector3(2.15f, 0.7f, 10.0f), 0f, fit: 0.3f);
            PlaceModel("Shelf_Small1", bedFurn, new Vector3(6.4f, 0f, 14.9f), 180f, fit: 0.6f);

            // ---- Bathroom furniture ----
            PlaceModel("Bathroom_Toilet", bathFurn, new Vector3(10.8f, 0f, 12.4f), 0f, fit: 0.5f);
            PlaceModel("Bathroom_Bathtub", bathFurn, new Vector3(12.8f, 0f, 13.6f), 0f, fit: 1.6f);
            PlaceModel("Bathroom_Sink", bathFurn, new Vector3(14.6f, 0f, 12.6f), 90f, fit: 0.7f);
            PlaceModel("Bathroom_Shower1", bathFurn, new Vector3(14.9f, 0f, 10.0f), 90f, fit: 0.9f);
            PlaceModel("Bathroom_Mirror1", bathFurn, new Vector3(14.6f, 1.5f, 12.6f), 90f, fit: 0.6f);
            PlaceModel("Bathroom_WashingMachine", bathFurn, new Vector3(9.9f, 0f, 10.2f), 90f, fit: 0.7f);
            PlaceModel("Bathroom_ToiletPaper", bathFurn, new Vector3(10.25f, 0.9f, 12.6f), 0f, fit: 0.25f);
            PlaceModel("Bathroom_Towel", bathFurn, new Vector3(14.0f, 1.6f, 15.2f), 0f, fit: 0.7f);
            PlaceModel("Carpet_Round", bathFurn, new Vector3(12.4f, 0f, 12.0f), 0f, fit: 0.8f);

            // ---- Doors (real models; rotated open so doorways stay walkable) ----
            // Placed exactly in the 1m partition-wall gaps. Each door has a BoxCollider,
            // so a closed door would block the player, but these are swung open to keep
            // every room explorable.
            PlaceModel("Door_1", doors, new Vector3(4.5f, 0f, 6.0f), 90f, fit: 1.0f);
            PlaceModel("Door_3", doors, new Vector3(12.5f, 0f, 6.0f), 90f, fit: 1.0f);
            PlaceModel("Door_5", doors, new Vector3(4.5f, 0f, 9.0f), 90f, fit: 1.0f);
            PlaceModel("Door_7", doors, new Vector3(12.5f, 0f, 9.0f), 90f, fit: 1.0f);

            // ---- Lights (real ceiling/wall light models) + Light components ----
            var lights = Child(root, "Lights");
            CreateDirectionalLight(lights);
            PlaceModel("Light_Ceiling1", lights, new Vector3(3.5f, 2.55f, 3.0f), 0f, fit: 0.4f);
            PlaceModel("Light_Ceiling2", lights, new Vector3(12.5f, 2.55f, 3.0f), 0f, fit: 0.4f);
            PlaceModel("Light_Chandelier", lights, new Vector3(8.0f, 2.0f, 7.5f), 0f, fit: 0.5f);
            PlaceModel("Light_Ceiling3", lights, new Vector3(3.5f, 2.55f, 11.0f), 0f, fit: 0.4f);
            PlaceModel("Light_Ceiling4", lights, new Vector3(12.5f, 2.55f, 11.0f), 0f, fit: 0.4f);
            AddRoomLights(lights);

            // ---- Gameplay: player, camera, audio, event system, UI ----
            var gameplay = Child(root, "Gameplay");
            BuildGameplay(gameplay);

            // Save scene
            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log("[ApartmentGameBuilder] Built and saved: " + ScenePath);
        }

        // ============================================================
        //  Environment shell
        // ============================================================

        private static void BuildOuterWalls(GameObject env)
        {
            MeshRenderer south = CreateSolidBox("Wall_South", env, new Vector3(ExtX / 2f, WallHeight / 2f, -WallThickness / 2f),
                new Vector3(ExtX + WallThickness, WallHeight, WallThickness), MatPath("Wall"));
            MeshRenderer north = CreateSolidBox("Wall_North", env, new Vector3(ExtX / 2f, WallHeight / 2f, ExtZ + WallThickness / 2f),
                new Vector3(ExtX + WallThickness, WallHeight, WallThickness), MatPath("Wall"));
            // West/East split into two to leave hallway open (hallway ends at outer walls though, so full)
            MeshRenderer west = CreateSolidBox("Wall_West", env, new Vector3(-WallThickness / 2f, WallHeight / 2f, ExtZ / 2f),
                new Vector3(WallThickness, WallHeight, ExtZ + WallThickness * 2f), MatPath("Wall"));
            MeshRenderer east = CreateSolidBox("Wall_East", env, new Vector3(ExtX + WallThickness / 2f, WallHeight / 2f, ExtZ / 2f),
                new Vector3(WallThickness, WallHeight, ExtZ + WallThickness * 2f), MatPath("Wall"));
        }

        // Adds the interior partition walls. Door gaps are 1.0 m wide openings.
        private static void BuildPartitionWalls(GameObject env)
        {
            // LivingRoom north (z:0->6) divider to hallway, gap at x[4.0,5.0]
            CreateSolidBox("Wall_Liv_Hall_A", env, new Vector3(2.0f, WallHeight / 2f, 6f), new Vector3(4.0f, WallHeight, WallThickness), MatPath("Wall"));
            CreateSolidBox("Wall_Liv_Hall_B", env, new Vector3(6.0f, WallHeight / 2f, 6f), new Vector3(2.0f, WallHeight, WallThickness), MatPath("Wall"));
            // Kitchen north divider to hallway, gap at x[12.0,13.0]
            CreateSolidBox("Wall_Kit_Hall_A", env, new Vector3(10.5f, WallHeight / 2f, 6f), new Vector3(3.0f, WallHeight, WallThickness), MatPath("Wall"));
            CreateSolidBox("Wall_Kit_Hall_B", env, new Vector3(14.5f, WallHeight / 2f, 6f), new Vector3(3.0f, WallHeight, WallThickness), MatPath("Wall"));
            // Bedroom south divider to hallway, gap at x[4.0,5.0]
            CreateSolidBox("Wall_Bed_Hall_A", env, new Vector3(2.0f, WallHeight / 2f, 9f), new Vector3(4.0f, WallHeight, WallThickness), MatPath("Wall"));
            CreateSolidBox("Wall_Bed_Hall_B", env, new Vector3(6.0f, WallHeight / 2f, 9f), new Vector3(2.0f, WallHeight, WallThickness), MatPath("Wall"));
            // Bathroom south divider to hallway, gap at x[12.0,13.0]
            CreateSolidBox("Wall_Bath_Hall_A", env, new Vector3(10.5f, WallHeight / 2f, 9f), new Vector3(3.0f, WallHeight, WallThickness), MatPath("Wall"));
            CreateSolidBox("Wall_Bath_Hall_B", env, new Vector3(14.5f, WallHeight / 2f, 9f), new Vector3(3.0f, WallHeight, WallThickness), MatPath("Wall"));
            // LivingRoom east divider x=7 (z 0..6)
            CreateSolidBox("Wall_Liv_Kit", env, new Vector3(7f, WallHeight / 2f, 3f), new Vector3(WallThickness, WallHeight, 6f), MatPath("Wall"));
            // Kitchen west divider x=9 handled by same shape on the other side; use x=9
            CreateSolidBox("Wall_Kit_Liv", env, new Vector3(9f, WallHeight / 2f, 3f), new Vector3(WallThickness, WallHeight, 6f), MatPath("Wall"));
            // Bedroom east divider x=7 (z 9..16)
            CreateSolidBox("Wall_Bed_Bath", env, new Vector3(7f, WallHeight / 2f, 12.5f), new Vector3(WallThickness, WallHeight, 7f), MatPath("Wall"));
            // Bathroom west divider x=9 (z 9..16)
            CreateSolidBox("Wall_Bath_Bed", env, new Vector3(9f, WallHeight / 2f, 12.5f), new Vector3(WallThickness, WallHeight, 7f), MatPath("Wall"));
        }

        // ============================================================
        //  Model placement (real FBX models, floor-snapped + colliders)
        // ============================================================

        /// <summary>
        /// Instantiates a model from the Interior pack, uniformly rescales it so its
        /// larger XZ dimension approaches 'fit' (meters), and snaps it so the caller-
        /// supplied bottom level (pos.y) is exact — floor items use pos.y = 0, wall
        /// mirrors/ceiling lights use an absolute height. Adds a fitted BoxCollider.
        /// </summary>
        private static void PlaceModel(string key, GameObject parent, Vector3 pos, float yRot,
            float fit = 1f, string mat = null)
        {
            if (!allModels.TryGetValue(key, out GameObject prefab) || prefab == null) return;

            float bottomY = pos.y; // desired absolute bottom of the model (0 = on floor)

            GameObject wrapper = new GameObject(key);
            wrapper.transform.SetParent(parent.transform, false);
            wrapper.transform.position = Vector3.zero;
            wrapper.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(wrapper.transform, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;

            // Measure the model's local bounds (at wrapper origin).
            Bounds b = GetBounds(wrapper);

            // Normalize scale to the target 'fit' dimension.
            if (b.size.x > 0.0001f)
            {
                float largest = Mathf.Max(b.size.x, b.size.z);
                if (largest < 0.01f) { Object.DestroyImmediate(wrapper); return; } // degenerate
                float scale = fit / largest;
                if (Mathf.Abs(scale - 1f) > 0.001f)
                {
                    inst.transform.localScale = Vector3.one * scale;
                    b = GetBounds(wrapper);
                }
            }

            // Place so the model's bottom lands exactly at bottomY.
            wrapper.transform.position = new Vector3(pos.x, bottomY - b.min.y, pos.z);

            AddColliders(wrapper);
            if (mat != null) ApplyMaterial(wrapper, mat);
        }

        private static void AddColliders(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            foreach (var mr in renderers)
            {
                if (mr.gameObject.GetComponent<Collider>() != null) continue;
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;

                // BoxCollider is used throughout (preferred for simple props / performance).
                Bounds rb = mr.bounds;
                var bc = mr.gameObject.AddComponent<BoxCollider>();
                bc.center = mr.transform.InverseTransformPoint(rb.center);
                bc.size = new Vector3(rb.size.x, rb.size.y, rb.size.z);
            }
        }

        private static Bounds GetBounds(GameObject go)
        {
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return b;
        }

        // ============================================================
        //  Lights
        // ============================================================

        private static void CreateDirectionalLight(GameObject parent)
        {
            var go = new GameObject("Directional Light");
            go.transform.SetParent(parent.transform, false);
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.9f;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.shadows = LightShadows.Soft;
        }

        private static void AddRoomLights(GameObject parent)
        {
            float[] xs = { 3.5f, 12.5f };
            float[] zs = { 3.0f, 11.0f };
            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    var go = new GameObject("PointLight_" + x + "_" + z);
                    go.transform.SetParent(parent.transform, false);
                    go.transform.position = new Vector3(x, 2.4f, z);
                    var light = go.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.range = 6f;
                    light.intensity = 2.2f;
                    light.color = new Color(1f, 0.96f, 0.9f);
                    light.shadows = LightShadows.Soft;
                }
            }
        }

        // ============================================================
        //  Gameplay (player, camera, audio, UI)
        // ============================================================

        private static void BuildGameplay(GameObject parent)
        {
            // Player + FPS camera
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.SetParent(parent.transform, false);
            player.transform.position = new Vector3(3.5f, 0.2f, 2.0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.4f; cc.center = new Vector3(0f, 0.9f, 0f);
            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<FootstepController>();

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGo.AddComponent<UnityEngine.Camera>().nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PlayerCameraController>();
            camGo.AddComponent<ApartmentAfterDark.Interaction.InteractionSystem>();

            CreateGameBootstrap(parent);
        }

        private static void CreateGameBootstrap(GameObject parent)
        {
            var go = new GameObject("GameBootstrap");
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<GameBootstrap>();
        }

        // ============================================================
        //  Primitives / helpers
        // ============================================================

        private static MeshRenderer CreateSolidBox(string name, GameObject parent, Vector3 pos, Vector3 scale, string matPath)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent.transform, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (matPath != null) ApplyMaterial(go, matPath);
            return go.GetComponent<MeshRenderer>();
        }

        private static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        // ============================================================
        //  Assets / materials
        // ============================================================

        private static string MatPath(string name) => $"{MatDir}/{name}.mat";

        private static void EnsureMaterials()
        {
            if (Directory.Exists(MatDir) == false) Directory.CreateDirectory(MatDir);
            EnsureMaterial("Wall.mat", new Color(0.88f, 0.86f, 0.82f));
            EnsureMaterial("Floor.mat", new Color(0.58f, 0.45f, 0.32f));
        }

        private static void EnsureMaterial(string file, Color color)
        {
            string path = $"{MatDir}/{file}";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }

        private static void ApplyMaterial(GameObject root, string matPath)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) return;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
                mr.sharedMaterial = mat;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == path) == false)
                list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // Index the real FBX models present in the pack.
        private static void IndexModels()
        {
            allModels.Clear();
            if (!Directory.Exists(InteriorDir)) return;
            foreach (string f in Directory.GetFiles(InteriorDir, "*.fbx"))
                allModels[Path.GetFileNameWithoutExtension(f)] = AssetDatabase.LoadAssetAtPath<GameObject>(f);
        }
    }
}
#endif
