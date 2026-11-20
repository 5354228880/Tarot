using System;
using UnityEngine;
using Tarot.Core;

namespace Tarot.Game
{
    public sealed class GameSession : MonoBehaviour
    {
        static GameSession s_Instance;

        public static GameSession Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var go = new GameObject("GameSession");
                    s_Instance = go.AddComponent<GameSession>();
                }
                return s_Instance;
            }
        }

        public RunState Run;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void EnsureRun()
        {
            if (Run == null || Run.IsComplete)
            {
                Run = new RunState(CardCatalog.StartingDeck(), Environment.TickCount);
            }
        }

        public int CurrentLevel => Run != null ? Run.LevelIndex : 0;
        public bool IsComplete => Run != null && Run.IsComplete;

        public void AdvanceLevel()
        {
            if (Run != null) Run.LevelIndex++;
        }

        public void SaveCombat(CombatState state)
        {
            if (Run != null) CombatEngine.SaveToRun(state, Run);
        }

        public void DiscardRun()
        {
            Run = null;
        }
    }
}
