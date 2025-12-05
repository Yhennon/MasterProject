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
            // 1) full observable snapshot
            var obs = gameState.SerializeGameState();  // JObject

            // 2) Legacy string-based moves for debugging (optional)
            var legalStrings = new List<string>(possibleMoves.Count);
            foreach (var m in possibleMoves)
                legalStrings.Add(MoveToStableString(m));

            // 3) indexed legal actions + which ones couldn't be mapped
            var legalIndices = new List<int>(possibleMoves.Count);
            var unmappedLegal = new List<string>();

            foreach (var m in possibleMoves)
            {
                int idx = ActionIndexer.ToIndex(gameState, m, _playerId);
                if (idx >= 0)
                {
                    legalIndices.Add(idx);
                }
                else
                {
                    // Keep track of legal moves that are outside of action space
                    unmappedLegal.Add(MoveToStableString(m));
                }
            }

            // 4) Let the inner bot choose a move
            var move = _inner.Play(gameState, possibleMoves, remainingTime);

            // 5) Map chosen move to index
            var chosenString = MoveToStableString(move);
            int rawChosenIndex = ActionIndexer.ToIndex(gameState, move, _playerId);

            bool indexerFailedForChosen = rawChosenIndex < 0;
            int? chosenIndexOrNull      = rawChosenIndex >= 0 ? rawChosenIndex : (int?)null;

            string commandName = move.Command.ToString();

            // 6) Compute the diagnostic stats (hand size, etc) as i already do
            var current = gameState.CurrentPlayer;
            int handSize        = current.Hand.Count;
            int drawPileSize    = current.DrawPile.Count;
            int cooldownPileSize    = current.CooldownPile.Count;
            int agentsCount     = current.Agents.Count;
            int tavernCount     = gameState.TavernAvailableCards.Count;
            int numLegalActions = possibleMoves.Count;
            int pendingChoiceOptions = 0;
            if (gameState.PendingChoice is { } choice)
            {
                if (choice.Type == Choice.DataType.CARD)
                    pendingChoiceOptions = choice.PossibleCards.Count;
                else if (choice.Type == Choice.DataType.EFFECT)
                    pendingChoiceOptions = choice.PossibleEffects.Count;
            }

            // 7) Write log entry
            _logger.Write(new TurnLog
            {
                GameId   = _gameId,
                TurnIndex = _turnIndex++,
                PlayerId  = _playerId,

                State        = obs,
                LegalActions = legalStrings,
                ActionTaken  = chosenString,

                ChosenActionIndex     = chosenIndexOrNull,
                LegalActionIndices    = legalIndices,
                IndexerFailedForChosen= indexerFailedForChosen,
                UnmappedLegalActions  = unmappedLegal,
                Command               = commandName,

                HandSize         = handSize,
                DrawPileSize     = drawPileSize,
                CooldownPileSize = cooldownPileSize,
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
            // at least Command + UniqueId is available and stable
            // additionally, if "Move" exposes more (like m.Card?.Id or patron info)i could optionally append them
            return $"{m.Command}#{m.UniqueId}";
        }
    }
}
