// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Task 3.12 — the bidirectional selection bridge. Proves on the real product host that
	/// <c>RecordClerkNavigationContext</c> follows the clerk's actual mediator broadcast (no manual
	/// handler calls) and publishes a surface-originated selection back through the same bus.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class RecordClerkNavigationContextTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			// The legacy DataTree's ShowObject (driven by EnsureLegacySurfaceInitialized) needs the
			// legacy layout/parts Inventory loaded; that Inventory is keyed by the project path, so
			// give the in-memory test project a writable temp path before the inventory bootstrap.
			Cache.ProjectId.Path = Path.Combine(Path.GetTempPath(), Cache.ProjectId.Name,
				Cache.ProjectId.Name + ".junk");
		}

		[SetUp]
		public void SetUpWindow()
		{
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			// Bootstrap the legacy layout/parts Inventory the production RecordEditView loads via
			// EnsureLegacySurfaceInitialized (LayoutCache loads the real lexicon .fwlayout/Parts).
			// Without it, DataTree.GetTemplateForObjLayout finds a null layout inventory and ShowObject
			// throws an NRE once the idle-queued show actually runs.
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateLexiconTestData);
		}

		[TearDown]
		public void TearDownWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DestroyLexiconTestData);
			m_createdObjects = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		[Test]
		public void SelectionBridge_FollowsRealBroadcast_AndPublishesSelectionBack()
		{
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null);
			EnsureCurrentRecord(control);
			Assert.That(control.Clerk.ListSize, Is.GreaterThanOrEqualTo(2), "need at least two records to navigate");

			var bridge = control.RecordNavigationContext;
			Assert.That(bridge, Is.Not.Null, "the host must expose the selection bridge once its clerk exists");

			var changed = 0;
			bridge.CurrentRecordChanged += (s, e) => changed++;

			control.Clerk.JumpToIndex(0);
			DrainMediatorAndIdleQueues();
			var first = bridge.CurrentRecord as ICmObject;
			Assert.That(first, Is.Not.Null);

			// Follow direction: moving the bus selection must reach the bridge through the real
			// mediator RecordNavigation broadcast handled by the sponsoring host.
			var changedBeforeMove = changed;
			Assert.That(bridge.MoveNext(), Is.True);
			DrainMediatorAndIdleQueues();
			var second = bridge.CurrentRecord as ICmObject;
			Assert.That(second, Is.Not.Null);
			Assert.That(second.Hvo, Is.Not.EqualTo(first.Hvo), "MoveNext must change the bus selection");
			Assert.That(changed, Is.GreaterThan(changedBeforeMove),
				"the bridge must observe the broadcast (follow direction)");

			// Publish direction: a surface-originated selection (by record object) must route through
			// the clerk's real OnJumpToRecord and broadcast back to the host.
			var changedBeforePublish = changed;
			Assert.That(bridge.PublishSelection(first), Is.True);
			DrainMediatorAndIdleQueues();
			Assert.That(((ICmObject)bridge.CurrentRecord).Hvo, Is.EqualTo(first.Hvo),
				"PublishSelection must move the bus selection (publish direction)");
			Assert.That(control.Clerk.CurrentObject.Hvo, Is.EqualTo(first.Hvo),
				"the legacy clerk must see the surface-published selection");
			Assert.That(changed, Is.GreaterThan(changedBeforePublish),
				"the publishing surface also follows its own published change via the bus");
		}

		[Test]
		public void SelectionBridge_PublishSelection_ByHvo_AndRejectsUnknownKeys()
		{
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null);
			EnsureCurrentRecord(control);

			var bridge = control.RecordNavigationContext;
			control.Clerk.JumpToIndex(0);
			DrainMediatorAndIdleQueues();
			var first = (ICmObject)bridge.CurrentRecord;

			Assert.That(bridge.MoveNext(), Is.True);
			DrainMediatorAndIdleQueues();

			Assert.That(bridge.PublishSelection(first.Hvo), Is.True, "an hvo key publishes");
			DrainMediatorAndIdleQueues();
			Assert.That(((ICmObject)bridge.CurrentRecord).Hvo, Is.EqualTo(first.Hvo));

			Assert.That(bridge.PublishSelection("not-a-record-key"), Is.False,
				"unknown key shapes are rejected, not guessed");
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

		private void CreateLexiconTestData()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var nounPartOfSpeech = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			AddLexeme(m_createdObjects, "alpha-entry", stemMorphType, "first gloss", nounPartOfSpeech);
			AddLexeme(m_createdObjects, "beta-entry", stemMorphType, "second gloss", nounPartOfSpeech);
		}

		private void DestroyLexiconTestData()
		{
			if (m_createdObjects == null)
				return;
			foreach (var obj in m_createdObjects)
			{
				if (!obj.IsValidObject)
					continue;
				if (obj is ILexEntry)
					obj.Delete();
			}
		}

		// DrainMediatorAndIdleQueues is inherited from XWorksAppTestBase.

		private void EnsureCurrentRecord(RecordEditView control)
		{
			if (control.Clerk.CurrentObject != null)
				return;
			control.Clerk.JumpToIndex(0);
			DrainMediatorAndIdleQueues();
			Assert.That(control.Clerk.CurrentObject, Is.Not.Null);
		}
	}
}
