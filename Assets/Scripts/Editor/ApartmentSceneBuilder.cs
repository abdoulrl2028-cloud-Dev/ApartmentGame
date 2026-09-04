#if UNITY_EDITOR
using System.IO;
using System.Linq;
using ApartmentAfterDark.Audio;
using ApartmentAfterDark.Camera;
using ApartmentAfterDark.Interaction;
using ApartmentAfterDark.Player;
using ApartmentAfterDark.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ApartmentAfterDark.EditorTools
{
    /// <summary>
    /// One-click scene/prefab builder. Access via Tools > Game > Build Apartment Scene.
    /// Assembles a playable apartment scene: players, camera, lighting, interactables,
    /// audio zones and UI, and adds it to Build Settings.
    /// </summary>
    public static class ApartmentSceneBuilder
    {
        private const string SceneDir = "Assets/Scenes";
        private const string PrefabDir = "Assets/Prefabs";

        [MenuItem("Tools/Game/Build Apartment Scene")]
        public static void BuildApartmentScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Lighting ---
            CreateDirectionalLight();

            // --- Apartment model (if present) ---
            bool placedModel = TryPlaceApartmentModel();

            // --- Player ---
            var player = CreatePlayer(ApartmentSpawnPoint());

            // --- Audio ---
            CreateAudioManager();
            // NOTE: An AudioMixer is best created manually (Window > Audio > Audio Mixer):
            // groups Master/Music/Ambient/SFX/Voice, then assign it to the AudioManager's
            // "Mixer" field. AudioManager still works without one (uses plain sources).
            if (!placedModel)
            {
                CreateProceduralRoom();
            }
            CreateBasicMaterials();

            // --- Event system + UI ---
            CreateEventSystem();
            CreateUICanvas();

            // --- Save scene ---
            Directory.CreateDirectory(SceneDir);
            string scenePath = SceneDir + "/ApartmentScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            AddSceneToBuildSettings(scenePath);
            Debug.Log("[ApartmentSceneBuilder] Scene built and saved: " + scenePath);
        }

        [MenuItem("Tools/Game/Create Basic Materials")]
        public static void CreateBasicMaterials()
        {
            CreateBasicMaterial("Assets/Environment/Apartment/Materials/Wall.mat", new Color(0.85f, 0.84f, 0.8f));
            CreateBasicMaterial("Assets/Environment/Apartment/Materials/Wood.mat", new Color(0.55f, 0.38f, 0.22f));
            CreateBasicMaterial("Assets/Environment/Apartment/Materials/Metal.mat", new Color(0.5f, 0.5f, 0.52f));
            CreateBasicMaterial("Assets/Environment/Apartment/Materials/Floor.mat", new Color(0.62f, 0.5f, 0.34f));
            CreateBasicMaterial("Assets/Characters/Materials/Character.mat", new Color(0.8f, 0.75f, 0.7f));
            Debug.Log("[ApartmentSceneBuilder] Basic materials created.");
        }

        private static void CreateBasicMaterial(string path, Color color)
        {
            Shader std = Shader.Find("Standard");
            if (std == null) return;

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return;

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            var mat = new Material(std);
            mat.color = color;
            mat.SetFloat("_Glossiness", 0.4f);
            AssetDatabase.CreateAsset(mat, path);
        }

        // ---------------- Building blocks ----------------

        private static void CreateDirectionalLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = new Color(1f, 0.96f, 0.9f);
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            try { go.tag = "Untagged"; } catch { }
        }

        // Bounds/world info of the last apartment shell placed, so the player can be
        // spawned standing on the floor inside it. Populated by TryPlaceApartmentModel.
        private static Bounds lastApartmentBounds = new Bounds(Vector3.zero, Vector3.zero);
        private static bool apartmentPlaced;

        // Returns true if the real "Apartamento" shell was placed (floor/walls/doors/stairs
        // and its furniture), with colliders attached so the player cannot clip through
        // walls or fall through the floor. Falls back to furniture-only/procedural room.
        private static bool TryPlaceApartmentModel()
        {
            apartmentPlaced = false;
            const string fbxPath = "Assets/Environment/Apartment/ApartamentoModel/Apartamento.fbx";
            if (!File.Exists(fbxPath)) return false;

            // Ensure meshes are readable (required for MeshColliders).
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer != null)
            {
                importer.isReadable = true;
                importer.globalScale = 1f;
                importer.SaveAndReimport();
            }

            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (prefabAsset == null) return false;

            // Organized hierarchy: clear root + geometry container + support objects.
            var root = new GameObject("Apartamento");
            var shell = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            shell.name = "Apartamento_Geometry";
            shell.transform.SetParent(root.transform, false);
            shell.transform.position = Vector3.zero;
            shell.transform.rotation = Quaternion.identity;

            // Measure raw world-space bounds.
            Bounds b = GetBounds(shell);

            // Normalize so the apartment footprint (larger of X/Z) is ~12m wide.
            float largest = Mathf.Max(b.size.x, b.size.z);
            if (largest > 0.0001f)
            {
                float s = 12f / largest;
                shell.transform.localScale = Vector3.one * s;
                b = GetBounds(shell);
            }

            // Snap floor (min Y) to y=0 so the player stands on it.
            shell.transform.position -= new Vector3(0f, b.min.y, 0f);
            b = GetBounds(shell);

            // Re-center X/Z so the apartment is roughly centered on the origin.
            shell.transform.position -= new Vector3(b.center.x - b.size.x * 0.05f, 0f, b.center.z - b.size.z * 0.05f);
            b = GetBounds(shell);

            // Full collision: walls, floor, doors, stairs and furniture all block the player.
            AddMeshCollidersRecursively(shell);

            // Guaranteed anti-fall-through floor spanning the whole footprint.
            GameObject safetyFloor = new GameObject("SafetyFloor_NoFallThrough");
            safetyFloor.transform.SetParent(root.transform, false);
            var sfb = safetyFloor.AddComponent<BoxCollider>();
            sfb.size = new Vector3(b.size.x + 1f, 0.2f, b.size.z + 1f);
            sfb.center = new Vector3(b.center.x, b.min.y - 0.12f, b.center.z);

            lastApartmentBounds = b;
            apartmentPlaced = true;
            return true;
        }

        /// <summary>Spawn the player standing on the apartment floor, inside the model.</summary>
        private static Vector3 ApartmentSpawnPoint()
        {
            if (!apartmentPlaced || lastApartmentBounds.size.sqrMagnitude <= 0f)
                return new Vector3(0f, 0.2f, 0f);
            Bounds b = lastApartmentBounds;
            // Slightly inset from the X/Z face so we land in open floor, not a wall.
            return new Vector3(b.center.x, b.min.y + 0.35f, b.center.z);
        }

        private static Bounds GetBounds(GameObject go)
        {
            Bounds b = new Bounds(go.transform.position, Vector3.zero);
            var renderers = go.GetComponentsInChildren<Renderer>();
            bool any = false;
            foreach (var r in renderers)
            {
                if (r is SkinnedMeshRenderer || r is MeshRenderer)
                {
                    if (!any) { b = r.bounds; any = true; }
                    else b.Encapsulate(r.bounds);
                }
            }
            return b;
        }

        private static void AddMeshCollidersRecursively(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            foreach (var mr in renderers)
            {
                if (mr.gameObject.GetComponent<Collider>() != null) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mc = mr.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }

            // Skinned characters are rare here; skip them (no added colliders).
        }

        private static GameObject CreatePlayer(Vector3 position)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = position;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<FootstepController>();

            var camGo = CreateCamera(player);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            return player;
        }

        private static GameObject CreateCamera(GameObject parent)
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(parent.transform, false);
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PlayerCameraController>();
            camGo.AddComponent<InteractionSystem>();
            return camGo;
        }

        private static void CreateAudioManager()
        {
            if (AudioManager.Instance != null) return;
            CreateAudioManagerObject();
        }

        private static GameObject CreateAudioManagerObject()
        {
            var go = new GameObject("AudioManager");
            go.AddComponent<AudioManager>();
            return go;
        }

        private static void CreateEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static void CreateUICanvas()
        {
            var canvasGo = new GameObject("HUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            var hud = canvasGo.AddComponent<HUD>();

            var textGo = new GameObject("PromptText");
            textGo.transform.SetParent(canvasGo.transform, false);
            var rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 120f);
            rt.sizeDelta = new Vector2(600f, 60f);
            var text = textGo.AddComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 28;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            hud.SetPromptText(text);
        }

        private static void CreateProceduralRoom()
        {
            // Fallback room so the scene is always navigable
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(12f, 0.2f, 12f);
            floor.transform.position = new Vector3(0f, -0.1f, 0f);

            CreateWall(new Vector3(0f, 1.5f, -5f), new Vector3(12f, 3f, 0.2f));
            CreateWall(new Vector3(0f, 1.5f, 5f), new Vector3(12f, 3f, 0.2f));
            CreateWall(new Vector3(-5f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f));
            CreateWall(new Vector3(5f, 1.5f, 0f), new Vector3(0.2f, 3f, 10f));
        }

        private static void CreateWall(Vector3 pos, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.transform.position = pos;
            wall.transform.localScale = scale;
        }

        // ---------------- Prefabs ----------------

        private static string CreateDoorPrefab()
        {
            var go = new GameObject("Door");
            var mesh = go.AddComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            go.AddComponent<MeshRenderer>();
            go.AddComponent<BoxCollider>();
            go.AddComponent<DoorController>();
            return SavePrefab(go, PrefabDir + "/Door.prefab");
        }

        private static string CreateLightSwitchPrefab()
        {
            var go = new GameObject("LightSwitch");
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(0.15f, 0.2f, 0.05f);
            go.AddComponent<LightSwitch>();
            return SavePrefab(go, PrefabDir + "/LightSwitch.prefab");
        }

        private static string CreateTelevisionPrefab()
        {
            var go = new GameObject("Television");
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(1.2f, 0.6f, 0.4f);
            go.AddComponent<TelevisionController>();
            return SavePrefab(go, PrefabDir + "/Television.prefab");
        }

        private static string SavePrefab(GameObject go, string path)
        {
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved != null ? path : null;
        }

        private static void CreatePrefab(string name, GameObject source)
        {
            string path = PrefabDir + "/" + name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(source, path);
            Object.DestroyImmediate(source);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == scenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }
    }
}
#endif
