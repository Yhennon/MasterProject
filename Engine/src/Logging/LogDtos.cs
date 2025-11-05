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

        // <<< IMPORTANT: strings, not ints
        public List<string> LegalActions { get; set; } = new List<string>();

        // <<< IMPORTANT: string, not int
        public string ActionTaken { get; set; } = "";

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
