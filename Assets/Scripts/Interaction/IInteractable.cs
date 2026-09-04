using UnityEngine;

namespace ApartmentAfterDark.Interaction
{
    /// <summary>
    /// Interface implementada por objetos que podem ser interagidos com a tecla E.
    /// Objetos podem implementar <see cref="MonoBehaviour"/> ou não.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Nome exibido ao jogador (ex.: "Porta da frente").</summary>
        string InteractableName { get; }

        /// <summary>Mensagem do prompt (ex.: "Pressione E para abrir").</summary>
        string InteractionPrompt { get; }

        /// <summary>Distância máxima de interação em metros (0 = usar padrão do sistema).</summary>
        float InteractionRange { get; }

        /// <summary>Chamado quando o jogador pressiona E sobre o objeto.</summary>
        void OnInteract(GameObject interactor);

        /// <summary>Se o objeto está atualmente disponível para interação.</summary>
        bool IsInteractable { get; }
    }
}
