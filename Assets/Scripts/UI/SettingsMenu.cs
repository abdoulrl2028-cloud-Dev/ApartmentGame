using ApartmentAfterDark.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace ApartmentAfterDark.UI
{
    /// <summary>
    /// Settings menu: resolution, fullscreen, quality, master/ambient/music/sfx/voice
    /// sliders and look sensitivity. Persists via PlayerPrefs.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Canvas settingsCanvas;

        [Header("Resolution")]
        [SerializeField] private Dropdown resolutionDropdown;

        [Header("Fullscreen")]
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Quality")]
        [SerializeField] private Dropdown qualityDropdown;

        [Header("Volumes")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider ambientSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider voiceSlider;

        [Header("Sensitivity")]
        [SerializeField] private Slider sensitivitySlider;

        private Resolution[] resolutions;

        private void Awake()
        {
            if (settingsCanvas == null) settingsCanvas = GetComponent<Canvas>();
        }

        private void Start()
        {
            PopulateResolutions();
            PopulateQuality();

            // Load saved settings
            masterSlider.value = PlayerPrefs.GetFloat("masterVolume", -1f);
            musicSlider.value = PlayerPrefs.GetFloat("musicVolume", 0.5f);
            ambientSlider.value = PlayerPrefs.GetFloat("ambientVolume", 0.5f);
            sfxSlider.value = PlayerPrefs.GetFloat("sfxVolume", 1f);
            voiceSlider.value = PlayerPrefs.GetFloat("voiceVolume", 1f);
            sensitivitySlider.value = PlayerPrefs.GetFloat("sensitivity", 2f);
            fullscreenToggle.isOn = Screen.fullScreen;

            // Wire events
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (ambientSlider != null) ambientSlider.onValueChanged.AddListener(OnAmbientChanged);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (voiceSlider != null) voiceSlider.onValueChanged.AddListener(OnVoiceChanged);
            if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        }

        private void PopulateResolutions()
        {
            if (resolutionDropdown == null) return;
            resolutions = Screen.resolutions;
            resolutionDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            int current = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                options.Add($"{resolutions[i].width} x {resolutions[i].height}");
                if (resolutions[i].width == Screen.currentResolution.width &&
                    resolutions[i].height == Screen.currentResolution.height)
                    current = i;
            }
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = current;
            resolutionDropdown.RefreshShownValue();
        }

        private void PopulateQuality()
        {
            if (qualityDropdown == null) return;
            var names = QualitySettings.names;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(names));
            qualityDropdown.value = QualitySettings.GetQualityLevel();
            qualityDropdown.RefreshShownValue();
        }

        private void OnResolutionChanged(int index)
        {
            if (resolutions == null || index >= resolutions.Length) return;
            Resolution r = resolutions[index];
            Screen.SetResolution(r.width, r.height, Screen.fullScreen);
            PlayerPrefs.SetInt("resIndex", index);
        }

        private void OnFullscreenChanged(bool value)
        {
            Screen.fullScreen = value;
            PlayerPrefs.SetInt("fullscreen", value ? 1 : 0);
        }

        private void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index, true);
            PlayerPrefs.SetInt("quality", index);
        }

        private void OnMasterChanged(float value) => PlayerPrefs.SetFloat("masterVolume", value);
        private void OnMusicChanged(float value)
        {
            PlayerPrefs.SetFloat("musicVolume", value);
            if (AudioManager.Instance != null) AudioManager.Instance.SetMixerVolume("musicVol", value);
        }
        private void OnAmbientChanged(float value)
        {
            PlayerPrefs.SetFloat("ambientVolume", value);
            if (AudioManager.Instance != null) AudioManager.Instance.SetMixerVolume("ambientVol", value);
        }
        private void OnSfxChanged(float value)
        {
            PlayerPrefs.SetFloat("sfxVolume", value);
            if (AudioManager.Instance != null) AudioManager.Instance.SetMixerVolume("sfxVol", value);
        }
        private void OnVoiceChanged(float value)
        {
            PlayerPrefs.SetFloat("voiceVolume", value);
            if (AudioManager.Instance != null) AudioManager.Instance.SetMixerVolume("voiceVol", value);
        }
        private void OnSensitivityChanged(float value)
        {
            PlayerPrefs.SetFloat("sensitivity", value);
        }

        public void Open() { if (settingsCanvas != null) settingsCanvas.gameObject.SetActive(true); }
        public void Close() { if (settingsCanvas != null) settingsCanvas.gameObject.SetActive(false); PlayerPrefs.Save(); }
    }
}
