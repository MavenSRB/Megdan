namespace GuardsDigital.Core.Cards
{
    public enum ActionInvocationContext
    {
        None = 0,
        PrintedPrimary = 1,
        PrintedSecondary = 2,
        GeneralRuleSecondary = 3,
        Replacement = 4,
        Reaction = 5,
        Repeated = 6,
        TextGranted = 7,

        /// <summary>
        /// A printed secondary action performed from another card's text. Unlike a borrowed
        /// primary it does not satisfy a trigger asking for a primary action, but it is still
        /// text-granted and never resolves or discards the borrowed card.
        /// </summary>
        TextGrantedSecondary = 8
    }
}
