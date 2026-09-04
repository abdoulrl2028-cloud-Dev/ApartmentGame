using System.Collections;
using ApartmentAfterDark.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace ApartmentAfterDark.UI
{
    /// <summary>
    /// Shows subtitles on screen synced to a voice AudioClip. When playing a clip via
    /// AudioManager.PlayVoice, the subtitle is displayed while the clip plays.
    /// </summary>
    public class SubtitleManager : MonoBehaviour
    {
        [Tooltip("UI Text that displays the subtitle.")]
        [SerializeField] private Text subtitleText;

        [Tooltip("Default subtitle text shown while a voice clip is playing.")]
        [SerializeField] private string defaultText = "";

        private Coroutine activeCoroutine;

        private void Awake()
        {
            if (subtitleText != null)
                subtitleText.gameObject.SetActive(false);
        }

        /// <summary>Shows a subtitle and hides it after duration seconds.</summary>
        public void ShowSubtitle(string text, float duration)
        {
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            if (subtitleText != null)
            {
                subtitleText.text = text;
                subtitleText.gameObject.SetActive(true);
            }

            activeCoroutine = StartCoroutine(HideAfter(duration));
        }

        private IEnumerator HideAfter(float duration)
        {
            yield return new WaitForSecondsRealtime(duration);
            ClearSubtitle();
        }

        public void ClearSubtitle()
        {
            if (activeCoroutine != null)
            {
                StopCoroutine(activeCoroutine);
                activeCoroutine = null;
            }
            if (subtitleText != null)
            {
                subtitleText.text = string.Empty;
                subtitleText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Live subtitle driven by currently-playing voice clip
            if (AudioManager.Instance != null && AudioManager.Instance.VoicePlaying &&
                activeCoroutine == null)
            {
                ShowSubtitle(defaultText, -1f);
            }
            else if (AudioManager.Instance != null && !AudioManager.Instance.VoicePlaying &&
                     subtitleText != null && subtitleText.IsActive())
            {
                // Hide when voice ended but no explicit duration assigned
            }
        }
    }
}
