using System;
using ApartmentAfterDark.Core;
using UnityEngine;
using UnityEngine.Events;

namespace ApartmentAfterDark.Interaction
{
    /// <summary>
    /// Detecta objetos interativos na frente do jogador via Raycast e dispara a interação.
    /// A mensagem na tela é um <see cref="UnityEvent"/> para que a UI possa se conectar
    /// sem que este script conheça a UI.
    ///
    /// Colocar em uma câmera (ou no objeto vazio que representa os olhos do jogador).
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        [Header("Configuração")]
        [Tooltip("Distância máxima do raycast de interação em metros.")]
        [SerializeField] private float interactionRange = 3f;

        [Tooltip("Camada dos objetos interativos. -1 (Everything) detecta tudo.")]
        [SerializeField] private LayerMask interactionMask = ~0;

        [Tooltip("Origem do raycast. Se vazio, usa a posição deste transform.")]
        [SerializeField] private Transform rayOrigin;

        [Header("Referência (opcional)")]
        [Tooltip("A referência ao jogador, passada à interface ao interagir. Se vazio, usa este gameObject.")]
        [SerializeField] private GameObject playerObject;

        [Header("Eventos de UI")]
        [Tooltip("Disparado com o objeto focalizado atualmente (null quando nenhum).")]
        public UnityEvent<IInteractable> OnFocusChanged;

        [Tooltip("Disparado quando o jogador interage com sucesso.")]
        public UnityEvent<IInteractable> OnInteracted;

        private IInteractable _current;
        private IInteractable _lastFrame;

        public IInteractable CurrentInteractable => _current;
        public bool HasTarget => _current != null;

        private UnityEngine.Camera _cam;
        private Vector3 Origin => rayOrigin != null ? rayOrigin.position : transform.position;
        private Vector3 Direction => rayOrigin != null ? rayOrigin.forward : transform.forward;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.CurrentState.AllowsPlayerControl())
            {
                if (_current != null)
                {
                    _current = null;
                    OnFocusChanged?.Invoke(null);
                }
                return;
            }

            UpdateTarget();
            TryInteract();
        }

        private void UpdateTarget()
        {
            _current = null;

            var origin = Origin;
            var direction = Direction;

            // Raycast principal contra a máscara.
            if (Physics.Raycast(origin, direction, out RaycastHit hit, interactionRange, interactionMask))
            {
                var interactable = hit.collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.IsInteractable)
                {
                    _current = interactable;
                }
            }

            if (_current != _lastFrame)
            {
                _lastFrame = _current;
                OnFocusChanged?.Invoke(_current);
            }
        }

        private void TryInteract()
        {
            if (_current == null)
                return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                _current.OnInteract(playerObject != null ? playerObject : gameObject);
                OnInteracted?.Invoke(_current);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(Origin, Direction * interactionRange);
        }
    }
}
