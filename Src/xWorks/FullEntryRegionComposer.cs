// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>A composed full-entry region: the renderable model plus its LCModel-bound edit context.</summary>
	public sealed class ComposedEntryRegion
	{
		public ComposedEntryRegion(LexicalEditRegionModel model, IRegionEditContext editContext,
			IReadOnlyList<ComposedCustomEditorField> customEditorFields = null)
		{
			Model = model;
			EditContext = editContext;
			CustomEditorFields = customEditorFields ?? Array.Empty<ComposedCustomEditorField>();
		}

		public LexicalEditRegionModel Model { get; }

		public IRegionEditContext EditContext { get; }

		/// <summary>
		/// The legacy class/assembly identities of the dynamically loaded custom slices that composed
		/// as placeholder rows (unsupported or best-effort read-only), keyed back to the model by each
		/// row's StableId. The host uses this to promote designated WinForms-only slices (the Chorus
		/// Messages notes bar) to the hybrid companion strip instead of showing the placeholder row
		/// (see AvaloniaCompanionSlices).
		/// </summary>
		public IReadOnlyList<ComposedCustomEditorField> CustomEditorFields { get; }
	}

	/// <summary>
	/// Composes the COMPLETE Lexical Edit view for an entry (sections 6/7): walks the compiled
	/// `LexEntry/Normal` typed definition the same way legacy DataTree walks layouts — expanding
	/// object/sequence nodes across objects by compiling each target's own layout (with the legacy
	/// base-class walk), emitting section headers, indentation, per-writing-system editable text
	/// fields, the morph-type chooser, read-only reference rows, and `ifdata` hiding — every field
	/// bound to LCModel through metadata (class/field → flid) and editable through the fenced
	/// session. Labels localize through the same <see cref="StringTable"/> lane legacy slices use.
	/// Unsupported constructs render an explicit unsupported row (visibility=always) or are skipped
	/// (ifdata), never silently mis-rendered; compile diagnostics ride the region model.
	/// </summary>
	public static class FullEntryRegionComposer
	{
		private const int MaxDepth = 6;
		private static readonly ViewDefinitionCompiler Compiler = new ViewDefinitionCompiler();

		// Review task 10: deliberately NOT a Lazy — a failed load must not be cached as null for
		// the process lifetime (the old behavior: one transient IO hiccup silently demoted every
		// future compose to the 3-field first slice). A successful load is immutable and cached
		// forever; a failure logs (see LoadSources) and is retried on the next compose.
		private static readonly object SourcesSync = new object();
		private static CompilerSources s_sources;

		private static CompilerSources GetSources()
		{
			var sources = s_sources;
			if (sources != null)
				return sources;
			lock (SourcesSync)
			{
				return s_sources ?? (s_sources = LoadSources());
			}
		}

		// Review finding A (observable memoization): counts the expensive snapshot builds (layout
		// lookup + layout.ToString() + fingerprint + compile). A repeat compose must not grow it.
		private static int s_snapshotCompileCount;

		internal static int SnapshotCompileCount => s_snapshotCompileCount;

		// Review finding A: the loaded sources are immutable for the process lifetime, so the layout
		// lookup is indexed once and compiled definitions are memoized per (starting class, layout)
		// — repeat composes (and the per-item menu peeks below) never rebuild/re-fingerprint the
		// ~300KB parts snapshot. Class ids and the class hierarchy are fixed LCModel metadata, so
		// the memo is safe across caches.
		private sealed class CompilerSources
		{
			public string PartsXml;
			public Dictionary<(string ClassName, string Type, string Name), XElement> LayoutIndex;
			public readonly ConcurrentDictionary<(int ClassId, string LayoutName), ViewDefinitionModel> CompiledModels
				= new ConcurrentDictionary<(int, string), ViewDefinitionModel>();
		}

		public static ComposedEntryRegion Compose(ILexEntry entry, LcmCache cache, bool showHiddenFields = false,
			RegionEditorPluginRegistry plugins = null, RegionEditorServices services = null)
		{
			if (entry == null) throw new ArgumentNullException(nameof(entry));
			if (cache == null) throw new ArgumentNullException(nameof(cache));

			var root = CompileForObject(cache, entry, "Normal");
			if (root == null)
				return null;

			// winforms-free-lexeme-editor.md D1: plugin rows close over the region's own edit
			// context, which only exists after the walk has gathered every setter — a deferred
			// accessor bridges the gap (plugin factories run at render time, never during compose).
			// D4: host services (the legacy-dialog launcher seam) ride the same closure; null when
			// the host supplies none, and service-aware plugins must tolerate that.
			IRegionEditContext composedContext = null;
			var state = new ComposeState(cache, showHiddenFields,
				plugins ?? RegionEditorPluginRegistry.Default, () => composedContext, services);
			foreach (var node in root.Roots)
				state.Walk(node, entry, 0);

			var context = new ComposedRegionEditContext(cache, entry, state.TextSetters, state.OptionSetters,
				state.ReferenceAddSetters, state.ReferenceRemoveSetters);
			composedContext = context;
			var model = new LexicalEditRegionModel("LexEntry", "Normal", state.Fields, root.Diagnostics);
			return new ComposedEntryRegion(model, context, state.CustomEditorFields);
		}

		/// <summary>
		/// B8/B7: walks a possibility list's tree in document order (parent before children) into
		/// chooser options, hierarchy carried as <see cref="RegionChoiceOption.Depth"/> — exactly
		/// the indented tree the legacy chooser shows. <paramref name="flat"/> (a chooserInfo
		/// "FlatList" guicontrol spec, e.g. PeopleFlatList) keeps the order but suppresses the
		/// hierarchy, like the legacy flat chooser. Review task 12: the implementation lives in
		/// the shared <see cref="RegionValueFactory"/> so this composer and
		/// <see cref="LexicalEditRegionBuilder"/> cannot drift; this wrapper keeps the composer's
		/// established internal surface (and its tests).
		/// </summary>
		internal static IReadOnlyList<RegionChoiceOption> BuildPossibilityOptions(
			ICmPossibilityList list, bool flat)
			=> RegionValueFactory.BuildPossibilityOptions(list, flat);

		// The legacy generic possibility-list → lists-area-tool derivation, mirrored statically.
		// Research (gear = configure): when a legacy jump's target object is owned by a
		// CmPossibilityList, LinkListener.FollowActiveLink (Src/xWorks/LinkListener.cs:507-517)
		// publishes "GetToolForList", handled by AreaListener.GetToolForList
		// (Src/LexText/LexTextDll/AreaListener.cs:388-418): it walks the lists-area tools in the
		// window configuration, resolves each tool's clerk recordList (owner=/property=) to the
		// actual list through the SDA, and returns the first tool whose clerk edits that list;
		// unmatched (ownerless = user custom) lists derive Name-without-spaces + "Edit"
		// (AreaListener.GetCustomListToolName, AreaListener.cs:832-835 — the tool name the lists
		// area generates dynamically per custom list, AreaListener.CreateCustomToolNode).
		// The composer runs without a window configuration, so the clerk table itself
		// (DistFiles/Language Explorer/Configuration/Lists/areaConfiguration.xml clerks ↔
		// Lists/Edit/toolConfiguration.xml tools) is mirrored here, keyed (owner class, owning
		// field name). Owned lists missing from the table have no lists-area editor → null → no
		// gear on rows backed by them.
		private static readonly IReadOnlyDictionary<(string Owner, string Field), string> ListEditorToolByOwnerField =
			new Dictionary<(string, string), string>
			{
				{ ("LangProject", "AffixCategories"), "affixCategoryEdit" },
				{ ("LangProject", "AnnotationDefs"), "annotationDefEdit" },
				{ ("LangProject", "AnthroList"), "anthroEdit" },
				{ ("LangProject", "ConfidenceLevels"), "confidenceEdit" },
				{ ("LangProject", "Education"), "educationEdit" },
				{ ("LangProject", "GenreList"), "genresEdit" },
				{ ("LangProject", "Locations"), "locationsEdit" },
				{ ("LangProject", "People"), "peopleEdit" },
				{ ("LangProject", "Positions"), "positionsEdit" },
				{ ("LangProject", "Restrictions"), "restrictionsEdit" },
				{ ("LangProject", "Roles"), "roleEdit" },
				{ ("LangProject", "SemanticDomainList"), "semanticDomainEdit" },
				{ ("LangProject", "Status"), "statusEdit" },
				{ ("LangProject", "TextMarkupTags"), "textMarkupTagsEdit" },
				{ ("LangProject", "TimeOfDay"), "timeOfDayEdit" },
				{ ("LangProject", "TranslationTags"), "translationTypeEdit" },
				{ ("LexDb", "ComplexEntryTypes"), "complexEntryTypeEdit" },
				{ ("LexDb", "DialectLabels"), "dialectsListEdit" },
				{ ("LexDb", "DomainTypes"), "domainTypeEdit" },
				{ ("LexDb", "ExtendedNoteTypes"), "extNoteTypeEdit" },
				{ ("LexDb", "Languages"), "languagesListEdit" },
				{ ("LexDb", "MorphTypes"), "morphTypeEdit" },
				{ ("LexDb", "PublicationTypes"), "publicationsEdit" },
				{ ("LexDb", "References"), "lexRefEdit" },
				{ ("LexDb", "SenseTypes"), "senseTypeEdit" },
				{ ("LexDb", "Status"), "senseStatusEdit" },
				{ ("LexDb", "UsageTypes"), "usageTypeEdit" },
				{ ("LexDb", "VariantEntryTypes"), "variantEntryTypeEdit" },
				{ ("DsDiscourseData", "ChartMarkers"), "chartmarkEdit" },
				{ ("DsDiscourseData", "ConstChartTempl"), "charttempEdit" },
				{ ("RnResearchNbk", "RecTypes"), "recTypeEdit" }
			};

		/// <summary>
		/// Resolves the lists-area tool that edits <paramref name="list"/> — the configure gear's
		/// jump target when the layout authored no explicit chooserLink. Mirrors legacy
		/// <c>AreaListener.GetToolForList</c>: shipped lists match the lists-area clerk table by
		/// (owner class, owning field); ownerless lists are user custom lists, whose dynamically
		/// generated tool is Name-without-spaces + "Edit"; anything else resolves to null (no
		/// lists-area editor exists, so the row gets no gear).
		/// </summary>
		internal static string ResolveListEditorTool(ICmPossibilityList list)
		{
			if (list == null)
				return null;

			if (list.Owner == null)
			{
				// Legacy AreaListener.GetCustomListToolName (custom lists are ownerless).
				var name = list.Name?.BestAnalysisAlternative?.Text;
				return string.IsNullOrEmpty(name) || name == "***"
					? null
					: name.Replace(" ", string.Empty) + "Edit";
			}

			string fieldName;
			try
			{
				var mdc = (IFwMetaDataCacheManaged)list.Cache.DomainDataByFlid.MetaDataCache;
				fieldName = mdc.GetFieldName(list.OwningFlid);
			}
			catch (Exception)
			{
				return null;
			}

			return ListEditorToolByOwnerField.TryGetValue((list.Owner.ClassName, fieldName), out var tool)
				? tool
				: null;
		}

		private sealed class ComposeState
		{
			private readonly LcmCache _cache;
			private readonly ISilDataAccess _sda;
			private readonly IFwMetaDataCacheManaged _mdc;
			private readonly HashSet<(int hvo, string layout)> _visited = new HashSet<(int, string)>();

			public readonly List<LexicalEditRegionField> Fields = new List<LexicalEditRegionField>();
			public readonly Dictionary<string, Func<string, string, bool>> TextSetters
				= new Dictionary<string, Func<string, string, bool>>(StringComparer.Ordinal);
			public readonly Dictionary<string, Func<string, bool>> OptionSetters
				= new Dictionary<string, Func<string, bool>>(StringComparer.Ordinal);
			// 6.3: reference-vector add/remove staging, keyed like the other setters by StableId.
			public readonly Dictionary<string, Func<string, bool>> ReferenceAddSetters
				= new Dictionary<string, Func<string, bool>>(StringComparer.Ordinal);
			public readonly Dictionary<string, Func<string, bool>> ReferenceRemoveSetters
				= new Dictionary<string, Func<string, bool>>(StringComparer.Ordinal);
			// Companion lane: the unsupported rows that are really legacy dynamic custom slices,
			// keyed by the row's StableId (see ComposedEntryRegion.CustomEditorFields).
			public readonly List<ComposedCustomEditorField> CustomEditorFields
				= new List<ComposedCustomEditorField>();

			private readonly bool _showHidden;
			// winforms-free-lexeme-editor.md D1: the plugin registry consulted FIRST for every
			// custom slice, plus the deferred accessor for the edit context plugin factories
			// receive (resolved when the factory runs, after Compose has built the context).
			private readonly RegionEditorPluginRegistry _plugins;
			private readonly Func<IRegionEditContext> _editContextAccessor;
			// D4: the host-injected services handed to service-aware plugins (null when none).
			private readonly RegionEditorServices _services;
			// Finding A: per-compose memos — the morph-type option list is identical for every
			// IMoForm, and an item layout's menu/hotlinks binding is identical per (class, layout).
			private List<RegionChoiceOption> _morphTypeOptions;
			private readonly Dictionary<(int ClassId, string LayoutName), (string MenuId, string HotlinksId)> _itemMenuBindings
				= new Dictionary<(int, string), (string, string)>();

			public ComposeState(LcmCache cache, bool showHiddenFields,
				RegionEditorPluginRegistry plugins, Func<IRegionEditContext> editContextAccessor,
				RegionEditorServices services = null)
			{
				_cache = cache;
				_showHidden = showHiddenFields;
				_plugins = plugins;
				_editContextAccessor = editContextAccessor;
				_services = services;
				_sda = cache.DomainDataByFlid;
				_mdc = (IFwMetaDataCacheManaged)cache.DomainDataByFlid.MetaDataCache;
			}

			// Viewing parity: "show hidden fields" surfaces visibility=never fields and keeps empty
			// ifdata fields visible, exactly like legacy m_fShowAllFields.
			private bool IsHidden(ViewNode node) => node.Visibility == ViewVisibility.Never && !_showHidden;

			private bool HideWhenEmpty(ViewNode node) => node.Visibility == ViewVisibility.IfData && !_showHidden;

			public void Walk(ViewNode node, ICmObject obj, int depth)
			{
				if (IsHidden(node) || depth > MaxDepth)
					return;

				switch (node.Kind)
				{
					case ViewNodeKind.Field:
						WalkField(node, obj, depth);
						break;
					case ViewNodeKind.Group:
						WalkGroup(node, obj, depth);
						break;
					case ViewNodeKind.ObjectAtom:
						WalkObjectAtom(node, obj, depth);
						break;
					case ViewNodeKind.Sequence:
						WalkSequence(node, obj, depth);
						break;
					case ViewNodeKind.CustomFieldPlaceholder:
						// B1 (xml-retirement-blockers): runtime expansion of `customFields="here"` from
						// live MDC metadata. The `<generate>` compile-time lane stays 9.2/9.3.
						WalkCustomFields(node, obj, depth);
						break;
					case ViewNodeKind.Conditional:
						// B3: legacy <if>/<ifnot> — content composes only when the per-object condition
						// passes (DataTree.ProcessSubpartNode cases "if"/"ifnot").
						WalkConditional(node, obj, depth);
						break;
					case ViewNodeKind.ChoiceGroup:
						// B3: legacy <choice> — first passing <where> (or the <otherwise>) only.
						WalkChoiceGroup(node, obj, depth);
						break;
				}
			}

			// B3: <if>/<ifnot> wrapper — evaluate and pass through; failing branches drop entirely.
			private void WalkConditional(ViewNode node, ICmObject obj, int depth)
			{
				if (node.Condition != null && !ConditionPasses(node.Condition, obj))
					return;
				foreach (var child in node.Children)
					Walk(child, obj, depth);
			}

			// B3: <choice> semantics from DataTree.ProcessSubpartNode case "choice": expand only the
			// FIRST <where> whose condition passes; an <otherwise> branch (null condition) always
			// passes and stops the scan.
			private void WalkChoiceGroup(ViewNode node, ICmObject obj, int depth)
			{
				foreach (var branch in node.Children)
				{
					if (branch.Kind != ViewNodeKind.Conditional)
						continue;
					if (branch.Condition != null && !ConditionPasses(branch.Condition, obj))
						continue;
					foreach (var child in branch.Children)
						Walk(child, obj, depth);
					break;
				}
			}

			// B3: evaluate the imported condition per object, mirroring XmlVc.ConditionPasses exactly
			// as the legacy detail lane invokes it (DataTree.cs:2639-2696 over XmlVc.cs:3276-3290):
			// resolve the target object, then every test present must pass; <ifnot> negates the result.
			private bool ConditionPasses(ViewCondition condition, ICmObject obj)
			{
				var passes = EvaluateCondition(condition, obj);
				return condition.Negated ? !passes : passes;
			}

			private bool EvaluateCondition(ViewCondition condition, ICmObject obj)
			{
				// Target hop (XmlVc.GetActualTarget): "this" (default), "owner", or an atomic field.
				var target = obj;
				if (!string.IsNullOrEmpty(condition.Target)
					&& !string.Equals(condition.Target, "this", StringComparison.OrdinalIgnoreCase))
				{
					if (string.Equals(condition.Target, "owner", StringComparison.OrdinalIgnoreCase))
					{
						target = obj.Owner;
					}
					else
					{
						var targetFlid = GetFlid(obj, condition.Target);
						var targetHvo = targetFlid == 0 ? 0 : _sda.get_ObjectProp(obj.Hvo, targetFlid);
						target = targetHvo == 0
							|| !_cache.ServiceLocator.ObjectRepository.TryGetObject(targetHvo, out var resolved)
							? null
							: resolved;
					}

					// Legacy treats a missing target object as "can't have the expected value".
					if (target == null)
						return false;
				}

				// is= class test with the subclass walk (XmlVc.IsConditionPasses).
				if (!string.IsNullOrEmpty(condition.IsClass)
					&& !IsClassOrSubclass(target.ClassID, condition.IsClass, condition.ExcludeSubclasses))
				{
					return false;
				}

				// lengthatleast/lengthatmost (XmlVc.LengthConditionsPass: vector size; atomic 0/1).
				if (condition.LengthAtLeast.HasValue || condition.LengthAtMost.HasValue)
				{
					var length = GetPropertyLength(target, condition.Field);
					if (length < (condition.LengthAtLeast ?? 0) || length > (condition.LengthAtMost ?? int.MaxValue))
						return false;
				}

				// boolequals (XmlVc.BoolEqualsConditionPasses via GetBoolValueFromCache: a missing
				// object/field reads as the boolean value false, not as a failed condition).
				if (condition.BoolEquals.HasValue)
				{
					var flid = GetFlid(target, condition.Field);
					var value = flid != 0 && IntBoolPropertyConverter.GetBoolean(_sda, target.Hvo, flid);
					if (value != condition.BoolEquals.Value)
						return false;
				}

				// intequals/intlessthan/intgreaterthan/intmemberof (XmlVc.GetValueFromCache reads 0
				// for a missing field — "rather arbitrary", but legacy-faithful).
				if (condition.IntEquals.HasValue && GetIntValue(target, condition.Field) != condition.IntEquals.Value)
					return false;
				if (condition.IntLessThan.HasValue && GetIntValue(target, condition.Field) >= condition.IntLessThan.Value)
					return false;
				if (condition.IntGreaterThan.HasValue && GetIntValue(target, condition.Field) <= condition.IntGreaterThan.Value)
					return false;
				if (!string.IsNullOrEmpty(condition.IntMemberOf) && !IntMemberOfPasses(condition, target))
					return false;

				// guidequals (XmlVc.GuidEqualsConditionPasses): the atomic reference must point at the
				// object with the literal guid; an empty reference compares as Guid.Empty.
				if (!string.IsNullOrEmpty(condition.GuidEquals))
				{
					var flid = GetFlid(target, condition.Field);
					if (flid == 0 || !Guid.TryParse(condition.GuidEquals, out var expected))
						return false;
					var hvoRef = _sda.get_ObjectProp(target.Hvo, flid);
					var actual = hvoRef == 0
						? Guid.Empty
						: _sda.get_GuidProp(hvoRef, (int)CmObjectFields.kflidCmObject_Guid);
					if (expected != actual)
						return false;
				}

				return true;
			}

			private bool IsClassOrSubclass(int classId, string className, bool excludeSubclasses)
			{
				int expected;
				try
				{
					expected = _mdc.GetClassId(className);
				}
				catch (Exception)
				{
					return false;
				}

				if (classId == expected)
					return true;
				if (excludeSubclasses)
					return false;
				var baseId = _mdc.GetBaseClsId(classId);
				while (baseId != 0)
				{
					if (baseId == expected)
						return true;
					var next = _mdc.GetBaseClsId(baseId);
					if (next == baseId)
						break;
					baseId = next;
				}

				return false;
			}

			private int GetPropertyLength(ICmObject obj, string fieldName)
			{
				var flid = GetFlid(obj, fieldName);
				if (flid == 0)
					return 0;
				switch ((CellarPropertyType)_mdc.GetFieldType(flid))
				{
					case CellarPropertyType.OwningSequence:
					case CellarPropertyType.OwningCollection:
					case CellarPropertyType.ReferenceSequence:
					case CellarPropertyType.ReferenceCollection:
						return _sda.get_VecSize(obj.Hvo, flid);
					case CellarPropertyType.OwningAtomic:
					case CellarPropertyType.ReferenceAtomic:
						return _sda.get_ObjectProp(obj.Hvo, flid) == 0 ? 0 : 1;
					default:
						return 0;
				}
			}

			private int GetIntValue(ICmObject obj, string fieldName)
			{
				var flid = GetFlid(obj, fieldName);
				return flid == 0 ? 0 : _sda.get_IntProp(obj.Hvo, flid);
			}

			private bool IntMemberOfPasses(ViewCondition condition, ICmObject target)
			{
				var value = GetIntValue(target, condition.Field);
				foreach (var piece in condition.IntMemberOf.Split(','))
				{
					if (int.TryParse(piece.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var member)
						&& member == value)
					{
						return true;
					}
				}

				return false;
			}

			// B1: a layout can reach the same object through two placeholders (e.g. a persisted
			// user override duplicating the marker); legacy dedups generated parts by sibling
			// scan (DataTree.CheckCustomFieldsSibling) — here a (hvo, flid) set does the same.
			private readonly HashSet<(int Hvo, int Flid)> _emittedCustomFields = new HashSet<(int, int)>();

			// B1: expand the placeholder the way legacy DataTree.EnsureCustomFields +
			// SliceFactory.MakeAutoCustomSlice do — enumerate the MDC's custom fields whose class
			// is the object's class or a base class (legacy walks FieldDescription.FieldDescriptors,
			// i.e. the MDC field list; sorted by flid here for determinism = creation order per
			// class), synthesize a typed field node per custom field, and dispatch it through the
			// normal walk so text rows ride the same setter registry/fenced session as authored
			// fields. The legacy generated `<part ref="Custom" param=.../>` carries no visibility
			// attribute, so every node is visibility=always: empty custom fields still render,
			// with or without "show hidden fields".
			private void WalkCustomFields(ViewNode placeholder, ICmObject obj, int depth)
			{
				var interestingClasses = new HashSet<int>();
				var clsid = obj.ClassID;
				while (clsid != 0)
				{
					interestingClasses.Add(clsid);
					clsid = _mdc.GetBaseClsId(clsid);
				}

				foreach (var flid in _mdc.GetFieldIds()
					.Where(f => _mdc.IsCustom(f) && interestingClasses.Contains(_mdc.GetOwnClsId(f)))
					.OrderBy(f => f))
				{
					if (!_emittedCustomFields.Add((obj.Hvo, flid)))
						continue;
					Walk(MakeCustomFieldNode(placeholder, flid), obj, depth);
				}
			}

			// One synthesized node per custom field, typed like MakeAutoCustomSlice's editor
			// switch: String/MultiString/MultiUnicode take the text lane (multi-WS per the field's
			// WsSelector, resolved through the same legacy magic-ws pair WalkTextField uses);
			// Integer stays an editable int row, GenDate a read-only formatted row, references
			// read-only joined names (chooser write-back rides 6.3), and OwningAtomic StText
			// read-only paragraphs — all via WalkOtherField's type dispatch. The label is the
			// field's Userlabel (mdc.GetFieldLabel), the menu the autoCustom slice's
			// mnuDataTree-Help (StandardParts.xml CmObject-Detail-Custom).
			private ViewNode MakeCustomFieldNode(ViewNode placeholder, int flid)
			{
				var fieldName = _mdc.GetFieldName(flid);
				string rawEditor;
				string wsSpec = null;
				switch ((CellarPropertyType)_mdc.GetFieldType(flid))
				{
					case CellarPropertyType.String:
						rawEditor = EditorKindMap.StringEditor;
						wsSpec = WritingSystemServices.GetMagicWsNameFromId(_mdc.GetFieldWs(flid));
						break;
					case CellarPropertyType.MultiUnicode:
					case CellarPropertyType.MultiString:
						rawEditor = EditorKindMap.MultiStringEditor;
						wsSpec = WritingSystemServices.GetMagicWsNameFromId(_mdc.GetFieldWs(flid));
						break;
					default:
						// Resolved by CellarPropertyType in WalkOtherField, like autoCustom.
						rawEditor = EditorKindMap.AutoCustomEditor;
						break;
				}

				return new ViewNode($"{placeholder.StableId}/custom:{fieldName}", ViewNodeKind.Field,
					_mdc.GetFieldLabel(flid), null, fieldName, rawEditor, EditorClassification.Known,
					wsSpec, ViewVisibility.Always, ViewExpansion.NotApplicable, placeholder.Indented,
					null, null, menuId: "mnuDataTree-Help");
			}

			// B7: project the row's list-editor jump (the configure gear's direct dispatch target).
			// The node's imported chooserLink metadata wins — the legacy chooser dialog's "Edit
			// the … list" jump links (ReallySimpleListChooser.InitializeExtras,
			// ReallySimpleListChooser.cs:887-926). Only the "goto" kind is implemented: it is the
			// ONLY kind the lexeme-editor layouts use (all 95 shipped chooserLinks are
			// type="goto"); legacy "dialog"/"simple" links need ChooserCommand lanes and are
			// logged + skipped, never half-dispatched. The target guid stays empty like legacy
			// m_guidLink (no lexeme-editor chooserInfo sets flidTextParam); labels localize
			// through the same StringTable lane as XmlUtils.GetLocalizedAttributeValue.
			// When the layout authored NO goto link but the row IS backed by a possibility list,
			// the tool derives from the list the same way the legacy jump lane does (see
			// ResolveListEditorTool); a list with no resolvable editor tool yields no link — and
			// therefore NO gear on that row.
			private IReadOnlyList<RegionChooserLink> BuildChooserLinks(ViewNode node,
				ICmPossibilityList list = null)
			{
				List<RegionChooserLink> links = null;
				foreach (var link in node.ChooserLinks)
				{
					if (!string.Equals(link.Type, "goto", StringComparison.OrdinalIgnoreCase)
						|| string.IsNullOrEmpty(link.Label) || string.IsNullOrEmpty(link.Tool))
					{
						// Review task 10: skipped links must be visible in the product log, not
						// only on a debugger (the legacy "dialog"/"simple" kinds wait on the
						// ChooserCommand lanes).
						SIL.Reporting.Logger.WriteEvent(
							$"FullEntryRegionComposer: chooserLink type '{link.Type}' (tool '{link.Tool}') on {node.StableId} is not the goto kind the lexeme editor uses; skipped.");
						continue;
					}
					(links ?? (links = new List<RegionChooserLink>()))
						.Add(new RegionChooserLink(Localize(link.Label), link.Tool));
				}

				if (links == null && list != null)
				{
					var tool = ResolveListEditorTool(list);
					if (tool != null)
					{
						var listName = list.Name?.BestAnalysisAlternative?.Text ?? string.Empty;
						links = new List<RegionChooserLink>
						{
							new RegionChooserLink(string.Format(CultureInfo.CurrentCulture,
								SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaStrings.EditListFormat,
								listName), tool)
						};
					}
				}

				return links;
			}

			// The three section-header construction sites (group header, summary slice, sequence
			// banner) build the identical collapsible header row; one helper keeps them from drifting.
			private void AddHeader(ViewNode node, ICmObject obj, int depth, string label)
			{
				Fields.Add(new LexicalEditRegionField(StableId(node, obj), label, node.Field, node.WritingSystem,
					RegionFieldKind.Header, node.EditorClassification, node.AutomationId, node.LocalizationKey,
					node.Routing, null, null, null, isEditable: false, indent: depth,
					isCollapsible: true, isInitiallyExpanded: node.Expansion != ViewExpansion.Collapsed,
					menuId: node.MenuId, hotlinksId: node.HotlinksId, objectHvo: obj.Hvo));
			}

			// Empty value in an always-visible row renders blank; an ifdata row hides instead.
			private void AddRowUnlessHiddenWhenEmpty(ViewNode node, ICmObject obj, int depth)
			{
				if (!HideWhenEmpty(node))
					AddReadOnlyRow(node, obj, depth, string.Empty);
			}

			private void WalkGroup(ViewNode node, ICmObject obj, int depth)
			{
				var headerIndex = Fields.Count;
				var label = Localize(node.Label);
				if (!string.IsNullOrEmpty(label))
					AddHeader(node, obj, depth, label);

				var childDepth = string.IsNullOrEmpty(label) ? depth : depth + 1;
				foreach (var child in node.Children)
					Walk(child, obj, childDepth);

				// An ifdata section whose children all hid renders nothing, including its header.
				if (!string.IsNullOrEmpty(label) && Fields.Count == headerIndex + 1 && HideWhenEmpty(node))
				{
					Fields.RemoveAt(headerIndex);
				}
			}

			private void WalkField(ViewNode node, ICmObject obj, int depth)
			{
				// winforms-free-lexeme-editor.md D1: a custom slice resolves plugin registry →
				// companion strip → unsupported row, in that order and never the other way. The
				// registry is consulted FIRST so a migrated class composes as a real in-tree
				// Avalonia editor (a RegionFieldKind.Custom row carrying the plugin's control
				// factory); only unclaimed classes fall through to the companion/unsupported lanes.
				if (!string.IsNullOrEmpty(node.CustomEditorClass))
				{
					var plugin = _plugins?.Resolve(node.CustomEditorClass);
					if (plugin != null)
					{
						AddPluginRow(node, obj, depth, plugin);
						foreach (var pluginChild in node.Children)
							Walk(pluginChild, obj, depth + 1);
						return;
					}
				}

				var fieldCountBeforeDispatch = Fields.Count;
				// Review task 8: the editor-string → category knowledge lives ONCE, in
				// EditorKindMap (the same FwAvalonia home the importer's classification and the
				// mapper's kind projection use); this switch only routes categories. Categories
				// without a dedicated lane here (AtomicReferenceChooser, Grouping, Other) refine
				// by CellarPropertyType in WalkOtherField — that LCModel knowledge stays in the
				// composer.
				switch (EditorKindMap.ClassifyRegionFieldKind(node.RawEditor))
				{
					case RegionEditorCategory.Text:
						WalkTextField(node, obj, depth);
						break;
					case RegionEditorCategory.MorphTypeChooser:
						WalkMorphTypeChooser(node, obj, depth);
						break;
					case RegionEditorCategory.Summary:
						// Summary slices are section header rows in legacy too.
						AddHeader(node, obj, depth, Localize(node.Label) ?? node.Field);
						break;
					case RegionEditorCategory.Literal:
						// Literal text row: the label IS the content.
						AddReadOnlyRow(node, obj, depth, string.Empty);
						break;
					case RegionEditorCategory.Picture:
						WalkPictures(node, obj, depth);
						break;
					case RegionEditorCategory.EmbeddedView:
						// Embedded formatted view: render the object's summary text read-only (the
						// full embedded-view replacement rides the table/IR work).
						AddReadOnlyRow(node, obj, depth, obj.ShortName ?? string.Empty);
						break;
					case RegionEditorCategory.Command:
						// Command slices render their button; execution arrives with the xCore
						// command bridge (shell phase).
						Fields.Add(new LexicalEditRegionField(StableId(node, obj),
							Localize(node.Label) ?? node.Field, node.Field, node.WritingSystem,
							RegionFieldKind.Command, node.EditorClassification, node.AutomationId,
							node.LocalizationKey, node.Routing, null, null, null,
							isEditable: false, indent: depth));
						break;
					case RegionEditorCategory.EnumCombo:
						WalkEnumCombo(node, obj, depth);
						break;
					default:
						WalkOtherField(node, obj, depth);
						break;
				}

				// Companion lane (second in the D1 resolution order, after the plugin registry
				// claim above): a dynamically loaded custom slice (editor="Custom" class=...)
				// keeps its legacy class/assembly identity, keyed by the StableId of the row the
				// dispatch above produced for it — whether that was the explicit unsupported row or
				// a best-effort read-only rendering (e.g. the Messages slice's field="Self" resolves
				// to a reference-atomic flid and renders as read-only text). The host promotes
				// designated classes (the Chorus Messages notes bar) to the WinForms companion strip
				// and removes the row by this StableId.
				if (!string.IsNullOrEmpty(node.CustomEditorClass) && Fields.Count > fieldCountBeforeDispatch)
				{
					var row = Fields[fieldCountBeforeDispatch];
					CustomEditorFields.Add(new ComposedCustomEditorField(row.StableId,
						node.CustomEditorClass, node.CustomEditorAssembly, row.Label, obj.Hvo));
				}

				// Caller children under a slice (e.g. MorphType under the lexeme form) are fields of
				// the same object, one level in.
				foreach (var child in node.Children)
					Walk(child, obj, depth + 1);
			}

			private void WalkTextField(ViewNode node, ICmObject obj, int depth)
			{
				var flid = GetFlid(obj, node.Field);
				if (flid == 0)
				{
					WalkUnsupported(node, obj, depth);
					return;
				}

				var type = (CellarPropertyType)_mdc.GetFieldType(flid);
				switch (type)
				{
					case CellarPropertyType.MultiUnicode:
					case CellarPropertyType.MultiString:
					case CellarPropertyType.String:
					case CellarPropertyType.Unicode:
						break;
					default:
						WalkUnsupported(node, obj, depth);
						return;
				}

				var hvo = obj.Hvo;
				IReadOnlyList<CoreWritingSystemDefinition> systems = ResolveWritingSystems(_cache, node.WritingSystem);
				if ((type == CellarPropertyType.String || type == CellarPropertyType.Unicode)
					&& systems.Count > 0)
				{
					// Single-alternative property: one row. Review task 5 (plain String props):
					// get_StringProp reads the WHOLE string regardless of the layout ws= spec, but
					// the row's display metadata (abbreviation/font/RTL) and write-back previously
					// took the spec's FIRST writing system — asymmetric when the stored string was
					// typed in another ws (legacy StringSlice renders the string's own run
					// properties). Derive the row's ws from the existing string's first run; the
					// layout ws only seeds an EMPTY string.
					var rowWs = systems[0];
					if (type == CellarPropertyType.String)
					{
						var existing = _sda.get_StringProp(hvo, flid);
						if (existing != null && existing.Length > 0)
						{
							var runWs = TsStringUtils.GetWsOfRun(existing, 0);
							if (runWs > 0)
							{
								try
								{
									rowWs = _cache.ServiceLocator.WritingSystemManager.Get(runWs);
								}
								catch (Exception)
								{
									// Unknown run ws: keep the layout writing system.
								}
							}
						}
					}
					systems = new[] { rowWs };
				}

				var anyData = false;
				var rich = false;
				// 11.15: the lexeme form's legacy bold/120% <properties> emphasis.
				var fontSize = node.FontScalePercent > 0 ? 12.0 * node.FontScalePercent / 100.0 : 0;
				// Review task 12: the per-ws value rows build through the shared factory
				// (LexicalEditRegionBuilder uses the same one), this lane only supplies the text.
				var values = RegionValueFactory.BuildMultiWsValues(systems, ws =>
				{
					string text;
					if (type == CellarPropertyType.Unicode)
					{
						text = _sda.get_UnicodeProp(hvo, flid);
					}
					else
					{
						var tss = ReadTextProp(hvo, flid, ws.Handle, type);
						// Review task 4: rich content (multiple runs, or props beyond the ws)
						// makes the whole row read-only below.
						rich |= HasRichContent(tss);
						text = tss?.Text;
					}
					anyData |= !string.IsNullOrEmpty(text);
					return text;
				}, fontSize, node.BoldEmphasis);

				if (!anyData && HideWhenEmpty(node))
					return;

				var stableId = StableId(node, obj);
				// Review task 4: the plain-text setter below replaces the WHOLE alternative via
				// MakeString, which would flatten embedded writing systems, styles, and any other
				// run properties on the first keystroke. Until the rich TsString editor lands
				// (gated on 6.13), a row whose current content is rich composes READ-ONLY so a
				// keystroke cannot destroy it; plain single-run content stays editable.
				var editable = type != CellarPropertyType.Unicode && !rich;
				Fields.Add(new LexicalEditRegionField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, RegionFieldKind.Text, node.EditorClassification, node.AutomationId,
					node.LocalizationKey, node.Routing, values, null, null, editable, depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, hotlinksId: node.HotlinksId,
					objectHvo: obj.Hvo));

				if (!editable)
					return;

				// Edits key on the unique IETF tag (ws.Id): the user-editable Abbreviation can
				// collide across writing systems, which both crashed composition (ToDictionary)
				// and could misroute an edit to the wrong alternative. Unambiguous abbreviations
				// stay accepted as aliases for callers addressing the row's gutter label.
				var wsByKey = new Dictionary<string, int>(StringComparer.Ordinal);
				foreach (var ws in systems)
				{
					if (!string.IsNullOrEmpty(ws.Id))
						wsByKey[ws.Id] = ws.Handle;
				}
				foreach (var ws in systems)
				{
					if (!string.IsNullOrEmpty(ws.Abbreviation) && !wsByKey.ContainsKey(ws.Abbreviation)
						&& systems.Count(other => other.Abbreviation == ws.Abbreviation) == 1)
					{
						wsByKey.Add(ws.Abbreviation, ws.Handle);
					}
				}
				TextSetters[stableId] = (wsKey, value) =>
				{
					if (wsKey == null || !wsByKey.TryGetValue(wsKey, out var wsHandle))
						return false;
					return WriteTextProp(hvo, flid, wsHandle, type, value);
				};
			}

			// Review task 11: the ONE String-vs-multi text read dispatch every TsString-reading
			// site shares (Unicode props return a raw string and stay with get_UnicodeProp at the
			// call sites).
			private ITsString ReadTextProp(int hvo, int flid, int ws, CellarPropertyType type)
			{
				switch (type)
				{
					case CellarPropertyType.MultiUnicode:
					case CellarPropertyType.MultiString:
						return _sda.get_MultiStringAlt(hvo, flid, ws);
					case CellarPropertyType.String:
						return _sda.get_StringProp(hvo, flid);
					default:
						return null;
				}
			}

			// Review task 11: the matching write dispatch (plain-text MakeString round-trip; the
			// rich-content guard in WalkTextField keeps it away from rich strings).
			private bool WriteTextProp(int hvo, int flid, int ws, CellarPropertyType type, string value)
			{
				var tss = TsStringUtils.MakeString(value ?? string.Empty, ws);
				switch (type)
				{
					case CellarPropertyType.String:
						_sda.SetString(hvo, flid, tss);
						return true;
					case CellarPropertyType.MultiUnicode:
					case CellarPropertyType.MultiString:
						_sda.SetMultiStringAlt(hvo, flid, ws, tss);
						return true;
					default:
						return false;
				}
			}

			// Review task 4: "rich" = more than one run, or single-run properties beyond the
			// writing system itself — exactly the content a plain-text MakeString round-trip
			// would silently destroy.
			private static bool HasRichContent(ITsString tss)
			{
				if (tss == null || tss.Length == 0)
					return false;
				if (tss.RunCount > 1)
					return true;
				var props = tss.get_Properties(0);
				if (props.StrPropCount > 0)
					return true;
				for (var i = 0; i < props.IntPropCount; i++)
				{
					props.GetIntProp(i, out var tpt, out _);
					if (tpt != (int)FwTextPropType.ktptWs)
						return true;
				}
				return false;
			}

			private void WalkMorphTypeChooser(ViewNode node, ICmObject obj, int depth)
			{
				if (!(obj is IMoForm form))
				{
					WalkUnsupported(node, obj, depth);
					return;
				}

				var morphTypes = _cache.LangProject.LexDbOA?.MorphTypesOA;
				if (_morphTypeOptions == null)
				{
					_morphTypeOptions = new List<RegionChoiceOption>();
					if (morphTypes != null)
					{
						foreach (var possibility in morphTypes.ReallyReallyAllPossibilities.OfType<IMoMorphType>()
							.OrderBy(mt => mt.Name.BestAnalysisAlternative?.Text, StringComparer.Ordinal))
						{
							_morphTypeOptions.Add(new RegionChoiceOption(possibility.Guid.ToString(),
								possibility.Name.BestAnalysisAlternative?.Text ?? possibility.Guid.ToString()));
						}
					}
				}
				var options = _morphTypeOptions;

				var stableId = StableId(node, obj);
				Fields.Add(new LexicalEditRegionField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, RegionFieldKind.Chooser, node.EditorClassification, node.AutomationId,
					node.LocalizationKey, node.Routing, null, options, form.MorphTypeRA?.Guid.ToString(),
					isEditable: true, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, objectHvo: obj.Hvo,
					chooserLinks: BuildChooserLinks(node, morphTypes)));

				OptionSetters[stableId] = optionKey =>
				{
					if (!Guid.TryParse(optionKey, out var guid))
						return false;
					var repository = _cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
					if (!repository.TryGetObject(guid, out var morphType))
						return false;
					// Legacy MorphTypeAtomicLauncher gates stem<->affix swaps behind a data-loss
					// prompt AND a class conversion (MoStemAllomorph <-> MoAffixAllomorph). Assigning
					// blindly would create a model-invalid combination (e.g. a stem allomorph with an
					// affix morph type), so a boundary-crossing assignment is rejected until the
					// class-conversion lane lands (review round 2). The GUID -> kind classification
					// is the seam's single table (review consolidation: this file's 19-entry mirror
					// dictionary is gone; MorphTypeGuidConsolidationTests pins the seam's table to
					// the MoMorphTypeTags constants).
					if (MorphTypeSwapLogic.TryClassify(guid, out var toKind)
						&& (form is IMoStemAllomorph) != MorphTypeSwapLogic.IsStemType(toKind))
					{
						return false;
					}
					form.MorphTypeRA = morphType;
					return true;
				};
			}

			// 6.3: an atomic possibility reference takes the chooser lane (legacy
			// PossibilityAtomicReferenceSlice): options from the field's own list
			// (ReferenceTargetOwner), write-back through the fenced session.
			private void AddAtomicPossibilityChooser(ViewNode node, ICmObject obj, int depth, int flid,
				ICmPossibilityList list, int targetHvo)
			{
				// Review task 6: the legacy atomic possibility launcher lets the user CLEAR the
				// reference (PossibilityAtomicReferenceLauncher.OnLeave -> AddItem(null) when the
				// box is emptied; only a layout-authored nullLabel="" forbids it, which no
				// lexeme-editor part does), so the chooser leads with an explicit empty choice —
				// labeled with the SAME localized "<Empty>" the WinForms launchers use
				// (DetailControlsStrings.ksNullLabel). The morph-type chooser deliberately offers
				// no empty option (MorphTypeAtomicLauncher.AllowEmptyItem == false).
				var options = new List<RegionChoiceOption>
				{
					new RegionChoiceOption(string.Empty,
						SIL.FieldWorks.Common.Framework.DetailControls.DetailControlsResourceAccess.NullItemLabel)
				};
				// B7 remainder: chooserInfo FlatList specs are not yet imported onto the node;
				// until they are, the chooser renders the list's own hierarchy.
				options.AddRange(BuildPossibilityOptions(list, flat: false));
				var selected = targetHvo == 0
					? null
					: _cache.ServiceLocator.ObjectRepository.GetObject(targetHvo).Guid.ToString();
				var stableId = StableId(node, obj);
				Fields.Add(new LexicalEditRegionField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, RegionFieldKind.Chooser, node.EditorClassification, node.AutomationId,
					node.LocalizationKey, node.Routing, null, options, selected, isEditable: true, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, hotlinksId: node.HotlinksId,
					objectHvo: obj.Hvo, chooserLinks: BuildChooserLinks(node, list)));

				var hvo = obj.Hvo;
				OptionSetters[stableId] = key =>
				{
					// The empty option clears the reference — legacy AddItem(null), i.e.
					// SetObjProp(hvo, flid, 0) — inside the same fenced session (task 6).
					if (string.IsNullOrEmpty(key))
					{
						_sda.SetObjProp(hvo, flid, 0);
						return true;
					}
					var possibility = ResolvePossibilityInList(list, key);
					if (possibility == null)
						return false;
					_sda.SetObjProp(hvo, flid, possibility.Hvo);
					return true;
				};
			}

			// 6.3/B8: an editable possibility-vector row — current items in vector order plus the
			// whole list as hierarchical options; add/remove stage through sda.Replace on the flid
			// (the legacy VectorReferenceView update), one undo step per settled session.
			private void AddReferenceVector(ViewNode node, ICmObject obj, int depth, int flid,
				ICmPossibilityList list, int count)
			{
				var items = new List<RegionChoiceOption>();
				for (var i = 0; i < count; i++)
				{
					var itemHvo = _sda.get_VecItem(obj.Hvo, flid, i);
					var item = _cache.ServiceLocator.ObjectRepository.GetObject(itemHvo);
					items.Add(new RegionChoiceOption(item.Guid.ToString(), ResolveShortName(itemHvo)));
				}

				var options = BuildPossibilityOptions(list, flat: false); // B7 remainder, see above
				var stableId = StableId(node, obj);
				Fields.Add(new LexicalEditRegionField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, RegionFieldKind.ReferenceVector, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, options, null,
					isEditable: true, indent: depth, menuId: node.MenuId, contextMenuId: node.ContextMenuId,
					hotlinksId: node.HotlinksId, objectHvo: obj.Hvo, items: items,
					chooserLinks: BuildChooserLinks(node, list)));

				var hvo = obj.Hvo;
				ReferenceAddSetters[stableId] = key =>
				{
					var possibility = ResolvePossibilityInList(list, key);
					if (possibility == null)
						return false;
					var size = _sda.get_VecSize(hvo, flid);
					for (var i = 0; i < size; i++)
					{
						if (_sda.get_VecItem(hvo, flid, i) == possibility.Hvo)
							return false; // duplicates rejected, like the legacy chooser
					}
					_sda.Replace(hvo, flid, size, size, new[] { possibility.Hvo }, 1);
					return true;
				};
				ReferenceRemoveSetters[stableId] = key =>
				{
					var possibility = ResolvePossibilityInList(list, key);
					if (possibility == null)
						return false;
					var size = _sda.get_VecSize(hvo, flid);
					for (var i = 0; i < size; i++)
					{
						if (_sda.get_VecItem(hvo, flid, i) != possibility.Hvo)
							continue;
						_sda.Replace(hvo, flid, i, i + 1, new int[0], 0);
						return true;
					}
					return false;
				};
			}

			// Resolves an option key to a possibility belonging to THIS field's list — garbage,
			// unknown guids, and possibilities from OTHER lists all reject (no cross-list writes).
			private ICmPossibility ResolvePossibilityInList(ICmPossibilityList list, string key)
			{
				if (!Guid.TryParse(key, out var guid))
					return null;
				if (!_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().TryGetObject(guid, out var possibility))
					return null;
				return possibility.OwningList == list ? possibility : null;
			}

			// ---- winforms-free-lexeme-editor.md D3: the entry-reference vector lane ----

			internal const string EntrySequenceSliceClassName =
				"SIL.FieldWorks.XWorks.LexEd.EntrySequenceReferenceSlice";

			private const int MaxEntrySearchResults = 50;

			// The lane's gate: a NON-virtual reference vector whose destination signature is
			// LexEntry/LexSense — or CmObject when the layout identity is the legacy
			// EntrySequenceReferenceSlice (ComponentLexemes/PrimaryLexemes sign ILexEntryOrLexSense
			// as plain CmObject). Virtual back-ref vectors (ComplexFormEntries, Subentries,
			// VisibleComplexFormBackRefs, VariantFormEntries) stay read-only this wave: their writes
			// land on the OTHER entry's LexEntryRef, not on this flid (the legacy launcher's
			// AddNewObjectsToProperty overrides) — recorded as the lane's deferred note.
			private bool IsEntryOrSenseReferenceVector(ViewNode node, int flid)
			{
				if (_mdc.get_IsVirtual(flid))
					return false;
				int dstClass;
				try
				{
					dstClass = _mdc.GetDstClsId(flid);
				}
				catch (Exception)
				{
					return false;
				}
				if (dstClass == LexEntryTags.kClassId || dstClass == LexSenseTags.kClassId)
					return true;
				return dstClass == CmObjectTags.kClassId
					&& string.Equals(node.CustomEditorClass, EntrySequenceSliceClassName, StringComparison.Ordinal);
			}

			// D3: the editable entry/sense-reference vector — current refs as headword items, remove
			// in-pane, add via type-ahead headword-prefix search over the entry repository (never the
			// whole lexicon as options). Writes ride sda.Replace on the flid inside the fenced
			// session, like the possibility lane, plus the legacy ComponentLexemes coupling below.
			private void AddEntryReferenceVector(ViewNode node, ICmObject obj, int depth, int flid, int count)
			{
				var items = new List<RegionChoiceOption>();
				for (var i = 0; i < count; i++)
				{
					var itemHvo = _sda.get_VecItem(obj.Hvo, flid, i);
					var item = _cache.ServiceLocator.ObjectRepository.GetObject(itemHvo);
					items.Add(new RegionChoiceOption(item.Guid.ToString(), ResolveEntryOrSenseName(item)));
				}

				var stableId = StableId(node, obj);
				var hvo = obj.Hvo;
				// "Self" for the circular-reference guard: the entry whose pane this is — the row's
				// object when it IS an entry, else its owning entry (e.g. obj is the LexEntryRef).
				var owningEntry = obj as ILexEntry ?? obj.OwnerOfClass<ILexEntry>();

				Fields.Add(new LexicalEditRegionField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, RegionFieldKind.ReferenceVector, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, null, null,
					isEditable: true, indent: depth, menuId: node.MenuId, contextMenuId: node.ContextMenuId,
					hotlinksId: node.HotlinksId, objectHvo: obj.Hvo, items: items,
					searchOptions: query => SearchLexicon(query, hvo, flid, owningEntry),
					chooserLinks: BuildChooserLinks(node)));

				ReferenceAddSetters[stableId] = key =>
				{
					var target = ResolveEntryOrSense(key);
					if (target == null)
						return false;
					// Direct self-reference rejects up front (the legacy chooser filters the entry
					// itself out of the candidates); LCModel's own validation backstops the deeper
					// circular cases below.
					var targetEntry = target as ILexEntry ?? ((ILexSense)target).Entry;
					if (owningEntry != null && targetEntry == owningEntry)
						return false;
					var size = _sda.get_VecSize(hvo, flid);
					for (var i = 0; i < size; i++)
					{
						if (_sda.get_VecItem(hvo, flid, i) == target.Hvo)
							return false; // duplicates rejected, like the legacy launcher's AddItem
					}
					try
					{
						_sda.Replace(hvo, flid, size, size, new[] { target.Hvo }, 1);
					}
					catch (ArgumentException)
					{
						// LCModel's circular-component validation (the case legacy surfaces as
						// ReportLexEntryCircularReference); reject without staging.
						return false;
					}
					ApplyComponentLexemesAddCoupling(obj, flid, target);
					return true;
				};
				ReferenceRemoveSetters[stableId] = key =>
				{
					var target = ResolveEntryOrSense(key);
					if (target == null)
						return false;
					var size = _sda.get_VecSize(hvo, flid);
					for (var i = 0; i < size; i++)
					{
						if (_sda.get_VecItem(hvo, flid, i) != target.Hvo)
							continue;
						_sda.Replace(hvo, flid, i, i + 1, new int[0], 0);
						return true;
					}
					return false;
				};
			}

			// Legacy EntrySequenceReferenceLauncher.AddNewObjectsToProperty's ComponentLexemes
			// coupling, which LCModel does NOT apply as a side effect (verified by test): a component
			// added when PrimaryLexemes is empty becomes the primary lexeme, and (unless the ref is
			// typed as a derivative) the complex form shows under the new component
			// (ShowComplexFormsIn) — LT-12285 guards the duplicate. Removal needs no twin here:
			// LCModel's RemoveObjectSideEffects already clears PrimaryLexemes/ShowComplexFormsIn when
			// a component leaves (verified by test).
			private void ApplyComponentLexemesAddCoupling(ICmObject obj, int flid, ICmObject added)
			{
				if (flid != LexEntryRefTags.kflidComponentLexemes || !(obj is ILexEntryRef ler))
					return;
				if (ler.PrimaryLexemesRS.Count == 0)
					ler.PrimaryLexemesRS.Add(added);
				ILexEntryType derivation;
				_cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>()
					.TryGetObject(LexEntryTypeTags.kguidLexTypDerivation, out derivation);
				if ((derivation == null || !ler.ComplexEntryTypesRS.Contains(derivation))
					&& !ler.ShowComplexFormsInRS.Contains(added))
				{
					ler.ShowComplexFormsInRS.Add(added); // don't add it twice — LT-12285
				}
			}

			// Resolves a search/option key to an entry or sense — exactly the targets
			// EntrySequenceReferenceSlice references (ILexEntryOrLexSense); everything else rejects.
			private ICmObject ResolveEntryOrSense(string key)
			{
				if (!Guid.TryParse(key, out var guid))
					return null;
				if (!_cache.ServiceLocator.ObjectRepository.TryGetObject(guid, out var target))
					return null;
				return target is ILexEntry || target is ILexSense ? target : null;
			}

			// Item display: the headword (HeadWord for entries — homograph number and all — and the
			// owner-outline headword+sense-number for senses), the same display the legacy slice's
			// deParams displayProperty="HeadWord" yields; ShortName is the fallback.
			private string ResolveEntryOrSenseName(ICmObject target)
			{
				switch (target)
				{
					case ILexEntry entry:
						return entry.HeadWord?.Text ?? entry.ShortName ?? string.Empty;
					case ILexSense sense:
						return sense.OwnerOutlineNameForWs(_cache.DefaultVernWs)?.Text
							?? sense.ShortName ?? string.Empty;
					default:
						return target?.ShortName ?? string.Empty;
				}
			}

			// D3's type-ahead lane: case-insensitive headword-prefix search over the entry
			// repository (headword/citation form/lexeme form), excluding the pane's own entry and
			// the vector's current members (read live, so a staged add drops out of the next
			// search), capped at MaxEntrySearchResults, ordered by headword.
			private IReadOnlyList<RegionChoiceOption> SearchLexicon(string query, int hvo, int flid,
				ILexEntry owningEntry)
			{
				if (string.IsNullOrWhiteSpace(query))
					return Array.Empty<RegionChoiceOption>();
				query = query.Trim();

				var present = new HashSet<int>();
				var size = _sda.get_VecSize(hvo, flid);
				for (var i = 0; i < size; i++)
					present.Add(_sda.get_VecItem(hvo, flid, i));

				return _cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances()
					.Where(entry => entry != owningEntry && !present.Contains(entry.Hvo)
						&& MatchesHeadwordPrefix(entry, query))
					.Select(entry => new RegionChoiceOption(entry.Guid.ToString(), ResolveEntryOrSenseName(entry)))
					.OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
					.Take(MaxEntrySearchResults)
					.ToList();
			}

			private static bool MatchesHeadwordPrefix(ILexEntry entry, string query)
			{
				return StartsWithIgnoreCase(entry.HeadWord?.Text, query)
					|| StartsWithIgnoreCase(entry.CitationForm?.BestVernacularAlternative?.Text, query)
					|| StartsWithIgnoreCase(entry.LexemeFormOA?.Form?.BestVernacularAlternative?.Text, query);
			}

			private static bool StartsWithIgnoreCase(string text, string query)
				=> !string.IsNullOrEmpty(text)
					&& text.StartsWith(query, StringComparison.OrdinalIgnoreCase);

			// Review task 2: legacy enumComboBox is a CLOSED combo over the layout's stringList
			// labels (SliceFactory.cs case "enumcombobox" -> EnumComboSlice), never free-form
			// input. Falling through to the Integer lane composed it as an unrestricted int
			// editor whose SetInt could persist invalid enum values. Until the importer carries
			// the layout's stringList ids onto the node (it currently drops them), the row
			// composes READ-ONLY showing the raw stored value; the eventual fix is an option
			// chooser fed by that stringList.
			private void WalkEnumCombo(ViewNode node, ICmObject obj, int depth)
			{
				var flid = GetFlid(obj, node.Field);
				if (flid == 0)
				{
					WalkUnsupported(node, obj, depth);
					return;
				}

				int current;
				switch ((CellarPropertyType)_mdc.GetFieldType(flid))
				{
					case CellarPropertyType.Integer:
						current = _sda.get_IntProp(obj.Hvo, flid);
						break;
					case CellarPropertyType.Boolean:
						// Legacy EnumComboSlice serves boolean-backed enums too (e.g. the
						// Allomorph Status combo over IsAbstract), via IntBoolPropertyConverter.
						current = IntBoolPropertyConverter.GetBoolean(_sda, obj.Hvo, flid) ? 1 : 0;
						break;
					default:
						WalkUnsupported(node, obj, depth);
						return;
				}

				if (current == 0 && HideWhenEmpty(node))
					return;
				AddReadOnlyRow(node, obj, depth, current.ToString(CultureInfo.InvariantCulture));
			}

			// Viewing parity (11.x): every field type the legacy slices display has a rendering here:
			// booleans as checkboxes (editable), integers editable, dates/gendates formatted,
			// structured text as paragraph text, references as value rows; explicit unsupported rows
			// for the rest. Empty fields show under "show hidden fields" exactly like legacy.
			private void WalkOtherField(ViewNode node, ICmObject obj, int depth)
			{
				var flid = GetFlid(obj, node.Field);
				if (flid != 0)
				{
					var type = (CellarPropertyType)_mdc.GetFieldType(flid);
					switch (type)
					{
						case CellarPropertyType.ReferenceAtomic:
						{
							var targetHvo = _sda.get_ObjectProp(obj.Hvo, flid);

							// 6.3: an atomic ref whose target owner is a possibility list takes the
							// chooser lane (legacy PossibilityAtomicReferenceSlice), like morph type.
							if (obj.ReferenceTargetOwner(flid) is ICmPossibilityList list)
							{
								if (targetHvo == 0 && HideWhenEmpty(node))
									return;
								AddAtomicPossibilityChooser(node, obj, depth, flid, list, targetHvo);
								return;
							}

							if (targetHvo == 0)
							{
								AddRowUnlessHiddenWhenEmpty(node, obj, depth);
								return;
							}

							AddReadOnlyRow(node, obj, depth, ResolveShortName(targetHvo));
							return;
						}
						case CellarPropertyType.OwningAtomic:
						{
							var targetHvo = _sda.get_ObjectProp(obj.Hvo, flid);
							if (targetHvo == 0)
							{
								AddRowUnlessHiddenWhenEmpty(node, obj, depth);
								return;
							}

							// Structured text renders its paragraph contents (StTextSlice's view).
							if (_cache.ServiceLocator.ObjectRepository.GetObject(targetHvo) is IStText stText)
							{
								var paragraphs = stText.ParagraphsOS.OfType<IStTxtPara>()
									.Select(par => par.Contents?.Text ?? string.Empty);
								var text = string.Join(Environment.NewLine, paragraphs);
								if (string.IsNullOrWhiteSpace(text) && HideWhenEmpty(node))
									return;
								AddReadOnlyRow(node, obj, depth, text);
								return;
							}

							AddReadOnlyRow(node, obj, depth, ResolveShortName(targetHvo));
							return;
						}
						case CellarPropertyType.ReferenceSequence:
						case CellarPropertyType.ReferenceCollection:
						{
							var count = _sda.get_VecSize(obj.Hvo, flid);

							// 6.3/B8: a vector whose targets live in a possibility list becomes an
							// editable ReferenceVector row (the legacy possibility-vector slice with
							// its trailing type-ahead add slot) — even when empty, so an always-visible
							// field still offers the add affordance.
							if (obj.ReferenceTargetOwner(flid) is ICmPossibilityList list)
							{
								if (count == 0 && HideWhenEmpty(node))
									return;
								AddReferenceVector(node, obj, depth, flid, list, count);
								return;
							}

							// winforms-free-lexeme-editor.md D3: a vector whose targets are
							// entries/senses (the EntrySequenceReferenceSlice fields —
							// ComponentLexemes, PrimaryLexemes, ... on LexEntryRef) composes as an
							// editable ReferenceVector whose ADD is a type-ahead lexicon search
							// (lexicons search, possibility lists enumerate).
							if (IsEntryOrSenseReferenceVector(node, flid))
							{
								if (count == 0 && HideWhenEmpty(node))
									return;
								AddEntryReferenceVector(node, obj, depth, flid, count);
								return;
							}

							if (count == 0)
							{
								AddRowUnlessHiddenWhenEmpty(node, obj, depth);
								return;
							}

							var names = new List<string>();
							for (var i = 0; i < count; i++)
								names.Add(ResolveShortName(_sda.get_VecItem(obj.Hvo, flid, i)));
							AddReadOnlyRow(node, obj, depth, string.Join("; ", names));
							return;
						}
						case CellarPropertyType.Boolean:
						{
							var stableId = StableId(node, obj);
							var isChecked = _sda.get_BooleanProp(obj.Hvo, flid);
							Fields.Add(new LexicalEditRegionField(stableId, Localize(node.Label) ?? node.Field,
								node.Field, node.WritingSystem, RegionFieldKind.Boolean,
								node.EditorClassification, node.AutomationId, node.LocalizationKey, node.Routing,
								null, null, isChecked ? "true" : "false", isEditable: true, indent: depth));
							var hvo = obj.Hvo;
							OptionSetters[stableId] = key =>
							{
								if (!bool.TryParse(key, out var value))
									return false;
								_sda.SetBoolean(hvo, flid, value);
								return true;
							};
							return;
						}
						case CellarPropertyType.Integer:
						{
							var stableId = StableId(node, obj);
							var current = _sda.get_IntProp(obj.Hvo, flid);
							if (current == 0 && HideWhenEmpty(node))
								return;
							Fields.Add(new LexicalEditRegionField(stableId, Localize(node.Label) ?? node.Field,
								node.Field, node.WritingSystem, RegionFieldKind.Text,
								node.EditorClassification, node.AutomationId, node.LocalizationKey, node.Routing,
								new List<RegionWsValue> { new RegionWsValue("", current.ToString()) },
								null, null, isEditable: true, indent: depth));
							var hvo = obj.Hvo;
							TextSetters[stableId] = (ws, value) =>
							{
								// Review task 7 (clearing an int box): legacy IntegerSlice treats a
								// non-numeric box — INCLUDING empty — as invalid on focus loss: it
								// warns and restores the stored value, never committing empty as 0
								// (BasicTypeSlices.cs, IntegerSlice.m_tb_LostFocus's
								// Convert.ToInt32 FormatException path). Mirror that deliberately:
								// empty/whitespace stages NOTHING (false), so the control restores
								// the last committed value (its lastStaged advances only on
								// success), and a clear-then-retype only ever stages the parseable
								// intermediate states.
								if (!int.TryParse(value, NumberStyles.Integer,
									CultureInfo.InvariantCulture, out var parsed))
								{
									return false;
								}
								_sda.SetInt(hvo, flid, parsed);
								return true;
							};
							return;
						}
						case CellarPropertyType.Time:
						{
							var silTime = _sda.get_TimeProp(obj.Hvo, flid);
							if (silTime == 0 && HideWhenEmpty(node))
								return;
							// Legacy parity: DateSlice renders the full pattern ("f", CurrentUICulture)
							// — the day name and all — with no UTC conversion.
							var display = silTime == 0
								? string.Empty
								: SilTime.ConvertFromSilTime(silTime).ToString("f", CultureInfo.CurrentUICulture);
							AddReadOnlyRow(node, obj, depth, display);
							return;
						}
						case CellarPropertyType.GenDate:
						{
							string display;
							try
							{
								var genDate = ((ISilDataAccessManaged)_sda).get_GenDateProp(obj.Hvo, flid);
								display = genDate.IsEmpty ? string.Empty : genDate.ToLongString();
							}
							catch (Exception)
							{
								display = string.Empty;
							}

							if (string.IsNullOrEmpty(display) && HideWhenEmpty(node))
								return;
							AddReadOnlyRow(node, obj, depth, display);
							return;
						}
					}
				}

				if (!HideWhenEmpty(node))
					WalkUnsupported(node, obj, depth);
			}

			private void AddReadOnlyRow(ViewNode node, ICmObject obj, int depth, string display)
			{
				Fields.Add(new LexicalEditRegionField(StableId(node, obj), Localize(node.Label) ?? node.Field,
					node.Field, node.WritingSystem, RegionFieldKind.Text, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing,
					new List<RegionWsValue> { new RegionWsValue("", display ?? string.Empty) }, null, null,
					isEditable: false, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, objectHvo: obj.Hvo));
			}

			// winforms-free-lexeme-editor.md D1: the plugin-claimed row — RegionFieldKind.Custom
			// with the normal label/indent/menu metadata, carrying a factory that closes over
			// (object, node, deferred edit context, cache). The factory runs in the view at render
			// time, so composing stays side-effect free and the edit context exists by then.
			private void AddPluginRow(ViewNode node, ICmObject obj, int depth, IRegionEditorPlugin plugin)
			{
				// Review task 13: ONE plugin contract — the build context bundles everything a
				// plugin can need (object, node, deferred edit-context accessor, cache, optional
				// host services); the former IServiceAwareRegionEditorPlugin type test is gone.
				var context = new RegionEditorBuildContext(obj, node, _editContextAccessor, _cache, _services);
				Func<Avalonia.Controls.Control> factory = () => plugin.BuildControl(context);
				Fields.Add(new LexicalEditRegionField(StableId(node, obj), Localize(node.Label) ?? node.Field,
					node.Field, node.WritingSystem, RegionFieldKind.Custom, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, null, null,
					isEditable: true, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, hotlinksId: node.HotlinksId,
					objectHvo: obj.Hvo,
					controlFactory: factory));
			}

			private void WalkUnsupported(ViewNode node, ICmObject obj, int depth)
			{
				Fields.Add(new LexicalEditRegionField(StableId(node, obj), Localize(node.Label) ?? node.Field,
					node.Field, node.WritingSystem, RegionFieldKind.Unsupported, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, null, null,
					isEditable: false, indent: depth));
			}

			// Viewing parity (11.6): picture fields render the actual image (caption + file path row);
			// the view loads the bitmap when the file exists.
			private void WalkPictures(ViewNode node, ICmObject obj, int depth)
			{
				var flid = GetFlid(obj, node.Field);
				if (flid == 0)
				{
					WalkUnsupported(node, obj, depth);
					return;
				}

				var count = _sda.get_VecSize(obj.Hvo, flid);
				if (count == 0)
				{
					if (!HideWhenEmpty(node))
						AddGhostPrompt(node, obj, depth);
					return;
				}

				for (var i = 0; i < count; i++)
				{
					if (!(_cache.ServiceLocator.ObjectRepository.GetObject(_sda.get_VecItem(obj.Hvo, flid, i))
						is ICmPicture picture))
					{
						continue;
					}

					var caption = picture.Caption?.BestVernacularAnalysisAlternative?.Text
						?? Localize(node.Label) ?? node.Field;
					string path = null;
					try
					{
						path = picture.PictureFileRA?.AbsoluteInternalPath;
					}
					catch (Exception)
					{
					}

					Fields.Add(new LexicalEditRegionField($"{StableId(node, obj)}/pic{i}", caption,
						node.Field, null, RegionFieldKind.Image, node.EditorClassification,
						node.AutomationId, node.LocalizationKey, node.Routing,
						new List<RegionWsValue> { new RegionWsValue("", path ?? string.Empty) },
						null, null, isEditable: false, indent: depth));
				}
			}

			// Viewing parity (11.14) + 14.1: empty always-visible object/sequence fields show the
			// legacy ghost add-prompt as a WATERMARK on an editable row — clicking in clears the
			// prompt, and typing creates the missing object inside the fenced session (the legacy
			// ghost-slice create-on-edit lane), routing the text into the layout's ghost field
			// (ghost=/ghostWs=, e.g. the new allomorph's Form).
			private void AddGhostPrompt(ViewNode node, ICmObject obj, int depth)
			{
				var label = Localize(node.GhostLabel) ?? Localize(node.Label) ?? node.Field;
				if (string.IsNullOrEmpty(label))
					return;
				var prompt = string.Format(
					SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaStrings.GhostAddPromptFormat, label);

				var stableId = $"{StableId(node, obj)}/ghost";
				var ghost = ResolveGhostCreation(node, obj);
				Fields.Add(new LexicalEditRegionField(stableId, label, node.Field,
					node.WritingSystem, RegionFieldKind.Text, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing,
					new List<RegionWsValue> { new RegionWsValue(ghost?.WsAbbrev ?? "", string.Empty, wsTag: ghost?.WsTag) },
					null, null, isEditable: ghost != null, indent: depth,
					menuId: node.MenuId, hotlinksId: node.HotlinksId, objectHvo: obj.Hvo,
					ghostPrompt: prompt));

				if (ghost != null)
					TextSetters[stableId] = ghost.Setter;
			}

			private sealed class GhostCreation
			{
				public string WsAbbrev;
				public string WsTag; // unique IETF tag (ws.Id), the edit-routing identity
				public Func<string, string, bool> Setter;
			}

			// The create-on-edit half of the ghost lane: resolve the owning field's destination class
			// (the layout's ghostClass when the model class is abstract; MoStemAllomorph for MoForm,
			// matching legacy CreateAllomorph; Gloss-on-LexSense when no ghost field is authored) and
			// build a setter that creates the object inside the open session — cancel rolls the
			// creation back together with the text.
			private GhostCreation ResolveGhostCreation(ViewNode node, ICmObject obj)
			{
				var flid = GetFlid(obj, node.Field);
				if (flid == 0)
					return null;

				int insertOrd;
				switch ((CellarPropertyType)_mdc.GetFieldType(flid))
				{
					case CellarPropertyType.OwningAtomic: insertOrd = -2; break;
					case CellarPropertyType.OwningCollection: insertOrd = -1; break;
					case CellarPropertyType.OwningSequence: insertOrd = 0; break;
					default: return null; // reference ghosts take a chooser, not a text creator
				}

				int dstClass;
				try
				{
					dstClass = _mdc.GetDstClsId(flid);
					if (!string.IsNullOrEmpty(node.GhostClass))
						dstClass = _mdc.GetClassId(node.GhostClass);
					else if (_mdc.GetAbstract(dstClass))
					{
						if (dstClass != MoFormTags.kClassId)
							return null;
						dstClass = MoStemAllomorphTags.kClassId; // legacy CreateAllomorph default
					}
				}
				catch (Exception)
				{
					return null;
				}

				var ghostFieldName = node.GhostField
					?? (dstClass == LexSenseTags.kClassId ? "Gloss" : null);
				var ghostFlid = 0;
				if (!string.IsNullOrEmpty(ghostFieldName))
				{
					try { ghostFlid = _mdc.GetFieldId2(dstClass, ghostFieldName, true); }
					catch (Exception) { ghostFlid = 0; }
				}

				// Review task 3: with no resolvable ghost field AND no init method, typing could
				// only MakeNewObject a bare object while the typed text silently vanished (nothing
				// receives the string). No shipped layout authors such a ghost; render the prompt
				// NON-editable (null) instead of destroying input on the first keystroke.
				if (ghostFlid == 0 && string.IsNullOrEmpty(node.GhostInitMethod))
					return null;

				var ws = ResolveGhostWs(node.GhostWs);
				if (ws == null)
					return null;

				var hvoOwner = obj.Hvo;
				var ghostInitMethod = node.GhostInitMethod;
				var createdHvo = 0;
				return new GhostCreation
				{
					WsAbbrev = ws.Abbreviation,
					WsTag = ws.Id,
					Setter = (wsKey, value) =>
					{
						// The closure outlives the edit session: a Cancel rolls MakeNewObject back,
						// so a later edit through the same still-visible view must not write to the
						// deleted hvo — verify it still exists and re-create when it does not
						// (review round 2).
						if (createdHvo != 0
							&& !_cache.ServiceLocator.ObjectRepository.TryGetObject(createdHvo, out _))
						{
							createdHvo = 0;
						}
						var created = false;
						if (createdHvo == 0)
						{
							createdHvo = _sda.MakeNewObject(dstClass, hvoOwner, flid, insertOrd);
							created = true;
						}
						if (ghostFlid != 0)
						{
							// Task 11: the shared 3-way text write dispatch.
							WriteTextProp(createdHvo, ghostFlid, ws.Handle,
								(CellarPropertyType)_mdc.GetFieldType(ghostFlid), value);
						}
						// B2: invoke the layout's ghostInitMethod by reflection on the newly created
						// object, after the typed text lands — exactly GhostStringSliceView.
						// MakeRealObject's order (GhostStringSlice.cs:305-328); e.g. SetMorphTypeToRoot
						// on a new lexeme-form allomorph, SetTypeToFreeTrans on a new translation.
						if (created && !string.IsNullOrEmpty(ghostInitMethod))
						{
							var createdObj = _cache.ServiceLocator.ObjectRepository.GetObject(createdHvo);
							createdObj.GetType().GetMethod(ghostInitMethod)?.Invoke(createdObj, null);
						}
						return true;
					}
				};
			}

			private SIL.LCModel.Core.WritingSystems.CoreWritingSystemDefinition ResolveGhostWs(string ghostWs)
			{
				var systems = _cache.ServiceLocator.WritingSystems;
				switch (ghostWs)
				{
					case "vernacular":
						return systems.DefaultVernacularWritingSystem;
					case "pronunciation":
						return systems.DefaultPronunciationWritingSystem
							?? systems.DefaultVernacularWritingSystem;
					default:
						return systems.DefaultAnalysisWritingSystem;
				}
			}

			private void WalkObjectAtom(ViewNode node, ICmObject obj, int depth)
			{
				var flid = GetFlid(obj, node.Field);
				if (flid == 0)
					return;
				var targetHvo = _sda.get_ObjectProp(obj.Hvo, flid);
				if (targetHvo == 0)
				{
					// Ghost add-prompt for an empty always-visible object field (11.14).
					if (!HideWhenEmpty(node))
						AddGhostPrompt(node, obj, depth);
					return;
				}

				var target = _cache.ServiceLocator.ObjectRepository.GetObject(targetHvo);
				DescendInto(node, target, depth);
			}

			private void WalkSequence(ViewNode node, ICmObject obj, int depth)
			{
				var flid = GetFlid(obj, node.Field);
				if (flid == 0)
					return;
				var count = _sda.get_VecSize(obj.Hvo, flid);
				if (count == 0)
				{
					if (!HideWhenEmpty(node))
						AddGhostPrompt(node, obj, depth);
					return;
				}

				var expanded = node.Expansion != ViewExpansion.Collapsed;
				var isSenses = node.Field == "Senses";
				var sectionLabel = Localize(node.Label) ?? node.Field;
				// Nested sense sequences (Senses on a sense) don't repeat the section banner; the
				// numbered items carry it.
				if (!(isSenses && obj is ILexSense))
					AddHeader(node, obj, depth, sectionLabel);

				for (var i = 0; i < count; i++)
				{
					var item = _cache.ServiceLocator.ObjectRepository.GetObject(_sda.get_VecItem(obj.Hvo, flid, i));
					string itemLabel;
					if (isSenses && item is ILexSense sense)
					{
						// Legacy sense numbering: 1, 2, ... and 1.1 for subsenses, with the sense's
						// summary text (ShortName = gloss) in the header line. Finding B: the number
						// is the domain's own LexSenseOutline (the dictionary/bulk-edit outline), so
						// the entry pane cannot diverge from the other surfaces.
						itemLabel = ($"{sense.LexSenseOutline.Text}  {item.ShortName}").TrimEnd();
					}
					else
					{
						itemLabel = $"{sectionLabel} {i + 1}";
					}

					// 15.3: the item's own layout carries the slice menu (e.g. the sense's
					// HeavySummary part ref binds mnuDataTree-Sense in LexSense.fwlayout) — the
					// sequence node itself usually has none.
					var itemBinding = ResolveItemMenuBinding(node, item);
					Fields.Add(new LexicalEditRegionField($"{StableId(node, obj)}/item{i}",
						itemLabel, node.Field, null, RegionFieldKind.Header,
						EditorClassification.GroupingNone, null, null, SurfaceRouting.Inherit,
						null, null, null, isEditable: false, indent: depth + 1,
						isCollapsible: true, isInitiallyExpanded: expanded,
						menuId: itemBinding.MenuId ?? node.MenuId,
						hotlinksId: itemBinding.HotlinksId ?? node.HotlinksId,
						objectHvo: item.Hvo));
					DescendInto(node, item, depth + 1);
				}
			}

			// 15.3: first root-level menu/hotlinks binding of the item's compiled layout (compile
			// results are memoized per (class, layout), and the binding itself is memoized per
			// compose state — finding A).
			private (string MenuId, string HotlinksId) ResolveItemMenuBinding(ViewNode node, ICmObject item)
			{
				var layoutName = string.IsNullOrEmpty(node.TargetLayout) ? "Normal" : node.TargetLayout;
				if (_itemMenuBindings.TryGetValue((item.ClassID, layoutName), out var cached))
					return cached;

				var compiled = CompileForObject(_cache, item, layoutName);
				string menu = null, hotlinks = null;
				if (compiled != null)
				{
					foreach (var root in compiled.Roots)
					{
						menu = menu ?? (string.IsNullOrEmpty(root.MenuId) ? null : root.MenuId);
						hotlinks = hotlinks ?? (string.IsNullOrEmpty(root.HotlinksId) ? null : root.HotlinksId);
						if (menu != null && hotlinks != null)
							break;
					}
				}

				_itemMenuBindings[(item.ClassID, layoutName)] = (menu, hotlinks);
				return (menu, hotlinks);
			}

			private void DescendInto(ViewNode node, ICmObject target, int depth)
			{
				var layoutName = string.IsNullOrEmpty(node.TargetLayout) ? "Normal" : node.TargetLayout;
				if (!_visited.Add((target.Hvo, layoutName)))
					return;

				var compiled = CompileForObject(_cache, target, layoutName);
				if (compiled != null && compiled.Roots.Count > 0)
				{
					foreach (var child in compiled.Roots)
						Walk(child, target, depth + 1);
				}
				else
				{
					// No layout for the target: fall back to the caller-injected children, if any.
					foreach (var child in node.Children)
						Walk(child, target, depth + 1);
				}

				_visited.Remove((target.Hvo, layoutName));
			}

			private int GetFlid(ICmObject obj, string fieldName)
			{
				if (string.IsNullOrEmpty(fieldName))
					return 0;
				try
				{
					return _mdc.GetFieldId2(obj.ClassID, fieldName, true);
				}
				catch (Exception)
				{
					return 0;
				}
			}

			private string ResolveShortName(int hvo)
			{
				return _cache.ServiceLocator.ObjectRepository.TryGetObject(hvo, out var target)
					? target.ShortName ?? string.Empty
					: string.Empty;
			}

			private static string StableId(ViewNode node, ICmObject obj) => $"{node.StableId}@{obj.Hvo}";

			private static string Localize(string label)
				=> string.IsNullOrEmpty(label) ? label : StringTable.Table.LocalizeAttributeValue(label);
		}

		// Layout ws= spec resolution (the composer's read AND write writing-system lists): the
		// legacy pair — WritingSystemServices.GetMagicWsIdFromName then GetWritingSystemList —
		// exactly as SliceFactory's multistring lane resolves it, so list membership and ordering
		// ("analysis vernacular" vs "vernacular analysis") match legacy slices. Pronunciation
		// specs ride the project's pronunciation list (kwsPronunciations; GetWritingSystemList has
		// no kwsPronunciation branch), initialized on demand the same way legacy
		// DefaultPronunciationWritingSystem initializes it. Empty/unknown specs take
		// GetWritingSystemList's own analysis default — the legacy default for unmarked fields.
		internal static IReadOnlyList<CoreWritingSystemDefinition> ResolveWritingSystems(LcmCache cache, string spec)
		{
			var magicId = WritingSystemServices.GetMagicWsIdFromName(spec);
			switch (magicId)
			{
				case WritingSystemServices.kwsPronunciation:
				case WritingSystemServices.kwsFirstPronunciation:
				case WritingSystemServices.kwsPronunciations:
					WritingSystemServices.InitializePronunciationWritingSystems(cache);
					magicId = WritingSystemServices.kwsPronunciations;
					break;
			}

			return WritingSystemServices.GetWritingSystemList(cache, magicId, forceIncludeEnglish: false);
		}

		/// <summary>
		/// Compiles the layout for an object's class, walking base classes the way legacy DataTree
		/// does (e.g. MoStemAllomorph → MoForm) for both layout lookup and part resolution.
		/// Memoized per (starting class, layout) for the lifetime of the loaded sources (finding A).
		/// </summary>
		internal static ViewDefinitionModel CompileForObject(LcmCache cache, ICmObject obj, string layoutName)
		{
			var sources = GetSources();
			if (sources == null)
				return null;

			return sources.CompiledModels.GetOrAdd((obj.ClassID, layoutName),
				key => CompileForClass(cache, key.ClassId, key.LayoutName, sources));
		}

		private static ViewDefinitionModel CompileForClass(LcmCache cache, int classId, string layoutName,
			CompilerSources sources)
		{
			Interlocked.Increment(ref s_snapshotCompileCount);

			var mdc = (IFwMetaDataCacheManaged)cache.DomainDataByFlid.MetaDataCache;
			var baseClassMap = new Dictionary<string, string>(StringComparer.Ordinal);
			var clsid = classId;
			XElement layout = null;
			string className = null;
			while (true)
			{
				className = mdc.GetClassName(clsid);
				if (sources.LayoutIndex.TryGetValue((className, "detail", layoutName), out layout))
					break;
				if (clsid == 0)
					return null;
				var baseId = mdc.GetBaseClsId(clsid);
				if (baseId == clsid)
					return null;
				baseClassMap[className] = mdc.GetClassName(baseId);
				clsid = baseId;
			}

			// Part resolution may still need to climb from the layout's class upward.
			var chain = clsid;
			while (chain != 0)
			{
				var baseId = mdc.GetBaseClsId(chain);
				if (baseId == chain || baseId == 0)
					break;
				baseClassMap[mdc.GetClassName(chain)] = mdc.GetClassName(baseId);
				chain = baseId;
			}

			var snapshot = new ViewDefinitionSourceSnapshot(className, "detail", layout.ToString(),
				sources.PartsXml, baseClassMap);
			return Compiler.Compile(snapshot);
		}

		private static CompilerSources LoadSources()
		{
			try
			{
				// Finding D: the parts merge and layout glob live in the ONE shared loader
				// (LayoutSourceLoader) that LexicalEditFirstSlice also uses.
				var partsDirectory = FwDirectoryFinder.GetCodeSubDirectory(@"Language Explorer\Configuration\Parts");
				var partsXml = LayoutSourceLoader.LoadMergedPartsXml(partsDirectory);
				if (partsXml == null)
				{
					// Review task 10: never a silent permanent failure — log, fall back to the
					// 3-field first slice for THIS compose, and retry next time (GetSources).
					SIL.Reporting.Logger.WriteEvent(
						"FullEntryRegionComposer: no merged parts XML under '" + partsDirectory
						+ "'; falling back to the first slice (will retry on the next compose).");
					return null;
				}

				var layoutFiles = LayoutSourceLoader.LoadLayoutFiles(partsDirectory);
				return new CompilerSources
				{
					PartsXml = partsXml,
					LayoutIndex = LayoutSourceLoader.IndexLayouts(layoutFiles)
				};
			}
			catch (Exception e)
			{
				// Review task 10: never a silent permanent failure — log, fall back to the
				// 3-field first slice for THIS compose, and retry next time (GetSources).
				SIL.Reporting.Logger.WriteError(
					"FullEntryRegionComposer: failed to load layout sources; "
					+ "falling back to the first slice for this compose.", e);
				return null;
			}
		}
	}

	/// <summary>
	/// The composed region's edit context: staging keyed by composed stable id (unique per object
	/// occurrence, so each sense's Gloss binds its own sense), writes applied through the registered
	/// LCModel setters inside the fenced session owned by <see cref="RegionEditContextBase"/>
	/// (finding C — one shared session lifecycle + required-lexeme validation).
	/// </summary>
	public sealed class ComposedRegionEditContext : RegionEditContextBase
	{
		private readonly IReadOnlyDictionary<string, Func<string, string, bool>> _textSetters;
		private readonly IReadOnlyDictionary<string, Func<string, bool>> _optionSetters;
		private readonly IReadOnlyDictionary<string, Func<string, bool>> _referenceAddSetters;
		private readonly IReadOnlyDictionary<string, Func<string, bool>> _referenceRemoveSetters;

		public ComposedRegionEditContext(
			LcmCache cache,
			ILexEntry entry,
			IReadOnlyDictionary<string, Func<string, string, bool>> textSetters,
			IReadOnlyDictionary<string, Func<string, bool>> optionSetters,
			IReadOnlyDictionary<string, Func<string, bool>> referenceAddSetters = null,
			IReadOnlyDictionary<string, Func<string, bool>> referenceRemoveSetters = null)
			: base(cache, entry)
		{
			_textSetters = textSetters;
			_optionSetters = optionSetters;
			_referenceAddSetters = referenceAddSetters ?? new Dictionary<string, Func<string, bool>>();
			_referenceRemoveSetters = referenceRemoveSetters ?? new Dictionary<string, Func<string, bool>>();
		}

		public override bool TrySetText(LexicalEditRegionField field, string ws, string value)
		{
			if (field == null || !_textSetters.TryGetValue(field.StableId, out var setter))
				return false;
			return Stage(() => setter(ws, value));
		}

		public override bool TrySetOption(LexicalEditRegionField field, string optionKey)
		{
			if (field == null || !_optionSetters.TryGetValue(field.StableId, out var setter))
				return false;
			return Stage(() => setter(optionKey));
		}

		public override bool TryAddReferenceItem(LexicalEditRegionField field, string optionKey)
		{
			if (field == null || !_referenceAddSetters.TryGetValue(field.StableId, out var setter))
				return false;
			return Stage(() => setter(optionKey));
		}

		public override bool TryRemoveReferenceItem(LexicalEditRegionField field, string optionKey)
		{
			if (field == null || !_referenceRemoveSetters.TryGetValue(field.StableId, out var setter))
				return false;
			return Stage(() => setter(optionKey));
		}

		// Setters must run inside the fenced session, but a REJECTED edit must not leave an empty
		// fence open (it would hold the UOW write lock and gate refreshes). If this call opened the
		// session and staged nothing, close it again.
		private bool Stage(Func<bool> setter)
		{
			var wasOpen = IsOpen;
			EnsureOpen();
			var staged = setter();
			if (!staged && !wasOpen)
				Cancel();
			return staged;
		}
	}
}
