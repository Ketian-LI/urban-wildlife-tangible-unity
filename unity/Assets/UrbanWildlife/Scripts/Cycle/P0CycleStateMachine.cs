using System;

namespace UrbanWildlife.Cycle
{
    public enum P0Phase
    {
        Plan,
        Confirm,
        Run,
        Observe,
        Complete,
    }

    public sealed class P0CycleStateMachine
    {
        private readonly float runSeconds;
        private readonly float observeSeconds;
        private readonly int cyclesPerSession;
        private bool confirmApproved;

        public P0CycleStateMachine(float runSeconds, float observeSeconds, int cyclesPerSession)
        {
            if (runSeconds <= 0f || observeSeconds <= 0f || cyclesPerSession <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(runSeconds), "Positive phase durations and cycle count are required.");
            }

            this.runSeconds = runSeconds;
            this.observeSeconds = observeSeconds;
            this.cyclesPerSession = cyclesPerSession;
            ResetSession();
        }

        public P0Phase Phase { get; private set; }
        public int CycleIndex { get; private set; }
        public float RemainingSeconds { get; private set; }
        public bool CanStartRun => Phase == P0Phase.Confirm && confirmApproved;

        public event Action<P0Phase> PhaseChanged;

        public bool TryEnterConfirm()
        {
            if (Phase != P0Phase.Plan)
            {
                return false;
            }

            confirmApproved = false;
            SetPhase(P0Phase.Confirm, 0f);
            return true;
        }

        public void ResolveConfirm(bool approved)
        {
            if (Phase != P0Phase.Confirm)
            {
                return;
            }

            confirmApproved = approved;
            if (!approved)
            {
                SetPhase(P0Phase.Plan, 0f);
            }
        }

        public bool TryStartRun()
        {
            if (!CanStartRun)
            {
                return false;
            }

            confirmApproved = false;
            SetPhase(P0Phase.Run, runSeconds);
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || (Phase != P0Phase.Run && Phase != P0Phase.Observe))
            {
                return;
            }

            float unused = deltaSeconds;
            while (unused > 0f && (Phase == P0Phase.Run || Phase == P0Phase.Observe))
            {
                if (unused < RemainingSeconds)
                {
                    RemainingSeconds -= unused;
                    return;
                }

                unused -= RemainingSeconds;
                if (Phase == P0Phase.Run)
                {
                    SetPhase(P0Phase.Observe, observeSeconds);
                }
                else
                {
                    CycleIndex += 1;
                    SetPhase(
                        CycleIndex >= cyclesPerSession ? P0Phase.Complete : P0Phase.Plan,
                        0f);
                }
            }
        }

        public void ResetSession()
        {
            CycleIndex = 0;
            confirmApproved = false;
            SetPhase(P0Phase.Plan, 0f);
        }

        private void SetPhase(P0Phase next, float remainingSeconds)
        {
            bool changed = Phase != next;
            Phase = next;
            RemainingSeconds = remainingSeconds;
            if (changed)
            {
                PhaseChanged?.Invoke(Phase);
            }
        }
    }
}
