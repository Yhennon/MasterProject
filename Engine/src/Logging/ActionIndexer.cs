using System;
using System.Collections.Generic;
using System.Linq;
using ScriptsOfTribute;
using ScriptsOfTribute.Board.Cards;
using ScriptsOfTribute.Serializers;
using ScriptsOfTribute.Board.CardAction;

namespace ScriptsOfTribute.Engine.Logging
{
    /// <summary>
    /// Central place for action-space constants and index layout.
    /// </summary>
    internal static class ActionSpaceConfig
    {
        // --- configuration ---

        public const int MaxHandSlots       = 17;
        public const int MaxTavernSlots     = 5;
        public const int MaxAgentsPerPlayer = 7;
        public const int MaxChoiceSlots     = 64;

        public const int NumHandActions     = MaxHandSlots;  // 17
        public const int NumTavernActions   = MaxTavernSlots; // 5
        public const int NumPatronActions   = 5; 
        public const int NumActivateActions = MaxAgentsPerPlayer; // 7
        public const int NumAttackActions   = MaxAgentsPerPlayer;  // 7
        public const int NumChoiceActions   = MaxChoiceSlots; // 64

        public const int HandOffset     = 0;
        public const int TavernOffset   = HandOffset     + NumHandActions; // 17
        public const int PatronOffset   = TavernOffset   + NumTavernActions; // 22
        public const int ActivateOffset = PatronOffset   + NumPatronActions; // 27
        public const int AttackOffset   = ActivateOffset + NumActivateActions; // 34
        public const int ChoiceOffset   = AttackOffset   + NumAttackActions; // 41
        public const int EndTurnIndex   = ChoiceOffset   + NumChoiceActions; // 105

        public const int NumActions     = EndTurnIndex + 1; // 106{}
    }

    /// <summary>
    /// Maps engine Move objects to global action indices in [0..NumActions-1].
    /// Returns -1 if a move can't be represented in the current action space.
    /// </summary>
    internal static class ActionIndexer
    {
        /// <summary>
        /// Map a Move to a global action index, given the current player-perspective GameState.
        /// Literallyy encodes the move according to the layout in ActionSpaceConfig.
        /// </summary>
        public static int ToIndex(GameState state, Move move, int playerId)
        {
            // playerId is currently unused because GameState is already
            // from the acting player's perspective (CurrentPlayer / EnemyPlayer).
            _ = playerId;

            return move.Command switch
            {
                CommandEnum.PLAY_CARD      => EncodePlayCard(state, move),
                CommandEnum.BUY_CARD       => EncodeBuyCard(state, move),
                CommandEnum.ACTIVATE_AGENT => EncodeActivateAgent(state, move),
                CommandEnum.ATTACK         => EncodeAttack(state, move),
                CommandEnum.CALL_PATRON    => EncodeCallPatron(state, move),
                CommandEnum.MAKE_CHOICE    => EncodeMakeChoice(state, move),
                CommandEnum.END_TURN       => ActionSpaceConfig.EndTurnIndex,
                _                          => -1, // maybe throw exception for unknown commands, but for now just return -1
            };
        }

        // --- individual encoders ---

        private static int EncodePlayCard(GameState state, Move move)
        {
            if (move is not SimpleCardMove cardMove)
                return -1;

            var hand = state.CurrentPlayer.Hand;
            int slot = IndexOfCard(hand, cardMove.Card);
            if (slot < 0 || slot >= ActionSpaceConfig.MaxHandSlots)
                return -1;

            return ActionSpaceConfig.HandOffset + slot;
        }

        private static int EncodeBuyCard(GameState state, Move move)
        {
            if (move is not SimpleCardMove cardMove)
                return -1;

            var tavern = state.TavernAvailableCards;
            int slot = IndexOfCard(tavern, cardMove.Card);
            if (slot < 0 || slot >= ActionSpaceConfig.MaxTavernSlots)
                return -1;

            return ActionSpaceConfig.TavernOffset + slot;
        }

        private static int EncodeActivateAgent(GameState state, Move move)
        {
            if (move is not SimpleCardMove cardMove)
                return -1;

            var agents = state.CurrentPlayer.Agents; // List<SerializedAgent>
            int slot = IndexOfAgent(agents, cardMove.Card);
            if (slot < 0 || slot >= ActionSpaceConfig.MaxAgentsPerPlayer)
                return -1;

            return ActionSpaceConfig.ActivateOffset + slot;
        }

