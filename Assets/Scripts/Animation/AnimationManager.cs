using System.Collections.Generic;
using UnityEngine;

namespace ApartmentAfterDark.Animation
{
    /// <summary>
    /// Helper for driving an Animator with cached parameter hashes and one-shot
    /// transitions (e.g. Idle, Walk, Run, Interact). Call SetParam/PlayCrossfade by name.
    /// </summary>
    public class AnimationManager : MonoBehaviour
    {
        [Tooltip("Animator driven by this manager. Auto-detected if empty.")]
        [SerializeField] private Animator animator;

        [Tooltip("State names for crossfade (collapsed for clarity).")]
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string walkState = "Walk";
        [SerializeField] private string runState = "Run";

        private readonly Dictionary<int, int> paramCache = new Dictionary<int, int>();
        private readonly Dictionary<int, int> stateCache = new Dictionary<int, int>();

        public Animator Animator => animator;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        /// <summary>Sets a float parameter (cached hash).</summary>
        public void SetFloat(string name, float value)
        {
            if (animator == null || !animator.isActiveAndEnabled) return;
            int hash = HashParam(name);
            animator.SetFloat(hash, value);
        }

        public void SetBool(string name, bool value)
        {
            if (animator == null) return;
            animator.SetBool(HashParam(name), value);
        }

        public void SetTrigger(string name)
        {
            if (animator == null) return;
            animator.SetTrigger(HashParam(name));
        }

        /// <summary>Crossfades into a state by name with a transition duration.</summary>
        public void GoTo(string stateName, float transitionDuration = 0.2f)
        {
            if (animator == null) return;
            int hash = HashState(stateName);
            animator.CrossFadeInFixedTime(hash, transitionDuration);
        }

        public void PlayIdle(float duration = 0.2f) => GoTo(idleState, duration);
        public void PlayWalk(float duration = 0.2f) => GoTo(walkState, duration);
        public void PlayRun(float duration = 0.2f) => GoTo(runState, duration);

        private int HashParam(string name)
        {
            int key = Animator.StringToHash(name);
            paramCache[key] = key;
            return key;
        }

        private int HashState(string name)
        {
            int key = Animator.StringToHash(name);
            stateCache[key] = key;
            return key;
        }
    }
}
