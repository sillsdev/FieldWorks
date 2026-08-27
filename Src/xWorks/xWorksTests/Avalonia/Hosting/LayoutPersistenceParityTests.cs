// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
		private string m_configurationBoundary;
		private ConfigurationSettingsSnapshot m_configurationSnapshot;
		private string m_overridePath;
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
			m_configurationSnapshot = null;
			m_previousLayouts = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
			m_previousParts = Inventory.GetInventory("parts", Cache.ProjectId.Name);
			m_inventoryRegistrationCaptured = true;
			try
			{
				m_configurationDirectory = Path.GetFullPath(
					LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.Path))
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				m_configurationBoundary = m_configurationDirectory + Path.DirectorySeparatorChar;
				CaptureConfigurationSettings();
				Assert.That(m_configurationSnapshot, Is.Not.Null);
				m_overridePath = Path.GetFullPath(Path.Combine(m_configurationDirectory,
					"LexEntry.fwlayout"));
				AssertPathIsInConfigurationSettings(m_overridePath);

				m_window = new MockFwXWindow(m_application, m_configFilePath);
				((MockFwXWindow)m_window).Init(Cache);
				m_propertyTable = m_window.PropTable;
				m_propertyTable.RemoveLocalAndGlobalSettings();
				m_window.LoadUI(m_configFilePath);
				TestLocalizationManagerBootstrap.EnsureInitialized();
				TestLocalizationManagerBootstrap.EnsureHelpTopicProvider(m_propertyTable);
				if (m_previousLayouts == null || m_previousParts == null)
				{
					LayoutCache.InitializePartInventories(Cache.ProjectId.Name, m_application,
						Cache.ProjectId.Path);
				}
				m_layouts = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
				Assert.That(m_layouts, Is.Not.Null);
				Assert.That(Inventory.GetInventory("parts", Cache.ProjectId.Name), Is.Not.Null);
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
			catch
			{
				try
				{
					CleanupTestState(false);
				}
				catch
				{
				}
				throw;
			}
		}

		[TearDown]
		public void TearDownWindow()
		{
			CleanupTestState(true);
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
			var filesBefore = ConfigurationFileSnapshot();
			var field = GetHostedDetailModel().Fields.Single(item => item.Field == "CitationForm");

			ExecuteNative(field, "Normally hidden");

			var filesAfter = ConfigurationFileSnapshot();
			var created = filesAfter.Keys.Except(filesBefore.Keys,
				StringComparer.OrdinalIgnoreCase).ToArray();
			var removed = filesBefore.Keys.Except(filesAfter.Keys,
				StringComparer.OrdinalIgnoreCase).ToArray();
			Assert.That(created.Select(path => path.ToUpperInvariant()),
				Is.EqualTo(new[] { m_overridePath.ToUpperInvariant() }));
			Assert.That(created.Select(Path.GetExtension), Is.All.EqualTo(".fwlayout"));
			Assert.That(filesAfter[m_overridePath], Is.Not.Empty);
			Assert.That(removed, Is.Empty);
			foreach (var file in filesBefore)
			{
				Assert.That(filesAfter, Does.ContainKey(file.Key));
				Assert.That(filesAfter[file.Key], Is.EqualTo(file.Value));
			}
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

		private Dictionary<string, byte[]> ConfigurationFileSnapshot()
		{
			var snapshot = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
			if (!Directory.Exists(m_configurationDirectory))
				return snapshot;
			foreach (var path in Directory.GetFiles(m_configurationDirectory, "*",
				SearchOption.AllDirectories))
			{
				var fullPath = Path.GetFullPath(path);
				AssertPathIsInConfigurationSettings(fullPath);
				snapshot.Add(fullPath, File.ReadAllBytes(fullPath));
			}
			return snapshot;
		}

		private void AssertPathIsInConfigurationSettings(string path)
		{
			var fullPath = Path.GetFullPath(path);
			Assert.That(fullPath.StartsWith(m_configurationBoundary,
				StringComparison.OrdinalIgnoreCase), Is.True);
		}

		private void CaptureConfigurationSettings()
		{
			var directoryExisted = Directory.Exists(m_configurationDirectory);
			var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
			var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (directoryExisted)
			{
				foreach (var file in ConfigurationFileSnapshot())
					files.Add(RelativeConfigurationPath(file.Key), file.Value);
				foreach (var directory in Directory.GetDirectories(m_configurationDirectory, "*",
					SearchOption.AllDirectories))
				{
					var fullPath = Path.GetFullPath(directory);
					AssertPathIsInConfigurationSettings(fullPath);
					directories.Add(RelativeConfigurationPath(fullPath));
				}
			}
			m_configurationSnapshot = new ConfigurationSettingsSnapshot(directoryExisted,
				files, directories);
		}

		private string RelativeConfigurationPath(string path)
		{
			var fullPath = Path.GetFullPath(path);
			if (!fullPath.StartsWith(m_configurationBoundary, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Path is outside ConfigurationSettings: " + fullPath);
			return fullPath.Substring(m_configurationBoundary.Length);
		}

		private string FullConfigurationPath(string relativePath)
		{
			if (string.IsNullOrEmpty(relativePath) || Path.IsPathRooted(relativePath))
				throw new InvalidOperationException("ConfigurationSettings path must be relative.");
			var fullPath = Path.GetFullPath(Path.Combine(m_configurationDirectory, relativePath));
			AssertPathIsInConfigurationSettings(fullPath);
			return fullPath;
		}

		private void RestoreConfigurationSettings(bool assertBehavior)
		{
			if (m_configurationSnapshot == null)
				return;
			var currentFiles = ConfigurationFileSnapshot();
			foreach (var file in currentFiles.Keys)
			{
				var relative = RelativeConfigurationPath(file);
				if (!m_configurationSnapshot.Files.ContainsKey(relative))
					File.Delete(file);
			}
			foreach (var file in m_configurationSnapshot.Files)
			{
				var fullPath = FullConfigurationPath(file.Key);
				Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
				File.WriteAllBytes(fullPath, file.Value);
			}
			RestoreConfigurationDirectories();
			AssertConfigurationSettingsRestored();
			if (m_layouts == null)
				return;
			m_layouts.Reload();
			if (!assertBehavior || string.IsNullOrEmpty(m_originalLayoutXml))
				return;
			Assert.That(Inventory.GetInventory("layouts", Cache.ProjectId.Name), Is.SameAs(m_layouts));
			Assert.That(CurrentLexEntryLayout().OuterXml, Is.EqualTo(m_originalLayoutXml));
			if (m_view == null || m_view.IsDisposed)
				return;
			RefreshAvaloniaDetail();
			Assert.That(FieldIndex(GetHostedDetailModel(), "CitationForm"),
				Is.EqualTo(m_originalCitationIndex));
		}

		private void RestoreConfigurationDirectories()
		{
			if (Directory.Exists(m_configurationDirectory))
			{
				var directories = Directory.GetDirectories(m_configurationDirectory, "*",
					SearchOption.AllDirectories).OrderByDescending(path => path.Length).ToArray();
				foreach (var directory in directories)
				{
					var fullPath = Path.GetFullPath(directory);
					AssertPathIsInConfigurationSettings(fullPath);
					if (!m_configurationSnapshot.Directories.Contains(
						RelativeConfigurationPath(fullPath))
						&& !Directory.EnumerateFileSystemEntries(fullPath).Any())
					{
						Directory.Delete(fullPath, false);
					}
				}
			}
			foreach (var directory in m_configurationSnapshot.Directories
				.OrderBy(path => path.Length))
				Directory.CreateDirectory(FullConfigurationPath(directory));
			if (!m_configurationSnapshot.DirectoryExisted
				&& Directory.Exists(m_configurationDirectory)
				&& !Directory.EnumerateFileSystemEntries(m_configurationDirectory).Any())
			{
				Directory.Delete(m_configurationDirectory, false);
			}
		}

		private void AssertConfigurationSettingsRestored()
		{
			Assert.That(Directory.Exists(m_configurationDirectory),
				Is.EqualTo(m_configurationSnapshot.DirectoryExisted));
			if (!m_configurationSnapshot.DirectoryExisted)
				return;
			var restored = ConfigurationFileSnapshot().ToDictionary(
				item => RelativeConfigurationPath(item.Key), item => item.Value,
				StringComparer.OrdinalIgnoreCase);
			Assert.That(restored.Keys, Is.EquivalentTo(m_configurationSnapshot.Files.Keys));
			foreach (var file in m_configurationSnapshot.Files)
				Assert.That(restored[file.Key], Is.EqualTo(file.Value));
			var restoredDirectories = Directory.GetDirectories(m_configurationDirectory, "*",
				SearchOption.AllDirectories).Select(RelativeConfigurationPath).ToArray();
			Assert.That(restoredDirectories,
				Is.EquivalentTo(m_configurationSnapshot.Directories));
		}

		private void CleanupTestState(bool assertBehavior)
		{
			Exception firstFailure = null;
			CaptureCleanupFailure(() => RestoreConfigurationSettings(assertBehavior), ref firstFailure);
			if (m_createdObjects != null)
			{
				CaptureCleanupFailure(() => NonUndoableUnitOfWorkHelper.Do(
					Cache.ActionHandlerAccessor, DestroyTestData), ref firstFailure);
			}
			CaptureCleanupFailure(() => m_propertyTable?.RemoveLocalAndGlobalSettings(),
				ref firstFailure);
			if (m_window != null && !m_window.IsDisposed)
				CaptureCleanupFailure(() => m_window.Dispose(), ref firstFailure);
			if (m_inventoryRegistrationCaptured)
			{
				CaptureCleanupFailure(() => RestoreInventoryRegistration("layouts", m_previousLayouts),
					ref firstFailure);
				CaptureCleanupFailure(() => RestoreInventoryRegistration("parts", m_previousParts),
					ref firstFailure);
				m_inventoryRegistrationCaptured = false;
			}
			m_createdObjects = null;
			m_entry = null;
			m_view = null;
			m_layouts = null;
			m_previousLayouts = null;
			m_previousParts = null;
			m_propertyTable = null;
			m_window = null;
			m_overridePath = null;
			m_configurationSnapshot = null;
			if (firstFailure != null)
				ExceptionDispatchInfo.Capture(firstFailure).Throw();
		}

		private static void CaptureCleanupFailure(Action action, ref Exception firstFailure)
		{
			try
			{
				action();
			}
			catch (Exception error)
			{
				if (firstFailure == null)
					firstFailure = error;
			}
		}

		private sealed class ConfigurationSettingsSnapshot
		{
			internal ConfigurationSettingsSnapshot(bool directoryExisted,
				Dictionary<string, byte[]> files, HashSet<string> directories)
			{
				DirectoryExisted = directoryExisted;
				Files = files;
				Directories = directories;
			}

			internal bool DirectoryExisted { get; }

			internal Dictionary<string, byte[]> Files { get; }

			internal HashSet<string> Directories { get; }
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