        private static int EncodeAttack(GameState state, Move move)
        {
            if (move is not SimpleCardMove cardMove)
                return -1;

            var enemyAgents = state.EnemyPlayer.Agents; // List<SerializedAgent>
            int slot = IndexOfAgent(enemyAgents, cardMove.Card);
            if (slot < 0 || slot >= ActionSpaceConfig.MaxAgentsPerPlayer)
                return -1;

            // "Attack enemy agent in slot 'slot'"
            return ActionSpaceConfig.AttackOffset + slot;
        }

        private static int EncodeCallPatron(GameState state, Move move)
        {
            if (move is not SimplePatronMove patronMove)
                return -1;

            // Use the patron list from state; order must be used consistently at inference time too.
            var patrons = state.Patrons;
            int idx = patrons.IndexOf(patronMove.PatronId);
            if (idx < 0 || idx >= ActionSpaceConfig.NumPatronActions)
                return -1;

            return ActionSpaceConfig.PatronOffset + idx;
        }

        private static int EncodeMakeChoice(GameState state, Move move)
        {
            var choice = state.PendingChoice;
            if (choice == null)
                return -1;

            if (choice.Type == Choice.DataType.CARD && move is MakeChoiceMoveUniqueCard cardChoiceMove)
            {
                int k = choice.PossibleCards.Count;

                // Skip option (choose nothing)
                if (cardChoiceMove.Choices.Count == 0)
                {
                    int optionIndex = k; // "skip" = pseudo-option after all cards
                    if (optionIndex < 0 || optionIndex >= ActionSpaceConfig.MaxChoiceSlots)
                        return -1;

                    return ActionSpaceConfig.ChoiceOffset + optionIndex;
                }

                // Normal case: choosing exactly one card
                if (cardChoiceMove.Choices.Count == 1)
                {
                    var selected = cardChoiceMove.Choices[0];
                    int optionIndex = IndexOfCard(choice.PossibleCards, selected);

                    if (optionIndex < 0 || optionIndex >= ActionSpaceConfig.MaxChoiceSlots)
                        return -1;

                    return ActionSpaceConfig.ChoiceOffset + optionIndex;
                }

                // Multi-selection (>1) is currently not modeled in the action space
                return -1;
            }

            if (choice.Type == Choice.DataType.EFFECT && move is MakeChoiceMoveUniqueEffect effectChoiceMove)
            {
                int k = choice.PossibleEffects.Count;

                // Skip option
                if (effectChoiceMove.Choices.Count == 0)
                {
                    int optionIndex = k; // "skip" option
                    if (optionIndex < 0 || optionIndex >= ActionSpaceConfig.MaxChoiceSlots)
                        return -1;

                    return ActionSpaceConfig.ChoiceOffset + optionIndex;
                }

                // Normal case: choosing exactly one effect
                if (effectChoiceMove.Choices.Count == 1)
                {
                    var selected = effectChoiceMove.Choices[0];
                    int optionIndex = IndexOfEffect(choice.PossibleEffects, selected);

                    if (optionIndex < 0 || optionIndex >= ActionSpaceConfig.MaxChoiceSlots)
                        return -1;

                    return ActionSpaceConfig.ChoiceOffset + optionIndex;
                }

                return -1;
            }

            // Unknown combination of choice type + move type
            return -1;
        }


        // --- small helpers ---

        private static int IndexOfCard(IReadOnlyList<UniqueCard> cards, UniqueCard card)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].UniqueId.Value == card.UniqueId.Value)
                    return i;
            }

            return -1;
        }

        private static int IndexOfAgent(IReadOnlyList<SerializedAgent> agents, UniqueCard agentCard)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].RepresentingCard.UniqueId.Value == agentCard.UniqueId.Value)
                    return i;
            }

            return -1;
        }

        private static int IndexOfEffect(IReadOnlyList<UniqueEffect> effects, UniqueEffect effect)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (ReferenceEquals(effects[i], effect) || effects[i].Equals(effect))
                    return i;
            }

            return -1;
        }
    }
}
