using UnityEngine;

namespace ApartmentAfterDark.Core
{
    /// <summary>
    /// Estados globais do jogo.
    /// </summary>
    public enum GameState
    {
        Boot,        // inicialização
        MainMenu,    // menu principal
        Playing,     // jogando
        Paused,      // pausado (menu de pausa)
        Dialogue,    // diálogo/legendado
        Loading,     // carregando cena
        GameOver     // fim de jogo
    }

    public static class GameStateExtensions
    {
        /// <summary>
        /// Indica se, neste estado, o jogador tem controle livre do personagem.
        /// </summary>
        public static bool AllowsPlayerControl(this GameState state)
        {
            return state == GameState.Playing;
        }

        /// <summary>
        /// Indica se o jogo deve pausar (Time.timeScale == 0).
        /// </summary>
        public static bool PausesTime(this GameState state)
        {
            return state == GameState.Paused ||
                   state == GameState.MainMenu ||
                   state == GameState.Loading ||
                   state == GameState.GameOver;
        }
    }
}
