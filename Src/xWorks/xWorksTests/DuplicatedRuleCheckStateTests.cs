// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The Active check box of a row in Grammar > Phonological Rules after the list
	/// gains a rule.
	/// </summary>
	[TestFixture]
	public class DuplicatedRuleCheckStateTests : XWorksAppTestBase
	{
		private MockFwXWindow m_mockWindow;
		private BrowseActiveViewer m_viewer;
		private IPhRegularRule m_inactiveRule;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
		}

		[SetUp]
		public void OpenPhonologicalRules()
		{
			m_mockWindow = new MockFwXWindow(m_application, m_configFilePath);
			m_window = m_mockWindow;
			m_mockWindow.Init(Cache);
			m_mockWindow.PropTable.RemoveLocalAndGlobalSettings();
			m_mockWindow.ProcessPendingItems();
			m_mockWindow.LoadUI(m_configFilePath);
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_inactiveRule = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Add(m_inactiveRule);
				m_inactiveRule.Disabled = true;
			});
			m_mockWindow.ActivateTool("PhonologicalRuleEdit");
			m_viewer = FindViewer(m_mockWindow);
			Assert.That(m_viewer, Is.Not.Null, "the tool shows its rules in a BrowseActiveViewer");
			m_mockWindow.ActiveClerk.JumpToRecord(m_inactiveRule.Hvo);
			m_mockWindow.ProcessPendingItems();
		}

		[TearDown]
		public void CloseWindow()
		{
			m_mockWindow.PropTable.RemoveLocalAndGlobalSettings();
			m_mockWindow.Mediator.RemoveColleague(m_mockWindow);
			m_mockWindow.Dispose();
			m_mockWindow = null;
			m_window = null;
			m_viewer = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Clear());
		}

		[Test]
		public void DuplicateOfInactiveRule_ShowsUncheckedAtOnce()
		{
			var command = (Command)m_mockWindow.Mediator.CommandSet["CmdDuplicateSelectedObject"];

			Assert.That(m_mockWindow.ActiveClerk.OnDuplicateSelectedItem(command), Is.True);

			var copy = Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Single(rule => rule != m_inactiveRule);
			Assert.That(copy.Disabled, Is.True, "the copy is inactive like its original");
			// No pending Mediator message has been processed yet.
			Assert.That(CachedCheckState(copy), Is.EqualTo(0));

			m_mockWindow.ProcessPendingItems();
			Assert.That(CachedCheckState(copy), Is.EqualTo(0), "the pending messages keep it");
		}

		/// <summary>
		/// A rule that reaches the list through its data alone, as one duplicated in
		/// another window does.
		/// </summary>
		[Test]
		public void InactiveRuleAddedToTheData_ShowsUnchecked()
		{
			IPhRegularRule added = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				added = Cache.ServiceLocator.GetInstance<IPhRegularRuleFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonRulesOS.Add(added);
				added.Disabled = true;
			});

			Assert.That(CachedCheckState(added), Is.EqualTo(0));
		}

		/// <summary>The check state the viewer holds for the row, 1 when checked.</summary>
		private int CachedCheckState(ICmObject rule)
		{
			return m_viewer.SpecialCache.get_IntProp(rule.Hvo, XMLViewsDataCache.ktagItemSelected);
		}

		private static BrowseActiveViewer FindViewer(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				var viewer = child as BrowseActiveViewer ?? FindViewer(child);
				if (viewer != null)
					return viewer;
			}
			return null;
		}
	}
}
