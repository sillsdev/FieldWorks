using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
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
		private string m_fixtureProjectFolder;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			// The legacy DataTree's ShowObject (driven by EnsureDataTreeInitialized) needs the
			// legacy layout/parts Inventory loaded; that Inventory is keyed by the project path, so
			// give the in-memory test project a writable temp path before the inventory bootstrap.
			var projectName = Cache.ProjectId.Name;
			m_fixtureProjectFolder = Path.Combine(Path.GetTempPath(),
				"fw-record-edit-switch-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(m_fixtureProjectFolder);
			Cache.ProjectId.Path = Path.Combine(m_fixtureProjectFolder, projectName + ".junk");
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
			// EnsureDataTreeInitialized (LayoutCache loads the real lexicon .fwlayout/Parts).
			// Without it, DataTree.GetTemplateForObjLayout finds a null layout inventory and ShowObject
			// throws an NRE once the idle-queued show actually runs.
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application,
				Cache.ProjectId.ProjectFolder);
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

		protected override void TearDown()
		{
			Inventory.RemoveInventory("layouts", Cache.ProjectId.Name);
			Inventory.RemoveInventory("parts", Cache.ProjectId.Name);
			var configurationDirectory = LcmFileHelper.GetConfigSettingsDir(m_fixtureProjectFolder);
			DeleteEmptyFixtureDirectory(configurationDirectory, m_fixtureProjectFolder);
			DeleteEmptyFixtureDirectory(m_fixtureProjectFolder, Path.GetTempPath());
			base.TearDown();
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
			Assert.That(GetPrivateFieldValue(control, "m_activeUIFramework"), Is.EqualTo(UIFramework.Legacy));
		}

		[Test]
		public void LexiconEditTool_SwitchesUIFrameworkToAvalonia_WhenUIModePropertyBroadcasts()
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
			Assert.That(GetPrivateFieldValue(control, "m_activeUIFramework"), Is.EqualTo(UIFramework.Avalonia));
		}

		// LT-22582: flipping New->Legacy must tear down the Avalonia refresh controller + host NOW
		// (not defer to Dispose), and a subsequent flip back to New must rebuild a fresh entry form rather than
		// re-show a disposed one (the bug pinned here: TearDownAvaloniaEntryForm disposing without nulling the
		// host lets EnsureAvaloniaEntryFormActive's `== null` guard skip recreation and .Show() a disposed control).
		[Test]
		public void LexiconEditTool_FlipNewLegacyNew_TearsDownThenRebuildsAvaloniaEntryForm()
		{
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);

			LoadRecordEditView();
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null);
			EnsureCurrentRecord(control);
			Assert.That(GetPrivateFieldValue(control, "m_activeUIFramework"), Is.EqualTo(UIFramework.Avalonia));
			Assert.That(GetPrivateFieldValue(control, "m_avaloniaRefreshController"), Is.Not.Null,
				"the Avalonia view should own a refresh controller while active");

			// Flip to Legacy: the host + refresh controller are disposed AND nulled now.
			m_propertyTable.SetProperty("UIMode", "Legacy", true);
			DrainMediatorAndIdleQueues();
			Assert.That(GetPrivateFieldValue(control, "m_activeUIFramework"), Is.EqualTo(UIFramework.Legacy));
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
			}, "flip back to New must rebuild the Avalonia entry form, not re-show a disposed host");
			Assert.That(GetPrivateFieldValue(control, "m_activeUIFramework"), Is.EqualTo(UIFramework.Avalonia));
			Assert.That(GetPrivateFieldValue(control, "m_avaloniaRefreshController"), Is.Not.Null,
				"flipping back to New must rebuild the refresh controller");
		}

		[Test]
		public void LexiconEditTool_PersistedProjectLayoutChange_IsVisibleAfterFrameworkSwitch()
		{
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView();
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null)
				as RecordEditView;
			Assert.That(control, Is.Not.Null);
			EnsureCurrentRecord(control);
			Assert.That(GetHostedDetailModel(control).Fields,
				Has.Some.Property("Field").EqualTo("CitationForm"),
				"precondition: the initial project layout includes Citation Form");

			var layouts = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
			var configurationDirectory = LcmFileHelper.GetConfigSettingsDir(
				Cache.ProjectId.ProjectFolder);
			var overridePath = GetLexEntryOverridePath(configurationDirectory);
			var overrideExisted = File.Exists(overridePath);
			var overrideBytes = overrideExisted ? File.ReadAllBytes(overridePath) : null;
			var original = layouts.GetElement("layout",
				new[] { "LexEntry", "detail", "Normal", null }).Clone();
			var changed = new XmlDocument();
			changed.LoadXml("<layout class='LexEntry' type='detail' name='Normal'>"
				+ "<part ref='CitationForm' visibility='never'/></layout>");
			try
			{
				layouts.PersistOverrideElement(changed.DocumentElement);

				m_propertyTable.SetProperty("UIMode", "Legacy", true);
				DrainMediatorAndIdleQueues();
				m_propertyTable.SetProperty("UIMode", "New", true);
				DrainMediatorAndIdleQueues();
				EnsureCurrentRecord(control);

				Assert.That(GetHostedDetailModel(control).Fields,
					Has.None.Property("Field").EqualTo("CitationForm"),
					"the next host composition should read the persisted inventory content");
			}
			finally
			{
				RestoreOverrideFile(configurationDirectory, overridePath, overrideExisted,
					overrideBytes);
				LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application,
					Cache.ProjectId.ProjectFolder);
			}
			Assert.That(File.Exists(overridePath), Is.EqualTo(overrideExisted),
				"the persistence test should restore the override file's prior existence");
			if (overrideExisted)
			{
				Assert.That(File.ReadAllBytes(overridePath), Is.EqualTo(overrideBytes),
					"the persistence test should restore the override file's exact bytes");
			}
			var restored = Inventory.GetInventory("layouts", Cache.ProjectId.Name).GetElement("layout",
				new[] { "LexEntry", "detail", "Normal", null });
			Assert.That(restored.OuterXml, Is.EqualTo(original.OuterXml),
				"the effective layout inventory should be restored for later tests");
		}

		// Tools not registered for Avalonia fall back to legacy under
		// New mode. (domainTypeEdit = a Lists CmPossibility tool.) Analyses rides
		// the interlinear editor's Avalonia work -- see
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
				GetPrivateFieldValue(control, "m_activeUIFramework"),
				Is.EqualTo(UIFramework.Legacy),
				"Tool '{0}' should explicitly fall back to legacy while Avalonia support is not yet implemented.",
				toolValue);
		}

		// The detail-editor tools registered for Avalonia. They
		// resolve to Avalonia under New mode. The interlinear (Analyses) and rule-formula tools (PhonologicalRuleEdit,
		// EnvironmentEdit, compoundRuleAdvancedEdit, naturalClassedit, phonemeEdit, AdhocCoprohibEdit) are
		// INERT (see UIFrameworkRegistry.Phase1FollowUpTools); activating one registers it and
		// adds the corresponding TestCase row here.
		[TestCase("notebookEdit")]
		[TestCase("posEdit")]
		public void RegisteredRecordEditTools_ResolveToAvalonia_WhenUIModeIsNew(string toolValue)
		{
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);

			LoadRecordEditView(toolValue);
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null, "Expected RecordEditView for tool '{0}'.", toolValue);
			Assert.That(
				GetPrivateFieldValue(control, "m_activeUIFramework"),
				Is.EqualTo(UIFramework.Avalonia),
				"Tool '{0}' is registered for the Avalonia edit framework (§20.3), so New mode resolves to Avalonia.",
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

		private static DetailModel GetHostedDetailModel(RecordEditView control)
		{
			var entryForm = GetPrivateField<DetailHostControl>(control, "m_avaloniaEntryForm");
			Assert.That(entryForm, Is.Not.Null, "the Avalonia detail host should be initialized");
			var hostField = typeof(AvaloniaHostControlBase).GetField("Host",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(hostField, Is.Not.Null, "the host content field should exist");
			var host = hostField.GetValue(entryForm);
			var content = host.GetType().GetProperty("Content").GetValue(host, null);
			var tree = content as SIL.FieldWorks.Common.FwAvalonia.Detail.DataTree;
			Assert.That(tree, Is.Not.Null, "the Avalonia host should contain a detail tree");
			return tree.Model;
		}

		private static string GetLexEntryOverridePath(string configurationDirectory)
		{
			var fullDirectory = Path.GetFullPath(configurationDirectory);
			var overridePath = Path.GetFullPath(Path.Combine(fullDirectory, "LexEntry.fwlayout"));
			Assert.That(Path.GetDirectoryName(overridePath),
				Is.EqualTo(fullDirectory).IgnoreCase,
				"the override path must remain inside the fixture ConfigurationSettings directory");
			return overridePath;
		}

		private static void RestoreOverrideFile(string configurationDirectory, string overridePath,
			bool existed, byte[] bytes)
		{
			var validatedPath = GetLexEntryOverridePath(configurationDirectory);
			Assert.That(overridePath, Is.EqualTo(validatedPath).IgnoreCase,
				"cleanup must target the captured LexEntry override path");
			if (existed)
			{
				Directory.CreateDirectory(configurationDirectory);
				File.WriteAllBytes(validatedPath, bytes);
			}
			else if (File.Exists(validatedPath))
			{
				File.Delete(validatedPath);
			}
		}

		private static void DeleteEmptyFixtureDirectory(string directory, string expectedParent)
		{
			var fullDirectory = Path.GetFullPath(directory)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			var fullParent = Path.GetFullPath(expectedParent)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (!string.Equals(Path.GetDirectoryName(fullDirectory), fullParent,
				StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Fixture cleanup directory is outside its expected parent.");
			}
			if (Directory.Exists(fullDirectory)
				&& Directory.GetFileSystemEntries(fullDirectory).Length == 0)
			{
				Directory.Delete(fullDirectory);
			}
		}

		// DrainMediatorAndIdleQueues is inherited from XWorksAppTestBase.

		private void EnsureCurrentRecord(RecordEditView control)
		{
			// Force a fresh record navigation rather than early-returning when the clerk already has a
			// CurrentObject. When a tool loads directly into an Avalonia UIMode, the clerk's list load
			// can populate CurrentObject WITHOUT routing through RecordEditView.ShowRecord/ShowAvaloniaEntry
			// (the code path that realizes the Avalonia entry form and wires its refresh controller). An early
			// return on CurrentObject != null therefore left the entry form unrealized, so the refresh-controller
			// assertion saw null even though the production show path is correct. JumpToIndex re-broadcasts
			// RecordNavigation even when the index is unchanged (see RecordClerk.JumpToIndex, the LT-11401
			// handling), so this exercises the real ShowRecord -> ShowAvaloniaEntry -> EnsureAvaloniaRefreshController
			// path the way the running product does once the idle show fires against the selected record.
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
