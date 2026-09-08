using System;
using UnityEngine;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

namespace UrbanWildlife.Cycle
{
    [RequireComponent(typeof(LayoutPacketReader), typeof(P0ConstraintManager))]
    public sealed class P0CycleController : MonoBehaviour
    {
        [SerializeField]
        private float runSeconds = 60f;

        [SerializeField]
        private float observeSeconds = 30f;

        [SerializeField]
        private int cyclesPerSession = 3;

        private LayoutPacketReader reader;
        private P0ConstraintManager constraintManager;
        private P0CycleStateMachine state;

        public event Action<P0Phase> PhaseChanged;
        public P0Phase Phase => state.Phase;
        public int CycleIndex => state.CycleIndex;
        public int CyclesPerSession => cyclesPerSession;
        public float RemainingSeconds => state.RemainingSeconds;
        public bool CanStartRun => state.CanStartRun;

        private void Awake()
        {
            reader = GetComponent<LayoutPacketReader>();
            constraintManager = GetComponent<P0ConstraintManager>();
            state = new P0CycleStateMachine(runSeconds, observeSeconds, cyclesPerSession);
            state.PhaseChanged += OnPhaseChanged;
        }

        private void OnEnable()
        {
            if (constraintManager == null)
            {
                constraintManager = GetComponent<P0ConstraintManager>();
            }

            constraintManager.ConstraintsEvaluated += OnConstraintsEvaluated;
        }

        private void OnDisable()
        {
            if (constraintManager != null)
            {
                constraintManager.ConstraintsEvaluated -= OnConstraintsEvaluated;
            }
        }

        private void Update()
        {
            state.Tick(Time.deltaTime);
        }

        [ContextMenu("Confirm Current Plan")]
        public void ConfirmCurrentPlan()
        {
            if (!state.TryEnterConfirm())
            {
                Debug.LogWarning($"Cannot confirm while phase is {state.Phase}.", this);
                return;
            }

            if (!reader.TryLoadLatest(out _, out string error))
            {
                state.ResolveConfirm(false);
                Debug.LogWarning($"Confirm failed: {error}", this);
            }
        }

        [ContextMenu("Start Run")]
        public void StartRun()
        {
            if (!state.TryStartRun())
            {
                Debug.LogWarning("Run cannot start until the confirmed plan passes every P0 constraint.", this);
            }
        }

        [ContextMenu("Reset Session")]
        public void ResetSession()
        {
            state.ResetSession();
        }

        private void OnConstraintsEvaluated(P0ConstraintResult result)
        {
            state.ResolveConfirm(result.all_constraints_satisfied);
            if (!result.all_constraints_satisfied)
            {
                Debug.Log("Plan remains open because at least one P0 constraint failed.", this);
            }
        }

        private void OnPhaseChanged(P0Phase next)
        {
            PhaseChanged?.Invoke(next);
            Debug.Log($"P0 phase -> {next}; cycle={state.CycleIndex}; remaining={state.RemainingSeconds:F1}s", this);
        }
    }
}
