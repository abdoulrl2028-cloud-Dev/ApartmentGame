using UnityEngine;
using UnityEngine.Events;

namespace ApartmentAfterDark.Interaction
{
    /// <summary>
    /// Base reutilizável para objetos interativos (portas, luzes, televisão, móveis...).
    /// Apenas adicione uma subclasse ou um script derivado e conecte os eventos.
    /// </summary>
    public abstract class InteractableBase : MonoBehaviour, IInteractable
    {
        [Tooltip("Nome exibido ao jogador.")]
        [SerializeField] protected string interactableName = "Objeto";

        [Tooltip("Mensagem do prompt.")]
        [SerializeField] protected string interactionPrompt = "Pressione E para interagir";

        [Tooltip("Distância máxima de interação em metros. 0 = usar padrão do sistema.")]
        [SerializeField] protected float interactionRange = 0f;

        [Tooltip("Se o objeto está disponível para interação.")]
        [SerializeField] protected bool isInteractable = true;

        [Tooltip("Evento disparado ao interagir. Permite ligar qualquer comportamento no Inspector.")]
        public UnityEvent<GameObject> onInteract;

        public string InteractableName => interactableName;
        public string InteractionPrompt => interactionPrompt;
        public float InteractionRange => interactionRange;
        public bool IsInteractable => isInteractable;

        public virtual void OnInteract(GameObject interactor)
        {
            onInteract?.Invoke(interactor);
        }

        /// <summary>
        /// Editor lifecycle message: Unity calls this when a component is first added
        /// or when Reset is invoked. Declared virtual so subclasses can override it
        /// to set sensible defaults without the (non-virtual) base message being hidden.
        /// </summary>
        protected virtual void Reset()
        {
            // Defaults for interactableName/interactionPrompt are set on fields.
        }

        protected void SetInteractable(bool value) => isInteractable = value;
    }
}
