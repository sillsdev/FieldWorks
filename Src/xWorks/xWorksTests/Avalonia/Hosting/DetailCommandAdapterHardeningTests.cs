// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;
using DetailLayoutIdentity = SIL.FieldWorks.Common.FwAvalonia.Detail.DetailLayoutIdentity;
using LegacyDataTree = SIL.FieldWorks.Common.Framework.DetailControls.DataTree;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Hardening for the Avalonia lexical-edit object-command path.
	///
	/// <c>EnsureMenuCommandTarget</c> targeting: when the target hvo's slice is a lazy,
	/// unrealized <c>DummyObjectSlice</c> (a sequence with &gt;= <c>DataTree.kInstantSliceMax</c> items
	/// builds lazy placeholders whose <c>Object</c> is the OWNER, not the target), a naive walk finds no
	/// matching slice and leaves CurrentSlice pointed wherever the previous interaction left it
	/// -- the
	/// command mis-targets or (for Merge's class guard) silently fails. The adapter realizes the
	/// lazy slices and retries, and fails LOUD (clears CurrentSlice + logs) when no slice can be produced.
	///
	/// Splitter width SESSION persistence: the host's remembered label-column width is
	/// process-only, so the product host routes a PropertyTable LocalSetting so the width survives across
	/// sessions, mirroring the expansion-persistence pattern.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class DetailCommandAdapterHardeningTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private RecordEditView m_view;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
			// The hidden legacy DataTree's ShowObject needs the layout/parts Inventory, which is
			// keyed by project path: give the in-memory project a writable temp path first.
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
			// The lexicon detail layout includes the Chorus-backed MessageSlice, which localizes
			// strings when built; register a minimal LocalizationManager (as the product does at
			// startup) so ShowObject can build the full tree headlessly.
			TestLocalizationManagerBootstrap.EnsureInitialized();
			// The mock app has no inner help-topic provider; give the PropertyTable a null-returning
			// stub so the legacy Help command can be queried while materializing the menu.
			TestLocalizationManagerBootstrap.EnsureHelpTopicProvider(m_propertyTable);
			// Bootstrap the legacy layout/parts Inventory the production RecordEditView loads via
			// EnsureDataTreeInitialized (LayoutCache loads the real lexicon .fwlayout/Parts).
			// Without it, DataTree.GetTemplateForObjLayout finds a null layout inventory and ShowObject
			// throws an NRE. This is the same bootstrap the DictionaryConfigurationMigrator tests use.
			LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application, Cache.ProjectId.Path);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateEntryWithManySenses);

			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();

			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(m_view, Is.Not.Null);
			EnsureCurrentRecord(m_view);
		}

		[TearDown]
		public void TearDownWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DestroyTestData);
			m_createdObjects = null;
			m_entry = null;
			m_view = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		// ----------------------------------------------------------------------------------------
		// Adapter targeting through (and past) lazy slices
		// ----------------------------------------------------------------------------------------

		// Skipped (desktop environment only): realizing lazy DummyObjectSlices and pointing CurrentSlice at a deep
		// target runs through DataTree.FieldAt/MakeSliceRealAt, which depend on a laid-out, VISIBLE
		// tree (ClientRectangle width, AutoScrollPosition, MakeSliceVisible). The command-routing
		// adapter tree is hidden + detached while Avalonia is active, so headlessly the
		// lazy slices do not realize and CurrentSlice cannot be resolved. Runnable in the desktop environment
		// where the legacy tree is shown. (The DataTreeMove reachability tests cover the targeting/
		// reachability logic that CAN be exercised without a live tree.)
		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree to realize lazy slices and resolve CurrentSlice; see note above. Runs in the desktop environment.")]
		public void EnsureMenuCommandTarget_TargetInLazySliceRange_RealizesAndTargetsTheRightObject()
		{
			// The entry has well over DataTree.kInstantSliceMax (20) senses, so Senses
			// builds lazy DummyObjectSlices; a deep target's slice does not exist yet,
			// so a single-pass match leaves CurrentSlice mis-pointed.
			Assert.That(m_entry.SensesOS.Count, Is.GreaterThan(20),
				"precondition: enough senses to force lazy DummyObjectSlices");
			var deepSenseHvo = m_entry.SensesOS[m_entry.SensesOS.Count - 1].Hvo;

			EnsureAdapter(deepSenseHvo);

			var dataTree = (LegacyDataTree)GetField(m_view, "m_dataEntryForm");
			var current = dataTree.CurrentSlice;
			Assert.That(current, Is.Not.Null,
				"the adapter must realize the lazy slice and point CurrentSlice at the deep target");
			Assert.That(current.Object, Is.Not.Null);
			Assert.That(current.Object.Hvo, Is.EqualTo(deepSenseHvo),
				"CurrentSlice targets the requested object, not a lazy dummy's owner or a stale slice");
			Assert.That(current.IsRealSlice, Is.True, "the targeted slice was realized");
		}

		[Test]
		[Explicit("Requires the live (laid-out, visible) legacy DataTree to realize slices and resolve/clear CurrentSlice; the hidden detached adapter tree never lays out headlessly. Runs in the desktop environment.")]
		public void EnsureMenuCommandTarget_NoSliceMatchesHvo_ClearsCurrentSliceRatherThanMisTarget()
		{
			// First point the adapter at a real sense, so CurrentSlice is non-null...
			var realSenseHvo = m_entry.SensesOS[0].Hvo;
			EnsureAdapter(realSenseHvo);
			var dataTree = (LegacyDataTree)GetField(m_view, "m_dataEntryForm");
			Assert.That(dataTree.CurrentSlice, Is.Not.Null, "precondition: a slice is current");

			// ...then target an hvo that has NO slice in this entry's tree (a foreign object). The hardened
			// adapter must NOT leave the previous sense's slice current (that would mis-target the command);
			// it clears CurrentSlice so the command handlers see "no current slice" and no-op.
			ILexEntry foreign = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var stem = GetMorphTypeOrCreateOne("stem");
				var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
				foreign = AddLexeme(m_createdObjects, "foreign", stem, "foreign gloss", noun);
			});

			EnsureAdapter(foreign.Hvo);

			Assert.That(dataTree.CurrentSlice, Is.Null,
				"when no slice matches the target, CurrentSlice is cleared so the command no-ops rather "
				+ "than mis-targeting the previously selected object");
		}

		// ----------------------------------------------------------------------------------------
		// Splitter width persists to a PropertyTable LocalSetting (round-trips across sessions)
		// ----------------------------------------------------------------------------------------

		[Test]
		public void LabelColumnWidth_PersistsToLocalSetting_AndRoundTrips()
		{
			var persist = typeof(RecordEditView).GetMethod("PersistLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var read = typeof(RecordEditView).GetMethod("GetPersistedLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(persist, Is.Not.Null, "splitter persistence setter must exist");
			Assert.That(read, Is.Not.Null, "splitter persistence getter must exist");

			// Nothing persisted yet -> null, so the view falls back to the density default.
			Assert.That(read.Invoke(m_view, null), Is.Null,
				"with no stored setting the getter returns null (density default applies)");

			persist.Invoke(m_view, new object[] { 173.5 });

			Assert.That((double?)read.Invoke(m_view, null), Is.EqualTo(173.5).Within(0.001),
				"the persisted width round-trips through the getter");

			// It really landed in the PropertyTable local settings keyed by the current tool, marked
			// persistent (so it survives across sessions, unlike a process-only host field).
			var key = "LexEditLabelColumnWidth:lexiconEdit";
			var stored = m_propertyTable.GetStringProperty(key, null, PropertyTable.SettingsGroup.LocalSettings);
			Assert.That(stored, Is.Not.Null.And.Not.Empty,
				"the width is stored in a PropertyTable LocalSetting keyed by tool");
		}

		[Test]
		public void LabelColumnWidth_IgnoresNonPositiveWidths()
		{
			var persist = typeof(RecordEditView).GetMethod("PersistLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);
			var read = typeof(RecordEditView).GetMethod("GetPersistedLabelColumnWidth",
				BindingFlags.Instance | BindingFlags.NonPublic);

			persist.Invoke(m_view, new object[] { 0.0 });
			Assert.That(read.Invoke(m_view, null), Is.Null, "a zero width must not be persisted");
			persist.Invoke(m_view, new object[] { -5.0 });
			Assert.That(read.Invoke(m_view, null), Is.Null, "a negative width must not be persisted");
		}

		[Test]
		public void AvaloniaComposition_UsesProjectLayoutInventoryInitializedByLayoutCache()
		{
			var expected = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
			Assert.That(expected, Is.Not.Null,
				"LayoutCache.InitializePartInventories should install the project inventory");

			var source = GetField(m_view, "m_inventoryViewDefinitionSource");

			Assert.That(source, Is.Not.Null,
				"showing the record should lazily create the project view-definition source");
			Assert.That(GetField(source, "_layouts"), Is.SameAs(expected),
				"the host source must use the project-keyed Inventory singleton");
		}

		[Test]
		public void ImportedCallerPath_IsCanonicalWhenPartsSkipOrExpandOutput()
		{
			var parts = new DictionaryPartResolver(XElement.Parse(@"
<PartInventory><bin>
  <part id='LexEntry-Detail-Multiple'>
    <slice field='CitationForm' editor='string'/>
    <slice field='CitationForm' editor='string'/>
  </part>
  <part id='LexEntry-Detail-Single'>
    <slice field='CitationForm' editor='string'/>
  </part>
</bin></PartInventory>"));
			const string layoutXml = @"
<layout class='LexEntry' type='detail' name='Normal'>
  <part ref='Missing'/>
  <part ref='Multiple'/>
  <part ref='Single'/>
</layout>";

			var model = new XmlLayoutImporter().Import(XElement.Parse(layoutXml), parts);
			var xml = new XmlDocument();
			xml.LoadXml(layoutXml);
			var legacyCaller = xml.SelectSingleNode("/layout/part[@ref='Multiple']");

			Assert.That(model.Roots.Take(2).Select(node => node.SourceCallerPath),
				Is.All.EqualTo("part[1]"),
				"every output expanded from one caller must retain the same source identity");
			Assert.That(model.Roots[2].SourceCallerPath, Is.EqualTo("part[2]"),
				"a skipped caller must not collapse the source address to the output index");
			Assert.That(LegacyLayoutCallerPath.Get(legacyCaller), Is.EqualTo("part[1]"),
				"the XmlNode slice key and XElement importer clones must compute the same identity");
		}

		[TestCase(0, 0, "Reject")]
		[TestCase(1, 0, "UseRealized")]
		[TestCase(0, 1, "RealizeLazy")]
		[TestCase(1, 1, "Reject")]
		[TestCase(0, 2, "Reject")]
		public void PersistentTargetArbitration_RequiresExactlyOneCombinedMatch(int realized,
			int lazy, string expectedName)
		{
			var expected = (RecordEditView.PersistentTargetAction)Enum.Parse(
				typeof(RecordEditView.PersistentTargetAction), expectedName);
			Assert.That(RecordEditView.ArbitratePersistentTarget(realized, lazy), Is.EqualTo(expected));
		}

		[Test]
		public void PersistentTargetArbitration_SoleRealizedUsesItOnce()
		{
			var realizedCalls = 0;
			var lazyCalls = 0;
			var rescanCalls = 0;

			Assert.That(RecordEditView.ExecutePersistentTargetArbitration(1, 0,
				() => { realizedCalls++; return true; },
				() => { lazyCalls++; return true; },
				() => { rescanCalls++; return true; },
				() => false), Is.True);
			Assert.That(realizedCalls, Is.EqualTo(1));
			Assert.That(lazyCalls, Is.Zero);
			Assert.That(rescanCalls, Is.Zero);
		}

		[Test]
		public void PersistentTargetArbitration_SoleLazyRealizesOnceAndRescansOnce()
		{
			var realizedCalls = 0;
			var lazyCalls = 0;
			var rescanCalls = 0;

			Assert.That(RecordEditView.ExecutePersistentTargetArbitration(0, 1,
				() => { realizedCalls++; return true; },
				() => { lazyCalls++; return true; },
				() => { rescanCalls++; return true; },
				() => false), Is.True);
			Assert.That(realizedCalls, Is.Zero);
			Assert.That(lazyCalls, Is.EqualTo(1));
			Assert.That(rescanCalls, Is.EqualTo(1));
		}

		[Test]
		public void PersistentTargetArbitration_RescanFailureAfterSoleLazyMatchFailsClosed()
		{
			var realizedCalls = 0;
			var lazyCalls = 0;
			var rescanCalls = 0;
			var rejectCalls = 0;

			Assert.That(RecordEditView.ExecutePersistentTargetArbitration(0, 1,
				() => { realizedCalls++; return true; },
				() => { lazyCalls++; return true; },
				() => { rescanCalls++; return false; },
				() => { rejectCalls++; return false; }), Is.False);
			Assert.That(realizedCalls, Is.Zero);
			Assert.That(lazyCalls, Is.EqualTo(1));
			Assert.That(rescanCalls, Is.EqualTo(1));
			Assert.That(rejectCalls, Is.Zero);
		}

		[Test]
		public void PersistentTargetArbitration_RealizationFailureDoesNotRescan()
		{
			var lazyCalls = 0;
			var rescanCalls = 0;
			var rejectCalls = 0;

			Assert.That(RecordEditView.ExecutePersistentTargetArbitration(0, 1,
				() => true,
				() => { lazyCalls++; return false; },
				() => { rescanCalls++; return true; },
				() => { rejectCalls++; return false; }), Is.False);
			Assert.That(lazyCalls, Is.EqualTo(1));
			Assert.That(rescanCalls, Is.Zero);
			Assert.That(rejectCalls, Is.EqualTo(1));
		}

		[Test]
		public void IsValidPersistentTarget_EmptyLayoutNameIsValidButNullIsAbsent()
		{
			var validator = typeof(RecordEditView).GetMethod("IsValidPersistentTarget",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(validator, Is.Not.Null);

			Assert.That((bool)validator.Invoke(m_view, new object[] { PersistentField(string.Empty) }),
				Is.True);
			Assert.That((bool)validator.Invoke(m_view, new object[] { PersistentField(null) }),
				Is.False);
		}

		private DetailField PersistentField(string layoutName)
		{
			var field = new DetailField("LexEntry/test", "Citation Form", "CitationForm", null,
				DetailFieldKind.Text, EditorClassification.Known, null, null, HostRouting.Product,
				null, null, null, objectHvo: m_entry.Hvo);
			field.ClassName = "LexEntry";
			field.LayoutType = "detail";
			field.LayoutName = layoutName;
			field.SourceCallerPath = "part[0]";
			field.LayoutPath = new[]
			{
				new DetailLayoutIdentity("LexEntry", "detail", layoutName, null)
			};
			return field;
		}

		[Test]
		public void PersistentSliceIdentity_CarriesCallerAndSelectedLayoutAcrossObjectDescent()
		{
			var document = new XmlDocument();
			document.LoadXml(@"<root>
  <layout class='LexEntry' type='detail' name='Normal'>
    <part ref='Outer'><indent><part ref='Inner'/></indent></part>
  </layout>
  <layout class='LexSense' type='detail' name='Normal' choiceGuid='choice-a'>
    <part ref='Nested'/>
  </layout>
</root>");
			var layouts = document.SelectNodes("/root/layout");
			var inner = layouts[0].SelectSingleNode("part/indent/part");
			var nested = layouts[1].SelectSingleNode("part");
			using (var slice = new Slice
			{
				Cache = Cache,
				Object = m_entry,
				ConfigurationNode = document.CreateElement("slice"),
				Key = new object[] { layouts[0], inner, m_entry.Hvo, layouts[1], nested }
			})
			{
				var identity = m_view.PersistentSliceIdentity(slice);

				Assert.That(identity.CallerPath,
					Is.EqualTo("part[0]/indent[0]/part[0]|part[0]"));
				Assert.That(identity.LayoutPath, Has.Count.EqualTo(2));
				Assert.That(identity.LayoutPath[1].ChoiceGuid, Is.EqualTo("choice-a"));
			}
		}

		[Test]
		public void PersistentSliceIdentity_FinalSublayoutResetsLayoutAndCallerChain()
		{
			var document = new XmlDocument();
			document.LoadXml(@"<root>
  <layout class='LexEntry' type='detail' name='Normal'><part ref='Outer'/></layout>
  <sublayout name='Inline'/>
  <layout class='LexEntry' type='detail' name='Inline'><part ref='Inner'/></layout>
</root>");
			var outerLayout = document.SelectSingleNode("/root/layout[@name='Normal']");
			var outerPart = outerLayout.SelectSingleNode("part");
			var sublayout = document.SelectSingleNode("/root/sublayout");
			var innerLayout = document.SelectSingleNode("/root/layout[@name='Inline']");
			var innerPart = innerLayout.SelectSingleNode("part");
			using (var slice = new Slice
			{
				Cache = Cache,
				Object = m_entry,
				ConfigurationNode = document.CreateElement("slice"),
				Key = new object[] { outerLayout, outerPart, sublayout, innerLayout, innerPart }
			})
			{
				var identity = m_view.PersistentSliceIdentity(slice);

				Assert.That(identity.LayoutName, Is.EqualTo("Inline"));
				Assert.That(identity.CallerPath, Is.EqualTo("part[0]"));
				Assert.That(identity.LayoutPath, Has.Count.EqualTo(1));
			}
		}

		// ----------------------------------------------------------------------------------------
		// Bootstrap helpers
		// ----------------------------------------------------------------------------------------

		private void CreateEntryWithManySenses()
		{
			var stem = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "many-sense-entry", stem, "gloss 0", noun);
			// 25 senses: comfortably above DataTree.kInstantSliceMax (20) so the Senses sequence builds
			// lazy DummyObjectSlices in the adapter tree.
			for (var i = 1; i < 25; i++)
				AddSenseToEntry(m_createdObjects, m_entry, "gloss " + i, noun);
		}

		private void DestroyTestData()
		{
			if (m_createdObjects == null)
				return;
			foreach (var obj in m_createdObjects)
			{
				if (obj.IsValidObject && obj is ILexEntry)
					obj.Delete();
			}
		}

		// Targets the hidden adapter tree exactly as a native menu item's Execute does.
		private void EnsureAdapter(int targetHvo, string fieldName = null)
		{
			var method = typeof(RecordEditView).GetMethod("EnsureMenuCommandTarget",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null);
			method.Invoke(m_view, new object[] { targetHvo, fieldName });
		}

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			var controlNode = windowConfiguration.SelectSingleNode(
				string.Format("//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']", toolValue));
			Assert.That(controlNode, Is.Not.Null);
			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
		}

		private void EnsureCurrentRecord(RecordEditView control)
		{
			if (control.Clerk.CurrentObject?.Hvo != m_entry.Hvo)
			{
				control.Clerk.JumpToRecord(m_entry.Hvo);
				DrainMediatorAndIdleQueues();
			}
			Assert.That(control.Clerk.CurrentObject, Is.Not.Null);
		}

		private static object GetField(object target, string fieldName)
		{
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
			return field.GetValue(target);
		}

		// DrainMediatorAndIdleQueues is inherited from XWorksAppTestBase.
	}
}
