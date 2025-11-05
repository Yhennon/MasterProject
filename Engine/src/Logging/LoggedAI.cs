using System;
using System.Collections.Generic;
using ScriptsOfTribute.Board;        // PatronId, Move, PlayerEnum, etc.
using ScriptsOfTribute.Serializers;  // GameState, EndGameState, FullGameState
using ScriptsOfTribute.Engine.Logging;

namespace ScriptsOfTribute.AI
{
    public sealed class LoggedAI : AI
    {
        private readonly AI _inner;
        private readonly JsonlLogger _logger;
        private readonly string _gameId;
        private readonly int _playerId;
        private int _turnIndex;

        public LoggedAI(AI inner, JsonlLogger logger, string gameId, int playerId)
        {
            _inner = inner;
            _logger = logger;
            _gameId = gameId;
            _playerId = playerId;
            _turnIndex = 0;
        }

        // Matches AI.cs
        public override PatronId SelectPatron(List<PatronId> availablePatrons, int round)
        {
            return _inner.SelectPatron(availablePatrons, round);
        }

        // Matches AI.cs: Play(GameState gameState, List<Move> possibleMoves, TimeSpan remainingTime)
        public override Move Play(GameState gameState, List<Move> possibleMoves, TimeSpan remainingTime)
        {
            // 1) full observable snapshot
            var obs = gameState.SerializeGameState();  // JObject

            // 2) legal actions from possibleMoves (for masking later)
            var legal = new List<string>(possibleMoves.Count);
            foreach (var m in possibleMoves)
                legal.Add(MoveToStableString(m));

            // 3) delegate to the real bot
            var move = _inner.Play(gameState, possibleMoves, remainingTime);

            // 4) chosen action
            var chosen = MoveToStableString(move);

            // 5) log
            _logger.Write(new TurnLog
            {
                GameId = _gameId,
                TurnIndex = _turnIndex++,
                PlayerId = _playerId,
                State = obs,
                LegalActions = legal,
                ActionTaken = chosen,
                Done = false
            });

            return move;
        }

        // Matches AI.cs: GameEnd(EndGameState state, FullGameState? finalBoardState)
        public override void GameEnd(EndGameState state, FullGameState? finalBoardState)
        {
            _inner.GameEnd(state, finalBoardState);

            int? winnerId = null;
            // Map PlayerEnum -> 0/1 (or leave null if no winner)
            if (state != null && state.Winner != PlayerEnum.NO_PLAYER_SELECTED)
            {
                // Adjust if your enum uses different names; this matches typical values.
                winnerId = state.Winner == PlayerEnum.PLAYER1 ? 0
                          : state.Winner == PlayerEnum.PLAYER2 ? 1
                          : (int?)null;
            }

            _logger.Write(new GameSummary
            {
                GameId = _gameId,
                WinnerPlayerId = winnerId,
                NumTurns = _turnIndex
                // Add scores/seed later if you want—they’re not required for training.
            });
        }

        // Build a stable string for the move using fields present in Move.cs
        private static string MoveToStableString(Move m)
        {
            // Minimum: Command + UniqueId is available and stable.
            // If Move exposes more (e.g., m.Card?.Id or patron info), you can append them.
            return $"{m.Command}#{m.UniqueId}";
        }
    }
}
