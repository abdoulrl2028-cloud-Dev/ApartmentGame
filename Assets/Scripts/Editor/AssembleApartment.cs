#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ApartmentAfterDark.EditorTools
{
    /// <summary>
    /// Assembles the complete apartment model into the CURRENT scene, keeping any
    /// existing Main Camera / Directional Light. The apartment is centered on the
    /// origin (X/Z) with its floor on Y=0, rescaled to a realistic size, wrapped in
    /// an "Apartment" GameObject, and saved to Assets/Scenes/ApartmentGame.unity.
    ///
    /// Access: Tools > Game > Assemble Apartment in Scene
    /// </summary>
    public static class AssembleApartment
    {
        private const string ModelPath = "Assets/Environment/Apartment/ApartamentoModel/Apartamento.fbx";
        private const string ScenePath = "Assets/Scenes/ApartmentGame.unity";

        /// <summary>Target uniform scale: largest footprint dimension reaches this (m).</summary>
        private const float TargetFootprint = 12f;
        /// <summary>Cap so the apartment height stays plausible (~3m).</summary>
        private const float MaxHeight = 3.2f;

        [MenuItem("Tools/Game/Assemble Apartment in Scene")]
        public static void Assemble()
        {
            // Ensure the model's meshes are readable (needed for colliders).
            var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer != null)
            {
                importer.isReadable = true;
                importer.globalScale = 1f;
                importer.SaveAndReimport();
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (prefab == null)
            {
                Debug.LogError("[AssembleApartment] Model not found: " + ModelPath);
                return;
            }

            // Drop any previous assembly so re-runs are idempotent.
            var prev = GameObject.Find("Apartment");
            if (prev != null) Object.DestroyImmediate(prev);

            // Organized root + geometry container.
            var root = new GameObject("Apartment");
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "Apartamento_Geometry";
            inst.transform.SetParent(root.transform, false);
            inst.transform.position = Vector3.zero;
            inst.transform.rotation = Quaternion.identity;

            Bounds b = GetBounds(inst);
            if (b.size.sqrMagnitude < 0.0001f)
            {
                Debug.LogError("[AssembleApartment] Model has no measurable bounds.");
                return;
            }

            // Uniform rescale: footprint (larger XZ) -> TargetFootprint, but never push
            // the height past MaxHeight (protects against non-uniform source units).
            float largest = Mathf.Max(b.size.x, b.size.z);
            float scale = largest > 0.0001f ? TargetFootprint / largest : 1f;
            float scaledY = b.size.y * scale;
            if (scaledY > MaxHeight) scale = MaxHeight / b.size.y;
            inst.transform.localScale = Vector3.one * scale;

            b = GetBounds(inst);

            // Center on the origin in X/Z and place the floor at Y=0.
            // The model is added around (0,0,0) as requested, standing on the ground.
            inst.transform.position = new Vector3(-b.center.x, -b.min.y, -b.center.z);
            b = GetBounds(inst);

            // Add colliders so the walls/floor block a player (no fall-through / clips).
            AddColliders(inst);

            Debug.Log("[AssembleApartment] Apartment ready. Size: "
                + Vector3.Scale(b.size, Vector3.one).ToString("F2") + " m, at origin.");

            // Save the scene (existing camera/light preserved).
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            AddSceneToBuildSettings(ScenePath);
            Debug.Log("[AssembleApartment] Saved scene: " + ScenePath);
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

        private static void AddColliders(GameObject root)
        {
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
            {
                if (mr.gameObject.GetComponent<Collider>() != null) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                var mc = mr.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
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
