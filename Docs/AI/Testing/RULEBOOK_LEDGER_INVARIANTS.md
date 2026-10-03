# Rulebook ledger invariants (9.13e)

Source: `Docs/Guards of Atlantis II - 2nd Printing_web.pdf`, visually read across every printed page 1-23. Page numbers here are the printed numbers. The independent source-only reading is preserved in `tmp/9-13e/rulebook-reader-map.md`; this durable document carries the actionable trace for `Assets/Tests/EditMode/RulebookLedgerInvariantTests.cs`.

The fixture states literal expected numbers from the source, then calls the configured public rule or match API. It never derives the expected value from another production schedule. The normal configured formats are the printed four- and six-player, one-lane Quick and Long games.

| Rulebook clause | Invariant and executable assertion |
| --- | --- |
| p. 5 setup step 2 | `SetupSteps2_4_5_EqualTeamsWavesAndLivesMatchThePrintedRows`: 4 players = 2 a side; 6 = 3 a side. |
| p. 5 setup step 4 | Same test: 3 waves in Quick and 5 in Long, all four supported format rows. |
| p. 5 setup step 5 and table | Same test: per-team Life counts 4/6 for 4-player Quick/Long and 5/8 for 6-player Quick/Long. |
| p. 5 setup step 6 | `SetupSteps6_8_11_StartAtLevelOneWithNoCoinGrantAndThePrintedFiveCardHand`: placing the coin supply makes no initial hero coin grant. |
| p. 5 setup step 8 | Same test: every hero begins at Level 1. |
| p. 6 setup step 11 | Same test: each opening hand has three Tier I non-basic cards and two untiered basics. |
| p. 8 Push the Lane step 1 | `PushStep1_OnlyTheLastWaveFlipExhaustsTheCounterPool`: a penultimate flip leaves one counter; a last flip records FinalWaveFlipped and its winner. |
| p. 8 Push the Lane step 2 | `PushStep2_EnteringTheOpposingThroneWinsWithWavesStillAvailable`: the next Zone is the enemy Throne, so that side wins with four wave counters still unspent. |
| p. 8 Push the Lane step 3 | `PushStep3_RemovesOldZoneMinionsAndRespawnsInNewZone` checks the three numbered stages and physical removal/respawn. `PushStep3_LiveRespawnDoesNotAwardDefeatCoins` isolates the preceding Minion Battle, captures the live session's hero balances before the requested push, and checks that the push emits its stage event without changing balances or publishing Defeat/coin-award log rows. `MinionBattleRuleSourceTests.TheSubstitution_AwardsNoCoinsAndMovesNoTeamLife` separately checks the earlier battle-removal boundary. |
| p. 14 Attack/Defense Detailed Order step 6 | `AttackOrderStep6_EqualDefenseSucceedsAndOneBelowFails` checks the exact 5-vs-5 and 5-vs-4 boundary; `AttackOrderStep6_SuccessfulDefenseLeavesBothEconomiesUntouched` checks a live defended target stays on the board and neither team Life nor either hero's coin balance changes. |

| Unnumbered source or table | Invariant and executable assertion |
| --- | --- |
| p. 13 “When a Hero is Defeated,” table | `HeroDefeatTable_AttackerAssistAndLifeCostUseSeparatePrintedColumns`: all eight levels, with level 3→4 and 6→7 boundaries; attacker receives defeated level in coins, friendly assistants receive 1/2/3 by lowest card Tier, defeated team spends 1/2/3 Life. `HeroDefeatTable_CoinsReachEachOtherFriendlyHeroAndLifeComesFromVictimsTeam` checks three recipient seats and the team Life ledger. |
| p. 13 last-counter condition | `HeroDefeatAtLastLife_SpendsAvailableCountersAndEndsTheTeam`: a request for 2 with only 1 remaining spends that last counter. |
| p. 15 basic/lowest card Tier | `BasicCardsAndReturnedLowerCards_DoNotReplaceTheLowestActiveNonBasicTier`: returned Tier I in deck and untiered basic in hand do not undercut active Tier II. The p. 13 level row is the normal progression consequence, while the actual card Tier governs a legal defeat. |
| p. 13 “Attacking a Minion”; p. 19 “Remove”; p. 12 Bolster illustration | `MinionDefeat_BountyMatchesPageThirteen` checks the declared melee/ranged 2 and heavy 4. Live routing is covered by `MortimerGreenFacesTests.GatheringHorde_ReplacesAnEnemyMinionBesideTwoZombies_WithoutAReward` for removal without coins, `MinionBattleRuleSourceTests.TheSubstitution_AwardsNoCoinsAndMovesNoTeamLife` for battle removal, and `DefeatRemovalStageTests.AShieldedMinion_StopsThePageAfterItsRewardAndBeforeItsRemoval` plus `APaidPrevention_KeepsTheMiniatureDiscardsTheCardAndPublishesTheOutcome` for a paid Defeat whose physical removal is prevented while reward remains. These named fixtures are required affected evidence; the new fixture does not create a disconnected coin ledger as a substitute. |
| p. 8 “Collect a Pity Coin” | Public match command cases `PityCoinOrderingTests.EveryHeroThatDidNotLevel_CollectsExactlyOneCoinAtTheStage`, `AHeroThatBoughtALevel_CollectsNoPityCoinWhileTheOtherStillDoes`, and `APityCoinThatReachesALevelThreshold_CannotBeSpentUntilALaterRound` check amount, eligibility and the following-round boundary. They are required affected evidence; the new fixture does not reenact eligibility in a test-owned loop. |
| p. 15 level cost, compulsory purchases and max Tier table; p. 16 Ultimate | `LevelTable_ConsecutivePurchaseCostAndMaxTierArePrintedValues` checks all seven cost columns and max card Tier through Level 7; `MandatoryLevelUp_SpendsThreeCoinsOnTwoConsecutivePurchasesAndLowestTierChoices` runs the public transition and checks both spends and upgrades; `LevelEight_UltimateColumnHasNoNinthPurchase` checks Tier IV at Level 8, total cost 28, and no Level 9. |

The p. 13 assist/Life amounts are a discrete table: levels 1-3 = Tier I/1, 4-6 = Tier II/2, 7-8 = Tier III/3. The rulebook prints no general rounding formula; tests must not infer one.

Printed p. 20 variants remain source-only and outside the configured one-lane four/six-player formats: 2 players controlling two heroes each; odd-player Handicap setups (3 and 5 use four-player Life counts); and double-lane 6-10 player setups (2×7 waves, 6 or 7 Life per team and a two-lane victory comparison). These rows are preserved as source facts, not reported as green implementation coverage.
