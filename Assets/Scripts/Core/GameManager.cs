using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApartmentAfterDark.Core
{
    /// <summary>
    /// Gerencia o estado global do jogo: início, pausa, configurações e carga de cenas.
    /// É um singleton acessível via <see cref="GameManager.Instance"/>.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Startup")]
        [Tooltip("Cena carregada automaticamente no Start, se nenhuma já estiver configurada.")]
        [SerializeField] private string startSceneName = "ApartmentScene";

        [Tooltip("Se ativo, bloqueia o cursor do mouse durante o jogo.")]
        [SerializeField] private bool lockCursorOnPlay = true;

        public event Action<GameState> OnGameStateChanged;
        public event Action OnGamePaused;
        public event Action OnGameResumed;

        public GameState CurrentState { get; private set; } = GameState.Boot;

        /// <summary>True quando o jogo está pausado (menus bloqueando o jogador).</summary>
        public bool IsPaused => CurrentState.PausesTime();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // If a MainMenu scene is configured, go to menu; otherwise start playing directly
            // so the apartment scene is immediately playable.
            bool hasMenuScene = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string p = SceneUtility.GetScenePathByBuildIndex(i);
                if (p.EndsWith("MainMenu.unity") || p.EndsWith("mainmenu.unity"))
                {
                    hasMenuScene = true;
                    break;
                }
            }

            if (hasMenuScene)
            {
                SetState(GameState.MainMenu);
                LoadScene(startSceneName);
            }
            else
            {
                SetState(GameState.Playing);
                LoadScene(startSceneName);
            }
        }

        /// <summary>
        /// Define um novo estado global e notifica os ouvintes.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (CurrentState == newState)
                return;

            CurrentState = newState;
            Time.timeScale = newState.PausesTime() ? 0f : 1f;

            if (lockCursorOnPlay)
                Cursor.visible = !newState.AllowsPlayerControl();

            OnGameStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Carrega uma cena pelo nome. É async e segura para usar a qualquer momento.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadSceneAsync(sceneName);
        }

        /// <summary>
        /// Pausa o jogo (Time.timeScale = 0).
        /// </summary>
        public void PauseGame()
        {
            if (CurrentState == GameState.Playing)
            {
                SetState(GameState.Paused);
                OnGamePaused?.Invoke();
            }
        }

        /// <summary>
        /// Retoma o jogo (Time.timeScale = 1).
        /// </summary>
        public void ResumeGame()
        {
            if (CurrentState == GameState.Paused)
            {
                SetState(GameState.Playing);
                OnGameResumed?.Invoke();
            }
        }

        /// <summary>
        /// Inicia o jogo (chamado pelo menu principal).
        /// </summary>
        public void StartGame()
        {
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Sai da aplicação (ou para o Play no editor).
        /// </summary>
        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }
}
