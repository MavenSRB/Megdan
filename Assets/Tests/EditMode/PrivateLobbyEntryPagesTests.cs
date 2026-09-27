using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.107. The lobby's entry as three pages: a landing that offers only HOST and JOIN,
    /// a host menu, and a join menu. Each page draws its own fields and buttons, and CANCEL keeps
    /// what was typed.
    /// </summary>
    public sealed class PrivateLobbyEntryPagesTests
    {
        private const string Format = "3V3-QUICK";

        [Test]
        public void TheLanding_OffersHostAndJoin_AndNothingElse()
        {
            var view = EntryView();

            Assert.That(view.Page, Is.EqualTo(PrivateLobbyEntryPage.Landing));
            Assert.That(ShownControlLabels(view), Is.EqualTo(new[] { "HOST", "JOIN" }));
            Assert.That(ShownFields(view), Is.Empty);
            Assert.That(Shown(Named(view, PrivateLobbyUnityView.SeatsName)), Is.False,
                "The landing is two buttons; the seat table belongs to a session.");
            Assert.That(Shown(Named(view, PrivateLobbyUnityView.StatusName)), Is.False);
        }

        [Test]
        public void TheEntryPanel_FitsItsPage_AndASessionGetsTheWidePanel()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var view = EntryView();
            var panel = Named(view, PrivateLobbyUnityView.PanelName);

            Assert.That(panel.style.width.value, Is.EqualTo(new Length(PrivateLobbyUnityView.LandingPanelWidth)),
                "The landing's panel is as wide as its two buttons need, not 72% of the screen.");
            Assert.That(Named(view, PrivateLobbyUnityView.ControlsName).style.justifyContent.value,
                Is.EqualTo(Justify.Center));

            view.ShowPage(PrivateLobbyEntryPage.Join);
            Assert.That(panel.style.width.value, Is.EqualTo(new Length(PrivateLobbyUnityView.MenuPanelWidth)));

            view.Render(PrivateLobbyModel.ForHost(harness.Session, string.Empty));
            Assert.That(panel.style.width.value, Is.EqualTo(new Length(72f, LengthUnit.Percent)),
                "A session's lobby keeps the wide panel its seat table and hero picker need.");
        }
        [Test]
        public void TheHostMenu_OffersTheSetupPortSessionIdHostAndCancel()
        {
            var view = EntryView();
            view.ShowPage(PrivateLobbyEntryPage.Host);

            Assert.That(ShownControlLabels(view), Is.EqualTo(new[]
            {
                "2V2", "3V3", "SHORT GAME", "LONG GAME", "HOST", PrivateLobbyUnityView.CancelLabel
            }));
            Assert.That(ShownFields(view).Select(field => field.label), Is.EqualTo(new[] { "NAME", "PORT", "SESSION ID" }));
            Assert.That(Label(view, PrivateLobbyUnityView.ModeName), Is.EqualTo("MODE: 3V3 SHORT GAME"));
            Assert.That(Label(view, PrivateLobbyUnityView.TitleName), Is.EqualTo("HOST A GAME"));
            // Milestone 8.109: the fields first, then the four setup choices on one row.
            var hostMenu = Named(view, PrivateLobbyUnityView.HostMenuName);
            var order = hostMenu.Children().Select(child => child.name).ToList();
            Assert.That(order.IndexOf(PrivateLobbyUnityView.FormatChoicesName),
                Is.GreaterThan(order.IndexOf(PrivateLobbyUnityView.HostKeyFieldName)));
            var row = Named(view, PrivateLobbyUnityView.FormatChoicesName);
            Assert.That(row.style.flexWrap.value, Is.EqualTo(Wrap.NoWrap));
            Assert.That(row.childCount * (PrivateLobbyUnityView.FormatChoiceWidth + 10f),
                Is.LessThanOrEqualTo(PrivateLobbyUnityView.MenuPanelWidth - 52f),
                "Four setup choices fit one row inside the menu panel's padding and border.");

            var chosen = ShownButtons(view)
                .Where(button => button.ClassListContains("gd-lobby-format-chosen"))
                .Select(button => button.text);
            Assert.That(chosen, Is.EquivalentTo(new[] { "3V3", "SHORT GAME" }),
                "The two halves of the composed setup are the highlighted ones.");
        }

        [Test]
        public void TheHostMenu_StartsWithAFreshSessionId_AndLeavingRenewsIt()
        {
            var first = EntryView();
            var second = EntryView();

            Assert.That(first.HostSessionId, Is.Not.Empty);
            Assert.That(first.HostSessionId, Is.Not.EqualTo(second.HostSessionId),
                "Every launch hosts under its own session ID.");

            var before = first.HostSessionId;
            first.RenewHostSessionId();
            Assert.That(first.HostSessionId, Is.Not.EqualTo(before));
        }

        [Test]
        public void TheJoinMenu_OffersHostIpPortSessionIdJoinAndCancel()
        {
            var view = EntryView();
            view.ShowPage(PrivateLobbyEntryPage.Join);

            Assert.That(ShownControlLabels(view), Is.EqualTo(new[] { "JOIN", PrivateLobbyUnityView.CancelLabel }));
            Assert.That(ShownFields(view).Select(field => field.label),
                Is.EqualTo(new[] { "NAME", "HOST IP", "PORT", "SESSION ID" }));
            Assert.That(Label(view, PrivateLobbyUnityView.TitleName), Is.EqualTo("JOIN A GAME"));
        }

        [Test]
        public void Cancel_ReturnsToTheLanding_AndKeepsWhatWasTyped()
        {
            var view = EntryView();
            view.ShowPage(PrivateLobbyEntryPage.Join);
            Field(view, PrivateLobbyUnityView.AddressFieldName).value = "81.2.69.160";
            Field(view, PrivateLobbyUnityView.PortFieldName).value = "5000";

            view.ShowPage(PrivateLobbyEntryPage.Landing);
            Assert.That(ShownControlLabels(view), Is.EqualTo(new[] { "HOST", "JOIN" }));

            view.ShowPage(PrivateLobbyEntryPage.Join);
            Assert.That(Field(view, PrivateLobbyUnityView.AddressFieldName).value, Is.EqualTo("81.2.69.160"));
            Assert.That(Field(view, PrivateLobbyUnityView.PortFieldName).value, Is.EqualTo("5000"));
        }

        [Test]
        public void EachMenu_ReadsItsOwnFields_Cleaned()
        {
            var view = EntryView();
            Field(view, PrivateLobbyUnityView.AddressFieldName).value = " 192.168.1.20​";
            Field(view, PrivateLobbyUnityView.PortFieldName).value = "45677 ";
            Field(view, PrivateLobbyUnityView.KeyFieldName).value = " JOIN-KEY ";
            Field(view, PrivateLobbyUnityView.HostPortFieldName).value = " 6000";
            Field(view, PrivateLobbyUnityView.HostKeyFieldName).value = "HOST-KEY\n";
            Field(view, PrivateLobbyUnityView.PlayerNameFieldName).value = "  Stefan\u200B";

            view.ShowPage(PrivateLobbyEntryPage.Join);
            var join = view.CurrentEntry();
            Assert.That(join.Address, Is.EqualTo("192.168.1.20"));
            Assert.That(join.Port, Is.EqualTo("45677"));
            Assert.That(join.SessionKey, Is.EqualTo("JOIN-KEY"));
            Assert.That(join.PlayerName, Is.EqualTo("Stefan"), "8.108: one NAME field serves both menus.");

            view.ShowPage(PrivateLobbyEntryPage.Host);
            var host = view.CurrentEntry();
            Assert.That(host.Address, Is.Empty, "8.100: the host menu names no address of its own.");
            Assert.That(host.Port, Is.EqualTo("6000"));
            Assert.That(host.SessionKey, Is.EqualTo("HOST-KEY"));
            Assert.That(host.PlayerName, Is.EqualTo("Stefan"));
        }

        [Test]
        public void RememberedFields_FillTheMenus_ButNeverTheHostsSessionId()
        {
            var view = EntryView();
            var fresh = view.HostSessionId;

            view.LoadRememberedFields(new PrivateLobbyRememberedFields("81.2.69.160", "5000", "JOIN-KEY", "6000", "Stefan"));
            Assert.That(Field(view, PrivateLobbyUnityView.PlayerNameFieldName).value, Is.EqualTo("Stefan"));

            Assert.That(Field(view, PrivateLobbyUnityView.AddressFieldName).value, Is.EqualTo("81.2.69.160"));
            Assert.That(Field(view, PrivateLobbyUnityView.PortFieldName).value, Is.EqualTo("5000"));
            Assert.That(Field(view, PrivateLobbyUnityView.KeyFieldName).value, Is.EqualTo("JOIN-KEY"));
            Assert.That(Field(view, PrivateLobbyUnityView.HostPortFieldName).value, Is.EqualTo("6000"));
            Assert.That(view.HostSessionId, Is.EqualTo(fresh));
        }

        [Test]
        public void ComingBackFromASession_LandsOnTheFirstScreen()
        {
            var harness = PrivateOnlineWireHarness.Compose("K7QM-4TZP-9WXR");
            var view = EntryView();
            view.ShowPage(PrivateLobbyEntryPage.Host);

            view.Render(PrivateLobbyModel.ForHost(harness.Session, string.Empty));
            view.Render(PrivateLobbyModel.Entry(harness.Session.Roster, string.Empty, Format));

            Assert.That(view.Page, Is.EqualTo(PrivateLobbyEntryPage.Landing));
            Assert.That(ShownControlLabels(view), Is.EqualTo(new[] { "HOST", "JOIN" }));
        }

        [Test]
        public void TheInvitation_IsDrawnOnTheHostsScreen_AndNowhereElse()
        {
            var harness = PrivateOnlineWireHarness.Compose("K7QM-4TZP-9WXR");
            var view = EntryView();
            view.ShowInvitation(PrivateLobbyInvitation.For(
                "K7QM-4TZP-9WXR", 45677, HostAddressDiscovery.RouterAnswered, "81.2.69.160", null));

            Assert.That(Shown(Named(view, PrivateLobbyUnityView.InvitationSessionName)), Is.False,
                "The entry form is not a host's screen.");

            view.Render(PrivateLobbyModel.ForHost(harness.Session, string.Empty));
            Assert.That(Label(view, PrivateLobbyUnityView.InvitationSessionName),
                Is.EqualTo("SESSION ID: K7QM-4TZP-9WXR"));
            Assert.That(Label(view, PrivateLobbyUnityView.InvitationAddressName),
                Is.EqualTo("INTERNET IP: 81.2.69.160   PORT 45677"));
            Assert.That(Shown(Named(view, PrivateLobbyUnityView.InvitationAddressName)), Is.True);

            harness.Connect(1, "red-player-01");
            harness.Pump();
            view.Render(PrivateLobbyModel.ForClient(harness.Client(1)));
            Assert.That(Shown(Named(view, PrivateLobbyUnityView.InvitationSessionName)), Is.False,
                "A joining player's screen never shows the host's key or address.");
        }

        [Test]
        public void TheLobbyEntry_ReadsTheCleanedValues()
        {
            var entry = new PrivateLobbyEntry(" 192.168.1.20​", "45677 ", " KEY-1 ", default);

            Assert.That(entry.Address, Is.EqualTo("192.168.1.20"));
            Assert.That(entry.Port, Is.EqualTo("45677"));
            Assert.That(entry.SessionKey, Is.EqualTo("KEY-1"));
        }

        [TestCase(PrivateLobbyControl.ChooseTwoVsTwo, "2V2-LONG", true)]
        [TestCase(PrivateLobbyControl.ChooseThreeVsThree, "2V2-LONG", false)]
        [TestCase(PrivateLobbyControl.ChooseLongGame, "2V2-LONG", true)]
        [TestCase(PrivateLobbyControl.ChooseShortGame, "2V2-LONG", false)]
        [TestCase(PrivateLobbyControl.ChooseShortGame, "3V3-QUICK", true)]
        [TestCase(PrivateLobbyControl.ChooseShortGame, "", false)]
        public void TheChosenFormatButtons_AreTheTwoHalvesOfTheComposedSetup(
            PrivateLobbyControl control, string label, bool chosen)
        {
            Assert.That(PrivateLobbyUnityView.IsChosenFormat(control, label), Is.EqualTo(chosen));
        }

        private static PrivateLobbyUnityView EntryView()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var view = new PrivateLobbyUnityView();
            view.Render(PrivateLobbyModel.Entry(harness.Session.Roster, string.Empty, Format));
            return view;
        }

        private static IEnumerable<VisualElement> Descendants(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                yield return child;
                foreach (var grandchild in Descendants(child))
                {
                    yield return grandchild;
                }
            }
        }

        private static bool Shown(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (current.style.display.keyword == StyleKeyword.Undefined &&
                    current.style.display.value == DisplayStyle.None)
                {
                    return false;
                }
            }

            return true;
        }

        private static VisualElement Named(PrivateLobbyUnityView view, string name) =>
            Descendants(view.Root).Single(element => element.name == name);

        private static TextField Field(PrivateLobbyUnityView view, string name) => (TextField)Named(view, name);

        private static string Label(PrivateLobbyUnityView view, string name) => ((Label)Named(view, name)).text;

        private static IReadOnlyList<Button> ShownButtons(PrivateLobbyUnityView view) =>
            Descendants(view.Root)
                .OfType<Button>()
                .Where(button => button.name == PrivateLobbyUnityView.ControlName && Shown(button))
                .ToArray();

        private static IReadOnlyList<string> ShownControlLabels(PrivateLobbyUnityView view) =>
            ShownButtons(view).Select(button => button.text).ToArray();

        private static IReadOnlyList<TextField> ShownFields(PrivateLobbyUnityView view) =>
            Descendants(view.Root).OfType<TextField>().Where(Shown).ToArray();
    }
}
