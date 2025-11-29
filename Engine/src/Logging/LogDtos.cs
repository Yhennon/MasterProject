using System.Collections.Generic;

namespace ScriptsOfTribute.Engine.Logging
{
    public sealed class TurnLog
    {
        public string GameId { get; set; } = "";
        public int TurnIndex { get; set; }
        public int PlayerId { get; set; }

        // Serialized JObject from GameState.SerializeGameState()
        public object? State { get; set; }

        public List<string> LegalActions { get; set; } = new List<string>();

        public string ActionTaken { get; set; } = "";

        // --- NEW: diagnostic fields for measuring bounds ---

        // How many cards the current player has in hand at this decision.
        public int HandSize { get; set; }

        // Sizes of some important zones (but these are described in game desc).
        public int DrawPileSize { get; set; }
        public int CooldownPileSize { get; set; }

        // How many agents the current player has on the board.
        public int AgentsCount { get; set; }

        // How many cards are currently available in the tavern.
        public int TavernCount { get; set; }

        // Total number of legal moves in this state.
        public int NumLegalActions { get; set; }

        // If there is a pending choice, how many options does it offer?
        // (0 if no choice is pending.)
        public int PendingChoiceOptions { get; set; }

        // --- existing fields ---
        public bool Done { get; set; }
        public double? Reward { get; set; }
    }

    public sealed class GameSummary
    {
        public string GameId { get; set; } = "";
        public bool Summary { get; set; } = true;

        // <<< IMPORTANT: nullable int — winner may be "no player selected"
        public int? WinnerPlayerId { get; set; }

        public int NumTurns { get; set; }
        public int? FinalScoreP0 { get; set; }
        public int? FinalScoreP1 { get; set; }
        public long? Seed { get; set; }
    }
}
