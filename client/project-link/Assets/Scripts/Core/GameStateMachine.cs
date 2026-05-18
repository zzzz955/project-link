using System;
using System.Collections.Generic;

namespace ProjectLink.Core
{
    public class GameStateMachine
    {
        public GameState Current { get; private set; } = GameState.Idle;

        public event Action<GameState, GameState> OnStateChanged;

        static readonly HashSet<(GameState, GameState)> _validTransitions = new()
        {
            (GameState.Idle,    GameState.Drawing),
            (GameState.Idle,    GameState.Completed),
            (GameState.Drawing, GameState.Idle),
            (GameState.Drawing, GameState.Completed),
        };

        public bool TryTransition(GameState next)
        {
            if (!_validTransitions.Contains((Current, next))) return false;
            var prev = Current;
            Current = next;
            OnStateChanged?.Invoke(prev, next);
            return true;
        }

        public void ForceTransition(GameState next)
        {
            if (!TryTransition(next))
                throw new InvalidOperationException($"Invalid transition: {Current} → {next}");
        }
    }
}
