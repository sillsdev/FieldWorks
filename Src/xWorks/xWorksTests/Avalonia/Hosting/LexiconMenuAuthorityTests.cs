// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The message-keyed authority for the Lexicon detail menus, as the product's
	/// <see cref="RecordEditView"/> wires it: which context-menu ids it owns (pinned, so a PR
	/// that adds an answerer shows the menus it switched over), the predicate behind that
	/// list, and the contracts every owned id must meet.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class LexiconMenuAuthorityTests : XWorksAppTestBase
	{
		// The ids the product authority owns today. Every answerer a PR adds extends this.
		private static readonly string[] PinnedOwnedIds = new string[0];

		// The messages of mnuDataTree-Object's six leaves, the test subject for the predicate.
		private static readonly string[] ObjectMenuMessages =
		{
			"ShowFieldAlwaysVisible", "ShowFieldIfData", "ShowFieldNormallyHidden",
			"MoveFieldUp", "MoveFieldDown", "DataTreeHelp"
		};

		private PropertyTable m_propertyTable;
		private RecordEditView m_view;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
		}

		[SetUp]
		public void SetUpWindow()
		{
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();
			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null, "expected the lexicon edit RecordEditView to load");
		}

		[TearDown]
		public void TearDownWindow()
		{
			m_view = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		// Contract: the owned-id list is exact, and every owned id builds in full through the
		// bridge, so an unanswered leaf fails here instead of reverting a menu to the adapter.
		[Test]
		public void OwnsExactlyThePinnedIds_AndAnswersEveryLeafOfEachOne()
		{
			var authority = m_view.CreateLexiconMenuAuthority();
			var window = Window;

			var owned = ContextMenuIds().Where(authority.Owns).ToList();

			Assert.That(owned, Is.EquivalentTo(PinnedOwnedIds),
				"the authority's owned ids changed; update the pin so the PR shows which menus switched over");
			foreach (var id in owned)
			{
				foreach (var leaf in XCoreMenuBridge.ResolveMenu(window, id).Leaves)
				{
					Assert.That(() => authority.Build(id, leaf), Throws.Nothing,
						"menu '{0}': the authority does not answer leaf '{1}'", id, leaf.CommandId);
				}
				Assert.That(() => XCoreMenuBridge.CreateMenuItems(window, new[] { id }, null, null, authority),
					Throws.Nothing, "menu '{0}' builds in full through the bridge", id);
			}
		}

		[Test]
		public void Owns_WhenEveryLeafsMessageIsAnswered_AndNotOneMessageShort()
		{
			var authority = NewAuthority();
			foreach (var message in ObjectMenuMessages.Take(ObjectMenuMessages.Length - 1))
				authority.Answer(message, Echo);
			Assert.That(authority.Owns(RecordEditView.ObjectMenuId), Is.False,
				"one unanswered message leaves the id with the adapter");

			authority.Answer(ObjectMenuMessages.Last(), Echo);

			Assert.That(authority.Owns(RecordEditView.ObjectMenuId), Is.True,
				"the id is owned the moment its last message has an answerer");
			var items = XCoreMenuBridge.CreateMenuItems(Window, new[] { RecordEditView.ObjectMenuId },
				null, null, authority);
			Assert.That(items.Select(i => i.Label), Does.Contain("Help..."),
				"the owned menu builds from the answerers alone");
		}

		[Test]
		public void NeverOwns_AMenuWithoutLeaves_OrWithAListSubmenu_OrWithAPropertyToggle()
		{
			var authority = NewAuthority();
			foreach (var message in ObjectMenuMessages)
				authority.Answer(message, Echo);
			authority.Answer("DataTreeWritingSystemsShowAll", Echo);
			authority.Answer("DataTreeWritingSystemsConfigureDlg", Echo);

			Assert.That(authority.Owns(ObjectMenuAuthority.HelpMenuId), Is.False,
				"mnuDataTree-Help has no leaf, so there is nothing to answer");
			Assert.That(XCoreMenuBridge.ResolveMenu(Window, RecordEditView.MultiStringSliceMenuId).Leaves
				.All(l => authority.AnsweredMessages.Contains(l.Message)), Is.True,
				"precondition: every configured leaf of the multi-string menu is answered");
			Assert.That(authority.Owns(RecordEditView.MultiStringSliceMenuId), Is.False,
				"a list-populated submenu has no configured leaves to answer, so the id is not owned");

			var window = Window;
			var withToggle = ContextMenuIds().Select(id => XCoreMenuBridge.ResolveMenu(window, id))
				.First(m => m.Unanswerable.Any(u => u.StartsWith("property", StringComparison.Ordinal)) && m.Leaves.Count > 0);
			foreach (var message in withToggle.Leaves.Select(l => l.Message).Distinct()
				.Where(m => !authority.AnsweredMessages.Contains(m)))
			{
				authority.Answer(message, Echo);
			}
			Assert.That(authority.Owns(withToggle.MenuId), Is.False,
				"menu '{0}': a property toggle belongs to the property table, so the id is not owned", withToggle.MenuId);
		}

		[Test]
		public void Build_RejectsALeafWhoseMessageHasNoAnswerer_AndEveryList()
		{
			var authority = m_view.CreateLexiconMenuAuthority();
			var help = XCoreMenuBridge.ResolveMenu(Window, RecordEditView.ObjectMenuId).Leaves
				.First(l => l.CommandId == ObjectMenuAuthority.HelpCommandId);

			Assert.That(() => authority.Build(RecordEditView.ObjectMenuId, help),
				Throws.InvalidOperationException, "an owned id must be answered in full, never partially");
			Assert.That(() => authority.BuildList(RecordEditView.MultiStringSliceMenuId,
				MultiStringMenuAuthority.WritingSystemListId), Throws.InvalidOperationException);
		}

		private XWindow Window => m_propertyTable.GetValue<XWindow>("window");

		private LexiconMenuAuthority NewAuthority()
		{
			var window = Window;
			return new LexiconMenuAuthority(id => XCoreMenuBridge.ResolveMenu(window, id));
		}

		private static DetailMenuItem Echo(DetailMenuLeaf leaf)
			=> new DetailMenuItem(XCoreMenuBridge.StripAccelerator(leaf.Label), isEnabled: true);

		// Every context-menu id the window configuration defines, each once.
		private IEnumerable<string> ContextMenuIds()
		{
			var configuration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(configuration, Is.Not.Null);
			return configuration.SelectNodes("//contextMenus/menu[@id]").Cast<XmlNode>()
				.Select(n => n.Attributes["id"].Value).Distinct(StringComparer.Ordinal).ToList();
		}

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(windowConfiguration, Is.Not.Null);
			var controlNode = windowConfiguration.SelectSingleNode(
				string.Format("//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']", toolValue));
			Assert.That(controlNode, Is.Not.Null, "Expected the RecordEditView configuration node for tool '{0}'.", toolValue);

			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
		}
	}
}
