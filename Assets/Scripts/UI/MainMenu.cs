using ApartmentAfterDark.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ApartmentAfterDark.UI
{
    /// <summary>
    /// Main menu controller. Wire up Buttons (Start/Quit) in the Inspector, or let the
    /// scene builder auto-connect them by GameObject name.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Tooltip("Button that starts the game.")]
        [SerializeField] private Button startButton;

        [Tooltip("Button that quits the application.")]
        [SerializeField] private Button quitButton;

        [Tooltip("Canvas to hide when the game starts.")]
        [SerializeField] private Canvas menuCanvas;

        private void Awake()
        {
            if (menuCanvas == null) menuCanvas = GetComponent<Canvas>();
        }

        private void Start()
        {
            TryFindButtons();
            if (startButton != null) startButton.onClick.AddListener(StartGame);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void TryFindButtons()
        {
            if (menuCanvas == null) return;
            if (startButton == null)
                startButton = menuCanvas.transform.Find("StartButton")?.GetComponent<Button>();
            if (quitButton == null)
                quitButton = menuCanvas.transform.Find("QuitButton")?.GetComponent<Button>();
        }

        private void StartGame()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartGame();

            if (menuCanvas != null) menuCanvas.gameObject.SetActive(false);
        }

        private void QuitGame()
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            else Application.Quit();
        }
    }
}
