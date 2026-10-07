namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.31. The fixed constants of `guards-private-online` protocol version 1's session
    /// slice: what a frame looks like, how large anything may be, and how long an unadmitted
    /// connection may stand.
    /// </summary>
    /// <remarks>
    /// <para>These are protocol facts, not tuning knobs: a change to any of them changes what a
    /// peer accepts, so the amended architecture record's upgrade rule applies - if wire meaning
    /// moves, <see cref="Version"/> moves with it and the prior version is rejected.</para>
    /// <para>The bounds exist so that nothing about an incoming byte sequence is trusted before it
    /// is checked: a declared payload length is validated against <see cref="MaxPayloadBytes"/>
    /// before any buffer for it exists, and a handshake that has not completed by its deadline is
    /// closed rather than held open indefinitely.</para>
    /// </remarks>
    public static class PrivateSessionProtocol
    {
        /// <summary>The protocol family the amended 8.29 decision record pins.</summary>
        public const string FamilyName = "guards-private-online";

        /// <summary>
        /// Protocol version 9: the ELEVENTH amendment's host-selectable match formats. The admission
        /// tuple gains a format element, so the <c>Hello</c> payload carries one more string between
        /// the map revision and the content digest. A version 8 peer's reader takes that string as
        /// the content digest and then finds the key proof missing, so its hello does not
        /// mis-compare - it fails to decode at all, which is precisely why the version is bumped:
        /// version 8 is refused from the bounded header, before either side interprets a payload.
        ///
        /// <para><b>The schema does NOT move, and that is the measured half.</b> Rulebook p.5-6's
        /// four printed setups differ in three numbers - how many players, how many Wave counters,
        /// how many Life counters a team starts with - and the snapshot has written all three as
        /// ordinary counted values since long before this milestone: the Wave total as an
        /// <c>Int32</c> in the round section, each team's Life counters beside it since v20, and the
        /// participants as a counted list. A 2v2 payload is therefore a v36 payload of different
        /// values, not of a different shape, and no corpus digest, pinned constant or bare version
        /// assertion moves. The wire bump is not evidence to the contrary: the wire grew a NEW
        /// element that never existed, and the snapshot grew nothing.</para>
        ///
        /// <para>Protocol version 8: milestone 8.35z14 (the blocker-identity channel) gave the
        /// seat-public match log's own <c>BlockedAction</c> row two more optional fields - the hero controlling
        /// the standing rule modification that refused the step, and the card its source came off -
        /// appended after <c>amount</c> at the end of <c>NetworkMatchLogRow</c>. A version 7 peer's
        /// reader stops after <c>amount</c> and never reads these two fields at all, so it does not
        /// mis-decode a truncated record - it silently drops a fact a newer peer would have carried -
        /// which is exactly the kind of drift the bounded header exists to make loud instead: version
        /// 7 is refused before either side interprets a payload.
        ///
        /// <para>It rides the existing <c>BlockedAction</c> row rather than opening a new message
        /// kind, the same reasoning milestone 8.35z3's frame-degradation corrective and milestone
        /// 8.35r's own event channel both record: the row already exists, is already seat-public
        /// (rulebook p.17 makes an Active effect's controller and source card public information),
        /// and the two new fields are absent on every OTHER row kind and on a <c>BlockedAction</c>
        /// row whose refusing node could not explain itself - the same honest absence milestone
        /// 8.35l's <c>ActionUnavailability.None</c> already leaves rather than guesses. It is carried
        /// rather than derived for the same reason 8.35l's own offer-side reason is: a seat holds only
        /// a flat list of the cards standing effects came off, which cannot say WHICH one refused THIS
        /// step, so deriving it client-side would be a second reading of the host's rules, free to
        /// disagree with it.</para>
        ///
        /// <para>Schema is UNMOVED at v36: <c>MatchLogEntry</c> is derived from the retained rule-event
        /// log on every reconstruction and never itself persisted (see its own remark), so widening it
        /// changes what crosses the wire and nothing an <c>AuthoritativeSnapshotSerializer</c> payload
        /// contains.</para>
        /// </summary>
        /// <remarks>
        /// Protocol version 10: milestone 8.35z37 wired hero selection to the host. ONE new
        /// client-to-host kind, <see cref="SessionMessageKind.ChooseHero"/>, carrying a bounded hero
        /// id and nothing else - the connection identifies the seat, exactly as
        /// <see cref="SessionMessageKind.SetReadiness"/> does, so a seat cannot pick for another.
        ///
        /// <para><b>The downstream half moved no bytes at all, and that is measured rather than
        /// hoped for.</b> A seat's chosen hero reaches every client on the roster row that has
        /// carried <c>HeroId</c> since milestone 8.31, and the refusal rides the reject path every
        /// other lobby refusal rides. So the only reason this is a bump is that a version 9 peer has
        /// no case for kind 21 and would refuse it as an unexpected message - which is a protocol
        /// surface change and is taken as one, on the standing rule that a version which only moves
        /// when a hole opens is a version nobody trusts.</para>
        ///
        /// <para><b>Schema is UNMOVED at v36, and the composition seam is why.</b> The milestone
        /// also makes a match composable AFTER the scene loads, from a line-up a lobby chose. That
        /// changes WHEN a match is built and never WHAT a built match contains: a match composed
        /// late from a line-up is byte-identical to the same match composed at scene load, which is
        /// asserted by comparing two composed sessions' snapshot payloads over all four printed
        /// formats rather than argued here. Composition timing is not serialized state, so there is
        /// nothing for a schema version to describe.</para>
        ///
        /// <para><b>The identity tuple does not move either, and that is load-bearing.</b> A
        /// session's admission tuple - including <c>ContentDigest</c>, which hashes the DEALT
        /// configuration's card definitions - is computed once when hosting begins and kept for the
        /// session's life. It must be: a client that returns under milestone 8.34's reserved-seat
        /// reconnect re-runs the whole handshake, and it computes its own tuple from its own local
        /// composition, which knows nothing of anybody's picks. Recomposing the host's match with a
        /// chosen line-up while republishing the identity would refuse every reconnect with
        /// <c>ContentDigestMismatch</c> at a 2v2, where a chosen line-up genuinely deals a different
        /// four sets. Freezing the tuple is also the honest reading of what it is for: it answers
        /// "can these two builds interoperate", and both builds can deal every registered hero. Which
        /// heroes THIS match deals is a decision inside a compatible pair, exactly as the Tie Breaker
        /// coin is.</para>
        ///
        /// <para>Protocol version 6: milestone 8.35r opened the seat-public event channel and published
        /// who is being waited on. Three things move, and they move together deliberately - this is
        /// Protocol version 7: milestone 8.35u made rulebook p.15's leveling simultaneous, so the
        /// authority can be waiting on several heroes at once and <c>DecisionOwner</c> carries a
        /// repeated hero group where version 6 wrote one optional hero. A version 6 peer reads the
        /// count where it expects a present/absent boolean, so it does not read a truncated record -
        /// it reads a different one - and version 6 is refused from the bounded header.
        ///
        /// <para><b>What did NOT move, and it is the more interesting half.</b> The paused
        /// transition's own detail row - item, chooser, options - is byte-identical to version 6,
        /// because milestone 8.32's per-seat encoding discipline had already made each seat's payload
        /// its own: the seat projection narrows a pause standing on six heroes to the one item that
        /// seat may answer, so the row has never had more than one item to carry and still does not.
        /// The public half of a simultaneous pause is who is being waited on, and that is exactly the
        /// field that grew. A shape change absorbed by an existing privacy boundary is worth
        /// recording as a property of that boundary rather than as luck.</para>
        ///
        /// <para>The bump is honesty rather than a hole being closed: milestone 8.35u also moves the
        /// snapshot schema to v36, and <c>PrivateSessionIdentity</c> already refuses admission on a
        /// <c>SnapshotSchemaMismatch</c>, so a mixed-version session was impossible before this line
        /// was written. The ratchet is kept anyway - a protocol version that only moves when it has
        /// to is a protocol version nobody trusts.</para>
        ///
        /// <para>Version 6 was the one bump the remaining wire growth was gathered into, rather than
        /// three.</para>
        ///
        /// <para>(1) The seat projection message now carries the match log beside that seat's own
        /// record, as a bounded window of typed lines. A version 5 peer reaches the end of the
        /// record and then meets a line count it has no field for. (2) The projection detail
        /// record gained the standing rewind marker at its end; a version 5 peer reading a version 6
        /// record would run off the end of the payload. (3) <c>DecisionOwner</c> joined
        /// <c>IMatchReadModel</c> - and it is the one part that moves NO bytes, because the seat
        /// projection record has carried it since milestone 8.32 and only the read-model contract
        /// was missing it. It is named here so the record does not read as if it grew for it.</para>
        ///
        /// <para>The event channel is a separate kind rather than a field on the seat projection
        /// because the size was measured, not estimated: the largest complete projection across the
        /// seeded six-seat walk is 30,574 bytes, and the synchronization sweep's own guard is half
        /// the sealed frame bound - 32,747 - leaving 2,173 bytes. A window of recent history does
        /// not fit there, and that guard is a ratchet rather than a budget to spend.</para>
        ///
        /// <para>Protocol version 5: milestone 8.35l gave a greyed action offer a reason. Each offer row
        /// now carries an optional block naming the standing rule modification that refused it - its
        /// kind, the activity barred, and the controlling hero, source card and Active effect. A
        /// version 4 peer would read that block as the next offer's card id and mis-decode the whole
        /// remainder of the detail record, so version 4 is rejected before either side interprets a
        /// payload.
        ///
        /// <para>It is carried rather than derived, and the wire-growth rule's exclusion floor is why:
        /// the only material a seat holds is a flat list of the cards standing effects came off, which
        /// cannot say which of them refused this action, and a seat that worked the answer out for
        /// itself would be re-deriving the rules the host already applied - the exact split the
        /// blocked-reason design forbids. The cost is five fields on offers that are unavailable, and
        /// one bool on every other offer.</para>
        ///
        /// <para>Protocol version 4: milestone 8.35g added the turn release - one new command kind, and
        /// the eleventh pending family carried in the projection detail as
        /// <c>PendingTurnRelease</c>. It is carried rather than derived from the standing decision
        /// owner because the shared status derivation reads the pause itself, and the wire-growth
        /// rule's exclusion floor exists to stop exactly that kind of excuse. A version 3 peer would
        /// read the release row out of the middle of a detail record and mis-decode everything after
        /// it, so it is rejected before either side interprets the payload.
        ///
        /// <para>Version 3 was milestone 8.35f's seat undo - two message kinds and one carried
        /// projection fact, <c>IsSeatUndoAvailable</c>, which a client cannot derive because the
        /// bounds are facts about the host's recorded history. Version 2 applied the same rule to
        /// version 1 when milestone 8.35b gave SelectedActor its Tie Breaker meaning. Every
        /// superseded version is refused from the bounded header rather than reinterpreted.</para>
        /// </remarks>
        /// <remarks>
        /// <para>Protocol version 11: milestone 8.35z60 carries rulebook p.8's shared Wave-counter
        /// pool to every seat. ONE new <c>Int32</c> on <c>NetworkSeatProjectionRecord</c>, written
        /// beside the per-team Life counters it is the table-wide counterpart of. A version 10 peer's
        /// reader would take that integer as the following hero-row count and mis-decode the entire
        /// remainder of the frame - a silent structural drift rather than a dropped tail - which is
        /// exactly what the bounded header exists to refuse: version 10 is rejected before either
        /// side interprets a payload.</para>
        ///
        /// <para><b>Why carried rather than derived, and why not optional.</b> A seat holds no rule
        /// material that can say how many pushes a match has spent, so a client-side count would be a
        /// second reading of the host's rules, free to disagree with it - the same call milestone
        /// 8.35z14's blocker channel records. It is a plain integer rather than an optional pair
        /// because the host always knows the number: it reads off
        /// <c>LanePushTransitionAuthority.RemainingWaveCounters</c>, which exists for the whole life
        /// of a composed match, so an "absent" branch would be one no run can reach.</para>
        ///
        /// <para><b>This closes a debt the milestone's own worktree recorded rather than hid.</b>
        /// 8.35z60 was authored under a "zero wire" constraint and therefore took an exclusion slot
        /// in <c>PrivateOnlineSynchronizationTests</c>'s carrier map, raising that suite's exclusion
        /// floor from 7 to 8 with the note "tracked as integration-owed". The integration pass paid
        /// it: the exclusion entry is gone, the floor is back at 7, and the member is carried for
        /// real. Every real playtest of this project is networked, so a host-only readout would have
        /// left the milestone correct for exactly one seat at the table.</para>
        ///
        /// <para><b>Schema is UNMOVED at v36.</b> The Wave total has been an ordinary <c>Int32</c> in
        /// the snapshot's round section since long before this milestone; what moved is which number
        /// a REMOTE READ MODEL reports, not what an <c>AuthoritativeSnapshotSerializer</c> payload
        /// contains. No corpus digest, pinned constant or bare version assertion moves.</para>
        /// </remarks>
        // Version 12 requires authentication for a keyed client's forfeiting Goodbye.
        // Older peers are rejected before admission; snapshot and payload layouts are unchanged.
        //
        // Version 13 (milestone 8.37) carries a pending effect choice's VALUE VOCABULARY, so a joined
        // seat can draw six colours or six directions rather than the digits 0 through 5. Tali the
        // Spiritcaller is the first hero whose integer candidates are not counts, and Commune with
        // Spirits puts one of those decisions to a hero who is not the acting seat - so the seat that
        // must answer it is, by construction, a REMOTE one, and a host-only reading would have left
        // her signature Skill unanswerable at every table this project actually plays at.
        //
        // SCHEMA IS UNMOVED at v41. The vocabulary is the handler's own declaration about the set it
        // just offered, and a snapshot restore re-asks that handler, so nothing needs to persist it -
        // it is derived on restore exactly as the candidates it describes already are. What moved is
        // what a REMOTE READ MODEL is told, not what a payload contains: no corpus digest, pinned
        // constant or bare version assertion moves with it.
        // Version 14 (milestone 8.39g) carries a SOLO ending. Every projected outcome before it named
        // a winning team and a losing team, because every one of the rulebook's three victory
        // sentences is about teams; Cutter's A Fistful of Coins awards the match to one HERO and
        // leaves that hero's own team-mates among the losers, so an outcome row that could only name
        // teams could not state it to any seat - least of all to a seat that lost to it.
        //
        // Version 15 (milestone 8.40f) carries a REVEAL that card text performed. The seat-public
        // match log gained one member - `MatchLogEntryKind.CardRevealedByCardText` - and
        // `NetworkMatchLog.TryRead` validates every row's kind with `Enum.IsDefined`, so a version 14
        // peer meeting the new number does not mis-read the row: it refuses the whole FRAME and the
        // seat goes dark. That is why this moves rather than being absorbed, and it is the one kind
        // of change this ratchet exists for.
        //
        // THE ROW ITSELF GREW NO BYTES. `MatchLogEntry` has carried `SubjectHeroId` and
        // `SubjectCardInstanceId` since milestone 8.35r, and this member spends exactly those two, so
        // the layout is byte-identical and only the VOCABULARY widened.
        //
        // WHY A NEW MEMBER RATHER THAN `CardRevealed`, measured rather than argued. Every consumer of
        // that member reads it as rulebook p.7's turn-card commitment: the HUD's flip cue borrows the
        // hero's CURRENT CARD SLOT to animate it at, the reveal batch is re-staged in printed-Initiative
        // order because the commitment flow emits its reveals in roster order, and the channel's own
        // one-for-one ledger asserts exactly as many rows of that kind as `Commitment.CardRevealed`
        // events occurred. A card turned face up out of a HAND mid-resolution is in neither place, so
        // reuse would have animated the wrong card and falsified a declared ledger - three rules the
        // printed sentence does not carry, bought to save a version number.
        //
        // SCHEMA IS UNMOVED at v42. The reveal's memory is the DERIVED seat-public log, re-derived
        // from the retained rule-event log on restore, replay and Undo exactly as every other row of
        // it is; nothing new is persisted, and no corpus digest or pinned constant moves with it.
        //
        // Version 16 (milestone 8.51) carries a HOLD. The seat-public match log gained one member -
        // `MatchLogEntryKind.CardHeld`, rulebook p.12's Hold performed by the seat's own command -
        // and `NetworkMatchLog.TryRead` validates every row's kind with `Enum.IsDefined`, so a
        // version 15 peer meeting the new number refuses the whole frame exactly as a version 14
        // peer did at 8.40f. The row spends `SubjectHeroId` and `SubjectCardInstanceId`, which the
        // row layout has carried since 8.35r: NO BYTES GREW, only the vocabulary widened. It exists
        // because the 2026-09-18 playtest's "Silverarrow's card got resolved without input" could
        // be read off the wire log only by the timing of two silent submissions; the host narration
        // and the in-game tray now state the Hold outright.
        //
        // SCHEMA IS UNMOVED at v42: the row is re-derived from the retained rule-event log, whose
        // `Turn.HoldActionPerformed` event the engine has recorded for as long as the Hold existed;
        // no corpus digest or pinned constant moves with it.
        // Version 17 (milestone 8.53) carries the MATCH SCOREBOARD. `NetworkSeatProjectionRecord`
        // gains a repeated row - hero id plus six counters: kills, deaths, attacks at heroes, times
        // defended, minions killed and coins earned - written between the per-hero progression rows
        // and the Active-effect source list. A version 16 peer's reader takes that row count as the
        // Active-effect source count and mis-decodes the entire remainder of the frame - a silent
        // structural drift rather than a dropped tail - which is exactly what the bounded header
        // exists to refuse: version 16 is rejected before either side interprets a payload.
        //
        // CARRIED RATHER THAN DERIVED, and here the exclusion floor's rule does not even have to be
        // invoked, because derivation is not available at all. Every fact the board counts is
        // seat-public and does cross the wire - as a `MatchLogEntry` row - but that log is a BOUNDED
        // WINDOW of the most recent `MatchLogProjection.RetainedEntries` lines, by construction,
        // because a bounded window is what lets one frame carry the whole thing to a seat that just
        // arrived. A client counting its own totals out of that window would show a player FEWER
        // kills the longer they played, and would show a seat that reconnected a board that started
        // at the moment it returned. The host accumulates the totals once; the wire carries them.
        //
        // SCHEMA MOVES WITH IT, to v43, which is the unusual half and is stated rather than glossed:
        // almost every wire bump in this series has left the snapshot alone, because what moved was
        // what a remote read model is TOLD. This one moves both because the scoreboard is genuinely
        // new authoritative state - it is accumulated, not derived, so it has to be persisted - and
        // `PrivateSessionIdentity` refuses admission on a `SnapshotSchemaMismatch` anyway, so a
        // mixed-version session was impossible before this line was written. The ratchet is kept
        // regardless, on the standing rule that a version which only moves when a hole opens is a
        // version nobody trusts.
        // Version 18 (milestone 8.61e) carries the SPRUNG-TRAP ROW. `MatchLogEntryKind` gains
        // `MoveThroughTrapSprung`, which is Min the Dragonmonk's mines going off: "After an enemy hero
        // moves through a mine, remove it; for every blast removed, that hero discards a card, if
        // able." NO BYTE OF THE FRAME MOVES - the log row's layout is the same fourteen fields it has
        // been since version 8, and what is new is a value in the `Kind` int it already carries, spent
        // on `SubjectHeroId`, `Origin` and `Amount`, which are three fields that row already has.
        //
        // THE BUMP IS NOT ABOUT LAYOUT, AND THAT IS THE WHOLE REASON IT IS HERE. `NetworkMatchLogCodec
        // .TryRead` refuses any kind its own build does not declare, so a version 17 peer meeting this
        // number does not lose a line - it refuses the frame and the seat goes dark. Versions 15
        // (8.40f's card-text reveal) and 16 (8.51's held card) were bumped for exactly this, and the
        // rule they set is the one followed here: a new member of a closed wire vocabulary is a
        // breaking change even when nothing about the bytes is.
        //
        // WHY THE ROW HAS TO EXIST AT ALL. Until the moment it goes off, a mine is facedown, and every
        // seat but its controller's is handed a face that names no kind (milestone 8.61d). The removal
        // is where p.4's hidden component becomes public, and the board shows only a space emptying -
        // so without this line the reveal would happen at the table and be nowhere in the record.
        // That is the same hole 8.40f's reveal row was minted to close, and the log is again the only
        // surface that can close it.
        //
        // SCHEMA IS UNMOVED at v43: the row is re-derived from the retained rule-event log, and what a
        // pause the trap opens carries across a snapshot is an ordinary effect interrupt, whose kind
        // the card-action payload already writes as an int. No corpus digest or pinned constant moves.
        // Version 19 (milestone 8.35z73) carries the COIN-AWARD ROW. `MatchLogEntryKind` gains
        // `CoinAwarded`, one row for each authoritative Defeat, assist, or minion-bounty ledger
        // operation. The existing subject and amount fields name the recipient and exact award; no
        // frame field or authoritative snapshot state is added. As with version 18, the closed enum
        // vocabulary makes the otherwise layout-neutral addition breaking, so version 18 peers are
        // refused before they can encounter a kind they do not declare. Schema therefore remains v43.
        //
        // Version 20 (milestone 8.71a) carries the REWRITTEN PRINTED INITIATIVE. Hanu the Trickster's
        // Hurry up! prints "Set the printed [Initiative] Initiative value of an unresolved card of a
        // hero in range to 11, until it is resolved or otherwise changes state", and each seat
        // projection's two card sections now end in a SPARSE block - a count, then one card instance
        // id, the number its printed icon reads, and the card instance whose text wrote it, per
        // rewritten card. A version 19 peer's reader stops after the last card row and then takes this
        // count as whatever field follows, so it does not silently drop a fact - it mis-decodes the
        // rest of the frame, which is exactly why the version moves and version 19 is refused from the
        // bounded header before either side interprets a payload.
        //
        // It rides the card rows rather than opening a message kind, because it is a fact ABOUT those
        // cards and is table-public for the same reason their states are: the rewritten card sits face
        // up in the turn area where every player can read it, and p.7 orders the round by the number
        // on it. It is carried rather than derived because no client holds anything that says a
        // sentence was spoken - deriving it would be a second reading of the host's rules, free to
        // disagree with the one the round is being ordered by.
        //
        // SCHEMA MOVES WITH IT, to v44, and that is the honest half: the rewrite is authoritative
        // state that no live state remembers, so the snapshot grew the same sparse block. See
        // `AuthoritativeSnapshotSerializer.CurrentSchemaVersion` for the measured cost and for why a
        // v43 payload is rejected rather than migrated.
        //
        // Version 21 (milestone 8.80) carries the ATTACKING CARD ON THE DEFENSE QUESTION. The
        // pending Defense row gained four strings between the attacker id and the attack total - the
        // attacking card's instance id, its definition id, that definition's content revision, and
        // the action definition id being performed off it. A version 20 peer's reader takes the
        // first of them as the attack total's four bytes and then reads the offer count out of the
        // middle of a string, so it does not lose a fact - it mis-decodes the rest of the detail
        // record, which is why the version moves and version 20 is refused from the bounded header
        // before either side interprets a payload.
        //
        // It rides the CHOICE rather than the Attack frame because the frame is withheld by design:
        // `SeatProjection` hands `PendingAttackResolution` to the ACTING seat alone (8.40f's privacy
        // arm), and the seat being asked to defend is by construction not that seat. So since 8.58
        // drew the attacker's card beside the offer, every network seat - the host's own loopback
        // seat included - has been shown the candidates and no attack. The three facts are TABLE
        // PUBLIC: the attacking card was revealed at p.7's commitment and stands face up in the turn
        // area, where each seat's own `Cards` already carries it, and the action being performed off
        // it is the table's own event the match log already narrates. Nothing private of the
        // attacker's rides with them.
        //
        // SCHEMA IS UNMOVED at v44: the choice is derived from the authoritative Attack state every
        // time a projection is built and is never persisted, so no snapshot field, corpus digest or
        // pinned constant moves with this bump.
        // v22: the admission tuple's snapshot schema advances to 45 for persisted marker turn slots.
        // Peers with older authoritative-state shape must refuse each other before gameplay.
        // v23: marker projection rows carry the temporary counts-as-active state (snapshot v46).
        // v24 (milestone 8.108): the seat claim and every roster row carry the player's name. A v23
        // reader would take the name's length prefix as the next field, so v23 peers are refused
        // from the header. Lobby-only: no projection, match-log, snapshot or schema field moves.
        // v25 (milestone 9.11): two fields the completeness walk found dropped are carried. The
        // system-transition description row gains the item it describes (a level-up description
        // named `hero:level:upgrade` on the host and nothing on a client), and the effect-choice row
        // gains the node's own DeclinableRegardlessOfMinimum (a client had passed the Decline OFFER
        // as that flag). A v24 reader would take the new trailing bytes as the next row, so v24
        // peers are refused from the header. Projection-only: no snapshot or schema field moves
        // (v48 stands) - both facts are derived afresh every time a projection is built.
        // v26 (milestone 9.21a): a hero may stand on several spaces as one hero (Razzle, RZ-Q1-RZ-Q3).
        // The board's hero rows carry the body ordinal, an Attack target and its Defense preview name
        // the attacked body, and an Attack-target answer carries the body it names (0 for the hero
        // alone). A v25 reader would take the body ordinal for the next field, so v25 peers are
        // refused from the header. The admission tuple's snapshot schema advances to 49 with it.
        // v27 (milestone 9.21b): "which of you acts" (RZ-Q4). A new command kind, ChooseActingBody
        // (hero, card, body), rides the decision contract and the seat submission under the new
        // acting-body family. A v26 reader has no such kind and would refuse the contract that carries
        // it, so v26 peers are refused from the header. No projection row moves: the pick's answers
        // name every body and the bodies' spaces are already on the hero rows. The admission tuple's
        // snapshot schema advances to 50 with it.
        // v28 (milestone 9.21j): each hero row ends with whether that body is its hero's settled acting
        // body (IMatchReadModel.ActingBodies, 9.21i's acting cue), which /27 carried on no row, so a
        // network client drew no acting cue on a many-bodied hero. A v27 reader would take the flag
        // for the next row's id, so v27 peers are refused from the header. Projection-only: the
        // snapshot already persists the settled body (v50 stands).
        // v29 (milestone 9.19): the seat-public match log gained MatchLogEntryKind.
        // CardDiscardedByCardText, the row for a card that card text sent out of a hand (Charged
        // Runestone, the Seismic faces, Trample, Mortimer's Dead faces), which until then nobody at
        // the table could see. No bytes grew - the row spends SubjectHeroId and SubjectCardInstanceId
        // as the reveal row does - but NetworkMatchLog.TryRead validates every kind with
        // Enum.IsDefined, so a v28 peer meeting the new number would refuse the whole frame and go
        // dark; the header refuses it first. Snapshot schema unmoved at v50.
        // v30 (milestone 9.29): each choosing seat picks its own Minion Battle removals. A team
        // agreement row's participants each carry their own picks (a counted unit list) and a
        // picks-complete flag in place of the retired agreed flag, the row drops the shared selected
        // units and confirmable flag, and an agreement statement carries an optional unit after its
        // preferred card. A v29 reader would misread every participant after the first, so v29 peers
        // are refused from the header. Snapshot schema unmoved at v50: picks are host-local agreement
        // state and never persisted.
        // v31 (milestone 9.30b): the spellbook zone. Each hand-summary row ends with the hero's
        // facedown spellbook count (IMatchReadModel.HandSummaries, the one way a seat counts another
        // hero's book), and a card row's location may now name the spellbook or the faceup removed
        // pile. A v30 reader would take the count for the next row's hero id, so v30 peers are refused
        // from the header. The admission tuple's snapshot schema advances to 51 with it.
        // v32 (milestone 9.30h): the spellbook as the table sees it. Each hand-summary row ends with
        // the facedown book counted by the back each spell shows (a counted list of Tier and count,
        // the tier backs every seat sees, GY-Q4), and the seat-public match log gains three kinds -
        // a spell cast, a spell removed faceup by card text, and a refill's count - which a v31
        // peer's Enum.IsDefined check would refuse. A v31 reader would take the back count for the
        // next row's hero id, so v31 peers are refused from the header. Snapshot schema unmoved at
        // v51: the backs are derived from the persisted cards and the log from the rule-event log.
        // v33 (milestone 9.30g): Gydion's Wish (GY-Q10). The seat record carries every team's count
        // toward a printed team victory (team id, count, printed threshold) after the Wave pool, so
        // every seat's plaque reads "n/3"; and an outcome row's cause may be WishesCast, a team ending
        // in the existing two-team shape. A v32 reader would take the tally count for the hero rows'
        // count, so v32 peers are refused from the header. The admission tuple's snapshot schema
        // advances to 52 with it.
        // v34 (milestone 9.32a): a card placed facedown in a turn slot (Takahide's Bushido, p.10).
        // Every turn-slot assignment row - the current round's and the completed-round history's -
        // ends with whether its card was placed facedown, and a facedown card another seat may not
        // name arrives with no card id: withheld, p.4's hidden information, because a card instance
        // id names its definition. A v33 reader would take the flag for the next row's hero id, so
        // v33 peers are refused from the header. The admission tuple's snapshot schema advances to 54
        // with it (the slot record persists the fact).
        // v35 (milestone 9.32g): every seat sees a facedown gold as a gold (the user's 2026-10-02
        // ruling: a card Bushido turns facedown can only be a gold). Each turn-slot assignment row -
        // current and completed - ends with its known colour (None but on a withheld row), and each
        // hand-summary row ends with the colours of the facedown cards in that hero's discard pile.
        // Each match-log row ends with its card moves (a swap's, a kept swap's or Ready for War's,
        // named on the owner's seat alone), and the log gains three kinds a v34 peer's Enum.IsDefined
        // check would refuse. A v34 reader would take the colour for the next row's hero id, so v34
        // peers are refused from the header. The snapshot schema does not move: withheld rows exist
        // only in projections, and the log is derived from the rule-event log.
        // v36 (milestone 9.48): atomic two-card commitment and owner-scoped post-reveal
        // retrieval add two command kinds and each commitment row gains its two-card
        // permission flag; snapshot schema advances to v56 for the second commitment
        // identity. Older peers are refused at admission.
        // v37: experimental settings, private allied hands and independent teammate signals.
        // v39: isolated, independently sequenced clock display updates; gameplay views stay stable.
        // v43 (milestone 9.69): experimental options add an enabled flag and non-negative Minion
        // Battle removal cap. A v42 peer would stop before those fields and misread the enclosing
        // projection, so older peers are refused at admission. Core snapshot schema 59 is unchanged;
        // the experimental envelope advances separately to version 4.
        // v44 (milestone 9.71): experimental options add the one-Life hero base-cost flag. A v43
        // peer would misread the enclosing projection, so admission refuses it. The experimental
        // snapshot envelope advances separately to version 5.
        public const int Version = 45;

        /// <summary>
        /// Milestone 8.108. The most characters a player name may carry on the wire. Generous next
        /// to the 16 a lobby shows (<see cref="PrivateLobbyEntryText.MaxPlayerNameChars"/>), so a
        /// name with invisible characters to strip is still read and cleaned, while a larger one is
        /// refused as malformed rather than read into memory.
        /// </summary>
        public const int MaxPlayerNameWireChars = 64;

        /// <summary>
        /// Milestone 8.35z37. The most characters a hero id may carry in a pick.
        /// </summary>
        /// <remarks>
        /// Bounded like every other wire string, and bounded generously rather than at the longest
        /// registered id: the registration grows with every dealing milestone, and a bound that had
        /// to be edited by each of them would be a bound nobody maintained. The id is validated
        /// against the live registration by the selection stage regardless, so this only stops a
        /// payload from buying an allocation.
        /// </remarks>
        public const int MaxHeroIdChars = 64;

        /// <summary>The four ASCII bytes that open every frame: G, D, P, O.</summary>
        public const byte FrameMagic0 = 0x47;
        public const byte FrameMagic1 = 0x44;
        public const byte FrameMagic2 = 0x50;
        public const byte FrameMagic3 = 0x4F;

        /// <summary>Magic (4) + version (2) + kind (2) + payload length (4).</summary>
        public const int FrameHeaderBytes = 12;

        /// <summary>
        /// The largest payload any session message may declare. Validated against the declared
        /// length before a payload buffer is allocated, so an attacker's length field cannot buy an
        /// allocation.
        /// </summary>
        public const int MaxPayloadBytes = 64 * 1024;

        /// <summary>The largest challenge nonce a client will accept.</summary>
        public const int MaxNonceBytes = 64;

        /// <summary>The most characters a rejection detail may carry to a peer.</summary>
        public const int MaxRejectDetailChars = 200;

        /// <summary>The official configuration's six seats.</summary>
        public const int MaxSeats = 6;

        /// <summary>
        /// The most simultaneous connections the host keeps: the six seats plus headroom for a
        /// rejected straggler or two. Anything past this is refused at accept.
        /// </summary>
        public const int MaxConcurrentConnections = 12;

        /// <summary>How long an accepted connection may stand without completing its handshake.</summary>
        public const int HandshakeTimeoutSeconds = 5;

        /// <summary>
        /// Milestone 8.32. Every connection's first epoch. The gameplay envelope carries the
        /// sender's epoch so a stale connection's frames can be told from its replacement's.
        ///
        /// <para>Milestone 8.34 rotates it, exactly as the decision record scheduled: a seat that
        /// returns under its reconnect capability is granted the next epoch, and the envelope of
        /// any frame still stamped with the epoch the lost connection used is refused before it
        /// can reach a seat-level answer.</para>
        /// </summary>
        public const int InitialConnectionEpoch = 1;

        /// <summary>
        /// Milestone 8.34. The most characters a reconnect capability may carry.
        /// </summary>
        /// <remarks>
        /// The capability is the owner-private token the host issues with a seat grant and
        /// requires back before it will return that seat to a new connection. It is bounded like
        /// every other wire string, and it is never the session key: the key is the invitation,
        /// which every invited player holds, while the capability is one seat's own and is what
        /// keeps a reconnect from transferring seat control between invited players.
        /// </remarks>
        public const int MaxReconnectCapabilityChars = 64;

        /// <summary>
        /// Milestone 8.33. How many consumed submission identities the host remembers, so a
        /// retried submission inside the window is recognized as the duplicate it is and one
        /// older than the window is refused rather than executed twice.
        ///
        /// <para>Sized from measured traffic, as the decision record requires: across the
        /// deterministic seed-2 six-seat walk the longest any one seat goes between two of its own
        /// consumed submission identities is measured at 27 by
        /// <c>PrivateOnlineSynchronizationTests.TheIdempotencyWindow_CoversMeasuredRetryTraffic</c>,
        /// so 256 holds every seat's most recent history several times over - enough for a
        /// stale-view retry today and for 8.34's reconnect-and-retry to land inside it. That same
        /// fixture is the growth check: traffic outgrowing the margin fails it, and the constant
        /// is then re-sized deliberately rather than overtaken silently.</para>
        /// </summary>
        public const int SubmissionIdempotencyWindowEntries = 256;

        /// <summary>The default host port; an operator may name another.</summary>
        public const int DefaultPort = 45677;

        /// <summary>
        /// Milestone 8.35a1. Whether a session offered this bind and this key must be refused,
        /// because an unkeyed session may bind only the loopback interface.
        /// </summary>
        /// <remarks>
        /// <para>An unkeyed session has no key to derive channel keys from, so its link stays
        /// plaintext, and the architecture record permits an owner-private byte to cross plaintext
        /// only where its on-path observer does not exist - the loopback interface. A keyed session
        /// is sealed from the key proof on and may bind anything.</para>
        /// <para>The rule is stated here, once, because it has two callers that must not drift
        /// apart: the command line's own host role and the lobby's <c>HOST</c> control. It was
        /// written into the first alone, and milestone 8.36's readiness review found that the
        /// second - the path an operator actually uses to host an invited playtest - would open an
        /// unkeyed public listener. Each surface still says so in its own words; only the decision
        /// is shared.</para>
        /// </remarks>
        public static bool BindRequiresSessionKey(bool bindsLoopback, string sessionKey) =>
            !bindsLoopback && string.IsNullOrEmpty(sessionKey);

        /// <summary>The listener's pending-accept backlog.</summary>
        public const int ListenBacklog = 8;
    }
}
