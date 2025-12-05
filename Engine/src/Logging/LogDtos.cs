using System.Collections.Generic;

namespace ScriptsOfTribute.Engine.Logging
{
    public sealed class TurnLog
    {
        public string GameId { get; set; } = "";
        public int TurnIndex { get; set; }
        public int PlayerId { get; set; }

        // full observable snapshot
        public object? State { get; set; }

        // Old string-based moves (can keep for debugging, but im gonna use slot-based moves for NN input)
        public List<string> LegalActions { get; set; } = new List<string>();
        public string ActionTaken { get; set; } = "";

        // Global index of the chosen action in [0..N_ACTIONS-1]
        public int? ChosenActionIndex { get; set; }

        // Indices of legal actions in [0..N_ACTIONS-1]
        public List<int> LegalActionIndices { get; set; } = new List<int>();

        public bool IndexerFailedForChosen { get; set; }         // true if chosen move had no index
        public List<string> UnmappedLegalActions { get; set; } = new(); // legal moves that got idx < 0
        public string Command { get; set; } = ""; 

        // Diagnostic fields - can be used for RL training analysis maybe?
        public int HandSize { get; set; }
        public int DrawPileSize { get; set; }
        public int CooldownPileSize { get; set; }
        public int AgentsCount { get; set; }
        public int TavernCount { get; set; }
        public int NumLegalActions { get; set; }
        public int PendingChoiceOptions { get; set; }

        // RL-ish fields
        public bool Done { get; set; } // true if game over after this turn? or true if turn is over?
        public double? Reward { get; set; }
    }

    public sealed class GameSummary
    {
        public string GameId { get; set; } = "";
        public bool Summary { get; set; } = true;

        //IMPORTANT: nullable int — winner may be "no player selected"
        public int? WinnerPlayerId { get; set; }

        public int NumTurns { get; set; }
        public int? FinalScoreP0 { get; set; }
        public int? FinalScoreP1 { get; set; }
        public long? Seed { get; set; }
    }
}
