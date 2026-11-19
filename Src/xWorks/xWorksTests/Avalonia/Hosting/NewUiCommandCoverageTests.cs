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
using SIL.FieldWorks.Common.DetailRules;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;
using XCore;
using static SIL.FieldWorks.XWorks.DetailMenuTestSupport;
using Slice = SIL.FieldWorks.Common.Framework.DetailControls.Slice;
using WinFormsDataTree = SIL.FieldWorks.Common.Framework.DetailControls.DataTree;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The New UI has no WinForms detail view: no DataTree, menu handler or slices. These tests
	/// find every main-menu, toolbar, pane-bar and shortcut command and on/off item that
	/// WinForms runs only in that detail view, and check that each New UI tool runs the ones
	/// that apply to it through its command authority.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class NewUiCommandCoverageTests : XWorksAppTestBase
	{
		private const string Lexicon = "lexicon";
		private const string Notebook = "notebook";
		private const string Grammar = "grammar";
		private const string Lists = "lists";
		private const string TextsAndWords = "textsWords";

		/// <summary>
		/// The main-menu, toolbar, pane-bar and shortcut commands that WinForms runs only in its
		/// detail view, each with its area. With no WinForms detail view to fall back on, each
		/// New UI tool a command applies to must run it itself.
		/// </summary>
		private static readonly Dictionary<string, string> CommandsTheNewUiMustRun = new Dictionary<string, string>
		{
			{ "CmdInsertSense", Lexicon }, { "CmdInsertSubsense", Lexicon }, { "CmdInsertVariant", Lexicon },
			{ "CmdDataTree-Insert-AlternateForm", Lexicon }, { "CmdDataTree-Insert-Pronunciation", Lexicon },
			{ "CmdInsertMediaFile", Lexicon }, { "CmdDataTree-Insert-Etymology", Lexicon },
			{ "CmdInsertPicture", Lexicon }, { "CmdInsertExtNote", Lexicon },
			{ "CmdChangeToComplexForm", Lexicon }, { "CmdChangeToVariant", Lexicon },
			{ "CmdAddToLexicon", Notebook }, { "CmdDemoteRecord", Notebook },
			{ "CmdDataTree-Insert-POS-AffixSlot", Grammar }, { "CmdDataTree-Insert-POS-AffixTemplate", Grammar },
			{ "CmdDataTree-Insert-POS-InflectionClass", Grammar }, { "CmdDataTree-Insert-POS-SubPossibilities", Grammar },
			{ "CmdDataTree-Insert-ClosedFeature-Value", Grammar }, { "CmdDataTree-Insert-Phoneme-Code", Grammar },
			{ "CmdDataTree-Insert-Possibility", Lists }, { "CmdDataTree-Insert-CustomItem", Lists },
			{ "CmdDataTree-Insert-SemanticDomain", Lists }, { "CmdDataTree-Insert-AnthroCategory", Lists },
			{ "CmdDataTree-Insert-Location", Lists }, { "CmdDataTree-Insert-LexEntryType", Lists },
			{ "CmdDataTree-Insert-LexEntryInflType", Lists },
			{ "CmdInsertHumanApprovedAnalysis", TextsAndWords }
		};

		/// <summary>
		/// The main-menu and toolbar on/off items that WinForms enables only in its detail view.
		/// Every area shows them, so every New UI tool must answer them itself.
		/// </summary>
		private static readonly string[] PropertiesTheNewUiMustAnswer =
		{
			PropertyConstants.ShowHiddenFields, RecordEditView.ShowDictionaryPreviewProperty
		};

		// A tool shown in a pop-up window has no main menu or toolbar, and a pop-up refuses
		// its pane-bar menus, so none of these commands can be raised there.
		private static readonly string[] PopupTools = { "lexiconEditPopup" };

		// The message the Insert commands share; one is checked by what it inserts.
		private const string InsertMessage = "DataTreeInsert";

		private PropertyTable m_propertyTable;
		private XmlNode m_configuration;

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
			m_configuration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(m_configuration, Is.Not.Null);
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);
		}

		[TearDown]
		public void TearDownWindow()
		{
			m_configuration = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		/// <summary>
		/// This test confirms that the listed commands and on/off items are exactly the ones on
		/// the main menu, toolbars, pane bars and keyboard shortcuts that WinForms runs only in
		/// its detail view. When one is added or removed, the test fails, so the New UI gets an
		/// answer for it before it can be missed.
		/// </summary>
		[Test]
		public void EveryMenuCommandRunOnlyByTheWinFormsDetailView_IsListed()
		{
			var handlers = HandlerTypes();

			var commands = ReachableItems("command").Union(ShortcutCommands()).Where(id => MessageOf(id) != null
				&& AnsweredOnlyByTheDataTree(handlers, "On" + MessageOf(id))).ToList();
			var properties = ReachableItems("boolProperty")
				.Where(name => AnsweredOnlyByTheDataTree(handlers, "OnDisplay" + name)).ToList();

			const string advice = " The New UI has no WinForms detail view to fall back on: answer each new one in "
				+ "RecordEditView.CreateCommandAuthority for the tools it applies to, then list it here. Found: ";
			Assert.That(commands, Is.EquivalentTo(CommandsTheNewUiMustRun.Keys),
				"The commands WinForms runs only in its detail view have changed." + advice + string.Join(", ", commands));
			Assert.That(properties, Is.EquivalentTo(PropertiesTheNewUiMustAnswer),
				"The on/off items WinForms enables only in its detail view have changed." + advice
				+ string.Join(", ", properties));
			Assert.That(CommandsTheNewUiMustRun.Values.Distinct(), Is.SubsetOf(AreaNames()),
				"each listed command names an area of the configuration");
		}

		/// <summary>
		/// This test confirms that every New UI tool, except the pop-up, has an answer for each
		/// listed command and on/off item that applies to it. Without that answer, the command
		/// would do nothing in the New UI.
		/// </summary>
		[Test]
		public void EveryNewUiTool_RunsTheListedCommandsThatApplyToIt()
		{
			var unanswered = new List<string>();
			foreach (var tool in LexiconFeatureCatalog.ToolNames.Except(PopupTools))
			{
				var area = AreaOf(tool);
				var view = LoadView(tool);
				var authority = view.CreateCommandAuthority();
				var answered = new HashSet<string>(authority.AnsweredMessages, StringComparer.Ordinal);
				var recordClass = RecordClassOf(view);
				foreach (var command in CommandsTheNewUiMustRun.Where(entry => entry.Value == area).Select(entry => entry.Key))
				{
					if (AppliesTo(command, recordClass) && !answered.Contains(MessageOf(command)))
						unanswered.Add(tool + " does not answer " + command);
				}
				foreach (var property in PropertiesTheNewUiMustAnswer.Except(authority.AnsweredProperties))
					unanswered.Add(tool + " does not answer the property " + property);
			}

			Assert.That(unanswered, Is.Empty,
				"A New UI tool has no WinForms detail view, so these would do nothing there. Answer each in "
				+ "RecordEditView.CreateCommandAuthority.");
		}

		[Test]
		public void AnInsertCommand_AppliesToATool_WhoseRecordsCanHoldWhatItInserts()
		{
			var partOfSpeech = PartOfSpeechTags.kClassId;
			var possibility = CmPossibilityTags.kClassId;

			Assert.That(AppliesTo("CmdDataTree-Insert-POS-AffixSlot", partOfSpeech), Is.True, "owned by the record");
			Assert.That(AppliesTo("CmdInsertExtNote", LexEntryTags.kClassId), Is.True, "owned by a sense");
			Assert.That(AppliesTo("CmdDataTree-Insert-POS-SubPossibilities", partOfSpeech), Is.True,
				"a sub-item of its own class");
			Assert.That(AppliesTo("CmdDataTree-Insert-POS-SubPossibilities", possibility), Is.False,
				"a sub-item of another class");
			Assert.That(AppliesTo("CmdDataTree-Insert-POS-AffixSlot", possibility), Is.False, "nothing to own it");
			Assert.That(AppliesTo("CmdDataTree-Insert-Phoneme-Code", partOfSpeech), Is.False, "owned by a phoneme");
			Assert.That(AppliesTo("CmdDataTree-Insert-ClosedFeature-Value", partOfSpeech), Is.False,
				"owned by a closed feature");
			Assert.That(AppliesTo("CmdDemoteRecord", possibility), Is.True, "not an Insert command");
		}

		// The class of the tool's records: a possibility list's item class, else the class
		// its record list's field holds.
		private static int RecordClassOf(RecordEditView view)
		{
			var recordClass = view.Clerk.OwningObject is ICmPossibilityList list && list.ItemClsid != 0
				? list.ItemClsid
				: view.Clerk.ListItemsClass;
			Assert.That(recordClass, Is.Not.EqualTo(0), "the tool names the class of its records");
			return recordClass;
		}

		/// <summary>
		/// Whether a tool whose records have the class must answer a listed command. An Insert
		/// command applies when such a record can hold what it inserts: a sub-item only in a
		/// record of its own class, anything else in a field of that name the record owns,
		/// directly or through what it owns. Any other command applies to every tool.
		/// </summary>
		private bool AppliesTo(string commandId, int recordClass)
		{
			if (MessageOf(commandId) != InsertMessage)
				return true;
			var parameters = m_configuration.SelectSingleNode(string.Format(
				"//commands/command[@id='{0}']/parameters", commandId));
			var field = parameters?.Attributes["field"]?.Value;
			var className = parameters?.Attributes["className"]?.Value;
			Assert.That(field, Is.Not.Null.And.Not.Empty, "{0} names the field it inserts into", commandId);
			Assert.That(className, Is.Not.Null.And.Not.Empty, "{0} names the class it inserts", commandId);
			var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
			var inserted = mdc.GetClassId(className);
			if (field == DetailObjectInsertion.SubItemsField)
				return recordClass == inserted;

			var seen = new HashSet<int>();
			var pending = new Queue<int>(new[] { recordClass });
			while (pending.Count > 0)
			{
				var owner = pending.Dequeue();
				if (!seen.Add(owner))
					continue;
				foreach (var flid in mdc.GetFields(owner, true, (int)CellarPropertyTypeFilter.AllOwning))
				{
					var owned = mdc.GetDstClsId(flid);
					if (mdc.GetFieldName(flid) == field && (Cache.ClassIsOrInheritsFrom(inserted, owned)
						|| Cache.ClassIsOrInheritsFrom(owned, inserted)))
					{
						return true;
					}
					// The field's own class only: its subclasses would reach most of the model
					// through fields as general as SubPossibilities.
					pending.Enqueue(owned);
				}
			}
			return false;
		}

		// The command ids or boolean property names of the items on the main menu, the
		// toolbars and every tool's pane bar.
		private List<string> ReachableItems(string attribute)
		{
			var items = m_configuration.SelectNodes(string.Format(
				"//menubar//item[@{0}] | //toolbars//item[@{0}]", attribute)).Cast<XmlNode>().ToList();
			foreach (var paneBar in m_configuration.SelectNodes("//tool//parameters/@PaneBarGroupId").Cast<XmlNode>()
				.Select(a => a.Value).Distinct(StringComparer.Ordinal))
			{
				items.AddRange(m_configuration.SelectNodes(string.Format("//menu[@id='{0}']//item[@{1}]", paneBar, attribute))
					.Cast<XmlNode>());
			}
			return items.Select(item => item.Attributes[attribute].Value).Distinct(StringComparer.Ordinal)
				.OrderBy(id => id, StringComparer.Ordinal).ToList();
		}

		// The commands a keyboard shortcut runs, wherever the focus is.
		private IEnumerable<string> ShortcutCommands()
			=> m_configuration.SelectNodes("//commands/command[@shortcut]/@id").Cast<XmlNode>().Select(a => a.Value);

		// The message a command sends; null for an undefined command or one with no message.
		private string MessageOf(string commandId)
			=> m_configuration.SelectSingleNode(string.Format("//commands/command[@id='{0}']/@message", commandId))?.Value;

		private IEnumerable<string> AreaNames()
			=> m_configuration.SelectNodes("//item[parameters/tools/tool]/@value").Cast<XmlNode>().Select(a => a.Value);

		private string AreaOf(string tool)
		{
			var area = m_configuration.SelectSingleNode(string.Format(
				"//item[parameters/tools/tool[@value='{0}']]/@value", tool))?.Value;
			Assert.That(area, Is.Not.Null, "the configuration puts tool '{0}' in an area", tool);
			return area;
		}

		// The tool's own detail view.
		private RecordEditView LoadView(string tool)
		{
			LoadRecordEditView(m_propertyTable, tool);
			DrainMediatorAndIdleQueues();
			var view = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(view, Is.Not.Null, "tool '{0}' loads a RecordEditView", tool);
			return view;
		}

		private static bool AnsweredOnlyByTheDataTree(Dictionary<string, List<Type>> handlers, string method)
			=> handlers.TryGetValue(method, out var types) && types.All(IsOfTheDataTree);

		private static bool IsOfTheDataTree(Type type)
			=> typeof(WinFormsDataTree).IsAssignableFrom(type) || typeof(DTMenuHandler).IsAssignableFrom(type)
				|| typeof(Slice).IsAssignableFrom(type);

		// Every "On..." method of the product's classes, by name, with the classes declaring
		// it: the methods xCore finds a command's or a display query's handlers by.
		private Dictionary<string, List<Type>> HandlerTypes()
		{
			var directory = Path.GetDirectoryName(typeof(RecordEditView).Assembly.Location);
			foreach (var name in ConfiguredAssemblyNames())
			{
				var path = Path.Combine(directory, Path.GetFileName(name));
				if (!File.Exists(path))
					continue;
				try
				{
					Assembly.LoadFrom(path);
				}
				catch (BadImageFormatException)
				{
					// Not a managed assembly: it declares no handler.
				}
			}

			var handlers = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
			const BindingFlags declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.IsDynamic || string.IsNullOrEmpty(assembly.Location)
					|| assembly.GetName().Name.EndsWith("Tests", StringComparison.Ordinal)
					|| !string.Equals(Path.GetDirectoryName(assembly.Location), directory, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				foreach (var type in LoadableTypes(assembly))
				{
					foreach (var method in type.GetMethods(declared).Where(m => m.Name.StartsWith("On", StringComparison.Ordinal)))
					{
						if (!handlers.TryGetValue(method.Name, out var types))
							handlers[method.Name] = types = new List<Type>();
						types.Add(type);
					}
				}
			}
			Assert.That(handlers.ContainsKey("OnDataTreeInsert"), Is.True, "the scan reads the DataTree's menu handler");
			return handlers;
		}

		private static IEnumerable<Type> LoadableTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				return e.Types.Where(t => t != null);
			}
		}

		// The assemblies the window configuration and the layout parts load classes from.
		private IEnumerable<string> ConfiguredAssemblyNames()
		{
			var names = m_configuration.SelectNodes("//@assemblyPath").Cast<XmlNode>().Select(a => a.Value).ToList();
			foreach (var file in WindowConfigurationIntegrityTests.ShippedLayoutFiles())
			{
				var doc = new XmlDocument();
				doc.Load(file);
				names.AddRange(doc.SelectNodes("//@assemblyPath").Cast<XmlNode>().Select(a => a.Value));
			}
			return names.Distinct(StringComparer.OrdinalIgnoreCase);
		}
	}
}
