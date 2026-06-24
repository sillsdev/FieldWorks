using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class RecordEditViewSwitchTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;

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
		public void LexiconEditTool_UsesLegacyDataTree_WhenUIModeIsLegacy()
		{
			m_propertyTable.SetProperty("UIMode", "Legacy", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);

			LoadRecordEditView();
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null);
			EnsureCurrentRecord(control);
			Assert.That(control.DatTree, Is.Not.Null);
			Assert.That(GetPrivateFieldValue(control, "m_avaloniaEntryForm"), Is.Null);
			Assert.That(GetPrivateFieldValue(control, "m_lexicalEditSurface"), Is.EqualTo(LexicalEditSurface.WinForms));
		}

		[Test]
		public void LexiconEditTool_SwitchesSurfaceStateToAvalonia_WhenUIModePropertyBroadcasts()
		{
			m_propertyTable.SetProperty("UIMode", "Legacy", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);

			LoadRecordEditView();
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null);
			EnsureCurrentRecord(control);

			m_propertyTable.SetProperty("UIMode", "New", true);
			DrainMediatorAndIdleQueues();

			var sameControl = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(sameControl, Is.SameAs(control), "Changing the UI mode should update the live content control rather than requiring a tool reload in the test harness.");
			Assert.That(control.Clerk.CurrentObject, Is.Not.Null);
			Assert.That(GetPrivateFieldValue(control, "m_lexicalEditSurface"), Is.EqualTo(LexicalEditSurface.Avalonia));
		}

		// WIRE-01 (LT-22582): flipping New->Legacy must tear down the Avalonia refresh controller + host NOW
		// (not defer to Dispose), and a subsequent flip back to New must rebuild a fresh surface rather than
		// re-show a disposed one (the pre-fix bug: TearDownAvaloniaSurface disposed but did not null the host,
		// so EnsureAvaloniaSurfaceActive's `== null` guard skipped recreation and .Show()'d a disposed control).
		[Test]
		public void LexiconEditTool_FlipNewLegacyNew_TearsDownThenRebuildsAvaloniaSurface()
		{
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);

			LoadRecordEditView();
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null);
			EnsureCurrentRecord(control);
			Assert.That(GetPrivateFieldValue(control, "m_lexicalEditSurface"), Is.EqualTo(LexicalEditSurface.Avalonia));
			Assert.That(GetPrivateFieldValue(control, "m_avaloniaRefreshController"), Is.Not.Null,
				"the Avalonia surface should own a refresh controller while active");

			// Flip to Legacy: the host + refresh controller are disposed AND nulled now (WIRE-01).
			m_propertyTable.SetProperty("UIMode", "Legacy", true);
			DrainMediatorAndIdleQueues();
			Assert.That(GetPrivateFieldValue(control, "m_lexicalEditSurface"), Is.EqualTo(LexicalEditSurface.WinForms));
			Assert.That(GetPrivateFieldValue(control, "m_avaloniaRefreshController"), Is.Null,
				"flipping to Legacy must dispose+null the refresh controller, not leave it on the PropChanged bus");
			Assert.That(GetPrivateFieldValue(control, "m_avaloniaEntryForm"), Is.Null,
				"flipping to Legacy nulls the Avalonia host so a flip back rebuilds it cleanly");

			// Flip back to New: must rebuild without re-showing a disposed host (the pre-fix crash).
			Assert.DoesNotThrow(() =>
			{
				m_propertyTable.SetProperty("UIMode", "New", true);
				DrainMediatorAndIdleQueues();
				EnsureCurrentRecord(control);
			}, "flip back to New must rebuild the Avalonia surface, not re-show a disposed host");
			Assert.That(GetPrivateFieldValue(control, "m_lexicalEditSurface"), Is.EqualTo(LexicalEditSurface.Avalonia));
			Assert.That(GetPrivateFieldValue(control, "m_avaloniaRefreshController"), Is.Not.Null,
				"flipping back to New must rebuild the refresh controller");
		}

		// §20.3 / §20.5.2: tools whose record-edit surface is NOT yet registered still fall back to legacy under
		// New mode. (domainTypeEdit = a Lists CmPossibility tool pending the F-4 predicate.) Analyses graduated
		// to the Avalonia surface with the interlinear editor (avalonia-interlinear-editor W-4/W-5) — see
		// RegisteredRecordEditTools_ResolveToAvalonia below.
		[TestCase("domainTypeEdit")]
		public void NonMigratedRecordEditTools_FallBackToLegacy_WhenUIModeIsNew(string toolValue)
		{
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);

			LoadRecordEditView(toolValue);
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null, "Expected RecordEditView for tool '{0}'.", toolValue);
			Assert.That(
				GetPrivateFieldValue(control, "m_lexicalEditSurface"),
				Is.EqualTo(LexicalEditSurface.WinForms),
				"Tool '{0}' should explicitly fall back to legacy while Avalonia support is not yet implemented.",
				toolValue);
		}

		// §20.3 / §20.5.2: the edit tools now registered on the Avalonia surface (their record-edit detail
		// composes through the class-general composer + 4-key layout resolution). They resolve to Avalonia
		// under New mode — the per-tool flip the plan calls for as each registers.
		[TestCase("notebookEdit")]
		[TestCase("posEdit")]
		[TestCase("Analyses")] // avalonia-interlinear-editor (4.3): the Words Analyses interlinear editor
		[TestCase("PhonologicalRuleEdit")] // avalonia-rule-formula-editor: regular + metathesis rule editors landed
		[TestCase("EnvironmentEdit")]      // §3.2: environment editor + composer multi-child fix → full editable detail
		[TestCase("compoundRuleAdvancedEdit")] // §2.5: headed/non-headed compound rules compose editably (affix-process read-only)
		[TestCase("naturalClassedit")]         // §3.3: NC segments (editable phoneme vector) + NC features (launcher)
		[TestCase("phonemeEdit")]              // §3.1: IPA symbol editor + derive-on-commit + editable Codes vector
		[TestCase("AdhocCoprohibEdit")]        // §3.4: ad-hoc co-prohibition (Key chooser + Others vector editable; nested groups PARITY)
		public void RegisteredRecordEditTools_ResolveToAvalonia_WhenUIModeIsNew(string toolValue)
		{
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);

			LoadRecordEditView(toolValue);
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null, "Expected RecordEditView for tool '{0}'.", toolValue);
			Assert.That(
				GetPrivateFieldValue(control, "m_lexicalEditSurface"),
				Is.EqualTo(LexicalEditSurface.Avalonia),
				"Tool '{0}' is registered for the Avalonia edit surface (§20.3), so New mode resolves to Avalonia.",
				toolValue);
		}

		private void LoadRecordEditView()
		{
			LoadRecordEditView("lexiconEdit");
		}

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(windowConfiguration, Is.Not.Null, "The xWorks test window should load a merged WindowConfiguration before RecordEditView is activated.");
			var controlNode = windowConfiguration.SelectSingleNode(
				string.Format("//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']", toolValue));
			Assert.That(controlNode, Is.Not.Null, "Expected to find the RecordEditView configuration node for tool '{0}'.", toolValue);

			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
		}

		private void CreateLexiconTestData()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var nounPartOfSpeech = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			AddLexeme(m_createdObjects, "switch-entry", stemMorphType, "switch gloss", nounPartOfSpeech);
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

		private static T GetPrivateField<T>(object target, string fieldName) where T : class
		{
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
			return field.GetValue(target) as T;
		}

		private static object GetPrivateFieldValue(object target, string fieldName)
		{
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
			return field.GetValue(target);
		}

		private void DrainMediatorAndIdleQueues()
		{
			var idleQueue = m_window.Mediator.IdleQueue;
			var processIdle = idleQueue.GetType().GetMethod("Application_Idle", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(processIdle, Is.Not.Null, "Expected to access IdleQueue.Application_Idle for xWorks test pumping.");

			for (var iteration = 0; iteration < 8; iteration++)
			{
				((MockFwXWindow)m_window).ProcessPendingItems();
				if (idleQueue.Count == 0 && m_window.Mediator.JobItems == 0)
					break;

				if (idleQueue.Count > 0)
					processIdle.Invoke(idleQueue, new object[] { this, EventArgs.Empty });
			}

			Application.DoEvents();
		}

		private void EnsureCurrentRecord(RecordEditView control)
		{
			if (control.Clerk.CurrentObject != null)
				return;

			control.Clerk.JumpToIndex(0);
			DrainMediatorAndIdleQueues();
			Assert.That(control.Clerk.CurrentObject, Is.Not.Null, "Expected the RecordEditView clerk to resolve a current lexical record for the switch test.");
		}

		private static Control FindControlRecursive(Control root, string name)
		{
			if (root == null)
				return null;
			if (root.Name == name)
				return root;
			foreach (Control child in root.Controls)
			{
				var found = FindControlRecursive(child, name);
				if (found != null)
					return found;
			}
			return null;
		}
	}
}
