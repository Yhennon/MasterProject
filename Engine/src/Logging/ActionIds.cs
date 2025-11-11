namespace ScriptsOfTribute.Engine.Logging
{
    public static class ActionIds
    {
        public const int PlayCardBase = 1000;   // + cardId
        public const int BuyCardBase  = 2000;   // + cardId
        public const int ActivateBase = 3000;   // + patron/effect id
        public const int EndTurn      = 4000;

        public static int PlayCard(int cardId)   => PlayCardBase + cardId;
        public static int BuyCard(int cardId)    => BuyCardBase + cardId;
        public static int Activate(int effectId) => ActivateBase + effectId;
    }
}
