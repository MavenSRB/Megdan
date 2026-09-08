# Private-session bots

Canonical implementation contract for the lobby's FILL WITH BOTS control. Return to [private-online architecture](PRIVATE_ONLINE_ARCHITECTURE.md) or [project context](../PROJECT_CONTEXT.md).

## Ownership and execution

`PrivateLobbyDriver` owns one `PrivateSessionBotFleet` beside its human TCP listener. FILL creates host-owned clients only for open seats before the match starts; it never takes a claimed or reserved human seat. Repeated presses cannot create duplicate live bot clients. No child player process, socket, worker thread, or separate update loop is used for these bots.

Every bot is an ordinary `PrivateSessionClient` over encoded, framed, queued bytes. Negative connection ordinals are private to the fleet; the real TCP listener continues to use positive ordinals. Human bytes are forwarded to that listener without entering a bot endpoint. Keyed admission, roster/readiness, alternating draft, owner-scoped projection, command mapping, version/correlation checks, and host acceptance use the existing session protocol. The initial admission grant predates drafting; gameplay policy and team participation use the final hero on the current owner projection.

The fleet pumps at most 256 queued deliveries per pump, caps queued deliveries at 1,024, and makes at most one bot decision per Unity update, round-robin across live bots. Default think spacing is two seconds per bot. The existing `GUARDS_TESTDRIVER_THINK_SECONDS` test override accepts finite values from zero through 60. The existing greedy `BotPolicy` sees only that bot's owner projection and published commands; it receives no full-host lookahead. Selected effect choices are confirmed instead of repeatedly toggled. Refused commands are remembered only at their projection version. This is bounded baseline AI, not a stronger strategy tier or a guarantee that every possible match finishes unattended.

Bots ready and pick from their own lobby offers. They submit team statements through the normal agreement channel. A reserved human seat pauses all bot decisions; filling cannot replace that seat. Leave and destruction dispose local clients and queued work, shut down the host session, and release the human listener. The legacy standalone test-driver/rehearsal paths remain available for their existing separate validation purposes; they are not the lobby button's implementation.

## Compatibility and evidence

This wiring changes neither wire payload meaning nor rules, snapshot, map, or card-content identities: protocol remains `guards-private-online/11`, snapshot schema remains v40. The architecture owner's prior current-protocol 10 and schema 38 labels, including its 8.35z60 assessment's stale schema label, were corrected against the existing source constants; this is documentation reconciliation, not a new migration. New builds retain the normal unique build stamp and require matching-build peers. In-process execution does not change the trusted-operator threat model in the architecture owner.

Focused ownership/lifecycle and non-default-draft coverage lives in `PrivateSessionBotFleetTests`; the actual panel-button and teardown regression lives in `PrivateLobbyPlayModeTests`. Their recipes follow [testing policy](../Testing/POLICY.md). Final built-player evidence belongs to [8.35z69](../Roadmap/PHASE_8_CLEANUP.md). A local bot/human run is not the outstanding multi-network human gate.
