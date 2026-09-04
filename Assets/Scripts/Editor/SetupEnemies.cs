#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ApartmentAfterDark.EditorTools
{
    /// <summary>
    /// Spawns patrol enemies inside the apartment scene:
    ///   - Creates an "Enemies" container under the ApartmentGame root.
    ///   - Builds each enemy from primitives (dark body + glowing eyes) with a
    ///     CharacterController + EnemyController and a set of patrol waypoints.
    ///   - Saves the scene to Assets/Scenes/ApartmentGame.unity.
    ///
    /// Access: Tools > Game > Setup Enemies in Apartment
    /// </summary>
    public static class SetupEnemies
    {
        private const string ScenePath = "Assets/Scenes/ApartmentGame.unity";
        private const string MaterialsDir = "Assets/Environment/Apartment/Materials";

        // Enemy spawn points + patrol waypoints, all just above the floor (y=0).
        private static readonly Vector3[] EnemySpawns =
        {
            new Vector3(3f, 1.05f, -2f),
            new Vector3(-4f, 1.05f, 3f),
            new Vector3(1f, 1.05f, -5f),
        };

        private static readonly Vector3[][] PatrolLoops =
        {
            new[] { new Vector3(3f, 1.05f, -2f), new Vector3(5f, 1.05f, 2f), new Vector3(0f, 1.05f, 0f) },
            new[] { new Vector3(-4f, 1.05f, 3f), new Vector3(-2f, 1.05f, -1f), new Vector3(-6f, 1.05f, 4f) },
            new[] { new Vector3(1f, 1.05f, -5f), new Vector3(-1f, 1.05f, -2f), new Vector3(4f, 1.05f, -6f) },
        };

        [MenuItem("Tools/Game/Setup Enemies in Apartment")]
        public static void Setup()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            GameObject root = GameObject.Find("ApartmentGame");
            if (root == null) root = new GameObject("ApartmentGame");

            var enemiesRoot = root.transform.Find("Enemies")?.gameObject;
            if (enemiesRoot == null)
            {
                enemiesRoot = new GameObject("Enemies");
                enemiesRoot.transform.SetParent(root.transform, false);
            }

            var bodyMat = EnsureMaterial("Enemy_Body.mat", new Color(0.06f, 0.06f, 0.08f));
            var eyeMat = EnsureMaterial("Enemy_Eye.mat", new Color(1f, 0.1f, 0.05f));

            for (int i = 0; i < EnemySpawns.Length; i++)
            {
                CreateEnemy(enemiesRoot.transform, "Enemy_" + (i + 1), EnemySpawns[i], PatrolLoops[i], bodyMat, eyeMat);
            }

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log("[SetupEnemies] Spawned " + EnemySpawns.Length + " enemies. Scene saved -> " + ScenePath);
        }

        private static void CreateEnemy(Transform parent, string name, Vector3 spawn,
            Vector3[] loop, Material bodyMat, Material eyeMat)
        {
            var enemy = new GameObject(name);
            enemy.transform.SetParent(parent, false);
            enemy.transform.position = spawn;

            var cc = enemy.AddComponent<CharacterController>();
            cc.height = 1.6f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.8f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(enemy.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            body.transform.localScale = new Vector3(0.55f, 1.1f, 0.55f);
            body.transform.localRotation = Quaternion.identity;
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;
            RemoveAutoCollider(body);

            // Two glowing eyes.
            var left = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            left.name = "EyeL";
            left.transform.SetParent(enemy.transform, false);
            left.transform.localPosition = new Vector3(-0.16f, 1.35f, 0.34f);
            left.transform.localScale = new Vector3(0.18f, 0.18f, 0.12f);
            left.GetComponent<Renderer>().sharedMaterial = eyeMat;
            RemoveAutoCollider(left);

            var right = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            right.name = "EyeR";
            right.transform.SetParent(enemy.transform, false);
            right.transform.localPosition = new Vector3(0.16f, 1.35f, 0.34f);
            right.transform.localScale = new Vector3(0.18f, 0.18f, 0.12f);
            right.GetComponent<Renderer>().sharedMaterial = eyeMat;
            RemoveAutoCollider(right);

            // Waypoints as empty children so they move with the enemy.
            var waypoints = new Transform[loop.Length];
            for (int w = 0; w < loop.Length; w++)
            {
                var wp = new GameObject("Waypoint_" + (w + 1));
                wp.transform.SetParent(enemy.transform, false);
                wp.transform.position = loop[w];
                waypoints[w] = wp.transform;
            }

            var enemyCtrl = enemy.AddComponent<ApartmentAfterDark.Characters.EnemyController>();
            var so = new SerializedObject(enemyCtrl);
            var wpProp = so.FindProperty("waypoints");
            if (wpProp != null)
            {
                wpProp.arraySize = waypoints.Length;
                for (int w = 0; w < waypoints.Length; w++)
                    wpProp.GetArrayElementAtIndex(w).objectReferenceValue = waypoints[w];
            }
            var bodyProp = so.FindProperty("bodyRenderer");
            if (bodyProp != null)
                bodyProp.objectReferenceValue = body.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveAutoCollider(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        private static Material EnsureMaterial(string fileName, Color color)
        {
            string path = MaterialsDir + "/" + fileName;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Glossiness", 0.2f);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == path))
                list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
