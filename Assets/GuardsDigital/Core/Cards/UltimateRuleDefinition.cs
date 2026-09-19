using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards
{
    /// <summary>The typed completed-action fact that may awaken a permanent Ultimate ability.</summary>
    public enum UltimateTriggerCondition
    {
        None = 0,
        AfterControllerPerformsBasicSkill = 1,

        /// <summary>
        /// Milestone 8.19c's `after-action-trigger@2`: any completed basic action of the controller's,
        /// whatever kind it was - Tigerclaw's Cloak and Daggers, "After you perform a basic action you
        /// may repeat it once".
        ///
        /// It is a second condition rather than a widening of
        /// <see cref="AfterControllerPerformsBasicSkill"/> because the two are different sentences on
        /// two different cards, and one of them is already printed and implemented. Wasp's High Voltage
        /// says "basic skill" and means it; widening the member it reads would have made her Ultimate
        /// fire after her Movements.
        /// </summary>
        AfterControllerPerformsBasicAction = 2,

        /// <summary>
        /// Milestone 8.35z29's `after-self-place-trigger@1`: the controller's own card text put
        /// her somewhere - Misa's Power Overwhelming, "Each time after you place yourself, an enemy
        /// hero adjacent to you discards a card, if able."
        ///
        /// <para><b>It is a third condition rather than a qualifier on the two above, because the two
        /// above are about what KIND of action ended and this one is about what the action DID.</b>
        /// A hero can perform a basic Skill without placing herself, and the sentence fires on the
        /// placement rather than on the Skill: Misa's Sudden Breeze is a Skill of hers that moves her
        /// five spaces and must not reach this trigger, because moving is not placing. That is p.18's
        /// own distinction, and it is the reason the fact this reads is derived from the Place node in
        /// the definition rather than from the action's kind.</para>
        ///
        /// <para><b>What it excludes, by the user's 2026-09-06 ruling.</b> A placement performed by
        /// another hero's card is not her placing herself - "you place yourself" names her as the
        /// actor - and a Swap is never a placement at all, which p.18 states outright. Neither reaches
        /// this condition: the fact behind it is derived from the definition the acting hero is
        /// resolving, and no Swap capability contributes to it.</para>
        /// </summary>
        AfterControllerPlacesSelf = 3,

        /// <summary>
        /// Milestone 8.37's <c>after-basic-attack-defeats-minion-trigger@1</c>: Tali's
        /// Reign of Winter, "Each time after you defeat a minion with your basic attack, an enemy hero
        /// in radius discards a card of the color of your choice, if able."
        ///
        /// <para><b>A fourth condition rather than a further qualifier on the two Attack-shaped ones,
        /// because - like <see cref="AfterControllerPlacesSelf"/> before it - this reads a CONSEQUENCE
        /// of the completed action rather than which kind it was.</b> Wasp's High Voltage and
        /// Tigerclaw's Cloak and Daggers both ask what KIND of basic action just finished; this asks
        /// what that basic Attack DID. A basic Attack that missed, was defended, or defeated a hero
        /// instead must not reach this trigger, and only "defeated a minion" does.</para>
        ///
        /// <para><b>The fact is derived where p.13's own removal outcome is already recorded</b> -
        /// whether the minion was actually REMOVED rather than its removal merely attempted or
        /// prevented - and carried on <see cref="PerformedBasicAction"/> the same way
        /// <see cref="AfterControllerPlacesSelf"/>'s own fact is: read once, at the one place a
        /// completed action's supertype is decided, never re-derived by a second reader.</para>
        /// </summary>
        AfterControllerDefeatsMinionWithBasicAttack = 4,

        /// <summary>
        /// Milestone 8.38c2's <c>after-basic-card-resolution-trigger@1</c>: Swift's Bullet Time,
        /// "After you resolve a basic card you may perform the primary action on that card; you cannot
        /// target the same enemy hero twice in the same turn this way."
        ///
        /// <para><b>A fifth condition rather than a widening of
        /// <see cref="AfterControllerPerformsBasicAction"/>, and the two differ on both halves of the
        /// sentence.</b> Tigerclaw's Cloak and Daggers asks what KIND of thing just finished - any
        /// basic action - and then repeats THAT action. Bullet Time asks whether the CARD was resolved,
        /// which happens once a turn however many actions the turn performs, and then grants that
        /// card's PRINTED PRIMARY, which need not be the action that was performed. Widening the fourth
        /// member would have fired this Ultimate after a repeat, after a borrowed primary, and after
        /// every action a grant produced - the three things "resolve a card" excludes by counting once
        /// per turn.</para>
        ///
        /// <para>The fact is <see cref="PerformedBasicAction.WasCardResolution"/>, read at the one
        /// place a completed action's supertype is decided and never re-derived by a second reader -
        /// the same discipline <see cref="AfterControllerPlacesSelf"/> and
        /// <see cref="AfterControllerDefeatsMinionWithBasicAttack"/> follow.</para>
        /// </summary>
        AfterControllerResolvesBasicCard = 5,

        /// <summary>
        /// Milestone 8.39f1. Cutter's Legend of the Skies, "The first time each turn after you perform
        /// a primary action, you may perform the primary action of a card in the previous turn slot."
        ///
        /// <para><b>A sixth condition because no existing one can reach a non-basic card at all, and
        /// that is a fact about the FACT rather than about the five readings.</b> Every member above is
        /// matched against a <see cref="GuardsDigital.Core.Cards.PerformedBasicAction"/>, and
        /// <c>BasicActionQuery.ForCompletedAction</c> returns the named absence unless the source
        /// card's classification is <see cref="CardClassification.Basic"/> - p.9's Gold and Silver
        /// alone. So <see cref="AfterControllerPerformsBasicAction"/> is not a wider version of this
        /// one that merely says "basic": it is structurally unable to see the cards this clause is
        /// mostly played off, and a Tier IV Ultimate is unlocked exactly when most of its controller's
        /// deck is upgraded.</para>
        ///
        /// <para><b>So this member reads its own fact</b>,
        /// <see cref="GuardsDigital.Core.Cards.PerformedPrimaryAction"/>, produced at the same single
        /// site from the same inputs. `PerformedBasicAction` keeps meaning exactly what it meant, which
        /// is what keeps Wasp's, Tigerclaw's, Misa's, Tali's and Swift's Ultimates - and p.10's repeat
        /// machinery, and the authoritative snapshot - reading an unchanged value.</para>
        ///
        /// <para><b>"The first time each turn" is the trigger's frequency, and it is enforced where
        /// milestone 8.19a's once-per-turn before-action triggers are enforced</b> - the turn's
        /// consumed-key set, which is already serialized and already cleared by the turn boundary. A
        /// turn performs as many primary actions as its text grants; this clause fires on the first.
        /// It is the only member here that is bounded at all, because it is the only one whose printed
        /// sentence bounds it.</para>
        /// </summary>
        AfterControllerPerformsPrimaryAction = 6,

        /// <summary>
        /// Milestone 8.40h. Bain's A Complicated Profession, "After you give a hero the [bounty] bounty
        /// marker that hero discards a card, if able."
        ///
        /// <para><b>A seventh condition, and the first that is not about an action at all.</b> The six
        /// above are matched against a completed <see cref="GuardsDigital.Core.Cards.PerformedBasicAction"/>
        /// or <see cref="GuardsDigital.Core.Cards.PerformedPrimaryAction"/>, both of which are facts the
        /// action boundary produces. This one is matched against a give that has already attached its
        /// marker, INSIDE the effect that gave it - Dead or Alive's give sits in an "After the attack:"
        /// clause, so the enclosing action has not ended and may never end in a way that names the
        /// recipient. What is watched is therefore <see cref="GuardsDigital.Core.Timing.TimingCheckpoint.MarkerGivenToHero"/>,
        /// and the condition is carried in <see cref="UltimateRuleDefinition.TriggerCheckpoints"/> for
        /// the reason milestone 8.19c1 gives for keeping the two apart: a checkpoint is a position in
        /// the rules, and which objects at that position concern a card is the card's own words.</para>
        ///
        /// <para><b>It says "you", and nothing else.</b> The printed sentence's only qualifier on the
        /// give is the giver, so the declaration narrows the giver to the controller and narrows the
        /// RECIPIENT not at all - "a hero" on this card set's conventions is every hero, Bain included.
        /// That is why this condition needs no subject enum beside it, where
        /// <see cref="AfterPushTriggerSubject"/> needed one for "an enemy hero": the subject the
        /// ability learns is a fact the give produces rather than a choice the card makes.</para>
        ///
        /// <para><b>What it excludes, and why the exclusion is structural.</b> Milestone 8.40b's
        /// TRANSFER and RETURN are different capabilities, and only the give reports a recipient, so a
        /// marker arriving by either route reaches nothing here - and a give another hero performed is
        /// that hero's give, read exactly as <see cref="AfterPushTrigger"/> reads "if YOU push" on
        /// Sabina's. BA-D5.</para>
        /// </summary>
        AfterControllerGivesMarker = 7,

        AfterOwnedResolvedCardDiscarded = 8,

        /// <summary>Dragon Knight: every completed Movement action, primary or secondary.</summary>
        AfterControllerPerformsMovementAction = 9
    }

    /// <summary>
    /// The permanent ability one Ultimate card prints, as immutable definition content.
    ///
    /// Rulebook p.16 states the whole of its lifetime in one sentence: "It provides you with a powerful
    /// permanent ability that is always in effect (as long as your hero is on the board)." Everything
    /// this type does follows from that sentence rather than from a modelling preference.
    ///
    /// <para><b>The applicability is not content.</b> A token property declares its own applicability
    /// because different tokens can have different ones; p.16 states one applicability for every
    /// Ultimate there is, so <see cref="ApplicabilityKind"/> is a constant rather than a constructor
    /// argument. A per-definition field here would be a place for a future card to state something the
    /// page does not allow it to state.</para>
    ///
    /// <para><b>The rule is a declaration, not a lifetime.</b> Like
    /// <c>TokenPropertyDefinition</c>, this carries a typed <see cref="RuleModification"/> and nothing
    /// about when it applies. <c>UltimateUnlockQuery</c> binds it to the hero it affects and the board
    /// question that decides whether it applies right now, and nothing is stored: an unlocked Ultimate
    /// supplies its rule again on restore because the card is still beside the dashboard and the
    /// trusted definition still declares this.</para>
    /// </summary>
    public sealed class UltimateRuleDefinition
    {
        /// <summary>
        /// p.16's one applicability, shared by every Ultimate: the rule applies exactly while its
        /// controlling hero is on the board. It is the string a derived
        /// <c>ContinuousRuleSource</c> carries, so a payload records which question was asked rather
        /// than leaving the answer implicit in the source kind.
        /// </summary>
        public const string ApplicabilityKind = "WhileControllerHeroOnBoard";

        public UltimateRuleDefinition(string ruleId, string ruleKind, RuleModification modification)
            : this(
                ruleId,
                ruleKind,
                new[]
                {
                    modification ??
                        throw new ArgumentNullException(
                            nameof(modification),
                            "An Ultimate's whole printed content is its permanent ability (rulebook " +
                            "p.16); a declaration with no rule would be a card that does nothing.")
                },
                PrintedTextComposition.None,
                Array.Empty<PrintedValueDefinition>())
        {
        }

        private UltimateRuleDefinition(
            string ruleId,
            string ruleKind,
            IEnumerable<RuleModification> modifications,
            PrintedTextComposition textComposition,
            IEnumerable<PrintedValueDefinition> printedValues,
            EffectDefinitionReference? triggeredEffect = null,
            IEnumerable<TimingCheckpoint> triggerCheckpoints = null,
            UltimateTriggerCondition triggerCondition = UltimateTriggerCondition.None,
            BeforeActionTrigger beforeAction = default,
            GuardsDigital.Core.Rules.Consequences.MinionBattleRuleDeclaration minionBattleRule = null,
            AfterPushTrigger afterPush = default,
            GrantedTargetExclusion grantedTargetExclusion = GrantedTargetExclusion.None)
        {
            GrantedTargetExclusion = grantedTargetExclusion;
            BeforeAction = beforeAction;
            AfterPush = afterPush;
            MinionBattleRule = minionBattleRule;
            if (string.IsNullOrWhiteSpace(ruleId) || string.IsNullOrWhiteSpace(ruleKind))
            {
                throw new ArgumentException("An Ultimate's ability requires stable identity and a rule kind.");
            }

            RuleId = ruleId;
            RuleKind = ruleKind;
            Modifications = Array.AsReadOnly(
                (modifications ?? Enumerable.Empty<RuleModification>()).ToArray());
            if (Modifications.Any(value => value == null))
            {
                throw new ArgumentException(
                    "An Ultimate's declared rules are typed modifications, never null.",
                    nameof(modifications));
            }

            TextComposition = textComposition;
            TriggeredEffect = triggeredEffect;
            TriggerCondition = triggerCondition;
            TriggerCheckpoints = Array.AsReadOnly(
                (triggerCheckpoints ?? Enumerable.Empty<TimingCheckpoint>()).ToArray());

            // Milestone 8.19a. A before-action trigger runs an effect too, and it runs it at a moment
            // the lifecycle has no checkpoint for, so it satisfies the pairing below by declaring a
            // trigger rather than a checkpoint. The two shapes stay mutually exclusive: one Ultimate
            // prints one ability, and an ability that both preceded an action and followed one would be
            // two sentences the page does not print.
            var isCheckpointTriggered = TriggerCheckpoints.Count > 0;
            if (beforeAction.IsDeclared && isCheckpointTriggered)
            {
                throw new ArgumentException(
                    "An Ultimate declares one trigger: a checkpoint after an action, or a start " +
                    "before one.", nameof(beforeAction));
            }

            // Milestone 8.19c1. An after-push trigger is the third shape of the same pairing, and it
            // stays exclusive with the other two for the same reason they are exclusive with each
            // other: p.16 gives an Ultimate one permanent ability, and a card that both preceded an
            // action and followed a Push would be printing two sentences.
            if (afterPush.IsDeclared && (isCheckpointTriggered || beforeAction.IsDeclared))
            {
                throw new ArgumentException(
                    "An Ultimate declares one trigger: a checkpoint after an action, a start before " +
                    "one, or the moment a Push it performed applied.", nameof(afterPush));
            }

            // Milestone 8.19c. p.16 gives an Ultimate one permanent ability, and a Minion-Battle rule
            // is the whole of the one that declares it. An Ultimate that also modified a value or ran
            // an effect would be two abilities on a card that prints one sentence, and the reader would
            // have to decide which of them "the ability" meant.
            if (minionBattleRule != null &&
                (Modifications.Count > 0 || triggeredEffect.HasValue || beforeAction.IsDeclared ||
                    afterPush.IsDeclared || textComposition.IsAwaited))
            {
                throw new ArgumentException(
                    "An Ultimate that declares a Minion-Battle rule declares nothing else.",
                    nameof(minionBattleRule));
            }

            if (!Enum.IsDefined(typeof(UltimateTriggerCondition), triggerCondition) ||
                triggeredEffect.HasValue !=
                    (isCheckpointTriggered || beforeAction.IsDeclared || afterPush.IsDeclared) ||
                isCheckpointTriggered != (triggerCondition != UltimateTriggerCondition.None) ||
                (triggeredEffect.HasValue && !triggeredEffect.Value.IsDefined) ||
                TriggerCheckpoints.Any(value => value != WatchedMomentOf(triggerCondition)) ||
                TriggerCheckpoints.Distinct().Count() != TriggerCheckpoints.Count)
            {
                throw new ArgumentException(
                    "A triggered Ultimate requires one defined effect and unique supported checkpoints.");
            }

            var values = (printedValues ?? throw new ArgumentNullException(nameof(printedValues)))
                .ToArray();

            // p.9's one icon, on the one card that is not an action. High Voltage prints a Radius its
            // ability reads, and 2026-08-21's approved reading of ledger question 12 makes that icon an
            // item's target - so the value has to be printed content here rather than a number written
            // into whatever composes the ability later.
            if (values.Length > 1 ||
                values.Any(value =>
                    value.Channel != PrintedValueChannel.Radius &&
                    value.Channel != PrintedValueChannel.Range))
            {
                throw new ArgumentException(
                    "An Ultimate prints at most one of rulebook p.9's Range or Radius icons.",
                    nameof(printedValues));
            }

            if (values.Any(value => value.Amount < 1))
            {
                throw new ArgumentException(
                    "A printed Range or Radius icon states a positive value.", nameof(printedValues));
            }

            PrintedValues = Array.AsReadOnly(values);
        }

        /// <summary>
        /// Milestone 8.3's Ultimate whose printed ability is transcribed but not yet composed, naming
        /// the milestone that owes the composition.
        ///
        /// The invariant the ordinary constructor states is unchanged in substance - an Ultimate whose
        /// whole printed content is an ability may not print no ability - and this is the one way to
        /// satisfy it without a typed rule: by saying out loud which milestone owes one. Wasp's High
        /// Voltage needs it because its ability is a triggered one ("Each time after you perform a basic
        /// skill...") and none of <see cref="RuleModification"/>'s five families can express a trigger.
        /// Milestone 8.12 gives the continuous-source declaration a typed effect reference and 8.13
        /// composes this card onto it; until then the card carries its printed Radius, its transcribed
        /// words on the definition, and an explicit debt - never a stand-in rule chosen to fill the
        /// argument.
        /// </summary>
        public static UltimateRuleDefinition AwaitingComposition(
            string ruleId,
            string ruleKind,
            PrintedTextComposition textComposition,
            IEnumerable<PrintedValueDefinition> printedValues)
        {
            if (!textComposition.IsAwaited)
            {
                throw new ArgumentException(
                    "An Ultimate with no typed rule states the milestone that owes its composition.",
                    nameof(textComposition));
            }

            return new UltimateRuleDefinition(ruleId, ruleKind, null, textComposition, printedValues);
        }

        /// <summary>
        /// Milestone 8.24. An Ultimate whose printed ability is simply true while its hero is on the
        /// board, stating more than one rule.
        ///
        /// <para><b>Why the one-modification constructor was not enough.</b> Every Ultimate composed
        /// before Xargatha's either states one rule or runs an effect at a trigger, and the public
        /// constructor above was shaped to the first of those. Metamorphosis states three - "+1
        /// Movement and +1 Initiative for each enemy unit adjacent to you. You may move through
        /// obstacles." - and they are three because the card prints three, not because the modelling
        /// prefers it: two value channels scale independently and the permission scales with nothing.
        /// The private constructor has taken a sequence since milestone 6.2a; this is the public way in,
        /// and it adds no field, no schema and no lifetime.</para>
        ///
        /// <para><b>It stays exclusive with the trigger shapes for the reason they are exclusive with
        /// each other.</b> p.16 gives an Ultimate one permanent ability. Several declarations composing
        /// one printed sentence is not several abilities; an ability that also ran an effect at a
        /// moment would be a second sentence, and this factory cannot express one.</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.35z31. An Ultimate whose permanent ability is a standing <em>declaration</em>
        /// other rules read by name, rather than a rule that changes a value, a permission or a type.
        ///
        /// <para>Ursafar's Unbound Fury is what forces it: "You are always enraged and all your
        /// resolved cards count as active." Neither clause modifies anything. The first asserts a named
        /// hero condition his own faces test; the second widens which of his cards a borrow may reach.
        /// Both are read off the <see cref="ContinuousRuleSource.RuleKind"/> that
        /// <c>UnlockedUltimate</c> already builds from <paramref name="ruleKind"/> while the card sits
        /// beside the dashboard and its hero is on the board - so the ability lives exactly as long as
        /// p.16 says it does, and nothing about it is stored.</para>
        ///
        /// <para><b>It declares no modification, and that is a statement rather than an omission.</b>
        /// <see cref="PermanentAbility"/> refuses an empty declaration because an Ultimate whose whole
        /// printed content is an ability may not print no ability - and that invariant is satisfied here
        /// by the rule kind, which is not decoration: it is the name every reader matches on, so an
        /// Ultimate that declared nothing at all could not be constructed and one that declares this is
        /// read by something. Inventing a modification family for "is enraged" would have been worse
        /// than nothing: the five existing families each answer a question some engine query asks, and a
        /// tenth that no query asked would be a rule the seam conformance could not find a consumer
        /// for.</para>
        /// </summary>
        /// <summary>
        /// The one moment a condition watches. Milestone 8.40h.
        ///
        /// <para>It pairs the two halves of a triggered declaration rather than admitting a list, and
        /// that is the whole of the widening this milestone makes to the guard above: before it, every
        /// condition in this file was matched against a completed action and
        /// <see cref="TimingCheckpoint.ActionEnded"/> was the only checkpoint any of them could name.
        /// <see cref="UltimateTriggerCondition.AfterControllerGivesMarker"/> is matched against a give
        /// that happened inside a running effect, so it names a different moment - and naming the wrong
        /// one is refused here rather than discovered by a trigger that never fires.</para>
        /// </summary>
        private static TimingCheckpoint WatchedMomentOf(UltimateTriggerCondition condition) =>
            condition == UltimateTriggerCondition.AfterControllerGivesMarker
                ? TimingCheckpoint.MarkerGivenToHero
                : condition == UltimateTriggerCondition.AfterOwnedResolvedCardDiscarded
                    ? TimingCheckpoint.ResolvedCardDiscarded
                : TimingCheckpoint.ActionEnded;

        public static UltimateRuleDefinition PermanentDeclaration(string ruleId, string ruleKind) =>
            new UltimateRuleDefinition(
                ruleId,
                ruleKind,
                Array.Empty<RuleModification>(),
                PrintedTextComposition.None,
                Array.Empty<PrintedValueDefinition>());

        /// <param name="printedValues">
        /// Milestone 8.57e. p.9's one Range or Radius icon, where the Ultimate prints one - Rowenna's
        /// Fabled Lance prints a Range 2 beside the sentence that says her attacks count as having it.
        /// The private constructor has taken these since milestone 8.3, and the two trigger factories
        /// already require them; this is the way in for a standing ability, and it adds no field, no
        /// schema and no lifetime. Empty for every Ultimate that prints no icon, which is every
        /// standing ability composed before this milestone - so no earlier declaration's value moves.
        /// </param>
        public static UltimateRuleDefinition PermanentAbility(
            string ruleId,
            string ruleKind,
            IEnumerable<RuleModification> modifications,
            IEnumerable<PrintedValueDefinition> printedValues = null)
        {
            var declared = (modifications ?? Enumerable.Empty<RuleModification>()).ToArray();
            if (declared.Length == 0)
            {
                throw new ArgumentException(
                    "An Ultimate's whole printed content is its permanent ability (rulebook p.16); a " +
                    "declaration with no rule would be a card that does nothing.",
                    nameof(modifications));
            }

            return new UltimateRuleDefinition(
                ruleId,
                ruleKind,
                declared,
                PrintedTextComposition.None,
                printedValues ?? Array.Empty<PrintedValueDefinition>());
        }

        /// <summary>
        /// A permanent Ultimate ability whose trusted declaration enters the ordinary effect frame at
        /// its named checkpoint. It is a continuous source rather than an Active effect, so its
        /// lifetime remains derived from the unlocked card and the controller being on the board.
        /// </summary>
        public static UltimateRuleDefinition TriggeredAbility(
            string ruleId,
            string ruleKind,
            EffectDefinitionReference triggeredEffect,
            UltimateTriggerCondition triggerCondition,
            IEnumerable<TimingCheckpoint> triggerCheckpoints,
            IEnumerable<PrintedValueDefinition> printedValues,
            IEnumerable<RuleModification> standingRules = null,
            GrantedTargetExclusion grantedTargetExclusion = GrantedTargetExclusion.None) =>
            new UltimateRuleDefinition(
                ruleId, ruleKind, standingRules, PrintedTextComposition.None, printedValues,
                triggeredEffect, triggerCheckpoints, triggerCondition,
                grantedTargetExclusion: grantedTargetExclusion);

        /// <summary>
        /// Milestone 8.40h. A permanent Ultimate ability that runs immediately after its controller has
        /// given a hero a marker - Bain's A Complicated Profession, "After you give a hero the [bounty]
        /// bounty marker that hero discards a card, if able."
        ///
        /// <para><b>It is a convenience over <see cref="TriggeredAbility"/> rather than a sixth
        /// factory, and that is the measured point of this milestone.</b> Milestone 8.38d added
        /// <c>GrantedTargetExclusion</c> to this type and it was the first change ever to move all
        /// twelve corpus digests, because the identity oracle walks every public instance property of
        /// every object it reaches. A give-watching declaration expressed as a new property here would
        /// have moved all fifteen for a fact fourteen of them do not carry. It is expressed instead as
        /// one new member of <see cref="UltimateTriggerCondition"/> and one new member of
        /// <see cref="TimingCheckpoint"/> carried in the collection this type already publishes, and an
        /// added enum member changes no existing declaration's value - so no hero's digest moves and no
        /// revision string follows.</para>
        ///
        /// <para>It carries standing rules for the reason <see cref="AfterPushAbility"/> does: p.16
        /// gives the card one permanent ability, and a printed face may state it as a value sentence
        /// plus a trigger sentence. Bain's prints only the trigger.</para>
        /// </summary>
        public static UltimateRuleDefinition AfterMarkerGivenAbility(
            string ruleId,
            string ruleKind,
            EffectDefinitionReference triggeredEffect,
            IEnumerable<PrintedValueDefinition> printedValues,
            IEnumerable<RuleModification> standingRules = null) =>
            new UltimateRuleDefinition(
                ruleId, ruleKind, standingRules, PrintedTextComposition.None, printedValues,
                triggeredEffect,
                new[] { TimingCheckpoint.MarkerGivenToHero },
                UltimateTriggerCondition.AfterControllerGivesMarker);

        /// <summary>
        /// Milestone 8.19a. A permanent Ultimate ability that runs before its controller performs an
        /// action of a named kind, which is Living Tsunami's shape: "Once per turn, before performing
        /// an Attack action, you may move 1 space."
        ///
        /// <para>It is a third factory rather than a parameter on <see cref="TriggeredAbility"/>
        /// because the two answer different questions. That one names a lifecycle checkpoint and a
        /// completed-action condition, both of which describe something that has already happened; this
        /// one names an action that has not started and must start unchanged afterwards, so what it
        /// declares is a kind and a frequency and nothing about checkpoints at all.</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.19c. A permanent Ultimate ability whose whole content is a named rule changing
        /// how rulebook p.8's Minion Battle counts or removes - Brogan's One Man Army, "During minion
        /// battle you count as a heavy minion; if you would be removed, lose the push instead."
        ///
        /// <para>It is a fourth factory rather than a <see cref="RuleModification"/> because none of
        /// that type's five families can say either half. The counting half is not p.17's Counts As:
        /// that replaces the object's rules and qualities everywhere, which would hand this hero a
        /// Heavy minion's p.13 immunity and take his own actions away, while the card scopes both
        /// sentences to the Minion Battle. The removal half is not a modification at all - it replaces
        /// one consequence with another, which is a transition rather than a rule about an object.</para>
        ///
        /// <para>The ability stays as derived as every other Ultimate's: nothing is stored, the rule
        /// applies while the card is unlocked and its hero is on the board, and the reference is
        /// resolved through <c>MinionBattleRuleCatalog</c> so the battle never names a card or a
        /// hero.</para>
        /// </summary>
        public static UltimateRuleDefinition MinionBattleAbility(
            string ruleId,
            string ruleKind,
            GuardsDigital.Core.Rules.Consequences.MinionBattleRuleDeclaration minionBattleRule,
            IEnumerable<PrintedValueDefinition> printedValues)
        {
            if (minionBattleRule == null)
            {
                throw new ArgumentNullException(
                    nameof(minionBattleRule),
                    "A Minion-Battle Ultimate's whole printed content is the rule it declares.");
            }

            return new UltimateRuleDefinition(
                ruleId, ruleKind, null, PrintedTextComposition.None, printedValues,
                null, null, UltimateTriggerCondition.None, default, minionBattleRule);
        }

        public static UltimateRuleDefinition BeforeActionAbility(
            string ruleId,
            string ruleKind,
            EffectDefinitionReference triggeredEffect,
            BeforeActionTrigger beforeAction,
            IEnumerable<PrintedValueDefinition> printedValues)
        {
            if (!beforeAction.IsDeclared)
            {
                throw new ArgumentException(
                    "A before-action Ultimate declares the action kind it precedes.", nameof(beforeAction));
            }

            return new UltimateRuleDefinition(
                ruleId, ruleKind, null, PrintedTextComposition.None, printedValues,
                triggeredEffect, null, UltimateTriggerCondition.None, beforeAction);
        }

        /// <summary>
        /// Milestone 8.19c1. A permanent Ultimate ability that runs immediately after one of its
        /// controller's Pushes has applied to an object it names - Sabina's Big Sodding Gun, "If you
        /// push an enemy hero, that hero discards a card, or is defeated".
        ///
        /// <para>It is a fifth factory rather than a checkpoint on <see cref="TriggeredAbility"/>
        /// because that factory's checkpoints are the lifecycle's own action windows, and this one
        /// fires <em>inside</em> an effect that is still running. What resumes afterwards is the
        /// enclosing effect at the exact node it left, which is the whole reason the moment needed a
        /// milestone of its own.</para>
        ///
        /// <para><b>Milestone 8.22 let it carry standing rules beside the trigger, and the card is why.</b>
        /// Big Sodding Gun's full face is two sentences: "Your basic attack has +2 Range and +2 Attack.
        /// If you push an enemy hero, that hero discards a card, or is defeated." The first is two
        /// standing value declarations and the second is the trigger, and p.16 gives the card one
        /// permanent ability made of both - the escalation is what her own Push does, and the Push is
        /// reachable partly because the first sentence extended her basic Attack's reach. Refusing the
        /// combination would have forced one half of a printed face to be modelled as something else.
        /// This is not the exclusivity the three trigger shapes keep between <em>each other</em>: those
        /// are three answers to one question - when does the ability run - and an Ultimate still
        /// declares exactly one of them.</para>
        /// </summary>
        public static UltimateRuleDefinition AfterPushAbility(
            string ruleId,
            string ruleKind,
            EffectDefinitionReference triggeredEffect,
            AfterPushTrigger afterPush,
            IEnumerable<PrintedValueDefinition> printedValues,
            IEnumerable<RuleModification> standingRules = null)
        {
            if (!afterPush.IsDeclared)
            {
                throw new ArgumentException(
                    "An after-push Ultimate names the pushed object it fires for.", nameof(afterPush));
            }

            return new UltimateRuleDefinition(
                ruleId, ruleKind, standingRules, PrintedTextComposition.None, printedValues,
                triggeredEffect, null, UltimateTriggerCondition.None, default, null, afterPush);
        }

        /// <summary>This ability's stable identity inside its card, and the derived source's identity suffix.</summary>
        public string RuleId { get; }

        public string RuleKind { get; }

        /// <summary>
        /// The typed rules this Ultimate supplies while its hero is on the board, or empty when
        /// <see cref="TextComposition"/> names the milestone that owes them, when the ability is a
        /// trigger rather than a rule, or when it is a Minion-Battle declaration.
        ///
        /// <para><b>Milestone 8.22 made it a list, and the card that forced it is the reason.</b>
        /// Sabina's Big Sodding Gun prints "Your basic attack has +2 Range and +2 Attack" - one
        /// sentence, one ability, and two of <see cref="RuleModification"/>'s value declarations,
        /// because a modification names one channel. A single-valued field could only have carried it
        /// by inventing a two-channel modification, which would make every other value declaration in
        /// the build answer a question it does not have.</para>
        ///
        /// <para>Order is the order the definition declares, kept stable by
        /// <c>RuleModificationSet</c>'s sort rather than re-derived. It decides nothing here: two value
        /// declarations on different channels never interact, and two on one channel sum.</para>
        /// </summary>
        public IReadOnlyList<RuleModification> Modifications { get; }

        public EffectDefinitionReference? TriggeredEffect { get; }

        public UltimateTriggerCondition TriggerCondition { get; }

        public IReadOnlyList<TimingCheckpoint> TriggerCheckpoints { get; }

        /// <summary>
        /// Milestone 8.38d. The narrowing this ability puts on the targets of the actions it GRANTS,
        /// and on nothing else - Bullet Time's "you cannot target the same enemy hero twice in the same
        /// turn this way". <see cref="GrantedTargetExclusion.None"/> for every other ability in this
        /// build, which is what the enum's own doc records.
        /// </summary>
        public GrantedTargetExclusion GrantedTargetExclusion { get; }

        /// <summary>
        /// Milestone 8.19a. `before-action-trigger@1`, or <see cref="BeforeActionTrigger.None"/> on
        /// every Ultimate that declares no such trigger - which is every one in this build before
        /// Arien's.
        /// </summary>
        public BeforeActionTrigger BeforeAction { get; }

        /// <summary>
        /// Milestone 8.19c1. `after-push-trigger@1`, or <see cref="AfterPushTrigger.None"/> on every
        /// Ultimate that declares no such trigger - which is every one in this build before Sabina's.
        /// </summary>
        public AfterPushTrigger AfterPush { get; }

        /// <summary>
        /// Milestone 8.19c. The Minion-Battle rule this Ultimate declares, or <c>null</c> on every
        /// Ultimate that declares none - which is every one in this build before Brogan's.
        /// </summary>
        public GuardsDigital.Core.Rules.Consequences.MinionBattleRuleDeclaration MinionBattleRule { get; }

        /// <summary>
        /// Stable effect-frame identity for a permanent ability. An Ultimate is not an action, but the
        /// shared frame addresses its source text through the same typed slot used by action text.
        /// </summary>
        public ActionDefinitionId EffectSourceId => new ActionDefinitionId(RuleId);

        /// <summary>
        /// Milestone 8.3. Whether this Ultimate's printed ability still owes its typed composition, and
        /// to which milestone. <see cref="PrintedTextComposition.None"/> whenever
        /// <see cref="Modifications"/> is non-empty.
        /// </summary>
        public PrintedTextComposition TextComposition { get; }

        /// <summary>
        /// Milestone 8.3. The p.9 Range or Radius icon this Ultimate prints, or empty. Empty for every
        /// Ultimate built before 8.3, which printed none.
        /// </summary>
        public IReadOnlyList<PrintedValueDefinition> PrintedValues { get; }

        public override string ToString() =>
            $"{RuleId} ({RuleKind}): {DescribeContent()}";

        private string DescribeContent()
        {
            if (Modifications.Count > 0) return string.Join("; ", Modifications);
            if (MinionBattleRule != null) return MinionBattleRule.ToString();
            return TextComposition.ToString();
        }
    }
}
