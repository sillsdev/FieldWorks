// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using XCore;
using LegacyDataTree = SIL.FieldWorks.Common.Framework.DetailControls.DataTree;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	[NonParallelizable]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class LayoutPersistenceParityTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;
		private RecordEditView m_view;
		private Inventory m_layouts;
		private Inventory m_previousLayouts;
		private Inventory m_previousParts;
		private bool m_inventoryRegistrationCaptured;
		private string m_configurationDirectory;
		private string m_overridePath;
		private bool m_overrideExisted;
		private byte[] m_overrideBytes;
		private string m_originalLayoutXml;
		private int m_originalCitationIndex;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory,
				m_application.DefaultConfigurationPathname);
			Cache.ProjectId.Path = Path.Combine(Path.GetTempPath(), Cache.ProjectId.Name,
				Cache.ProjectId.Name + ".junk");
		}

		[SetUp]
		public void SetUpWindow()
		{
			m_inventoryRegistrationCaptured = false;
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			TestLocalizationManagerBootstrap.EnsureInitialized();
			TestLocalizationManagerBootstrap.EnsureHelpTopicProvider(m_propertyTable);
			m_previousLayouts = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
			m_previousParts = Inventory.GetInventory("parts", Cache.ProjectId.Name);
			m_inventoryRegistrationCaptured = true;
			if (m_previousLayouts == null || m_previousParts == null)
			{
				LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application,
					Cache.ProjectId.Path);
			}
			m_layouts = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
			Assert.That(m_layouts, Is.Not.Null);
			Assert.That(Inventory.GetInventory("parts", Cache.ProjectId.Name), Is.Not.Null);
			m_configurationDirectory = Path.GetFullPath(
				LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.Path));
			m_overridePath = Path.GetFullPath(Path.Combine(m_configurationDirectory,
				"LexEntry.fwlayout"));
			AssertPathIsInConfigurationSettings(m_overridePath);
			m_overrideExisted = File.Exists(m_overridePath);
			m_overrideBytes = m_overrideExisted ? File.ReadAllBytes(m_overridePath) : null;
			m_originalLayoutXml = CurrentLexEntryLayout().OuterXml;

			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateTestEntry);
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();
			m_view = m_propertyTable.GetValue<object>("currentContentControlObject", null)
				as RecordEditView;
			Assert.That(m_view, Is.Not.Null);
			EnsureCurrentRecord();
			Assert.That(GetField(m_view, "m_activeUIFramework"), Is.EqualTo(UIFramework.Avalonia));
			m_originalCitationIndex = FieldIndex(GetHostedDetailModel(), "CitationForm");
		}

		[TearDown]
		public void TearDownWindow()
		{
			try
			{
				RestoreLayoutOverride();
			}
			finally
			{
				try
				{
					NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DestroyTestData);
				}
				finally
				{
					try
					{
						m_createdObjects = null;
						m_entry = null;
						m_view = null;
						m_overridePath = null;
						m_propertyTable?.RemoveLocalAndGlobalSettings();
						m_propertyTable = null;
						if (m_window != null && !m_window.IsDisposed)
							m_window.Dispose();
						m_window = null;
					}
					finally
					{
						RestoreInventoryRegistrations();
						m_layouts = null;
						m_previousLayouts = null;
						m_previousParts = null;
					}
				}
			}
		}

		[Test]
		public void AvaloniaNormallyHidden_ReloadsIntoAvaloniaAndLegacyDataTree()
		{
			ResetToNoProjectOverride();
			var field = GetHostedDetailModel().Fields.Single(item => item.Field == "CitationForm");

			ExecuteNative(field, "Normally hidden");

			Assert.That(GetHostedDetailModel().Fields,
				Has.None.Property("Field").EqualTo("CitationForm"));
			var modelBeforeReload = GetHostedDetailModel();
			m_layouts.Reload();
			RefreshAvaloniaDetail();
			Assert.That(GetHostedDetailModel(), Is.Not.SameAs(modelBeforeReload));
			Assert.That(GetHostedDetailModel().Fields,
				Has.None.Property("Field").EqualTo("CitationForm"));
			AssertCitationVisibility("never");
			var legacyTree = LegacyTree();
			legacyTree.RefreshList(true);
			var legacyLayouts = (Inventory)GetField(legacyTree, "m_layoutInventory");
			Assert.That(legacyLayouts, Is.SameAs(m_layouts));
			var part = legacyLayouts.GetElement("layout",
				new[] { "LexEntry", "detail", "Normal", null })
				.SelectSingleNode("part[@ref='CitationFormAllV']");
			Assert.That(part.Attributes?["visibility"]?.Value, Is.EqualTo("never"));
			Assert.That(legacyTree.Slices,
				Has.None.Property("Flid").EqualTo(LexEntryTags.kflidCitationForm));
		}

		[Test]
		public void LegacyAlwaysVisible_ReloadsIntoReconstructedAvaloniaHost()
		{
			ResetToNoProjectOverride();
			var field = GetHostedDetailModel().Fields.Single(item => item.Field == "CitationForm");
			ExecuteNative(field, "Normally hidden, unless non-empty");
			m_layouts.Reload();
			AssertCitationVisibility("ifdata");
			EnsureAdapter(m_entry.Hvo, "CitationForm");
			var legacyTree = LegacyTree();
			legacyTree.SetCurrentSliceForCommandTarget(legacyTree.Slices.Single(slice =>
				slice.Flid == LexEntryTags.kflidCitationForm));

			ExecuteLegacy("Always visible");

			m_layouts.Reload();
			AssertCitationVisibility("always");
			RefreshAvaloniaDetail();
			Assert.That(GetHostedDetailModel().Fields,
				Has.Some.Property("Field").EqualTo("CitationForm"));
		}

		[Test]
		public void AvaloniaMove_ReloadsInWinFormsOrder()
		{
			ResetToNoProjectOverride();
			var beforeModel = GetHostedDetailModel();
			var beforeIndex = FieldIndex(beforeModel, "CitationForm");
			var citation = beforeModel.Fields.Single(item => item.Field == "CitationForm");
			EnsureAdapter(m_entry.Hvo, "CitationForm");
			var beforeLegacyIndex = LegacyTree().Slices.FindIndex(slice =>
				slice.Flid == LexEntryTags.kflidCitationForm);

			ExecuteNative(citation, "Move Down");

			var afterAvaloniaIndex = FieldIndex(GetHostedDetailModel(), "CitationForm");
			Assert.That(afterAvaloniaIndex, Is.GreaterThan(beforeIndex));
			m_layouts.Reload();
			EnsureAdapter(m_entry.Hvo, "CitationForm");
			var legacyIndex = LegacyTree().Slices.FindIndex(slice =>
				slice.Flid == LexEntryTags.kflidCitationForm);
			Assert.That(legacyIndex, Is.GreaterThan(beforeLegacyIndex));
		}

		[Test]
		public void WinFormsMove_ReloadsInAvaloniaOrder()
		{
			ResetToNoProjectOverride();
			var beforeIndex = FieldIndex(GetHostedDetailModel(), "CitationForm");
			EnsureAdapter(m_entry.Hvo, "CitationForm");
			var legacyTree = LegacyTree();
			legacyTree.SetCurrentSliceForCommandTarget(legacyTree.Slices.Single(slice =>
				slice.Flid == LexEntryTags.kflidCitationForm));

			ExecuteLegacy("Move Down");

			m_layouts.Reload();
			RefreshAvaloniaDetail();
			Assert.That(FieldIndex(GetHostedDetailModel(), "CitationForm"),
				Is.GreaterThan(beforeIndex));
		}

		[Test]
		public void PersistentLayoutCommand_CreatesOnlyProjectFwlayoutArtifact()
		{
			ResetToNoProjectOverride();
			var filesBefore = ConfigurationFiles();
			var field = GetHostedDetailModel().Fields.Single(item => item.Field == "CitationForm");

			ExecuteNative(field, "Normally hidden");

			var created = ConfigurationFiles().Except(filesBefore,
				StringComparer.OrdinalIgnoreCase).ToArray();
			Assert.That(created.Select(path => path.ToUpperInvariant()),
				Is.EqualTo(new[] { m_overridePath.ToUpperInvariant() }));
			Assert.That(created.Select(Path.GetExtension), Is.All.EqualTo(".fwlayout"));
		}

		private void ResetToNoProjectOverride()
		{
			if (File.Exists(m_overridePath))
				File.Delete(m_overridePath);
			m_layouts.Reload();
			Assert.That(File.Exists(m_overridePath), Is.False);
			RefreshAvaloniaDetail();
			Assert.That(GetHostedDetailModel().Fields,
				Has.Some.Property("Field").EqualTo("CitationForm"));
		}

		private void ExecuteNative(DetailField field, string label)
		{
			var method = typeof(RecordEditView).GetMethod("CreateNativeDetailMenuItems",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null);
			var items = (IReadOnlyList<DetailMenuItem>)method.Invoke(m_view,
				new object[] { field, new[] { "mnuDataTree-MultiStringSlice", RecordEditView.ObjectMenuId } });
			ExecuteItem(items, label);
		}

		private void ExecuteLegacy(string label)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			var items = XCoreMenuBridge.CreateMenuItems(window,
				new[] { "mnuDataTree-MultiStringSlice", RecordEditView.ObjectMenuId });
			ExecuteItem(items, label);
			m_window.Mediator.IdleQueue.Clear();
		}

		private static void ExecuteItem(IReadOnlyList<DetailMenuItem> items, string label)
		{
			var item = FindItem(items, label);
			Assert.That(item, Is.Not.Null, "expected the '{0}' command", label);
			Assert.That(item.IsEnabled, Is.True, "expected the '{0}' command to be enabled", label);
			item.Execute();
		}

		private static DetailMenuItem FindItem(IReadOnlyList<DetailMenuItem> items, string label)
		{
			foreach (var item in items)
			{
				if (string.Equals(item.Label, label, StringComparison.Ordinal))
					return item;
				var nested = FindItem(item.Children, label);
				if (nested != null)
					return nested;
			}
			return null;
		}

		private void EnsureAdapter(int targetHvo, string fieldName)
		{
			var method = typeof(RecordEditView).GetMethod("EnsureMenuCommandAdapter",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(method, Is.Not.Null);
			method.Invoke(m_view, new object[] { targetHvo, fieldName });
		}

		private void RefreshAvaloniaDetail()
		{
			var refresh = typeof(RecordEditView).GetMethod("RefreshAvaloniaDetail",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(refresh, Is.Not.Null);
			refresh.Invoke(m_view, null);
			DrainMediatorAndIdleQueues();
		}

		private DetailModel GetHostedDetailModel()
		{
			var entryForm = (DetailHostControl)GetField(m_view, "m_avaloniaEntryForm");
			var hostField = typeof(AvaloniaHostControlBase).GetField("Host",
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(hostField, Is.Not.Null);
			var host = hostField.GetValue(entryForm);
			var content = host.GetType().GetProperty("Content").GetValue(host, null);
			var tree = content as SIL.FieldWorks.Common.FwAvalonia.Detail.DataTree;
			Assert.That(tree, Is.Not.Null);
			return tree.Model;
		}

		private LegacyDataTree LegacyTree()
		{
			var tree = (LegacyDataTree)GetField(m_view, "m_dataEntryForm");
			Assert.That(tree.Slices, Is.Not.Empty);
			return tree;
		}

		private XmlNode CurrentLexEntryLayout()
		{
			var layout = m_layouts.GetElement("layout",
				new[] { "LexEntry", "detail", "Normal", null });
			Assert.That(layout, Is.Not.Null);
			return layout;
		}

		private void AssertCitationVisibility(string expected)
		{
			var part = CurrentLexEntryLayout().SelectSingleNode("part[@ref='CitationFormAllV']");
			Assert.That(part, Is.Not.Null);
			Assert.That(part.Attributes?["visibility"]?.Value, Is.EqualTo(expected));
		}

		private static int FieldIndex(DetailModel model, string fieldName)
		{
			return model.Fields.ToList().FindIndex(field => field.Field == fieldName);
		}

		private string[] ConfigurationFiles()
		{
			if (!Directory.Exists(m_configurationDirectory))
				return Array.Empty<string>();
			return Directory.GetFiles(m_configurationDirectory, "*", SearchOption.AllDirectories)
				.Select(Path.GetFullPath).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
		}

		private void AssertPathIsInConfigurationSettings(string path)
		{
			var relative = Path.GetFullPath(path).Substring(m_configurationDirectory.Length)
				.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			Assert.That(relative, Is.Not.Empty);
			Assert.That(relative.StartsWith(".." + Path.DirectorySeparatorChar,
				StringComparison.Ordinal), Is.False);
			Assert.That(Path.IsPathRooted(relative), Is.False);
		}

		private void RestoreLayoutOverride()
		{
			if (m_layouts == null || string.IsNullOrEmpty(m_overridePath))
				return;
			AssertPathIsInConfigurationSettings(m_overridePath);
			if (m_overrideExisted)
			{
				Directory.CreateDirectory(m_configurationDirectory);
				File.WriteAllBytes(m_overridePath, m_overrideBytes);
			}
			else if (File.Exists(m_overridePath))
			{
				File.Delete(m_overridePath);
			}
			m_layouts.Reload();
			Assert.That(Inventory.GetInventory("layouts", Cache.ProjectId.Name), Is.SameAs(m_layouts));
			Assert.That(File.Exists(m_overridePath), Is.EqualTo(m_overrideExisted));
			if (m_overrideExisted)
				Assert.That(File.ReadAllBytes(m_overridePath), Is.EqualTo(m_overrideBytes));
			Assert.That(CurrentLexEntryLayout().OuterXml, Is.EqualTo(m_originalLayoutXml));
			if (m_view != null && !m_view.IsDisposed)
			{
				RefreshAvaloniaDetail();
				Assert.That(FieldIndex(GetHostedDetailModel(), "CitationForm"),
					Is.EqualTo(m_originalCitationIndex));
			}
		}

		private void RestoreInventoryRegistrations()
		{
			if (!m_inventoryRegistrationCaptured)
				return;
			RestoreInventoryRegistration("layouts", m_previousLayouts);
			RestoreInventoryRegistration("parts", m_previousParts);
			m_inventoryRegistrationCaptured = false;
		}

		private void RestoreInventoryRegistration(string key, Inventory previous)
		{
			if (ReferenceEquals(Inventory.GetInventory(key, Cache.ProjectId.Name), previous))
				return;
			if (previous == null)
				Inventory.RemoveInventory(key, Cache.ProjectId.Name);
			else
				Inventory.SetInventory(key, Cache.ProjectId.Name, previous);
			Assert.That(Inventory.GetInventory(key, Cache.ProjectId.Name), Is.SameAs(previous));
		}

		private void CreateTestEntry()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var noun = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(m_createdObjects, "layout-parity-entry", stemMorphType,
				"first gloss", noun);
			m_entry.CitationForm.set_String(Cache.DefaultVernWs,
				TsStringUtils.MakeString("citation", Cache.DefaultVernWs));
			m_entry.Bibliography.set_String(Cache.DefaultAnalWs,
				TsStringUtils.MakeString("bibliography", Cache.DefaultAnalWs));
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

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			var controlNode = windowConfiguration.SelectSingleNode(string.Format(
				"//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']",
				toolValue));
			Assert.That(controlNode, Is.Not.Null);
			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
		}

		private void EnsureCurrentRecord()
		{
			if (m_view.Clerk.CurrentObject?.Hvo != m_entry.Hvo)
			{
				m_view.Clerk.JumpToRecord(m_entry.Hvo);
				DrainMediatorAndIdleQueues();
			}
			Assert.That(m_view.Clerk.CurrentObject?.Hvo, Is.EqualTo(m_entry.Hvo));
		}

		private static object GetField(object target, string fieldName)
		{
			var field = target.GetType().GetField(fieldName,
				BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "missing private field: " + fieldName);
			return field.GetValue(target);
		}
	}
}
