using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
namespace GuardsDigital.Tests.EditMode
{
    public sealed class MatchClockTests
    {
        private static MatchClock Create(int bank = 10)
        { var c = new MatchClock(new ExperimentalMatchOptions(turnTimer: true, cardCommitmentSeconds: 2,
            cardResolutionSeconds: 3, responseSeconds: 1, teamBankSeconds: bank)); c.SetWave(3); return c; }
        private static void Add(MatchClock c, string id, TeamSide side, MatchClockKind kind = MatchClockKind.Commitment)
        { c.Ensure(id, kind, new HeroId(id), side); }
        [Test] public void SimultaneousTeammatesChargeOneBankSecondAndOtherTeamStopsIndependently()
        {
            var c=Create(); Add(c,"a",TeamSide.Red); Add(c,"b",TeamSide.Red); Add(c,"c",TeamSide.Blue);
            c.SetRunning(new HashSet<string>{"a","b","c"}); c.Advance(TimeSpan.FromSeconds(5).Ticks);
            Assert.That(c.Capture().RedBankTicks,Is.EqualTo(TimeSpan.FromSeconds(7).Ticks));
            c.SetRunning(new HashSet<string>{"a","b"}); c.Advance(TimeSpan.FromSeconds(2).Ticks);
            Assert.That(c.Capture().RedBankTicks,Is.EqualTo(TimeSpan.FromSeconds(5).Ticks));
            Assert.That(c.Capture().BlueBankTicks,Is.EqualTo(TimeSpan.FromSeconds(7).Ticks));
        }
        [Test] public void PausedParentAndAnimationDoNotConsumeTimeOrRefreshOnResume()
        {
            var c=Create(); Add(c,"parent",TeamSide.Red,MatchClockKind.Resolution);
            c.SetRunning(new HashSet<string>{"parent"}); c.Advance(TimeSpan.FromSeconds(1).Ticks);
            Add(c,"response",TeamSide.Blue,MatchClockKind.Response); c.SetRunning(new HashSet<string>{"response"});
            c.Advance(TimeSpan.FromSeconds(2).Ticks); c.SetRunning(new HashSet<string>()); c.Advance(TimeSpan.FromSeconds(100).Ticks);
            c.Ensure("parent",MatchClockKind.Resolution,new HeroId("parent"),TeamSide.Red);
            Assert.That(c.Capture().Entries.Single(e=>e.Id=="parent").RemainingTicks,Is.EqualTo(TimeSpan.FromSeconds(2).Ticks));
            Assert.That(c.Capture().BlueBankTicks,Is.EqualTo(TimeSpan.FromSeconds(9).Ticks));
        }
        [Test] public void WaveResetsOnceAndEmptyBankStillGrantsNormalAllowance()
        {
            var c=Create(0); Add(c,"a",TeamSide.Red); c.SetRunning(new HashSet<string>{"a"});
            Assert.That(c.Expired("a"),Is.False); c.Advance(TimeSpan.FromSeconds(2).Ticks); Assert.That(c.Expired("a"),Is.True);
            Add(c,"b",TeamSide.Red); Assert.That(c.Expired("b"),Is.False);
            var d=Create(); Add(d,"a",TeamSide.Red); d.SetRunning(new HashSet<string>{"a"}); d.Advance(TimeSpan.FromSeconds(4).Ticks);
            d.SetWave(3); Assert.That(d.Capture().RedBankTicks,Is.EqualTo(TimeSpan.FromSeconds(8).Ticks));
            d.SetWave(2); Assert.That(d.Capture().RedBankTicks,Is.EqualTo(TimeSpan.FromSeconds(10).Ticks));
        }
        [Test] public void DirectorRestoresCheckpointWhileOrdinaryUndoCannotRefund()
        {
            var c=Create(); Add(c,"a",TeamSide.Red); c.SetRunning(new HashSet<string>{"a"}); var checkpoint=c.Capture();
            c.Advance(TimeSpan.FromSeconds(4).Ticks); c.Restore(checkpoint,false);
            Assert.That(c.Capture().Entries.Single().RemainingTicks,Is.Zero);
            Assert.That(c.Capture().RedBankTicks,Is.EqualTo(TimeSpan.FromSeconds(8).Ticks));
            c.Restore(checkpoint,true); Assert.That(c.Capture().Entries.Single().RemainingTicks,Is.EqualTo(TimeSpan.FromSeconds(2).Ticks));
            Assert.That(c.Capture().RedBankTicks,Is.EqualTo(TimeSpan.FromSeconds(10).Ticks));
        }
        [Test] public void FractionalAllowanceCrossingChargesOnlyActualBankInterval()
        {
            var c=Create(); Add(c,"a",TeamSide.Red); c.SetRunning(new HashSet<string>{"a"}); c.Advance(TimeSpan.FromMilliseconds(1900).Ticks);
            Add(c,"b",TeamSide.Red); c.SetRunning(new HashSet<string>{"a","b"}); c.Advance(TimeSpan.FromMilliseconds(200).Ticks);
            Assert.That(c.Capture().RedBankTicks,Is.EqualTo(TimeSpan.FromMilliseconds(9900).Ticks));
            Assert.That(c.Capture().Entries.Single(e=>e.Id=="b").RemainingTicks,Is.EqualTo(TimeSpan.FromMilliseconds(1800).Ticks));
        }
    }
}
