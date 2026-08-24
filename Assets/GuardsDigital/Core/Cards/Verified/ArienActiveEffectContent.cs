using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The typed composition of Arien's five persistent texts, at milestone 8.20: the Skill lock Spell
    /// Break raises, the duelling immunity two of his Blue Duelist cards leave behind their Defense,
    /// and the two footings Slippery Ground and Deluge lay down before their Movement.
    ///
    /// <para><b>These are declarations, not handlers.</b> All five register through
    /// <see cref="ActiveEffectCreationHandler"/> - the one capability shape whose whole content is an
    /// Active-effect definition, lifted out for exactly this at milestone 8.9 - and everything each
    /// card does is stated in that definition. No card name is read anywhere at runtime, and nothing
    /// here asks the board a question.</para>
    ///
    /// <para><b>Why they are Active effects and not something the action does.</b> p.17's keywords are
    /// printed on all five faces: four say "<b>This turn:</b>" and Master Duelist says "<b>This
    /// round:</b>", which is the first text in this build to outlive the turn that created it. An
    /// Active effect is the only shape here that survives its action and still ends at the right
    /// moment, and p.17 also gives the three radius-scoped ones their moving centre - "an Active
    /// effect's Radius moves with its hero" - which is why none of them names a space.</para>
    ///
    /// <para><b>Three of the five draw a disk, and only two of those disks can be widened.</b> p.9 says
    /// "If the card text mentions Range/Radius, it refers to this value", so Spell Break's and Deluge's
    /// are <see cref="DeclaredRadius.PrintedIcon"/> and p.16's matching item raises them. Slippery
    /// Ground's sentence says "adjacent to you" and its face prints no icon at all, so its disk is
    /// <see cref="DeclaredRadius.WrittenInText"/> - the same distinction p.16 draws in words: "Items
    /// never affect values in the card text". That is the whole of what Deluge buys over Slippery
    /// Ground, both reaching one space on the turn they are played.</para>
    /// </summary>
    public static class ArienActiveEffectContent
    {
        /// <summary>
        /// Rulebook p.17's three cancellations for an Active effect on your card: it no longer applies,
        /// you are defeated, or the card changes state "including at the end of the round". Resolving
        /// the source card is deliberately not one of them - p.17 has you turn the card on its side
        /// <em>after</em> performing the action precisely to signal the effect is still running - so
        /// these declare defeat and end-of-round retrieval, and each duration's own expiration
        /// checkpoint ends them normally. It is the same list Wasp's two basic cards declare, stated
        /// here rather than shared because a hero's content owns its own declarations.
        /// </summary>
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityId BreakEnemySpellsInRadius { get; } =
            new EffectCapabilityId("arien-break-enemy-spells-in-radius");

        public static EffectCapabilityId DuelThisTurn { get; } =
            new EffectCapabilityId("arien-duel-this-turn");

        public static EffectCapabilityId DuelThisRound { get; } =
            new EffectCapabilityId("arien-duel-this-round");

        public static EffectCapabilityId SlipperyGroundAdjacent { get; } =
            new EffectCapabilityId("arien-slippery-ground-adjacent");

        public static EffectCapabilityId DelugeInRadius { get; } =
            new EffectCapabilityId("arien-deluge-in-radius");

        public static EffectCapabilityReference BreakEnemySpellsInRadiusV1 =>
            new EffectCapabilityReference(BreakEnemySpellsInRadius, 1);

        public static EffectCapabilityReference DuelThisTurnV1 =>
            new EffectCapabilityReference(DuelThisTurn, 1);

        public static EffectCapabilityReference DuelThisRoundV1 =>
            new EffectCapabilityReference(DuelThisRound, 1);

        public static EffectCapabilityReference SlipperyGroundAdjacentV1 =>
            new EffectCapabilityReference(SlipperyGroundAdjacent, 1);

        public static EffectCapabilityReference DelugeInRadiusV1 =>
            new EffectCapabilityReference(DelugeInRadius, 1);

        /// <summary>
        /// Spell Break's Basic Skill: "<b>This turn:</b> Enemy heroes in radius cannot perform Skill
        /// actions, except on the gold cards."
        ///
        /// <para><b>Three printed clauses, three declarations.</b> "Enemy heroes in radius" is the
        /// affected scope - milestone 8.20's <see cref="ActiveEffectAffectedScopeKind.HeroesInRadius"/>
        /// opposing the source, re-derived at every ask so a hero who walks out of the disk walks out
        /// of the rule. "cannot perform Skill actions" is p.11's action kind, which
        /// <see cref="RuleActivity.Skill"/> names. "except on the gold cards" is the source-card scope
        /// this milestone added, and it is the reason the restriction had to grow a second
        /// qualification at all.</para>
        ///
        /// <para><b>Why the actors are unqualified where the cards are not.</b> The sentence says who
        /// is affected - enemy heroes - and the scope above is what says it; the restriction itself is
        /// on those heroes performing their own Skills, which is
        /// <see cref="RestrictedActorScope.AnyActor"/>'s reading of an affected object acting for
        /// itself. There is nobody else who could perform another hero's Skill action, so a
        /// qualification here would narrow nothing and read as though it might.</para>
        ///
        /// <para><b>What the carve-out costs at the table.</b> Ledger question A-03, answered by the
        /// user on 2026-08-23: a card text naming a colour names that colour, and "Basic" is the
        /// supertype spanning Gold and Silver. So a hero in this disk may still perform their Basic
        /// Skill if it is printed on Gold and may not if it is printed on Silver - which is Wasp's
        /// Static Barrier exactly, and the live consequence the user accepted when they answered
        /// it.</para>
        ///
        /// <para>A Skill's effect is the action (p.12), so it is created and activated at
        /// <see cref="TimingCheckpoint.ActionStarted"/> rather than at one of p.14's Attack
        /// moments.</para>
        /// </summary>
        public static ActiveEffectDefinition SpellBreakDefinition { get; } =
            new ActiveEffectDefinition(
                "arien-spell-break-effect",
                ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "ArienSpellBreak",
                "arien-spell-break",
                Array.Empty<TimingCheckpoint>(),
                new[]
                {
                    RuleModification.ForRestriction(
                        RuleActivity.Skill,
                        RestrictedActorScope.AnyActor,
                        RestrictedSourceCardScope.ExceptColors(CardColor.Gold))
                },
                null,
                DeclaredRadius.PrintedIcon(3),
                SourceTeamRelation.OpposingSource);

        /// <summary>
        /// Expert Duelist's Defense text: "<b>This turn:</b> You are immune to attack actions of all
        /// enemy heroes, except this attacker."
        ///
        /// <para><b>Whose immunity, against whom, and the one hero left out.</b> The affected object is
        /// the controller - the card is Arien's and the sentence says "You" - so the scope is
        /// <see cref="ActiveEffectAffectedScopeKind.ControllerHero"/> and no disk is drawn. "attack
        /// actions of all enemy heroes" is one activity and one class of actor, which milestone 8.19c's
        /// <c>immunity@2</c> states together. "except this attacker" is milestone 8.20's carve-out: the
        /// definition declares where "this" points - <see cref="ActiveEffectBoundActorSource
        /// .EnclosingAttacker"/>, the hero attacking at p.14's step 4 - and the occurrence carries whom
        /// it found.</para>
        ///
        /// <para><b>Why it is created at p.14's step 4 and nowhere else.</b> That step is "Defender
        /// applies Defense action text, if any", and it is the only moment at which there is an
        /// attacker for the word "this" to name. <see cref="ActiveEffectDefinition"/> refuses any other
        /// creation checkpoint for a definition that binds one, so a composition that moved this text
        /// would fail at content load rather than bind the undefined hero and quietly except
        /// nobody.</para>
        ///
        /// <para><b>What it does not say.</b> Nothing here suppresses minion Defense modifiers. That is
        /// the card's other sentence, and it is a statement about which contributions the Defense Total
        /// takes rather than a rule that persists, so it is declared on the action itself through
        /// <c>MinionDefenseContributionPolicy</c> - which is also why Aspiring Duelist, whose whole text
        /// is that sentence, needs no Active effect at all.</para>
        /// </summary>
        public static ActiveEffectDefinition ExpertDuelistDefinition { get; } =
            new ActiveEffectDefinition(
                "arien-expert-duelist-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.DefenseResponse,
                TimingCheckpoint.DefenseResponse,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "ArienExpertDuelist",
                "arien-expert-duelist",
                Array.Empty<TimingCheckpoint>(),
                new[] { DuellingImmunity },
                null,
                DeclaredRadius.None,
                SourceTeamRelation.None,
                null,
                ActiveEffectBoundActorSource.EnclosingAttacker);

        /// <summary>
        /// Master Duelist's Defense text, which is Expert Duelist's sentence with one word changed:
        /// "<b>This round:</b> You are immune to attack actions of all enemy heroes, except this
        /// attacker."
        ///
        /// <para>p.19 defines both keywords, and this is the first Active effect in this project that
        /// survives a turn boundary. Everything else about the two cards is identical - the same scope,
        /// the same activity, the same actor class, the same carve-out and the same creation moment -
        /// which is why the difference is expressed as the duration and its expiration checkpoint and
        /// nowhere else. The two are separate definitions rather than one parameterised by duration
        /// because <see cref="ActiveEffectDefinition.UniquenessKey"/> is what keeps one card's
        /// occurrence from suppressing the other's: a hero who defends with Expert Duelist and later in
        /// the round with Master Duelist has played two cards and is owed two rules.</para>
        /// </summary>
        public static ActiveEffectDefinition MasterDuelistDefinition { get; } =
            new ActiveEffectDefinition(
                "arien-master-duelist-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.DefenseResponse,
                TimingCheckpoint.DefenseResponse,
                TimingCheckpoint.RoundEnd,
                Cancellations,
                false,
                "ArienMasterDuelist",
                "arien-master-duelist",
                Array.Empty<TimingCheckpoint>(),
                new[] { DuellingImmunity },
                null,
                DeclaredRadius.None,
                SourceTeamRelation.None,
                null,
                ActiveEffectBoundActorSource.EnclosingAttacker);

        /// <summary>
        /// Slippery Ground's Movement text: "<b>This turn:</b> Enemy heroes adjacent to you cannot fast
        /// travel, or move more than 1 space with a movement action."
        ///
        /// <para><b>Two printed prohibitions and they are not the same rule.</b> "cannot fast travel"
        /// takes p.11's replacement away entirely, which is an ordinary restriction on
        /// <see cref="RuleActivity.FastTravel"/>. "cannot move more than 1 space with a movement
        /// action" leaves the Movement standing and caps it, which is milestone 8.20's
        /// <see cref="RuleModificationKind.ActivityLimit"/> - a hero this reaches may still take one
        /// step, and a declaration that had barred the Movement would be a stronger card than the
        /// printed one.</para>
        ///
        /// <para><b>When the cap is read.</b> p.11 answers it outright: "Effects which limit your
        /// movement distance, or prevent you from performing a movement action, are only checked at the
        /// start of the movement action." So the cap is composed into the Movement Total once, when the
        /// action starts, and a hero who walks out of the disk mid-move keeps the shorter allowance
        /// they started with.</para>
        ///
        /// <para><b>Why the disk is written in text.</b> The face prints no Range or Radius icon and
        /// the sentence says "adjacent to you", so there is no icon for p.16's item to raise - which is
        /// the whole of the difference between this card and Deluge below.</para>
        /// </summary>
        public static ActiveEffectDefinition SlipperyGroundDefinition { get; } =
            new ActiveEffectDefinition(
                "arien-slippery-ground-effect",
                ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "ArienSlipperyGround",
                "arien-slippery-ground",
                Array.Empty<TimingCheckpoint>(),
                SlowedFooting,
                null,
                DeclaredRadius.WrittenInText(1),
                SourceTeamRelation.OpposingSource);

        /// <summary>
        /// Deluge's Movement text, which is Slippery Ground's sentence with "adjacent to you" replaced
        /// by "in radius": "<b>This turn:</b> Enemy heroes in radius cannot fast travel, or move more
        /// than 1 space with a movement action."
        ///
        /// <para>The printed Radius is 1, which p.9 makes the smallest disk there is and which reaches
        /// exactly the spaces "adjacent to you" reaches. So on the turn it is played this card does
        /// what Slippery Ground does; what it buys is that p.16's Radius item can widen it, because the
        /// number is on an icon rather than in a sentence.</para>
        /// </summary>
        public static ActiveEffectDefinition DelugeDefinition { get; } =
            new ActiveEffectDefinition(
                "arien-deluge-effect",
                ActiveEffectAffectedScopeKind.HeroesInRadius,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "ArienDeluge",
                "arien-deluge",
                Array.Empty<TimingCheckpoint>(),
                SlowedFooting,
                null,
                DeclaredRadius.PrintedIcon(1),
                SourceTeamRelation.OpposingSource);

        /// <summary>
        /// "immune to attack actions of all enemy heroes, except this attacker", stated once because
        /// Expert Duelist and Master Duelist print the same clause and a second copy could drift from
        /// the first.
        /// </summary>
        private static RuleModification DuellingImmunity =>
            RuleModification.ForImmunity(
                new[] { RuleActivity.Attack },
                RestrictedActorScope.OfExceptingTheBoundActor(
                    RestrictedActorRelation.HeroesOpposingSource));

        /// <summary>
        /// "cannot fast travel, or move more than 1 space with a movement action", stated once for the
        /// same reason: Slippery Ground and Deluge print one sentence and differ only in the disk it is
        /// measured over.
        /// </summary>
        private static IEnumerable<RuleModification> SlowedFooting => new[]
        {
            RuleModification.ForRestriction(RuleActivity.FastTravel),
            RuleModification.ForActivityLimit(RuleActivity.Movement, 1)
        };

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(BreakEnemySpellsInRadiusV1, SpellBreakDefinition),
            new ActiveEffectCreationHandler(DuelThisTurnV1, ExpertDuelistDefinition),
            new ActiveEffectCreationHandler(DuelThisRoundV1, MasterDuelistDefinition),
            new ActiveEffectCreationHandler(SlipperyGroundAdjacentV1, SlipperyGroundDefinition),
            new ActiveEffectCreationHandler(DelugeInRadiusV1, DelugeDefinition)
        };

        /// <summary>Every definition these capabilities can create, for a fixture that needs them all.</summary>
        public static IReadOnlyList<ActiveEffectDefinition> Definitions { get; } = Array.AsReadOnly(new[]
        {
            SpellBreakDefinition,
            ExpertDuelistDefinition,
            MasterDuelistDefinition,
            SlipperyGroundDefinition,
            DelugeDefinition
        });
    }
}
