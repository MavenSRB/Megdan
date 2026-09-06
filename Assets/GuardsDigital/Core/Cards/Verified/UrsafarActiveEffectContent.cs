using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ursafar the Savage's <c>This round: You are enraged.</c> - one p.17 Active effect per face that
    /// prints it, and the twelve capabilities that create them.
    ///
    /// <para><b>Twelve, and the count is a correction.</b> Thirteen of his nineteen faces print the
    /// keyword line: both Gold cards, the Silver, all five Red and all five Blue. The five Green faces
    /// read the state and never grant it, and the Ultimate replaces the mechanism with a permanent
    /// assertion, so 19 - 5 - 1 = 13. The inventory draft says "fourteen"; that is an arithmetic slip
    /// in the prose and not a missing face - its own transcription table lists thirteen keyword lines
    /// and the shard this milestone writes carries the corrected figure. Twelve definitions ship here
    /// rather than thirteen because Jaws That Bite is p.20's Handicap card, whose composition is
    /// deferred whole; its grant arrives with the rest of its text.</para>
    ///
    /// <para><b>Why one definition per card rather than one shared definition.</b> Rulebook p.10 says
    /// "Only one instance of an active effect per <b>card</b> can be active", and the designer's own
    /// answer in the Ursafar thread says the same thing from the table's side - multiple enrage effects
    /// coexist, one per card, and the Silver may copy any of them. The lifecycle suppresses a duplicate
    /// by matching uniqueness key and controller, so a shared key would have let Ursafar's second card
    /// of the round silently create nothing and his first card's discard take the whole state with it.
    /// Twelve keys is what "per card" costs.</para>
    ///
    /// <para><b>Why creation and activation are different checkpoints, and this is the hero's central
    /// rule.</b> Every one of these is created when its action starts and becomes active only at that
    /// action's end. The lifecycle activates on creation only when a definition's two checkpoints
    /// match, so declaring them apart leaves the instance dormant for the whole of the action that made
    /// it - and <see cref="HeroConditionQuery"/> counts only active instances. That makes the user's
    /// build-up ruling <b>structurally</b> true rather than true by node ordering: a card cannot
    /// satisfy its own "If enraged", no matter how its text is composed, which is exactly what p.10's
    /// written order and p.17's "after you are done performing the action" say together. It is also why
    /// the Ultimate's first clause is worth its Tier - it is the only thing that lets his first card of
    /// a round pay off.</para>
    ///
    /// <para><b>Deferred, not registered</b>, on the standing rule that the registered capability list
    /// is serialized: nothing here joins the build's catalog until the milestone that deals him does it
    /// in the same change.</para>
    /// </summary>
    public static class UrsafarActiveEffectContent
    {
        /// <summary>
        /// The twelve faces whose <c>This round: You are enraged.</c> is composed by this build - the
        /// thirteen that print it, less p.20's Handicap card.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ComposedGrants { get; } = Array.AsReadOnly(new[]
        {
            UrsafarCardContent.ClawsThatCatch,
            UrsafarCardContent.AngryRoar,
            UrsafarCardContent.PreyDrive,
            UrsafarCardContent.PreyAbundance,
            UrsafarCardContent.Rip,
            UrsafarCardContent.FeedingFrenzy,
            UrsafarCardContent.Tear,
            UrsafarCardContent.ProwlingBrute,
            UrsafarCardContent.RampagingBeast,
            UrsafarCardContent.ColdIre,
            UrsafarCardContent.UnstoppableForce,
            UrsafarCardContent.EyesOfFlame
        });

        /// <summary>The capability that grants one named card's enraged effect.</summary>
        public static EffectCapabilityReference GrantFor(CardDefinitionId cardId) =>
            new EffectCapabilityReference(GrantCapabilityId(cardId), 1);

        /// <summary>
        /// The Active effect one named card grants, as catalog-owned semantics: an instance records
        /// only its controller, its source card and when it began, and a restore rebinds to this object
        /// rather than rebuilding an equivalent from the payload.
        /// </summary>
        public static ActiveEffectDefinition DefinitionFor(CardDefinitionId cardId)
        {
            if (!ComposedGrants.Contains(cardId))
            {
                throw new ArgumentException(
                    $"Ursafar's card '{cardId}' composes no enraged grant.", nameof(cardId));
            }

            return new ActiveEffectDefinition(
                cardId.Value + "-enraged",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,

                // Created as the action that prints it begins, and active only once that action is
                // over. See the type remarks: this pair is the build-up rule.
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.RoundEnd,

                // p.17's cancellation set, less the one the duration already covers. "At the end of the
                // round" is the ThisRound expiry above; the three that remain are the page's others -
                // the card changing state (discarded to defend, turned facedown), the hero being
                // defeated, and p.8's end-of-round retrieval, which is a card state change with a
                // checkpoint of its own.
                new[]
                {
                    ActiveEffectCancellationCondition.SourceCardStateChanged,
                    ActiveEffectCancellationCondition.ControllerHeroDefeated,
                    ActiveEffectCancellationCondition.CardsRetrieved
                },
                false,

                // The kind every reading matches on. It is the same string for all twelve, because
                // being enraged is one state however it was granted; only the uniqueness key below is
                // per-card, because p.10 counts instances per card.
                HeroConditionQuery.EffectKind(HeroConditionKind.Enraged),
                cardId.Value + "-enraged",

                // No trigger and no modification. Being enraged changes no value and forbids no action;
                // it exists so that this hero's own card text can read it, which is what makes it a
                // condition rather than a modifier.
                Array.Empty<TimingCheckpoint>());
        }

        /// <summary>
        /// The two Blue faces that raise their own Movement value, and what each adds - Cold Ire's
        /// <c>+1</c> and Eyes of Flame's <c>+2</c>. These are the faces printing the <c>1+</c> cue.
        /// </summary>
        public static IReadOnlyDictionary<CardDefinitionId, int> MovementRaises { get; } =
            new Dictionary<CardDefinitionId, int>
            {
                { UrsafarCardContent.ColdIre, 1 },
                { UrsafarCardContent.EyesOfFlame, 2 }
            };

        /// <summary>The capability that raises one named card's Movement allowance.</summary>
        public static EffectCapabilityReference MovementRaiseFor(CardDefinitionId cardId) =>
            new EffectCapabilityReference(
                new EffectCapabilityId(cardId.Value + "-movement"), 1);

        /// <summary>
        /// The raise itself: an ordinary value contribution on p.9's Movement channel, for this turn.
        ///
        /// <para>It is an Active effect rather than something the walk is told directly because the
        /// allowance is composed from declared contributions and nothing else - which means a raised
        /// total is composed <em>before</em> p.11's action-start restriction judges it, so a hero whose
        /// text raised him past a cap is judged on the raised number rather than being quietly capped
        /// first. The duration is the turn because the card's raise is about this walk; the round-long
        /// thing on these cards is the enraged state, which is a different effect entirely.</para>
        /// </summary>
        public static ActiveEffectDefinition MovementRaiseDefinitionFor(CardDefinitionId cardId)
        {
            if (!MovementRaises.TryGetValue(cardId, out var amount))
            {
                throw new ArgumentException(
                    $"Ursafar's card '{cardId}' prints no Movement raise.", nameof(cardId));
            }

            return new ActiveEffectDefinition(
                cardId.Value + "-movement-raise",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[]
                {
                    ActiveEffectCancellationCondition.ControllerHeroDefeated,
                    ActiveEffectCancellationCondition.CardsRetrieved
                },
                false,
                "UrsafarMovementRaise",
                cardId.Value + "-movement-raise",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForValue(PrintedValueChannel.Movement, amount) });
        }

        /// <summary>
        /// Tear's declaration that its defeat costs one Life counter more than p.13 charges.
        ///
        /// <para>It is one capability rather than one per card because exactly one face prints the
        /// clause, and it is an Active effect on him for the turn because the declaration has to be
        /// standing when p.13's cost is computed at p.14's step 6 - see
        /// <c>UrsafarEffectContent</c> for why the card's own "After the attack:" placement is when the
        /// condition is checked rather than when the counter is paid.</para>
        /// </summary>
        public static EffectCapabilityReference AdditionalDefeatLifeCostV1 =>
            new EffectCapabilityReference(
                new EffectCapabilityId("ursafar-additional-defeat-life-cost"), 1);

        public static ActiveEffectDefinition AdditionalDefeatLifeCostDefinition { get; } =
            new ActiveEffectDefinition(
                "ursafar-additional-defeat-life-cost",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[]
                {
                    ActiveEffectCancellationCondition.ControllerHeroDefeated,
                    ActiveEffectCancellationCondition.CardsRetrieved
                },
                false,
                "UrsafarAdditionalDefeatLifeCost",
                "ursafar-additional-defeat-life-cost",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForAdditionalDefeatLifeCost() });

        /// <summary>
        /// What this file contributes to the build's registered set today: nothing, on the standing
        /// rule about eager registration. See the type remarks.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The twelve handlers the milestone that deals Ursafar registers, and that this milestone's
        /// offline tests compose a catalog of their own to prove.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            ComposedGrants
                .Select(cardId => (IEffectCapabilityHandler)new ActiveEffectCreationHandler(
                    GrantFor(cardId), DefinitionFor(cardId)))
                .Concat(MovementRaises.Keys.Select(cardId =>
                    (IEffectCapabilityHandler)new ActiveEffectCreationHandler(
                        MovementRaiseFor(cardId), MovementRaiseDefinitionFor(cardId))))
                .Concat(new IEffectCapabilityHandler[]
                {
                    new ActiveEffectCreationHandler(
                        AdditionalDefeatLifeCostV1, AdditionalDefeatLifeCostDefinition)
                });

        private static EffectCapabilityId GrantCapabilityId(CardDefinitionId cardId) =>
            new EffectCapabilityId(cardId.Value + "-enraged");
    }
}
