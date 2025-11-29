using System;
using System.Collections.Generic;
using ScriptsOfTribute.Board;
using ScriptsOfTribute.Serializers;
using ScriptsOfTribute.Engine.Logging;
using ScriptsOfTribute.Board.CardAction;

namespace ScriptsOfTribute.AI
{
    public sealed class LoggedAI : AI
    {
        private readonly AI _inner;
        private readonly JsonlLogger _logger;
        private readonly string _gameId;
        private readonly int _playerId;
        private int _turnIndex;
        private readonly ulong? _seed;

        public LoggedAI(AI inner, JsonlLogger logger, string gameId, int playerId, ulong? seed = null)
        {
            _inner = inner;
            _logger = logger;
            _gameId = gameId;
            _playerId = playerId;
            _turnIndex = 0;
            _seed = seed;
        }

        public override PatronId SelectPatron(List<PatronId> availablePatrons, int round)
        {
            return _inner.SelectPatron(availablePatrons, round);
        }

        public override Move Play(GameState gameState, List<Move> possibleMoves, TimeSpan remainingTime)
        {
            // --- DIAGNOSTIC MEASUREMENT MODE ---

            // 1) Compute the stats i care about
            var current = gameState.CurrentPlayer;

            int handSize = current.Hand.Count;
            int drawPileSize = current.DrawPile.Count;
            int cooldownSize = current.CooldownPile.Count;
            int agentsCount = current.Agents.Count;
            int tavernCount = gameState.TavernAvailableCards.Count;
            int numLegalActions = possibleMoves.Count;

            int pendingChoiceOptions = 0;
            if (gameState.PendingChoice is { } choice)
            {
                if (choice.Type == Choice.DataType.CARD)
                    pendingChoiceOptions = choice.PossibleCards.Count;
                else if (choice.Type == Choice.DataType.EFFECT)
                    pendingChoiceOptions = choice.PossibleEffects.Count;
            }

            // 2) Delegate to the real bot
            var move = _inner.Play(gameState, possibleMoves, remainingTime);

            // 3) For measurement only, can skip heavy fields :
            _logger.Write(new TurnLog
            {
                GameId   = _gameId,
                TurnIndex = _turnIndex++,
                PlayerId  = _playerId,

                // Skip state serialization for speed:
                State        = null,

                // don't need to move strings for max-stat measurement:
                LegalActions = new List<string>(),
                ActionTaken  = string.Empty,

                HandSize         = handSize,
                DrawPileSize     = drawPileSize,
                CooldownPileSize = cooldownSize,
                AgentsCount      = agentsCount,
                TavernCount      = tavernCount,
                NumLegalActions  = numLegalActions,
                PendingChoiceOptions = pendingChoiceOptions,

                Done   = false,
                Reward = null
            });

            return move;
        }

        public override void GameEnd(EndGameState state, FullGameState? finalBoardState)
        {
            _inner.GameEnd(state, finalBoardState);

            int? winnerId = null;
            // Map PlayerEnum -> 0/1 (or leave null if no winner)
            if (state != null && state.Winner != PlayerEnum.NO_PLAYER_SELECTED)
            {
                winnerId = state.Winner == PlayerEnum.PLAYER1 ? 0
                          : state.Winner == PlayerEnum.PLAYER2 ? 1
                          : (int?)null;
            }

            _logger.Write(new GameSummary
            {
                GameId = _gameId,
                WinnerPlayerId = winnerId,
                NumTurns = _turnIndex,
                // optional: add finalscore / seed
                Seed = _seed.HasValue ? (long?)_seed.Value : (long?)null
            });
        }

        // Build a stable string for the move using fields present in Move.cs
        private static string MoveToStableString(Move m)
        {
            // at least Command + UniqueId is available and stable.
            // additionally, if "Move" exposes more (like m.Card?.Id or patron info), i could optionally append them
            return $"{m.Command}#{m.UniqueId}";
        }
    }
}
