using ApartmentAfterDark.Audio;
using ApartmentAfterDark.Camera;
using ApartmentAfterDark.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ApartmentAfterDark
{
    /// <summary>
    /// Runtime bootstrap: if the scene lacks essential objects (player, camera, audio
    /// manager, event system, basic light), this creates them procedurally so the game
    /// can always run. Place on any persistent object or let an editor tool create it.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("If a player is not found in the scene at Start, create one.")]
        [SerializeField] private bool autoCreatePlayer = true;

        [Tooltip("Player spawn position when auto-created.")]
        [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 1f, 0f);

        private void Start()
        {
            EnsureEventSystem();
            EnsureAudioManager();
            EnsureLight();
            if (autoCreatePlayer && FindObjectOfType<PlayerController>() == null)
                CreatePlayer();
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null) return;

            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            StandaloneInputModule module = es.AddComponent<StandaloneInputModule>();
            module.horizontalAxis = "Horizontal";
            module.verticalAxis = "Vertical";
            module.submitButton = "Submit";
        }

        private void EnsureAudioManager()
        {
            if (AudioManager.Instance == null)
            {
                GameObject go = new GameObject("AudioManager");
                go.AddComponent<AudioManager>();
            }
        }

        private void EnsureLight()
        {
            if (FindObjectOfType<Light>() != null) return;

            GameObject lightGo = new GameObject("MainDirectionalLight");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            lightGo.tag = "Untagged";
        }

        private void CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.tag = "Player";

            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            player.transform.position = spawnPosition;

            player.AddComponent<PlayerController>();
            player.AddComponent<PlayerHealth>();
            player.AddComponent<FootstepController>();

            // Camera as child
            GameObject camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            UnityEngine.Camera cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PlayerCameraController>().enabled = true;

            // Interaction system on camera uses camera forward
            camGo.AddComponent<Interaction.InteractionSystem>();
        }
    }
}
