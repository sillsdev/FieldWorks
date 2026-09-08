// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// End-to-end proof of the writing-system-under-cursor state channel on the real product
	/// host: <see cref="RecordEditView.OnDetailWritingSystemFocused"/> (the Avalonia detail
	/// view's editor-focus hook) publishes
	/// <see cref="EventConstants.WritingSystemUnderCursorChanged"/> window-scoped, and the
	/// window's <see cref="WritingSystemListHandler"/> subscriber mirrors it into the
	/// writing-system property the toolbar combo displays.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class DetailWritingSystemStateTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private RecordEditView m_view;
		// The real subscriber under test. MockFwXWindow's bare-minimum LoadUI never loads the
		// Main.xml listeners, so the fixture creates the one this channel needs itself.
		private WritingSystemListHandler m_wsListHandler;

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
			m_wsListHandler = new WritingSystemListHandler();
			m_wsListHandler.Init(m_window.Mediator, m_propertyTable, null);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateLexiconTestData);

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
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DestroyLexiconTestData);
			m_createdObjects = null;
			m_entry = null;
			m_view = null;
			// Unsubscribes from the process-wide Pub/Sub singleton; without it a disposed
			// handler would still hear later fixtures' publishes.
			m_wsListHandler?.Dispose();
			m_wsListHandler = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		[Test]
		public void EditorFocus_UpdatesWritingSystemHvoProperty_ThroughPubSub()
		{
			var vernacular = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			var analysis = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			Assert.That(vernacular.Handle, Is.Not.EqualTo(analysis.Handle),
				"precondition: distinct vernacular and analysis writing systems");
			Assert.That(CurrentWritingSystemHvoProperty(),
				Is.EqualTo(analysis.Handle.ToString(CultureInfo.InvariantCulture)),
				"precondition: WritingSystemListHandler.Init seeds the default analysis ws");

			m_view.OnDetailWritingSystemFocused(vernacular.Id);

			Assert.That(CurrentWritingSystemHvoProperty(),
				Is.EqualTo(vernacular.Handle.ToString(CultureInfo.InvariantCulture)),
				"focusing a vernacular editor must update the combo property through Pub/Sub");

			m_view.OnDetailWritingSystemFocused("xxx-unknown-tag");

			Assert.That(CurrentWritingSystemHvoProperty(),
				Is.EqualTo(vernacular.Handle.ToString(CultureInfo.InvariantCulture)),
				"an unresolvable tag must publish nothing and leave the property alone");

			m_view.OnDetailWritingSystemFocused(analysis.Id);

			Assert.That(CurrentWritingSystemHvoProperty(),
				Is.EqualTo(analysis.Handle.ToString(CultureInfo.InvariantCulture)),
				"focusing an analysis editor must move the combo property back");
		}

		// Plugin rows bypass SliceFactory, so the host's focus handler reaches them only
		// through the callback Compose threads into SlicePluginBuildContext.
		[Test]
		public void PluginRowEditorFocus_UpdatesWritingSystemHvoProperty_ThroughPubSub()
		{
			var vernacular = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			var analysis = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			var sense = m_entry.SensesOS[0];
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var repository = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
				var hadIndex = repository.AllInstances().Any(index => index.WritingSystem == analysis.Id);
				var reversalIndex = repository.FindOrCreateIndexForWs(analysis.Handle);
				if (!hadIndex)
					m_createdObjects.Add(reversalIndex);
				var reversalEntry = reversalIndex.FindOrCreateReversalEntry("dwelling");
				reversalEntry.SensesRS.Add(sense);
				m_createdObjects.Add(reversalEntry);
			});

			// Compose with the host's handler as the plugin callback, the way ShowAvaloniaEntry
			// does, and realize the view with the same handler on the composer-row path.
			var composed = DetailComposer.Compose(m_entry, Cache,
				writingSystemFocused: m_view.OnDetailWritingSystemFocused);
			FwAvaloniaRuntime.EnsureInitialized();
			var view = new DataTree(composed.Model, composed.EditContext, m_view.OnDetailWritingSystemFocused);
			var window = new Window { Content = view, Width = 520, Height = 360 };
			try
			{
				window.Show();
				Dispatcher.UIThread.RunJobs();
				// The plugin stamps <row automation id>.<ws tag> on each of its value boxes.
				var row = composed.Model.Fields.First(f => f.Kind == DetailFieldKind.Custom
					&& f.ObjectHvo == sense.Hvo);
				var boxId = (row.AutomationId ?? ReversalIndexEntryPlugin.DefaultAutomationId) + "." + analysis.Id;
				var reversalBox = view.GetVisualDescendants().OfType<TextBox>()
					.FirstOrDefault(box => AutomationProperties.GetAutomationId(box) == boxId);
				Assert.That(reversalBox, Is.Not.Null,
					"precondition: the Reversal Entries plugin row realized its analysis-ws editor");

				m_view.OnDetailWritingSystemFocused(vernacular.Id);
				Assert.That(CurrentWritingSystemHvoProperty(),
					Is.EqualTo(vernacular.Handle.ToString(CultureInfo.InvariantCulture)),
					"precondition: the combo property starts away from the reversal row's ws");

				reversalBox.Focus();
				Dispatcher.UIThread.RunJobs();

				Assert.That(CurrentWritingSystemHvoProperty(),
					Is.EqualTo(analysis.Handle.ToString(CultureInfo.InvariantCulture)),
					"focusing a plugin row's editor must publish its ws like every composer-built row");
			}
			finally
			{
				window.Close();
			}
		}

		private string CurrentWritingSystemHvoProperty()
		{
			return m_propertyTable.GetStringProperty(PropertyConstants.WritingSystemHvo, null);
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
			m_entry = AddLexeme(m_createdObjects, "ws-state-entry", stemMorphType, "ws state gloss",
				nounPartOfSpeech);
		}

		private void DestroyLexiconTestData()
		{
			if (m_createdObjects == null)
				return;
			// Reverse order: a reversal entry is deleted before the index that owns it.
			for (var i = m_createdObjects.Count - 1; i >= 0; i--)
			{
				if (m_createdObjects[i].IsValidObject)
					m_createdObjects[i].Delete();
			}
		}
	}
}
