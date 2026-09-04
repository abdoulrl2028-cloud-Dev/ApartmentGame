#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ApartmentAfterDark.EditorTools
{
    /// <summary>
    /// One-click scene setup for Assets/Scenes/ApartmentGame.unity. Performs, in the
    /// current open scene:
    ///  1. Fixes the "white model" by assigning the Apartamento.fbx embedded materials
    ///     (and locating textures) to every renderer under the Apartment object.
    ///  2. Builds a first-person Player (CharacterController) + PlayerCamera child.
    ///  3. Builds an AudioManager with AmbientAudio/MusicAudio/SFXAudio/VoiceAudio sources
    ///     wired to the real clips in Assets/Audio.
    ///  4. Ensures colliders on walls/floor/objects.
    ///  5. Disables the old Main Camera so PlayerCamera drives the game view.
    ///  6. Organizes everything under an ApartmentGame root and saves the scene.
    ///
    ///     Access: Tools > Game > Setup Complete Apartment Game
    /// </summary>
    public static class SetupApartmentGame
    {
        private const string FbxPath = "Assets/Environment/Apartment/ApartamentoModel/Apartamento.fbx";
        private const string TexturesDir = "Assets/Environment/Apartment/ApartamentoModel/Textures";
        private const string ScenePath = "Assets/Scenes/ApartmentGame.unity";
        private const string ApartamentoRootName = "ApartamentoModel";

        // Air clips
        private const string AmbientClip = "Assets/Audio/Ambient/AMB_apartment_window_closed.mp3";
        private const string MusicClip = "Assets/Audio/Music/MUS_ambient_lightless_dawn.mp3";
        private static readonly string[] WoodSteps =
        {
            "Assets/Audio/SFX/SFX_footsteps_wooden_floor.mp3",
        };
        private static readonly string[] ConcreteSteps =
        {
            "Assets/Audio/SFX/SFX_footsteps_fast_concrete.mp3",
        };

        // Spawn point (interior, just above the floor). Adjust if geometry dictates.
        private static readonly Vector3 SpawnPosition = new Vector3(0f, 1.1f, 2.5f);

        [MenuItem("Tools/Game/Setup Complete Apartment Game")]
        public static void Setup()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            var root = EnsureRoot(scene);

            FixApartmentWhiteModel(root);   // 1
            var player = BuildPlayer(root); // 2
            BuildAudio(root);               // 3
            EnsureAllColliders(root);       // 4
            FixCamera(root, player);        // 5
            PolishVisuals(root);            // 6

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log("[SetupApartmentGame] Scene ready -> " + ScenePath);
        }

        // --------------------------------------------------------------- visuals
        /// <summary>Warms up the world so the apartment looks inviting: a soft night
        /// key light with shadows, gentle fill lights, ambient/fog for depth, and a
        /// pleasant camera background.</summary>
        private static void PolishVisuals(GameObject root)
        {
            // Key light (rename handled in EnsureRoot as "Main Light").
            GameObject light = GameObject.Find("Main Light");
            if (light == null) light = GameObject.Find("Directional Light");
            if (light != null)
            {
                light.name = "Main Light";
                var l = light.GetComponent<Light>();
                if (l == null) l = light.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.15f;
                l.color = new Color(1f, 0.93f, 0.85f);          // warm, slightly golden
                l.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            }

            // A soft fill from the opposite side so shadows aren't pitch black.
            var fillPositions = new[]
            {
                new Vector3(8f, 4f, 10f),
                new Vector3(-8f, 5f, -12f),
            };
            for (int i = 0; i < fillPositions.Length; i++)
            {
                string nm = i == 0 ? "FillLight1" : "FillLight2";
                Transform existing = root.transform.Find(nm);
                var go = existing != null ? existing.gameObject : new GameObject(nm);
                if (existing == null) go.transform.SetParent(root.transform, false);
                go.transform.position = fillPositions[i];
                var fl = go.GetComponent<Light>();
                if (fl == null) fl = go.AddComponent<Light>();
                fl.type = LightType.Point;
                fl.range = 12f;
                fl.intensity = 0.9f;
                fl.color = new Color(1f, 0.98f, 0.9f);
                fl.shadows = LightShadows.None;
            }

            // Ambient + fog for depth and a cozier mood.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.36f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.2f, 0.21f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.13f, 0.13f, 0.15f);
            RenderSettings.ambientIntensity = 1f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 10f;
            RenderSettings.fogEndDistance = 40f;
            RenderSettings.fogColor = new Color(0.09f, 0.1f, 0.13f);

            // Nicer default sky/background tint.
            RenderSettings.skybox = null; // solid-ish; ambient already set
        }

        // ------------------------------------------------------------------ root
        private static GameObject EnsureRoot(UnityEngine.SceneManagement.Scene scene)
        {
            GameObject root = GameObject.Find("ApartmentGame");
            if (root == null) root = new GameObject("ApartmentGame");

            // Reparent / create the standard children.
            EnsureChild(root, "GameSystems");
            EnsureChild(root, "AudioManager");

            EnsureApartamento(root);

            GameObject light = GameObject.Find("Directional Light");
            if (light == null) light = GameObject.Find("Main Light");
            if (light != null)
            {
                if (light.name != "Main Light") light.name = "Main Light";
                if (light.transform.parent == null)
                    light.transform.SetParent(root.transform, false);
            }
            else
            {
                EnsureChild(root, "Main Light");
            }

            return root;
        }

        /// <summary>
        /// Ensures a single "ApartamentoModel" container exists under the root holding the
        /// apartment geometry. Reuses the existing "Apartamento" object (from the earlier
        /// assemble step) if present, renaming it; otherwise looks for the geometry child.
        /// </summary>
        private static void EnsureApartamento(GameObject root)
        {
            GameObject model = GameObject.Find(ApartamentoRootName);
            if (model != null)
            {
                if (model.transform.parent == null)
                    model.transform.SetParent(root.transform, false);
                EnsureVisibleTexturesChild(model);
                return;
            }

            // Reuse/rename the old "Apartment" container if it exists.
            var old = GameObject.Find("Apartment");
            if (old == null)
            {
                // Fallback: any child already holding the geometry.
                old = GameObject.Find("Apartamento_Geometry")?.transform.parent?.gameObject;
            }

            if (old != null)
            {
                old.name = ApartamentoRootName;
                if (old.transform.parent == null)
                    old.transform.SetParent(root.transform, false);
                EnsureVisibleTexturesChild(old);
                return;
            }

            // Nothing exists yet: instantiate the FBX.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (prefab == null) return;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = ApartamentoRootName;
            inst.transform.SetParent(root.transform, false);
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;
            CentralizeAndScale(inst);
            EnsureVisibleTexturesChild(inst);
        }

        /// <summary>Adds a (harmless) empty "Textures" node so the hierarchy shows the
        /// model + its textures together, matching the requested structure.</summary>
        private static void EnsureVisibleTexturesChild(GameObject model)
        {
            if (model.transform.Find("Textures") == null)
            {
                var t = new GameObject("Textures");
                t.transform.SetParent(model.transform, false);
            }
        }

        /// <summary>Centers on the origin (X/Z), sits the floor on Y=0, and scales the
        /// apartment to a large, walkable footprint.</summary>
        private static void CentralizeAndScale(GameObject model)
        {
            Bounds first = GetBounds(model);
            if (first.size.sqrMagnitude < 0.0001f) return;

            float largest = Mathf.Max(first.size.x, first.size.z);
            float target = 12f;
            float scale = largest > 0.0001f ? target / largest : 1f;
            model.transform.localScale = Vector3.one * scale;

            Bounds b = GetBounds(model);
            model.transform.position = new Vector3(-b.center.x, -b.min.y, -b.center.z);
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

        private static GameObject EnsureChild(GameObject parent, string name)
        {
            var t = parent.transform.Find(name);
            if (t != null) return t.gameObject;
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        // ------------------------------------------------------------ white model fix
        private static void FixApartmentWhiteModel(GameObject root)
        {
            var apartment = GameObject.Find(ApartamentoRootName);
            if (apartment == null) return;

            // Impeller: ensure readable so materials/textures resolve cleanly.
            var imp = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (imp != null)
            {
                imp.isReadable = true;
                imp.SaveAndReimport();
            }

            // All materials embedded in the FBX.
            var fbxMats = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
                .OfType<Material>().Where(m => m != null).ToList();

            // All textures shipped with the model.
            var textures = new List<Texture2D>();
            if (Directory.Exists(TexturesDir))
                foreach (var f in Directory.GetFiles(TexturesDir))
                    if (f.EndsWith(".jpeg") || f.EndsWith(".jpg") || f.EndsWith(".png"))
                    {
                        var tx = AssetDatabase.LoadAssetAtPath<Texture2D>(f);
                        if (tx != null) textures.Add(tx);
                    }

            int assignedSlots = 0;
            foreach (var mr in apartment.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = mr.sharedMaterials;
                if (mats == null) continue;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material m = mats[i];
                    bool usable = m != null
                        && m.name != "Default-Material"
                        && !(m.mainTexture == null && m.color == Color.white && !m.name.Contains("Apartamento"));

                    if (!usable)
                        m = FindMaterial(mr.gameObject, i, fbxMats);

                    if (m == null) continue;

                    // If still no texture, search the textures folder by name.
                    if (m.mainTexture == null)
                        m.mainTexture = FindTexture(mr.gameObject.name, textures);
                    if (m.mainTexture == null)
                        m.mainTexture = FindTexture(m.name, textures);

                    mats[i] = m;
                    assignedSlots++;
                }
                mr.sharedMaterials = mats;
            }

            Debug.Log("[SetupApartmentGame] Assigned materials to " + assignedSlots + " mesh slots.");
        }

        private static Material FindMaterial(GameObject go, int index, List<Material> fbxMats)
        {
            // 1) name match against the object (Sketchfab meshes are named by material)
            string n = go.name;
            foreach (var m in fbxMats)
                if (string.Compare(m.name, n, System.StringComparison.OrdinalIgnoreCase) == 0)
                    return m;
            foreach (var m in fbxMats)
                if (n.IndexOf(m.name, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || m.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return m;

            // 2) fallback: by submesh index
            var noDefault = fbxMats.Where(m => m.name != "Default-Material").ToList();
            if (noDefault.Count > 0)
                return noDefault[Mathf.Clamp(index, 0, noDefault.Count - 1)];

            return fbxMats.Count > 0 ? fbxMats[Mathf.Clamp(index, 0, fbxMats.Count - 1)] : null;
        }

        private static Texture2D FindTexture(string hint, List<Texture2D> textures)
        {
            if (textures.Count == 0 || string.IsNullOrEmpty(hint)) return null;
            foreach (var t in textures)
                if (string.Compare(System.IO.Path.GetFileNameWithoutExtension(t.name), hint,
                        System.StringComparison.OrdinalIgnoreCase) == 0)
                    return t;
            foreach (var t in textures)
                if (hint.IndexOf(System.IO.Path.GetFileNameWithoutExtension(t.name),
                        System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return t;

            // Structure/Door/Books etc. are ambiguous; default to structure for walls.
            Texture2D fallback = textures.FirstOrDefault(t =>
                t.name.Contains("1_Structure"));
            return fallback;
        }

        // --------------------------------------------------------------- player
        private static GameObject BuildPlayer(GameObject root)
        {
            var existing = GameObject.Find("Player");
            if (existing != null) Object.DestroyImmediate(existing);

            var player = new GameObject("Player");
            player.transform.SetParent(root.transform, false);
            player.tag = "Player";
            player.transform.position = SpawnPosition;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            player.AddComponent<ApartmentAfterDark.Player.PlayerController>();
            player.AddComponent<ApartmentAfterDark.Player.PlayerHealth>();
            var feet = player.AddComponent<ApartmentAfterDark.Player.FootstepController>();

            // Footstep clips: wooden + concrete surface types, wired via AudioManager.
            var so = new SerializedObject(feet);
            SetObjectArray(so, "woodFootsteps", LoadClips(WoodSteps));
            SetObjectArray(so, "concreteFootsteps", LoadClips(ConcreteSteps));
            so.ApplyModifiedPropertiesWithoutUndo();

            // Camera child
            var camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGo.tag = "MainCamera";

            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.nearClipPlane = 0.05f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<ApartmentAfterDark.Camera.PlayerCameraController>();
            camGo.AddComponent<ApartmentAfterDark.Interaction.InteractionSystem>();

            return player;
        }

        private static AudioClip[] LoadClips(string[] paths)
        {
            var list = new List<AudioClip>();
            foreach (var p in paths)
            {
                var c = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                if (c != null) list.Add(c);
            }
            return list.ToArray();
        }

        private static void SetObjectArray(SerializedObject so, string propName, Object[] values)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                var el = prop.GetArrayElementAtIndex(i);
                el.objectReferenceValue = values[i];
            }
        }

        // ---------------------------------------------------------------- audio
        private static void BuildAudio(GameObject root)
        {
            var audioGO = GameObject.Find("AudioManager")
                ?? new GameObject("AudioManager");
            if (audioGO.transform.parent == null)
                audioGO.transform.SetParent(root.transform, false);

            var am = audioGO.GetComponent<ApartmentAfterDark.Audio.AudioManager>();
            if (am == null) am = audioGO.AddComponent<ApartmentAfterDark.Audio.AudioManager>();

            // Dedicated sources (AudioManager adopts them by name). No playOnAwake:
            // AudioManager.Start() assigns clips + plays, so nothing double-plays.
            MakeSource(audioGO, "AmbientAudio", 0f, 0.6f, true);
            MakeSource(audioGO, "MusicAudio", 0f, 0.4f, true);
            MakeSource(audioGO, "SFXAudio", 0f, 1f, false);   // 2D bus; 3D world SFX uses pool
            MakeSource(audioGO, "VoiceAudio", 0f, 1f, false);

            var so = new SerializedObject(am);
            so.FindProperty("defaultAmbient").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(AmbientClip);
            so.FindProperty("defaultMusic").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(MusicClip);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void MakeSource(GameObject parent, string name, float blend, float volume, bool loop)
        {
            var t = parent.transform.Find(name);
            GameObject go = t != null ? t.gameObject : new GameObject(name);
            if (t == null) go.transform.SetParent(parent.transform, false);

            var src = go.GetComponent<AudioSource>();
            if (src == null) src = go.AddComponent<AudioSource>();
            src.spatialBlend = blend;
            src.volume = volume;
            src.loop = loop;
            src.playOnAwake = false;
        }

        // ------------------------------------------------------------ colliders
        private static void EnsureAllColliders(GameObject root)
        {
            int added = 0;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (mr.gameObject.GetComponent<Collider>() != null) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mc = mr.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                added++;
            }
            Debug.Log("[SetupApartmentGame] Collider pass done, added " + added + ".");
        }

        // --------------------------------------------------------------- camera
        private static void FixCamera(GameObject root, GameObject player)
        {
            var oldCam = GameObject.Find("Main Camera");
            if (oldCam != null)
            {
                var c = oldCam.GetComponent<UnityEngine.Camera>();
                if (c != null) c.enabled = false;   // disabled, not destroyed
                oldCam.SetActive(false);
            }

            var pcam = GameObject.Find("PlayerCamera");
            if (pcam != null)
            {
                var c = pcam.GetComponent<UnityEngine.Camera>();
                if (c != null) { c.enabled = true; }
                pcam.SetActive(true);
            }
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == path))
                list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
