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
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
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
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>A composed full-entry detail view: the renderable model plus its LCModel-bound edit context.</summary>
	public sealed class ComposedDetail
	{
		public ComposedDetail(DetailModel model, IDetailEditContext editContext)
		{
			Model = model;
			EditContext = editContext;
		}

		public DetailModel Model { get; }

		public IDetailEditContext EditContext { get; }
	}

	/// <summary>
	/// Resolves the per-project sparse override patch for a compiled (class, layout), or null when the
	/// project did not customize that layout. The host wires this to the
	/// <c>ViewDefinitionOverrideStore</c> in the project ConfigurationSettings folder; tests supply an
	/// in-memory resolver. Kept a delegate so the composer needs no reference to the file-backed store.
	/// </summary>
	public delegate ViewDefinitionOverride ViewDefinitionOverrideResolver(string className, string layoutName);

	/// <summary>
	/// Composes the COMPLETE Lexical Edit view for an entry (sections 6/7): walks the compiled
	/// `LexEntry/Normal` typed definition the same way legacy DataTree walks layouts -- expanding
	/// object/sequence nodes across objects by compiling each target's own layout (with the legacy
	/// base-class walk), emitting section headers, indentation, per-writing-system editable text
	/// fields, the morph-type chooser, read-only reference rows, and `ifdata` hiding -- every
	/// field
	/// bound to LCModel through metadata (class/field -> flid) and editable through the fenced
	/// session. Labels localize through the same <see cref="StringTable"/> lookup legacy slices use.
	/// Unsupported constructs render an explicit unsupported row (visibility=always) or are skipped
	/// (ifdata), never silently mis-rendered; compile diagnostics ride the detail model.
	/// </summary>
	public static class DetailComposer
	{
		// The visited (hvo, layout) guard is the real recursion stop. This depth cap is only a
		// backstop for malformed layouts, so it must still allow the deepest shipped lexeme-edit
		// detail paths (e.g. sense -> extended note -> example -> nested custom fields).
		private const int MaxDepth = 12;
		private static readonly ViewDefinitionCompiler Compiler = new ViewDefinitionCompiler();

		// Deliberately NOT a Lazy -- a failed load must not be cached as null for
		// the process lifetime (one transient IO hiccup would silently demote every
		// future compose to the 3-field first slice). A successful load is immutable and cached
		// forever; a failure logs (see LoadSources) and is retried on the next compose.
		private static readonly object SourcesSync = new object();
		private static CompilerSources s_sources;
		private static IReadOnlyList<string> s_searchPathOverride;

		/// <summary>
		/// The composer's source term, made explicit: the layout/parts search path it
		/// compiles from. By default that is the shipped path
		/// (<see cref="PartsInventory.SearchPath"/> over the code directory). Inside the
		/// returned scope it is <paramref name="searchPath"/> instead, and the memoized
		/// sources and compiled models are dropped on entry and on dispose, so a test can
		/// compose against a synthetic layout and assert the diagnostics the compile
		/// reports. Scopes do not nest and are not safe against concurrent composes; use
		/// one from one test at a time.
		/// </summary>
		internal static IDisposable OverrideSearchPath(IReadOnlyList<string> searchPath)
		{
			if (searchPath == null) throw new ArgumentNullException(nameof(searchPath));
			lock (SourcesSync)
			{
				s_searchPathOverride = searchPath;
				s_sources = null;
			}
			return new SearchPathScope();
		}

		private sealed class SearchPathScope : IDisposable
		{
			public void Dispose()
			{
				lock (SourcesSync)
				{
					s_searchPathOverride = null;
					s_sources = null;
				}
			}
		}

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

		// Observable memoization: counts the expensive snapshot builds (layout
		// lookup + layout.ToString() + fingerprint + compile). A repeat compose must not grow it.
		private static int s_snapshotCompileCount;

		internal static int SnapshotCompileCount => s_snapshotCompileCount;

		/// <summary>
		/// The loaded sources, immutable for the process lifetime: the layout lookup is
		/// indexed once and compiled definitions are memoized per (starting class, layout),
		/// so repeat composes and the per-item menu peeks never rebuild or re-fingerprint
		/// the ~300KB parts snapshot. Class ids and the class hierarchy are fixed LCModel
		/// metadata, so the memo is safe across caches.
		/// </summary>
		private sealed class CompilerSources
		{
			public string PartsXml;
			// The layout index keeps ALL (class,type,name) variants so a choiceGuid can pick
			// the right one (legacy distinguishes e.g. 11 RnGenericRec/Normal layouts only by
			// choiceGuid).
			public Dictionary<(string ClassName, string Type, string Name), List<XElement>> LayoutIndex;
			// Memoized per (starting class, layout, choiceGuid) -- choiceGuid is part of the
			// identity so two
			// record Types on the same class compile to two distinct models (never a cache collision).
			public readonly ConcurrentDictionary<(int ClassId, string LayoutName, string ChoiceGuid), ViewDefinitionModel> CompiledModels
				= new ConcurrentDictionary<(int, string, string), ViewDefinitionModel>();
		}

		public static ComposedDetail Compose(ILexEntry entry, LcmCache cache, bool showHiddenFields = false,
			SlicePluginRegistry plugins = null,
			ViewDefinitionOverrideResolver overrides = null,
			ISet<string> showAllWritingSystemsFields = null)
			=> Compose((ICmObject)entry, cache, "Normal", showHiddenFields, plugins, overrides,
				showAllWritingSystemsFields: showAllWritingSystemsFields);

		/// <summary>
		/// Compose the structured detail view for ANY record root + starting layout -- the
		/// lexicon's
		/// LexEntry/Normal, a Notebook RnGenericRec, a Lists CmPossibility, a Grammar PartOfSpeech, etc. The
		/// compile/walk engine is already class-general (<see cref="CompileForObject"/> keys on the object's
		/// ClassID and compiles each descended object's own layout); this overload parameterizes the root
		/// object and the starting layout instead of hardcoding LexEntry/"Normal", so wiring a new tool onto
		/// the Avalonia side needs only its registration + (when its layout uses one) a layoutChoiceField.
		/// </summary>
		/// <param name="showAllWritingSystemsFields">Template StableIds of parts under a
		/// transient "Show all right now" reveal: every row of those parts composes with its
		/// full writing-system set, ignoring per-field visibility restrictions.</param>
		public static ComposedDetail Compose(ICmObject obj, LcmCache cache, string layoutName = "Normal",
			bool showHiddenFields = false, SlicePluginRegistry plugins = null,
			ViewDefinitionOverrideResolver overrides = null,
			string layoutChoiceField = null,
			ISet<string> showAllWritingSystemsFields = null)
		{
			if (obj == null) throw new ArgumentNullException(nameof(obj));
			if (cache == null) throw new ArgumentNullException(nameof(cache));
			if (string.IsNullOrEmpty(layoutName)) layoutName = "Normal";

			// When the tool's layout is type-selected (e.g. Notebook RnGenericRec uses
			// layoutChoiceField="Type"), resolve the record's chosen possibility GUID so CompileForObject
			// picks the matching layout variant instead of the document-first one.
			var choiceGuid = ResolveLayoutChoiceGuid(cache, obj, layoutChoiceField);

			var root = CompileForObject(cache, obj, layoutName, choiceGuid, overrides);
			if (root == null)
				return null;

			// Plugin rows close over the detail view's own edit context, which
			// only exists once the walk gathers every setter -- a deferred accessor
			// bridges the gap (plugin factories run at render time, not compose).
			IDetailEditContext composedContext = null;
			var state = new ComposeState(cache, showHiddenFields,
				plugins ?? SlicePluginRegistry.Default, () => composedContext, overrides,
				showAllWritingSystemsFields);
			state.EnterModel(root);
			foreach (var node in root.Roots)
				state.Walk(node, obj, 0);
			state.ExitModel();

			var context = new ComposedDetailEditContext(cache, obj, state.Handlers);
			composedContext = context;
			var model = new DetailModel(obj.ClassName, layoutName, state.Fields, root.Diagnostics);
			return new ComposedDetail(model, context);
		}

		/// <summary>
		/// Walks a possibility list's tree in document order (parent before children) into
		/// chooser options, hierarchy carried as <see cref="DetailChoiceOption.Depth"/> --
		/// exactly
		/// the indented tree the legacy chooser shows. <paramref name="flat"/> (a chooserInfo
		/// "FlatList" guicontrol spec, e.g. PeopleFlatList) keeps the order but suppresses the
		/// hierarchy, like the legacy flat chooser. The implementation lives in
		/// the shared <see cref="DetailValueFactory"/> so this composer and
		/// <see cref="LexiconEditErrorFallback"/> cannot drift; this wrapper keeps the composer's
		/// established internal API (and its tests).
		/// </summary>
		internal static IReadOnlyList<DetailChoiceOption> CreatePossibilityOptions(
			ICmPossibilityList list, bool flat)
			=> DetailValueFactory.CreatePossibilityOptions(list, flat);

		// No runtime window config for a list editor tool; the legacy table is mirrored here.
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
		/// Resolves the lists-area tool that edits <paramref name="list"/> -- the configure
		/// gear's
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

			public readonly List<DetailField> Fields = new List<DetailField>();
			// One edit handler per composed field, keyed by StableId. A field's text/rich-text/option/
			// reference/paragraph write delegates live together on its handler (null where the field's kind
			// does not support a gesture), replacing the former nine parallel setter dictionaries that had
			// to be kept in sync by matching stable id across each one.
			public readonly Dictionary<string, FieldEditHandler> Handlers
				= new Dictionary<string, FieldEditHandler>(StringComparer.Ordinal);

			// The handler for a field, created on first use so a walker registers whichever gestures the
			// field's kind supports without pre-seeding every stable id.
			public FieldEditHandler HandlerFor(string stableId)
			{
				if (!Handlers.TryGetValue(stableId, out var handler))
				{
					handler = new FieldEditHandler();
					Handlers[stableId] = handler;
				}
				return handler;
			}

			private readonly bool _showHidden;
			// The per-project override resolver, threaded into every CompileForObject
			// so a descended object's layout gets its own patch applied; plus the (class, layout) of the
			// model currently being walked, captured onto each emitted field so the host's per-field
			// menu-button commands target the right override file. A stack so the entry
			// context restores after a nested object's walk returns.
			private readonly ViewDefinitionOverrideResolver _overrides;
			private readonly Stack<(string ClassName, string LayoutName)> _modelContext
				= new Stack<(string, string)>();
			// The plugin registry consulted FIRST for every
			// custom slice, plus the deferred accessor for the edit context plugin factories
			// receive (resolved when the factory runs, after Compose has built the context).
			private readonly SlicePluginRegistry _plugins;
			private readonly Func<IDetailEditContext> _editContextAccessor;
			// Parts under the host's transient "Show all right now" reveal (template StableIds);
			// every row of those parts composes with its full writing-system set.
			private readonly ISet<string> _showAllWsFields;
			// Per-compose memos -- the morph-type option list is identical for every
			// IMoForm, and an item layout's menu/hotlinks binding is identical per (class, layout).
			private List<DetailChoiceOption> _morphTypeOptions;
			// The project's character-type style names, computed once
			// per compose from Cache.LangProject.StylesOC and stamped onto every editable text row's
			// DetailField.AvailableNamedStyles (the host seam the per-WS style picker reads).
			// Sorted by name for a stable picker order; empty when no stylesheet/styles are reachable.
			private IReadOnlyList<string> _characterStyleNames;
			// The project's PARAGRAPH-type style names, computed once per compose
			// from Cache.LangProject.StylesOC and stamped onto every editable StText row's
			// DetailField.AvailableParagraphStyles (the host seam the per-paragraph style picker
			// reads). Sorted by name for a stable picker order; empty when no stylesheet/styles are reachable.
			private IReadOnlyList<string> _paragraphStyleNames;
			// The project's writing systems (analysis + vernacular),
			// computed once per compose from Cache and stamped onto every editable text row's
			// DetailField.AvailableWritingSystems (the host seam the per-WS retag picker reads).
			// Empty when no writing systems are reachable; the WS picker affordance is then suppressed.
			private IReadOnlyList<DetailWritingSystemOption> _writingSystemOptions;
			// A map from ws tag to that ws's default font (+ RTL), computed
			// once per compose from the project's analysis + vernacular writing systems and stamped onto
			// every editable text / StText row so the owned editors can draw the per-run font display.
			private IReadOnlyDictionary<string, DetailRunFont> _writingSystemFonts;
			// CHOICE-UNSAFE KEY: this cache key omits choiceGuid while the menu
			// binding is derived from the compiled layout's root, which can differ per choice variant. It is
			// correct ONLY because descent currently compiles every embedded object with choiceGuid=null
			// (CompileForObjectWithOverrides), so within one compose there is no choice variance to collide.
			// If descent is ever changed to thread choiceGuid through, change this key to
			// (ClassId, LayoutName, choiceGuid) in the SAME change, or this becomes a wrong-menu bug.
			private readonly Dictionary<(int ClassId, string LayoutName), (string MenuId, string HotlinksId)> _itemMenuBindings
				= new Dictionary<(int, string), (string, string)>();

			public ComposeState(LcmCache cache, bool showHiddenFields,
				SlicePluginRegistry plugins, Func<IDetailEditContext> editContextAccessor,
				ViewDefinitionOverrideResolver overrides = null,
				ISet<string> showAllWritingSystemsFields = null)
			{
				_cache = cache;
				_showHidden = showHiddenFields;
				_plugins = plugins;
				_editContextAccessor = editContextAccessor;
				_overrides = overrides;
				_showAllWsFields = showAllWritingSystemsFields;
				_sda = cache.DomainDataByFlid;
				_mdc = (IFwMetaDataCacheManaged)cache.DomainDataByFlid.MetaDataCache;
			}

			// Track which compiled model the walk is currently inside so each emitted
			// field is stamped with its (class, layout). EnterModel/ExitModel bracket each compiled model's
			// roots (the entry's own, and each descended object's), AddField stamps from the top of stack.
			public void EnterModel(ViewDefinitionModel model)
				=> _modelContext.Push((model?.ClassName, model?.LayoutName));

			public void ExitModel()
			{
				if (_modelContext.Count > 0)
					_modelContext.Pop();
			}

			private void AddField(DetailField field)
			{
				if (_modelContext.Count > 0)
				{
					var ctx = _modelContext.Peek();
					field.ClassName = ctx.ClassName;
					field.LayoutName = ctx.LayoutName;
				}

				Fields.Add(field);
			}

			// The project's character-type style names, sourced from
			// Cache.LangProject.StylesOC (the LcmStyleSheet's backing store) and filtered to
			// StyleType.kstCharacter -- the same set the legacy character-style combo offers.
			// Computed once
			// per compose and memoized; any failure reaching the styles (a bare/partial cache in a test)
			// yields an empty list, which simply suppresses the picker affordance. This is the host seam:
			// the composer is the LCModel-aware edge that supplies the names the FwAvalonia layer renders.
			private IReadOnlyList<string> CharacterStyleNames()
			{
				if (_characterStyleNames != null)
					return _characterStyleNames;
				try
				{
					var styles = _cache.LangProject?.StylesOC;
					_characterStyleNames = styles == null
						? (IReadOnlyList<string>)Array.Empty<string>()
						: styles.Where(s => s.Type == StyleType.kstCharacter && !string.IsNullOrEmpty(s.Name))
							.Select(s => s.Name)
							.OrderBy(name => name, StringComparer.Ordinal)
							.ToList();
				}
				catch (Exception e)
				{
					Logger.WriteError("Reading the project's character styles failed; the style picker will be empty.", e);
					_characterStyleNames = Array.Empty<string>();
				}
				return _characterStyleNames;
			}

			// The project's paragraph-type style names, sourced from
			// Cache.LangProject.StylesOC filtered to StyleType.kstParagraph -- the set the legacy
			// StText
			// paragraph-style combo offers. Computed once per compose and memoized; any failure reaching the
			// styles (a bare/partial cache in a test) yields an empty list, which suppresses the per-paragraph
			// style picker. The host seam: the composer is the LCModel-aware edge that supplies the names.
			private IReadOnlyList<string> ParagraphStyleNames()
			{
				if (_paragraphStyleNames != null)
					return _paragraphStyleNames;
				try
				{
					var styles = _cache.LangProject?.StylesOC;
					_paragraphStyleNames = styles == null
						? (IReadOnlyList<string>)Array.Empty<string>()
						: styles.Where(s => s.Type == StyleType.kstParagraph && !string.IsNullOrEmpty(s.Name))
							.Select(s => s.Name)
							.OrderBy(name => name, StringComparer.Ordinal)
							.ToList();
				}
				catch (Exception e)
				{
					Logger.WriteError("Reading the project's paragraph styles failed; the style picker will be empty.", e);
					_paragraphStyleNames = Array.Empty<string>();
				}
				return _paragraphStyleNames;
			}

			// The project's writing systems (analysis + vernacular,
			// in that legacy order, deduped by handle), each as a (stable IETF tag, display name) option the
			// FwAvalonia per-WS retag picker offers. The display name is the ws's full display label
			// (ws.DisplayLabel), falling back to its abbreviation, then its tag. Computed once per compose and
			// memoized; any failure reaching the writing systems (a bare/partial cache in a test) yields an
			// empty list, which simply suppresses the picker affordance. This is the host seam: the composer
			// is the LCModel-aware edge that supplies the writing systems the FwAvalonia layer renders.
			private IReadOnlyList<DetailWritingSystemOption> WritingSystemOptions()
			{
				if (_writingSystemOptions != null)
					return _writingSystemOptions;
				try
				{
					var seen = new HashSet<int>();
					var options = new List<DetailWritingSystemOption>();
					void AddAll(IEnumerable<CoreWritingSystemDefinition> systems)
					{
						if (systems == null)
							return;
						foreach (var ws in systems)
						{
							if (ws == null || string.IsNullOrEmpty(ws.Id) || !seen.Add(ws.Handle))
								continue;
							var displayName = ws.DisplayLabel;
							if (string.IsNullOrEmpty(displayName))
								displayName = string.IsNullOrEmpty(ws.Abbreviation) ? ws.Id : ws.Abbreviation;
							options.Add(new DetailWritingSystemOption(ws.Id, displayName));
						}
					}

					var langProject = _cache.LangProject;
					AddAll(langProject?.CurrentAnalysisWritingSystems);
					AddAll(langProject?.CurrentVernacularWritingSystems);
					_writingSystemOptions = options;
				}
				catch (Exception e)
				{
					Logger.WriteError("Reading the project's writing systems failed; per-writing-system options will be empty.", e);
					_writingSystemOptions = Array.Empty<DetailWritingSystemOption>();
				}
				return _writingSystemOptions;
			}

			// Each project writing system's default font + RTL, keyed by
			// its IETF tag, so the owned editors can render a multi-run value with TRUE per-run fonts.
			// Computed once per compose and memoized; any failure yields an empty map.
			private IReadOnlyDictionary<string, DetailRunFont> WritingSystemFonts()
			{
				if (_writingSystemFonts != null)
					return _writingSystemFonts;
				try
				{
					var map = new Dictionary<string, DetailRunFont>(StringComparer.Ordinal);
					void AddAll(IEnumerable<CoreWritingSystemDefinition> systems)
					{
						if (systems == null)
							return;
						foreach (var ws in systems)
						{
							if (ws == null || string.IsNullOrEmpty(ws.Id) || map.ContainsKey(ws.Id))
								continue;
							map[ws.Id] = new DetailRunFont(ws.DefaultFontName, ws.RightToLeftScript);
						}
					}

					var langProject = _cache.LangProject;
					AddAll(langProject?.CurrentAnalysisWritingSystems);
					AddAll(langProject?.CurrentVernacularWritingSystems);
					_writingSystemFonts = map;
				}
				catch (Exception e)
				{
					Logger.WriteError("Reading the project's writing-system fonts failed; rows will render in the default font.", e);
					_writingSystemFonts = new Dictionary<string, DetailRunFont>();
				}
				return _writingSystemFonts;
			}

			// Every CompileForObject in the walk goes through here so the per-project
			// override patch for the descended object's own (class, layout) is applied to its model too.
			private ViewDefinitionModel CompileForObjectWithOverrides(ICmObject obj, string layoutName)
				=> CompileForObject(_cache, obj, layoutName, _overrides);

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
						// Runtime expansion of `customFields="here"` from
						// live MDC metadata.
						WalkCustomFields(node, obj, depth);
						break;
					case ViewNodeKind.Conditional:
						// Legacy <if>/<ifnot> -- content composes only when the per-object
						// condition
						// passes (DataTree.ProcessSubpartNode cases "if"/"ifnot").
						WalkConditional(node, obj, depth);
						break;
					case ViewNodeKind.ChoiceGroup:
						// Legacy <choice> -- first passing <where> (or the <otherwise>) only.
						WalkChoiceGroup(node, obj, depth);
						break;
				}
			}

			// The <if>/<ifnot> wrapper -- evaluate and pass through; failing branches drop
			// entirely.
			private void WalkConditional(ViewNode node, ICmObject obj, int depth)
			{
				if (node.Condition != null && !ConditionPasses(node.Condition, obj))
					return;
				foreach (var child in node.Children)
					Walk(child, obj, depth);
			}

			// The <choice> semantics from DataTree.ProcessSubpartNode case "choice": expand only the
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

			// Evaluate the imported condition per object, mirroring XmlVc.ConditionPasses exactly
			// as the legacy detail path invokes it (DataTree.cs:2639-2696 over XmlVc.cs:3276-3290):
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

				// boolequals (XmlVc.BoolEqualsConditionPasses via GetBoolValueFromCache: a
				// missing
				// object/field reads as the boolean value false, not as a failed condition).
				if (condition.BoolEquals.HasValue)
				{
					var flid = GetFlid(target, condition.Field);
					var value = flid != 0 && IntBoolPropertyConverter.GetBoolean(_sda, target.Hvo, flid);
					if (value != condition.BoolEquals.Value)
						return false;
				}

				// intequals/intlessthan/intgreaterthan/intmemberof (XmlVc.GetValueFromCache reads 0
				// for a missing field -- "rather arbitrary", but legacy-faithful).
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

			// A layout can reach the same object through two placeholders (e.g. a
			// persisted user override); legacy dedupes by sibling scan
			// (DataTree.CheckCustomFieldsSibling) -- a (hvo, flid) set does the same.
			private readonly HashSet<(int Hvo, int Flid)> _emittedCustomFields = new HashSet<(int, int)>();

			// Expand the placeholder the way legacy DataTree.EnsureCustomFields +
			// SliceFactory.MakeAutoCustomSlice do -- enumerate the MDC's custom fields whose
			// class
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
			// switch: String/MultiString/MultiUnicode take the text path (multi-WS per the field's
			// WsSelector, resolved through the same legacy magic-ws pair WalkTextField uses);
			// Integer stays an editable int row, GenDate a read-only formatted row, references
			// read-only joined names (chooser write-back rides the reference-vector path), and OwningAtomic StText
			// read-only paragraphs -- all via WalkOtherField's type dispatch. The label is the
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
						var fieldWs = _mdc.GetFieldWs(flid);
						// A multi-alternative field with a singular (or unset) selector (LIFT
						// import mints these) shows one fixed alternative, so it composes a
						// plain string row without the Writing Systems menu.
						rawEditor = fieldWs == WritingSystemServices.kwsAnal
							|| fieldWs == WritingSystemServices.kwsVern
							|| fieldWs == 0
							? EditorKindMap.StringEditor
							: EditorKindMap.MultiStringEditor;
						wsSpec = WritingSystemServices.GetMagicWsNameFromId(fieldWs);
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

			// The node's chooserLink wins; else the row derives its tool like the legacy path.
			private IReadOnlyList<DetailChooserLink> CreateChooserLinks(ViewNode node,
				ICmPossibilityList list = null)
			{
				List<DetailChooserLink> links = null;
				foreach (var link in node.ChooserLinks)
				{
					if (!string.Equals(link.Type, "goto", StringComparison.OrdinalIgnoreCase)
						|| string.IsNullOrEmpty(link.Label) || string.IsNullOrEmpty(link.Tool))
					{
						// Skipped links must be visible in the product log, not
						// only on a debugger (the legacy "dialog"/"simple" kinds wait on the
						// ChooserCommand paths).
						SIL.Reporting.Logger.WriteEvent(
							$"DetailComposer: chooserLink type '{link.Type}' (tool '{link.Tool}') on {node.StableId} is not the goto kind the lexeme editor uses; skipped.");
						continue;
					}
					(links ?? (links = new List<DetailChooserLink>()))
						.Add(new DetailChooserLink(Localize(link.Label), link.Tool));
				}

				if (links == null && list != null)
				{
					var tool = ResolveListEditorTool(list);
					if (tool != null)
					{
						var listName = list.Name?.BestAnalysisAlternative?.Text ?? string.Empty;
						links = new List<DetailChooserLink>
						{
							new DetailChooserLink(string.Format(CultureInfo.CurrentCulture,
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
				// Header row construction is shared with the thin mapper -- one construction
				// site so the two projectors cannot drift. The composer passes its LCModel-enriched values.
				AddField(DetailStructureRules.CreateHeaderField(
					StableId(node, obj), label, node.Field, node.WritingSystem, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, depth,
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

				var childDepth = DetailStructureRules.ChildIndent(label, depth);
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
				if (string.Equals(node.CustomEditorClass, LexReferenceMultiSliceClassName, StringComparison.Ordinal))
				{
					AddLexicalRelationRows(node, obj, depth);
					return;
				}

				if (string.Equals(node.CustomEditorClass, GhostLexRefSliceClassName, StringComparison.Ordinal)
					&& obj is ILexEntry ghostLexEntry)
				{
					AddGhostLexRefVector(node, ghostLexEntry, depth);
					return;
				}

				// A custom slice resolves plugin registry ->
				// Unsupported row, in that order and never the other way. The registry is consulted FIRST
				// so a migrated class composes as a real in-tree Avalonia editor (a DetailFieldKind.Custom
				// row carrying the plugin's control factory). The reference-vector slices are recognized
				// by legacy class identity and compose as native ReferenceVector rows: GhostLexRefSlice and
				// LexReferenceMultiSlice via the early checks above, EntrySequenceReferenceSlice via the
				// field-type dispatch below. Every OTHER unclaimed custom slice renders the labeled
				// Unsupported worklist row.
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

					if (!string.Equals(node.CustomEditorClass, EntrySequenceSliceClassName,
						StringComparison.Ordinal))
					{
						WalkUnsupported(node, obj, depth);
						foreach (var child in node.Children)
							Walk(child, obj, depth + 1);
						return;
					}
				}

				// Category classification lives in EditorKindMap; this switch routes to walkers.
				switch (EditorKindMap.ClassifyDetailFieldKind(node.RawEditor))
				{
					case DetailEditorCategory.Text:
						WalkTextField(node, obj, depth);
						break;
					case DetailEditorCategory.MorphTypeChooser:
						WalkMorphTypeChooser(node, obj, depth);
						break;
					case DetailEditorCategory.MsaChooser:
						WalkMsaChooser(node, obj, depth);
						break;
					case DetailEditorCategory.Summary:
						// Summary slices are section header rows in legacy too.
						AddHeader(node, obj, depth, Localize(node.Label) ?? node.Field);
						break;
					case DetailEditorCategory.Literal:
						// A literal/"lit" slice (legacy MessageSlice) -- static label text
						// rendered as
						// the row content by the dedicated Literal renderer (the label IS the content).
						AddLiteralRow(node, obj, depth);
						break;
					case DetailEditorCategory.Picture:
						// Picture/image editing is not part of the Avalonia detail view; the slice renders the
						// labeled Unsupported worklist row.
						WalkUnsupported(node, obj, depth);
						break;
					case DetailEditorCategory.EmbeddedView:
						// An embedded formatted view (legacy jtview / ViewSlice + XmlView) composes the
						// nested layout's fields INLINE for this same object, at depth+1 -- the
						// recursive
						// sub-view the legacy XmlView renders. WalkEmbeddedView reuses the
						// CompileForObjectWithOverrides/EnterModel/Walk descent (the visited-set guards
						// cycles); when the nested layout cannot be resolved it degrades to the
						// read-only ShortName row rather than vanishing.
						WalkEmbeddedView(node, obj, depth);
						break;
					case DetailEditorCategory.Command:
						// Command slices (button rows) are not rendered in the Avalonia detail view; the slice
						// renders the labeled Unsupported worklist row.
						WalkUnsupported(node, obj, depth);
						break;
					case DetailEditorCategory.EnumCombo:
						// Closed enum combos are not part of the Avalonia detail view; the slice renders the
						// labeled Unsupported worklist row.
						WalkUnsupported(node, obj, depth);
						break;
					default:
						WalkOtherField(node, obj, depth);
						break;
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
				var systems = ResolveTextRowWritingSystems(hvo, flid, type, node);
				var values = CreateTextRowValues(hvo, flid, type, systems, node, out var anyData, out var anyAudio);

				if (!anyData && !anyAudio && HideWhenEmpty(node))
					return;

				var stableId = StableId(node, obj);
				// A row goes read-only when a keystroke's run-replay would corrupt it
				// (ORC or non-round-trippable TsString, via CanEditRichText); audio
				// rows stay editable via the same plain text setter as the filename.
				var editable = type != CellarPropertyType.Unicode
					&& values.All(v => v.CanEditRichText || v.IsAudio);
				var textField = new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.Text, node.EditorClassification, node.AutomationId,
					node.LocalizationKey, node.Routing, values, null, null, editable, depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, hotlinksId: node.HotlinksId,
					objectHvo: obj.Hvo);
				// A multistring editor is the legacy MultiStringSlice; its in-string context menu adds the
				// shared mnuDataTree-MultiStringSlice group (Writing Systems submenu), a single-ws string
				// editor does not. Carry that so the menu composition mirrors the legacy slice test.
				textField.IsMultiStringRow = string.Equals(node.RawEditor,
					EditorKindMap.MultiStringEditor, StringComparison.OrdinalIgnoreCase);
				if (editable)
				{
					// An editable text row over a run-bearing TsString property (String/MultiString)
					// can carry named character styles, so it gets the project's character style names for the
					// per-WS style picker. Unicode props (no run structure) are never editable here, so the
					// guard above already excludes them. Empty when no styles are reachable.
					textField.AvailableNamedStyles = CharacterStyleNames();
					// The same editable run-bearing row can be retagged per-run to another project
					// writing system, so it gets the project's writing systems for the per-WS retag picker.
					textField.AvailableWritingSystems = WritingSystemOptions();
					// The per-ws fonts so a multi-run value renders with TRUE per-run fonts.
					textField.WritingSystemFonts = WritingSystemFonts();
				}
				AddField(textField);

				if (!editable)
					return;

				RegisterTextRowEditHandler(stableId, hvo, flid, type, systems);
			}

			// A text row's writing systems: the layout set, restricted by visibleWritingSystems
			// unless the row's part is under the Show-all reveal, then collapsed to one ws for
			// String/Unicode props.
			private IReadOnlyList<CoreWritingSystemDefinition> ResolveTextRowWritingSystems(int hvo, int flid,
				CellarPropertyType type, ViewNode node)
			{
				IReadOnlyList<CoreWritingSystemDefinition> systems = ResolveWritingSystems(_cache, node.WritingSystem);
				// A per-field writing-system visibility override (legacy visibleWritingSystems) restricts
				// the resolved set to the authored subset (in the override's order), intersected with the
				// field's valid writing systems. An empty intersection keeps the full set rather than hiding
				// the field entirely (defensive -- a stale override must never blank a real
				// field).
				if (_showAllWsFields == null || !_showAllWsFields.Contains(node.StableId))
					systems = ApplyVisibleWritingSystems(systems, node.VisibleWritingSystems);
				if ((type == CellarPropertyType.String || type == CellarPropertyType.Unicode)
					&& systems.Count > 0)
				{
					// Single-alternative property: one row. Plain String props:
					// get_StringProp reads the WHOLE string regardless of the layout ws= spec, so
					// taking the spec's FIRST writing system for the row's display metadata
					// (abbreviation/font/RTL) and write-back would be asymmetric when the stored
					// string was typed in another ws (legacy StringSlice renders the string's own
					// run properties). Derive the row's ws from the existing string's first run;
					// the layout ws only seeds an EMPTY string.
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
				return systems;
			}

			// The per-writing-system display values of a text row, with any voice alternative swapped for
			// its audio-flagged form. Reports whether any text/audio data was present so
			// WalkTextField can honor HideWhenEmpty.
			private IReadOnlyList<DetailWsValue> CreateTextRowValues(int hvo, int flid, CellarPropertyType type,
				IReadOnlyList<CoreWritingSystemDefinition> systems, ViewNode node, out bool anyData, out bool anyAudio)
			{
				var dataSeen = false;
				// The lexeme form's legacy bold/120% <properties> emphasis.
				var fontSize = node.FontScalePercent > 0 ? 12.0 * node.FontScalePercent / 100.0 : 0;
				// The per-ws value rows build through the shared factory
				// (LexiconEditErrorFallback uses the same one), this path only supplies the text.
				IReadOnlyList<DetailWsValue> values;
				if (type == CellarPropertyType.Unicode)
				{
					values = DetailValueFactory.CreateMultiWsValues(systems, ws =>
					{
						var text = _sda.get_UnicodeProp(hvo, flid);
						dataSeen |= !string.IsNullOrEmpty(text);
						return text;
					}, fontSize, node.BoldEmphasis);
				}
				else
				{
					values = DetailValueFactory.CreateMultiWsValues(systems, ws =>
					{
						var tss = ReadTextProp(hvo, flid, ws.Handle, type);
						dataSeen |= !string.IsNullOrEmpty(tss?.Text);
						return tss;
					}, _cache.WritingSystemFactory, fontSize, node.BoldEmphasis);
				}

				// A voice WS stores an audio recording, not text. When the row has a voice alternative,
				// recompute the per-WS values, flagging the voice alternative IsAudio so the view renders
				// play/record affordances (the value is the audio file name).
				var audioSeen = false;
				if (systems.Any(ws => ws.IsVoice))
				{
					var rebuilt = new List<DetailWsValue>(values.Count);
					for (var i = 0; i < values.Count; i++)
					{
						var ws = systems[i];
						if (ws.IsVoice)
						{
							audioSeen = true;
							// A voice (IsVoice) alternative stores the audio FILENAME as its value. Keep the
							// real filename so the owned audio field can play the file and clear/replace the
							// value; isAudio:true tells the view to render the play/record affordances instead
							// of a plain text box.
							rebuilt.Add(new DetailWsValue(ws.Abbreviation,
								values[i]?.Value ?? string.Empty,
								ws.DefaultFontName, fontSize,
								ws.RightToLeftScript, ws.Id, node.BoldEmphasis, isAudio: true));
						}
						else
						{
							rebuilt.Add(values[i]);
						}
					}
					values = rebuilt;
				}

				anyData = dataSeen;
				anyAudio = audioSeen;
				return values;
			}

			// Registers the plain-text and rich-text write delegates of an editable text row on its field
			// handler, keyed by the unique IETF tag (with unambiguous abbreviations as aliases). Split out
			// of WalkTextField unchanged.
			private void RegisterTextRowEditHandler(string stableId, int hvo, int flid, CellarPropertyType type,
				IReadOnlyList<CoreWritingSystemDefinition> systems)
			{
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
				HandlerFor(stableId).Text = (wsKey, value) =>
				{
					if (wsKey == null || !wsByKey.TryGetValue(wsKey, out var wsHandle))
						return false;
					return WriteTextProp(hvo, flid, wsHandle, type, value);
				};
				HandlerFor(stableId).RichText = (wsKey, value) =>
				{
					if (value == null || wsKey == null || !wsByKey.TryGetValue(wsKey, out var wsHandle))
						return false;
					return WriteRichTextProp(hvo, flid, wsHandle, type, value);
				};
			}

			// The ONE String-vs-multi text read dispatch every TsString-reading
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

			// The matching write dispatch (plain-text MakeString round-trip; the
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

			private bool WriteRichTextProp(int hvo, int flid, int ws, CellarPropertyType type,
				DetailRichTextValue value)
			{
				var tss = DetailRichTextAdapter.ToTsString(value, _cache.WritingSystemFactory, ws);
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
					_morphTypeOptions = new List<DetailChoiceOption>();
					foreach (var possibility in form.ReferenceTargetCandidates(MoFormTags.kflidMorphType)
						.OfType<IMoMorphType>()
						.OrderBy(mt => mt.Name.BestAnalysisAlternative?.Text, StringComparer.Ordinal))
					{
						_morphTypeOptions.Add(new DetailChoiceOption(possibility.Guid.ToString(),
							possibility.Name.BestAnalysisAlternative?.Text ?? possibility.Guid.ToString()));
					}
				}
				var options = _morphTypeOptions;

				var stableId = StableId(node, obj);
				AddField(new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.Chooser, node.EditorClassification, node.AutomationId,
					node.LocalizationKey, node.Routing, null, options, form.MorphTypeRA?.Guid.ToString(),
					isEditable: true, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, objectHvo: obj.Hvo,
					chooserLinks: CreateChooserLinks(node, morphTypes)));

				HandlerFor(stableId).Option = optionKey =>
				{
					if (!Guid.TryParse(optionKey, out var guid))
						return false;
					var repository = _cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
					if (!repository.TryGetObject(guid, out var morphType))
						return false;
					// Legacy MorphTypeAtomicLauncher gates stem<->affix swaps behind a data-loss
					// prompt AND a class conversion (MoStemAllomorph <-> MoAffixAllomorph). Assigning
					// blindly would create a model-invalid combination (e.g. a stem allomorph
					// with an
					// affix morph type), so a boundary-crossing assignment is rejected until a
					// class-conversion path exists. The GUID -> kind classification
					// is the seam's single table -- this file keeps no mirror dictionary
					// (MorphTypeGuidConsolidationTests pins the seam's table to
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

			// Selecting a POS find-or-creates the matching MSA on the owning entry
			// via the liblcm SandboxMSA setter (cleanup included); only the POS
			// level is offered -- feature-structure "Details" comes later.
			private void WalkMsaChooser(ViewNode node, ICmObject obj, int depth)
			{
				var posList = _cache.LangProject.PartsOfSpeechOA;
				if (!(obj is ILexSense sense) || posList == null)
				{
					WalkOtherField(node, obj, depth);
					return;
				}

				var options = CreatePossibilityOptions(posList, flat: false);
				var selected = (sense.MorphoSyntaxAnalysisRA as IMoStemMsa)?.PartOfSpeechRA?.Guid.ToString();

				var stableId = StableId(node, obj);
				AddField(new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.Chooser, node.EditorClassification, node.AutomationId,
					node.LocalizationKey, node.Routing, null, options, selected, isEditable: true, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, hotlinksId: node.HotlinksId,
					objectHvo: obj.Hvo, chooserLinks: CreateChooserLinks(node, posList)));

				HandlerFor(stableId).Option = key =>
				{
					if (!Guid.TryParse(key, out var guid)
						|| !_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().TryGetObject(guid, out var pos))
						return false;
					// Reuse the legacy domain path: liblcm find-or-creates the right MSA (stem/affix per the
					// sense's morph type) on the owning entry, assigns it, and drops the now-unused old MSA.
					var generic = new SandboxGenericMSA
					{
						MsaType = sense.GetDesiredMsaType(),
						MainPOS = pos
					};
					if (sense.MorphoSyntaxAnalysisRA is IMoStemMsa existingStem)
						generic.FromPartsOfSpeech = existingStem.FromPartsOfSpeechRC;
					sense.SandboxMSA = generic;
					return true;
				};
			}

			// An atomic possibility reference takes the chooser path (legacy
			// PossibilityAtomicReferenceSlice): options from the field's own list
			// (ReferenceTargetOwner), write-back through the fenced session.
			private void AddAtomicPossibilityChooser(ViewNode node, ICmObject obj, int depth, int flid,
				ICmPossibilityList list, int targetHvo)
			{
				// The legacy atomic possibility launcher lets the user CLEAR the
				// reference (PossibilityAtomicReferenceLauncher.OnLeave -> AddItem(null) when the
				// box is emptied; only a layout-authored nullLabel="" forbids it, which no
				// lexeme-editor part does), so the chooser leads with an explicit empty choice --
				// labeled with the same localized "<Empty>" the WinForms launchers show. The morph-type chooser deliberately offers
				// no empty option (MorphTypeAtomicLauncher.AllowEmptyItem == false).
				var options = new List<DetailChoiceOption>
				{
					new DetailChoiceOption(string.Empty,
						FwAvaloniaStrings.ChooserEmptyItemLabel)
				};
				// chooserInfo FlatList specs are not imported onto the node;
				// so the chooser renders the list's own hierarchy.
				options.AddRange(CreatePossibilityOptions(list, flat: false));
				var selected = targetHvo == 0
					? null
					: _cache.ServiceLocator.ObjectRepository.GetObject(targetHvo).Guid.ToString();
				var stableId = StableId(node, obj);
				AddField(new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.Chooser, node.EditorClassification, node.AutomationId,
					node.LocalizationKey, node.Routing, null, options, selected, isEditable: true, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, hotlinksId: node.HotlinksId,
					objectHvo: obj.Hvo, chooserLinks: CreateChooserLinks(node, list)));

				var hvo = obj.Hvo;
				HandlerFor(stableId).Option = key =>
				{
					// The empty option clears the reference via SetObjProp(hvo, flid, 0),
					// matching legacy AddItem(null).
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

			// The atomic analog of AddGenericReferenceVector -- an atomic
			// reference whose target is NOT a possibility list (e.g. the ad-hoc co-prohibition Key
			// FirstMorpheme/FirstAllomorph) composes as an editable chooser over the field's
			// ReferenceTargetCandidates, with a leading empty option to clear (legacy launcher parity).
			private void AddGenericAtomicChooser(ViewNode node, ICmObject obj, int depth, int flid,
				IReadOnlyList<ICmObject> candidates, int targetHvo)
			{
				var candidateHvoByGuid = new Dictionary<Guid, int>();
				var options = new List<DetailChoiceOption>
				{
					new DetailChoiceOption(string.Empty,
						FwAvaloniaStrings.ChooserEmptyItemLabel)
				};
				foreach (var cand in candidates)
				{
					if (candidateHvoByGuid.ContainsKey(cand.Guid))
						continue;
					candidateHvoByGuid[cand.Guid] = cand.Hvo;
					options.Add(new DetailChoiceOption(cand.Guid.ToString(), cand.ShortName));
				}
				var selected = targetHvo == 0
					? null
					: _cache.ServiceLocator.ObjectRepository.GetObject(targetHvo).Guid.ToString();
				var stableId = StableId(node, obj);
				AddField(new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.Chooser, node.EditorClassification, node.AutomationId,
					node.LocalizationKey, node.Routing, null, options, selected, isEditable: true, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, hotlinksId: node.HotlinksId,
					objectHvo: obj.Hvo));

				var hvo = obj.Hvo;
				HandlerFor(stableId).Option = key =>
				{
					if (string.IsNullOrEmpty(key))
					{
						_sda.SetObjProp(hvo, flid, 0); // clear the reference
						return true;
					}
					if (!Guid.TryParse(key, out var guid) || !candidateHvoByGuid.TryGetValue(guid, out var newHvo))
						return false;
					_sda.SetObjProp(hvo, flid, newHvo);
					return true;
				};
			}

			// An editable possibility-vector row -- current items in vector order
			// plus the whole list as hierarchical options; add/remove stage through
			// sda.Replace on the flid, one undo step per settled session.
			private void AddReferenceVector(ViewNode node, ICmObject obj, int depth, int flid,
				ICmPossibilityList list, int count)
			{
				var items = new List<DetailChoiceOption>();
				for (var i = 0; i < count; i++)
				{
					var itemHvo = _sda.get_VecItem(obj.Hvo, flid, i);
					var item = _cache.ServiceLocator.ObjectRepository.GetObject(itemHvo);
					items.Add(new DetailChoiceOption(item.Guid.ToString(), ResolveShortName(itemHvo)));
				}

				var options = CreatePossibilityOptions(list, flat: false); // FlatList not imported; see above
				var stableId = StableId(node, obj);
				AddField(new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.ReferenceVector, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, options, null,
					isEditable: true, indent: depth, menuId: node.MenuId, contextMenuId: node.ContextMenuId,
					hotlinksId: node.HotlinksId, objectHvo: obj.Hvo, items: items,
					chooserLinks: CreateChooserLinks(node, list)));

				var hvo = obj.Hvo;
				HandlerFor(stableId).ReferenceAdd = key =>
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
				HandlerFor(stableId).ReferenceRemove = key =>
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

			// Editable via the field's ReferenceTargetCandidates (canonical
			// valid-target set legacy choosers use); add validates the guid,
			// remove uses the repository -- both Replace the vector, the same way.
			private void AddGenericReferenceVector(ViewNode node, ICmObject obj, int depth, int flid,
				int count, IReadOnlyList<ICmObject> candidates)
			{
				var items = new List<DetailChoiceOption>();
				for (var i = 0; i < count; i++)
				{
					var itemHvo = _sda.get_VecItem(obj.Hvo, flid, i);
					items.Add(new DetailChoiceOption(
						_cache.ServiceLocator.ObjectRepository.GetObject(itemHvo).Guid.ToString(),
						ResolveShortName(itemHvo)));
				}

				var candidateHvoByGuid = new Dictionary<Guid, int>();
				var options = new List<DetailChoiceOption>();
				foreach (var cand in candidates)
				{
					if (candidateHvoByGuid.ContainsKey(cand.Guid))
						continue;
					candidateHvoByGuid[cand.Guid] = cand.Hvo;
					options.Add(new DetailChoiceOption(cand.Guid.ToString(), cand.ShortName));
				}

				var stableId = StableId(node, obj);
				AddField(new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.ReferenceVector, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, options, null,
					isEditable: true, indent: depth, menuId: node.MenuId, contextMenuId: node.ContextMenuId,
					hotlinksId: node.HotlinksId, objectHvo: obj.Hvo, items: items));

				var hvo = obj.Hvo;
				HandlerFor(stableId).ReferenceAdd = key =>
				{
					if (!Guid.TryParse(key, out var guid) || !candidateHvoByGuid.TryGetValue(guid, out var targetHvo))
						return false;
					var size = _sda.get_VecSize(hvo, flid);
					for (var i = 0; i < size; i++)
						if (_sda.get_VecItem(hvo, flid, i) == targetHvo)
							return false; // duplicates rejected, like the legacy chooser
					_sda.Replace(hvo, flid, size, size, new[] { targetHvo }, 1);
					return true;
				};
				HandlerFor(stableId).ReferenceRemove = key =>
				{
					if (!Guid.TryParse(key, out var guid)
						|| !_cache.ServiceLocator.ObjectRepository.TryGetObject(guid, out var target))
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

			// Capped so a huge candidate set (entries/senses are already handled by the
			// type-ahead path above) falls back to a read-only row instead of eagerly
			// materializing thousands of options.
			private IReadOnlyList<ICmObject> SafeReferenceTargetCandidates(ICmObject obj, int flid)
			{
				// Only REAL stored reference properties are safely editable by a blind sda.Replace/SetObjProp.
				// A virtual/computed property (back-refs, derived collections) has no backing to write and the
				// legacy editor itself gates editability on !IsVirtual (VectorReferenceView.ReadOnlyView), so
				// such fields fall through to the read-only row instead of the generic editable chooser/vector.
				if (flid == 0 || _mdc.get_IsVirtual(flid))
					return null;
				try
				{
					var candidates = obj.ReferenceTargetCandidates(flid);
					if (candidates == null)
						return null;
					var list = new List<ICmObject>();
					foreach (var c in candidates)
					{
						list.Add(c);
						if (list.Count > MaxEditableVectorCandidates)
							return null; // too large to enumerate eagerly; read-only fallback
					}
					return list;
				}
				catch
				{
					return null;
				}
			}

			private const int MaxEditableVectorCandidates = 500;

			// Resolves an option key to a possibility belonging to THIS field's list -- garbage,
			// unknown guids, and possibilities from OTHER lists all reject (no cross-list writes).
			private ICmPossibility ResolvePossibilityInList(ICmPossibilityList list, string key)
			{
				if (!Guid.TryParse(key, out var guid))
					return null;
				if (!_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().TryGetObject(guid, out var possibility))
					return null;
				return possibility.OwningList == list ? possibility : null;
			}

			// ---- The entry-reference vector path ----

			internal const string EntrySequenceSliceClassName =
				"SIL.FieldWorks.XWorks.LexEd.EntrySequenceReferenceSlice";

			internal const string LexReferenceMultiSliceClassName =
				"SIL.FieldWorks.XWorks.LexEd.LexReferenceMultiSlice";

			internal const string GhostLexRefSliceClassName =
				"SIL.FieldWorks.XWorks.LexEd.GhostLexRefSlice";

			private const int MaxEntrySearchResults = 50;

			private sealed class LexicalRelationRowModel
			{
				public ILexReference Relation;
				public string Label;
				public string MenuId;
				public bool IsEditable;
				public LexRefTypeTags.MappingTypes MappingType;
				public bool IsReverseSide;
				public IReadOnlyList<ICmObject> Targets;
			}

			private void AddLexicalRelationRows(ViewNode node, ICmObject obj, int depth)
			{
				var relations = GetLexicalRelations(obj, node.Field);
				if (relations.Count == 0)
					return;

				foreach (var relation in relations)
				{
					var row = DescribeLexicalRelationRow(relation, obj);
					if (row == null)
						continue;

					var stableId = StableId(node, obj) + "/lexref:" + relation.Guid;
					var items = row.Targets
						.Select(target => new DetailChoiceOption(target.Guid.ToString(), ResolveEntryOrSenseName(target)))
						.ToList();
					Func<string, IReadOnlyList<DetailChoiceOption>> searchOptions = null;
					if (row.IsEditable)
						searchOptions = query => SearchLexicalRelationTargets(query, obj, relation, row.MappingType);

					AddField(new DetailField(stableId, row.Label, node.Field, node.WritingSystem,
						DetailFieldKind.ReferenceVector, node.EditorClassification, node.AutomationId,
						node.LocalizationKey, node.Routing, null, null, null,
						isEditable: row.IsEditable, indent: depth, menuId: row.MenuId,
						contextMenuId: node.ContextMenuId, hotlinksId: node.HotlinksId,
						objectHvo: relation.Hvo, items: items,
						searchOptions: searchOptions));

					if (!row.IsEditable)
						continue;

					HandlerFor(stableId).ReferenceAdd = key => TryAddLexicalRelationTarget(obj, relation, row.MappingType, key);
					HandlerFor(stableId).ReferenceRemove = key => TryRemoveLexicalRelationTarget(relation, key);
				}
			}

			private IReadOnlyList<ILexReference> GetLexicalRelations(ICmObject obj, string fieldName)
			{
				if (obj == null || string.IsNullOrEmpty(fieldName))
					return Array.Empty<ILexReference>();

				var refs = SIL.LCModel.Utils.ReflectionHelper.GetProperty(obj, fieldName);
				if (refs is System.Collections.Generic.IEnumerable<int> refIds)
				{
					var repository = _cache.ServiceLocator.GetInstance<ILexReferenceRepository>();
					return refIds.Select(repository.GetObject).Where(r => r != null).ToList();
				}

				var refsObjs = refs as System.Collections.IEnumerable;
				if (refsObjs == null)
					return Array.Empty<ILexReference>();

				return refsObjs.Cast<ILexReference>().Where(r => r != null).ToList();
			}

			private LexicalRelationRowModel DescribeLexicalRelationRow(ILexReference relation, ICmObject current)
			{
				var type = relation?.Owner as ILexRefType;
				if (type == null)
					return null;

				var targets = relation.TargetsRS.Cast<ICmObject>().ToList();
				if (targets.Count == 0)
					return null;

				var mapping = (LexRefTypeTags.MappingTypes)type.MappingType;
				var firstIsCurrent = targets[0].Hvo == current.Hvo;
				var isReverseSide = false;
				IReadOnlyList<ICmObject> displayTargets;

				switch (mapping)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
					case LexRefTypeTags.MappingTypes.kmtEntryUnidirectional:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseUnidirectional:
						if (!firstIsCurrent)
							return null;
						displayTargets = targets.Skip(1).ToList();
						break;
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
						isReverseSide = !firstIsCurrent;
						displayTargets = firstIsCurrent
							? targets.Skip(1).ToList()
							: targets.Take(1).ToList();
						break;
					default:
						displayTargets = targets.Where(target => target.Hvo != current.Hvo).ToList();
						break;
				}

				if (displayTargets.Count == 0)
					return null;

				return new LexicalRelationRowModel
				{
					Relation = relation,
					Label = ResolveLexicalRelationLabel(type, isReverseSide),
					MenuId = ResolveLexicalRelationMenuId(mapping, isReverseSide),
					IsEditable = CanEditLexicalRelation(mapping, isReverseSide),
					MappingType = mapping,
					IsReverseSide = isReverseSide,
					Targets = displayTargets
				};
			}

			private static string ResolveLexicalRelationLabel(ILexRefType type, bool reverse)
			{
				string label;
				if (reverse)
				{
					label = type.ReverseName.BestAnalysisVernacularAlternative?.Text;
					if (string.IsNullOrEmpty(label))
						label = type.ReverseAbbreviation.BestAnalysisAlternative?.Text;
				}
				else
				{
					label = type.ShortName;
					if (string.IsNullOrEmpty(label))
						label = type.Abbreviation.BestAnalysisAlternative?.Text;
				}

				return string.IsNullOrEmpty(label)
					? "***"
					: label;
			}

			private static string ResolveLexicalRelationMenuId(LexRefTypeTags.MappingTypes mappingType, bool reverse)
			{
				switch (mappingType)
				{
					case LexRefTypeTags.MappingTypes.kmtSensePair:
					case LexRefTypeTags.MappingTypes.kmtEntryPair:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
						return "mnuDataTree-DeleteReplaceLexReference";
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
						return reverse ? "mnuDataTree-DeleteReplaceLexReference" : "mnuDataTree-DeleteAddLexReference";
					default:
						return "mnuDataTree-DeleteAddLexReference";
				}
			}

			private static bool CanEditLexicalRelation(LexRefTypeTags.MappingTypes mappingType, bool reverse)
			{
				if (reverse)
					return false;

				switch (mappingType)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtEntryCollection:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtSenseSequence:
					case LexRefTypeTags.MappingTypes.kmtEntrySequence:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
					case LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
					case LexRefTypeTags.MappingTypes.kmtEntryUnidirectional:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseUnidirectional:
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
					case LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
						return true;
					default:
						return false;
				}
			}

			private IReadOnlyList<DetailChoiceOption> SearchLexicalRelationTargets(string query,
				ICmObject current, ILexReference relation, LexRefTypeTags.MappingTypes mappingType)
			{
				if (string.IsNullOrWhiteSpace(query))
					return Array.Empty<DetailChoiceOption>();
				query = query.Trim();

				var present = new HashSet<int>(relation.TargetsRS.Select(target => target.Hvo));
				return EnumerateLexicalRelationCandidates(mappingType)
					.Where(target => target.Hvo != current.Hvo && !present.Contains(target.Hvo)
						&& StartsWithIgnoreCase(ResolveEntryOrSenseName(target), query))
					.Select(target => new DetailChoiceOption(target.Guid.ToString(), ResolveEntryOrSenseName(target)))
					.OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
					.Take(MaxEntrySearchResults)
					.ToList();
			}

			private IEnumerable<ICmObject> EnumerateLexicalRelationCandidates(LexRefTypeTags.MappingTypes mappingType)
			{
				switch (mappingType)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtSensePair:
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
					case LexRefTypeTags.MappingTypes.kmtSenseSequence:
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
						return _cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances().Cast<ICmObject>();
					case LexRefTypeTags.MappingTypes.kmtEntryCollection:
					case LexRefTypeTags.MappingTypes.kmtEntryPair:
					case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryUnidirectional:
					case LexRefTypeTags.MappingTypes.kmtEntrySequence:
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
						return _cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().Cast<ICmObject>();
					default:
						return _cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().Cast<ICmObject>()
							.Concat(_cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances().Cast<ICmObject>());
				}
			}

			private bool TryAddLexicalRelationTarget(ICmObject current, ILexReference relation,
				LexRefTypeTags.MappingTypes mappingType, string key)
			{
				var target = ResolveEntryOrSense(key);
				if (target == null || target.Hvo == current.Hvo || !IsLexicalRelationTargetAllowed(mappingType, target))
					return false;

				if (relation.TargetsRS.Any(existing => existing.Hvo == target.Hvo))
					return false;

				relation.TargetsRS.Add(target);
				return true;
			}

			private bool TryRemoveLexicalRelationTarget(ILexReference relation, string key)
			{
				var target = ResolveEntryOrSense(key);
				if (target == null)
					return false;

				var existing = relation.TargetsRS.FirstOrDefault(item => item.Hvo == target.Hvo);
				if (existing == null)
					return false;

				relation.TargetsRS.Remove(existing);
				if (relation.TargetsRS.Count < 2)
					_cache.DomainDataByFlid.DeleteObj(relation.Hvo);
				return true;
			}

			private static bool IsLexicalRelationTargetAllowed(LexRefTypeTags.MappingTypes mappingType, ICmObject target)
			{
				switch (mappingType)
				{
					case LexRefTypeTags.MappingTypes.kmtSenseCollection:
					case LexRefTypeTags.MappingTypes.kmtSensePair:
					case LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtSenseUnidirectional:
					case LexRefTypeTags.MappingTypes.kmtSenseSequence:
					case LexRefTypeTags.MappingTypes.kmtSenseTree:
						return target is ILexSense;
					case LexRefTypeTags.MappingTypes.kmtEntryCollection:
					case LexRefTypeTags.MappingTypes.kmtEntryPair:
					case LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
					case LexRefTypeTags.MappingTypes.kmtEntryUnidirectional:
					case LexRefTypeTags.MappingTypes.kmtEntrySequence:
					case LexRefTypeTags.MappingTypes.kmtEntryTree:
						return target is ILexEntry;
					default:
						return target is ILexEntry || target is ILexSense;
				}
			}

			private void AddGhostLexRefVector(ViewNode node, ILexEntry entry, int depth)
			{
				var stableId = StableId(node, entry);
				AddField(new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.ReferenceVector, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, null, null,
					isEditable: true, indent: depth, menuId: node.MenuId, contextMenuId: node.ContextMenuId,
					hotlinksId: node.HotlinksId, objectHvo: entry.Hvo, items: Array.Empty<DetailChoiceOption>(),
					searchOptions: query => SearchGhostLexRefTargets(query, entry)));

				HandlerFor(stableId).ReferenceAdd = key => TryCreateGhostEntryRef(entry, node.ForVariant, key);
			}

			private IReadOnlyList<DetailChoiceOption> SearchGhostLexRefTargets(string query, ILexEntry owningEntry)
			{
				if (string.IsNullOrWhiteSpace(query))
					return Array.Empty<DetailChoiceOption>();
				query = query.Trim();

				return _cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances()
					.Where(entry => entry != owningEntry && MatchesHeadwordPrefix(entry, query))
					.Select(entry => new DetailChoiceOption(entry.Guid.ToString(), ResolveEntryOrSenseName(entry)))
					.OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
					.Take(MaxEntrySearchResults)
					.ToList();
			}

			private bool TryCreateGhostEntryRef(ILexEntry entry, bool forVariant, string key)
			{
				var target = ResolveEntryOrSense(key);
				if (target == null)
					return false;

				var targetEntry = target as ILexEntry ?? (target as ILexSense)?.Entry;
				if (targetEntry == entry)
					return false;

				if (forVariant ? entry.VariantEntryRefs.Any() : entry.ComplexFormEntryRefs.Any())
					return false;

				var ler = entry.Services.GetInstance<ILexEntryRefFactory>().Create();
				entry.EntryRefsOS.Add(ler);

				if (forVariant)
				{
					const string unspecVariantEntryTypeGuid = "3942addb-99fd-43e9-ab7d-99025ceb0d4e";
					var type = entry.Cache.LangProject.LexDbOA.VariantEntryTypesOA.PossibilitiesOS
						.First(lrt => lrt.Guid.ToString() == unspecVariantEntryTypeGuid) as ILexEntryType;
					ler.VariantEntryTypesRS.Add(type);
					ler.RefType = LexEntryRefTags.krtVariant;
					ler.HideMinorEntry = 0;
				}
				else
				{
					const string unspecComplexFormEntryTypeGuid = "fec038ed-6a8c-4fa5-bc96-a4f515a98c50";
					var type = entry.Cache.LangProject.LexDbOA.ComplexEntryTypesOA.PossibilitiesOS
						.First(lrt => lrt.Guid.ToString() == unspecComplexFormEntryTypeGuid) as ILexEntryType;
					ler.RefType = LexEntryRefTags.krtComplexForm;
					ler.ComplexEntryTypesRS.Add(type);
					ler.HideMinorEntry = 0;
					ler.PrimaryLexemesRS.Add(target);
					entry.ChangeRootToStem();
				}

				try
				{
					ler.ComponentLexemesRS.Add(target);
				}
				catch (ArgumentException)
				{
					entry.EntryRefsOS.Remove(ler);
					return false;
				}

				return true;
			}

			// This path's gate: a NON-virtual reference vector whose destination signature is
			// LexEntry/LexSense -- or CmObject when the layout identity is the legacy
			// EntrySequenceReferenceSlice (ComponentLexemes/PrimaryLexemes sign ILexEntryOrLexSense
			// as plain CmObject). Virtual back-ref vectors (ComplexFormEntries, Subentries,
			// VisibleComplexFormBackRefs, VariantFormEntries) stay read-only on this path: their writes
			// land on the OTHER entry's LexEntryRef, not on this flid (the legacy launcher's
			// AddNewObjectsToProperty overrides).
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

			// The editable entry/sense-reference vector -- current refs as headword items, remove
			// in-pane, add via type-ahead headword-prefix search over the entry repository (never the
			// whole lexicon as options). Writes ride sda.Replace on the flid inside the fenced
			// session, like the possibility path, plus the legacy ComponentLexemes coupling below.
			private void AddEntryReferenceVector(ViewNode node, ICmObject obj, int depth, int flid, int count)
			{
				var items = new List<DetailChoiceOption>();
				for (var i = 0; i < count; i++)
				{
					var itemHvo = _sda.get_VecItem(obj.Hvo, flid, i);
					var item = _cache.ServiceLocator.ObjectRepository.GetObject(itemHvo);
					items.Add(new DetailChoiceOption(item.Guid.ToString(), ResolveEntryOrSenseName(item)));
				}

				var stableId = StableId(node, obj);
				var hvo = obj.Hvo;
				// "Self" for the circular-reference guard: the entry whose pane this is -- the
				// row's
				// object when it IS an entry, else its owning entry (e.g. obj is the LexEntryRef).
				var owningEntry = obj as ILexEntry ?? obj.OwnerOfClass<ILexEntry>();

				AddField(new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.ReferenceVector, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, null, null,
					isEditable: true, indent: depth, menuId: node.MenuId, contextMenuId: node.ContextMenuId,
					hotlinksId: node.HotlinksId, objectHvo: obj.Hvo, items: items,
					searchOptions: query => SearchLexicon(query, hvo, flid, owningEntry),
					chooserLinks: CreateChooserLinks(node)));

				HandlerFor(stableId).ReferenceAdd = key =>
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
				HandlerFor(stableId).ReferenceRemove = key =>
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
			// added when PrimaryLexemes is empty becomes the primary lexeme, and (unless the ref
			// is
			// typed as a derivative) the complex form shows under the new component
			// (ShowComplexFormsIn) -- LT-12285 guards the duplicate. Removal needs no twin here:
			// LCModel's RemoveObjectSideEffects already clears PrimaryLexemes/ShowComplexFormsIn
			// when
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

			// Resolves a search/option key to an entry or sense -- exactly the targets
			// EntrySequenceReferenceSlice references (ILexEntryOrLexSense); everything else rejects.
			private enum BackRefVectorKind
			{
				None,
				// Vector items are ILexEntry (the complex-form entries whose PrimaryLexemes contain
				// m_obj). Add/remove m_obj on the chosen entry's complex-form LER.PrimaryLexemesRS.
				Subentries,
				// Vector items are ILexEntryRef (complex-form refs whose ShowComplexFormsIn contains
				// m_obj). Add/remove m_obj on the ref's ShowComplexFormsInRS.
				VisibleComplexFormBackRefs
			}

			// This path's gate: a VIRTUAL reference vector on a LexEntry/LexSense whose field name
			// is one of the back-ref relationships we can write across objects safely. Everything
			// else (including VariantFormEntryBackRefs, whose legacy add inserts a NEW variant
			// entry rather than choosing an existing ref) stays read-only.
			private BackRefVectorKind ResolveEditableBackRefKind(ViewNode node, int flid, ICmObject obj)
			{
				if (!_mdc.get_IsVirtual(flid))
					return BackRefVectorKind.None;
				// These relationships hang off a LexEntry or LexSense pane object.
				if (!(obj is ILexEntry) && !(obj is ILexSense))
					return BackRefVectorKind.None;
				switch (node.Field)
				{
					case "Subentries":
						return BackRefVectorKind.Subentries;
					case "VisibleComplexFormBackRefs":
						return BackRefVectorKind.VisibleComplexFormBackRefs;
					default:
						return BackRefVectorKind.None;
				}
			}

			// The editable virtual back-ref vector: current refs as headword items, remove in-pane,
			// add via type-ahead headword-prefix search over THIS entry's complex-form candidates.
			// Writes cross to the owning LexEntryRef (legacy launcher overrides) inside the fenced
			// session. Items display the owning complex-form entry's headword either way.
			private void AddBackRefReferenceVector(ViewNode node, ICmObject obj, int depth, int flid,
				int count, BackRefVectorKind kind)
			{
				var items = new List<DetailChoiceOption>();
				for (var i = 0; i < count; i++)
				{
					var itemHvo = _sda.get_VecItem(obj.Hvo, flid, i);
					var item = _cache.ServiceLocator.ObjectRepository.GetObject(itemHvo);
					// Subentries items are ILexEntry; VisibleComplexFormBackRefs items are
					// ILexEntryRef. Key on the OWNING complex-form entry either way, so the option
					// key round-trips to the same entry the chooser offers.
					var owningEntry = BackRefItemOwningEntry(item);
					items.Add(new DetailChoiceOption(owningEntry.Guid.ToString(),
						ResolveEntryOrSenseName(owningEntry)));
				}

				var stableId = StableId(node, obj);

				AddField(new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.ReferenceVector, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, null, null,
					isEditable: true, indent: depth, menuId: node.MenuId, contextMenuId: node.ContextMenuId,
					hotlinksId: node.HotlinksId, objectHvo: obj.Hvo, items: items,
					searchOptions: query => SearchBackRefCandidates(query, obj),
					chooserLinks: CreateChooserLinks(node)));

				HandlerFor(stableId).ReferenceAdd = key => TryAddBackRef(obj, kind, key);
				HandlerFor(stableId).ReferenceRemove = key => TryRemoveBackRef(obj, flid, kind, key);
			}

			// The owning complex-form entry of a back-ref vector item: the entry itself for the
			// Subentries (ILexEntry) items, or the ref's owning entry for ILexEntryRef items.
			private ILexEntry BackRefItemOwningEntry(ICmObject item)
			{
				return item as ILexEntry
					?? (item as ILexEntryRef)?.OwnerOfClass<ILexEntry>()
					?? item.OwnerOfClass<ILexEntry>();
			}

			// The chooser candidates for back-ref vectors: m_obj's complex-form
			// entries -- same source legacy HandleChooserForBackRefs uses.
			// Type-ahead headword-prefix search, excluding entries already in the
			// vector.
			private IReadOnlyList<DetailChoiceOption> SearchBackRefCandidates(string query, ICmObject obj)
			{
				if (string.IsNullOrWhiteSpace(query))
					return Array.Empty<DetailChoiceOption>();
				query = query.Trim();

				var candidates = BackRefComplexFormEntries(obj);
				return candidates
					.Where(entry => MatchesHeadwordPrefix(entry, query))
					.Select(entry => new DetailChoiceOption(entry.Guid.ToString(),
						ResolveEntryOrSenseName(entry)))
					.OrderBy(option => option.Name, StringComparer.OrdinalIgnoreCase)
					.Take(MaxEntrySearchResults)
					.ToList();
			}

			// m_obj.ComplexFormEntries (the legacy chooser's option source), regardless of whether
			// the pane object is a LexEntry or a LexSense.
			private IEnumerable<ILexEntry> BackRefComplexFormEntries(ICmObject obj)
			{
				switch (obj)
				{
					case ILexEntry entry:
						return entry.ComplexFormEntries;
					case ILexSense sense:
						return sense.ComplexFormEntries;
					default:
						return Array.Empty<ILexEntry>();
				}
			}

			// Cross-object ADD (legacy AddNewObjectsToProperty): the chosen object is a complex-form
			// entry of m_obj. Find its complex-form LexEntryRef (the one whose components include
			// m_obj) and add m_obj to the owned property: PrimaryLexemes for Subentries,
			// ShowComplexFormsIn for VisibleComplexFormBackRefs. Returns false WITHOUT staging for
			// keys outside the candidate set or already present.
			private bool TryAddBackRef(ICmObject obj, BackRefVectorKind kind, string key)
			{
				if (!Guid.TryParse(key, out var guid))
					return false;
				if (!_cache.ServiceLocator.ObjectRepository.TryGetObject(guid, out var chosen))
					return false;
				var chosenEntry = chosen as ILexEntry;
				if (chosenEntry == null)
					return false;
				// Only entries that are genuinely complex forms of m_obj are valid (the legacy
				// chooser's option list); this also locates the owning LexEntryRef.
				var ler = FindComplexFormRef(chosenEntry, obj);
				if (ler == null)
					return false;

				switch (kind)
				{
					case BackRefVectorKind.Subentries:
						if (ler.PrimaryLexemesRS.Contains(obj))
							return false; // already a subentry: duplicate
						ler.PrimaryLexemesRS.Add(obj);
						return true;
					case BackRefVectorKind.VisibleComplexFormBackRefs:
						if (ler.ShowComplexFormsInRS.Contains(obj))
							return false; // already shown: duplicate
						ler.ShowComplexFormsInRS.Add(obj);
						return true;
					default:
						return false;
				}
			}

			// Cross-object REMOVE (legacy RemoveFromPropertyAt): resolve the option key to the
			// owning complex-form entry that currently appears in the vector, find its complex-form
			// LexEntryRef, and remove m_obj from the owned property. Returns false WITHOUT staging
			// when the key is not in the vector.
			private bool TryRemoveBackRef(ICmObject obj, int flid, BackRefVectorKind kind, string key)
			{
				if (!Guid.TryParse(key, out var guid))
					return false;
				if (!_cache.ServiceLocator.ObjectRepository.TryGetObject(guid, out var chosen))
					return false;
				var chosenEntry = chosen as ILexEntry;
				if (chosenEntry == null)
					return false;

				// The option key is the OWNING entry guid; confirm it is currently in the vector by
				// matching the item's owning entry.
				var size = _sda.get_VecSize(obj.Hvo, flid);
				var present = false;
				for (var i = 0; i < size; i++)
				{
					var item = _cache.ServiceLocator.ObjectRepository.GetObject(_sda.get_VecItem(obj.Hvo, flid, i));
					if (BackRefItemOwningEntry(item) == chosenEntry)
					{
						present = true;
						break;
					}
				}
				if (!present)
					return false;

				var ler = FindComplexFormRef(chosenEntry, obj);
				if (ler == null)
					return false;

				switch (kind)
				{
					case BackRefVectorKind.Subentries:
						if (!ler.PrimaryLexemesRS.Contains(obj))
							return false;
						ler.PrimaryLexemesRS.Remove(obj);
						return true;
					case BackRefVectorKind.VisibleComplexFormBackRefs:
						if (!ler.ShowComplexFormsInRS.Contains(obj))
							return false;
						ler.ShowComplexFormsInRS.Remove(obj);
						return true;
					default:
						return false;
				}
			}

			/// <summary>
			/// Finds the complex-form ref on <paramref name="complexFormEntry"/> whose
			/// components include <paramref name="component"/> -- the one ref that links them.
			/// Mirrors legacy ChangeItemsInLexEntryRefs ("the LER which has item as a
			/// component").
			/// </summary>
			/// <returns>
			/// The matching ref, or null when the entry is not a complex form of the component.
			/// </returns>
			private ILexEntryRef FindComplexFormRef(ILexEntry complexFormEntry, ICmObject component)
			{
				return complexFormEntry.EntryRefsOS.FirstOrDefault(ler =>
					ler.RefType == LexEntryRefTags.krtComplexForm
					&& ler.ComponentLexemesRS.Contains(component));
			}

			private ICmObject ResolveEntryOrSense(string key)
			{
				if (!Guid.TryParse(key, out var guid))
					return null;
				if (!_cache.ServiceLocator.ObjectRepository.TryGetObject(guid, out var target))
					return null;
				return target is ILexEntry || target is ILexSense ? target : null;
			}

			// Item display: the headword (HeadWord for entries, owner-outline
			// headword+sense-number for senses) -- the same display legacy deParams
			// displayProperty="HeadWord" yields; ShortName is the fallback.
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

			// The type-ahead path: case-insensitive headword-prefix search over the entry
			// repository (headword/citation form/lexeme form), excluding the pane's own entry and
			// the vector's current members (read live, so a staged add drops out of the next
			// search), capped at MaxEntrySearchResults, ordered by headword.
			private IReadOnlyList<DetailChoiceOption> SearchLexicon(string query, int hvo, int flid,
				ILexEntry owningEntry)
			{
				if (string.IsNullOrWhiteSpace(query))
					return Array.Empty<DetailChoiceOption>();
				query = query.Trim();

				var present = new HashSet<int>();
				var size = _sda.get_VecSize(hvo, flid);
				for (var i = 0; i < size; i++)
					present.Add(_sda.get_VecItem(hvo, flid, i));

				return _cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances()
					.Where(entry => entry != owningEntry && !present.Contains(entry.Hvo)
						&& MatchesHeadwordPrefix(entry, query))
					.Select(entry => new DetailChoiceOption(entry.Guid.ToString(), ResolveEntryOrSenseName(entry)))
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

			// Viewing parity: every field type the legacy slices display has a rendering here:
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
							WalkReferenceAtomicField(node, obj, depth, flid);
							return;
						case CellarPropertyType.OwningAtomic:
							WalkOwningAtomicField(node, obj, depth, flid);
							return;
						case CellarPropertyType.ReferenceSequence:
						case CellarPropertyType.ReferenceCollection:
							WalkReferenceVectorField(node, obj, depth, flid);
							return;
					}
				}

				if (!HideWhenEmpty(node))
					WalkUnsupported(node, obj, depth);
			}

			// The atomic-reference dispatch of WalkOtherField, split out unchanged: a possibility-list or
			// enumerable-target atomic ref composes as a chooser; anything else is a read-only short-name row.
			private void WalkReferenceAtomicField(ViewNode node, ICmObject obj, int depth, int flid)
			{
				var targetHvo = _sda.get_ObjectProp(obj.Hvo, flid);

				// An atomic ref whose target owner is a possibility list takes the
				// chooser path (legacy PossibilityAtomicReferenceSlice), like morph type.
				if (obj.ReferenceTargetOwner(flid) is ICmPossibilityList list)
				{
					if (targetHvo == 0 && HideWhenEmpty(node))
						return;
					AddAtomicPossibilityChooser(node, obj, depth, flid, list, targetHvo);
					return;
				}

				// An atomic ref whose targets are enumerable (e.g. the
				// ad-hoc Key FirstMorpheme/FirstAllomorph) composes as an editable chooser over its
				// ReferenceTargetCandidates (the atomic analog of the generic editable vector).
				var atomicCandidates = SafeReferenceTargetCandidates(obj, flid);
				if (atomicCandidates != null && atomicCandidates.Count > 0)
				{
					if (targetHvo == 0 && HideWhenEmpty(node))
						return;
					AddGenericAtomicChooser(node, obj, depth, flid, atomicCandidates, targetHvo);
					return;
				}

				if (targetHvo == 0)
				{
					AddRowUnlessHiddenWhenEmpty(node, obj, depth);
					return;
				}

				AddReadOnlyRow(node, obj, depth, ResolveShortName(targetHvo));
			}

			// The owning-atomic dispatch of WalkOtherField: an StText target composes
			// as the editable multi-paragraph row; anything else is a read-only short-name row.
			private void WalkOwningAtomicField(ViewNode node, ICmObject obj, int depth, int flid)
			{
				var targetHvo = _sda.get_ObjectProp(obj.Hvo, flid);
				if (targetHvo == 0)
				{
					AddRowUnlessHiddenWhenEmpty(node, obj, depth);
					return;
				}

				// Structured text is an EDITABLE multi-paragraph row (legacy
				// StTextSlice rich editor): paragraph text, add/delete, and named
				// style, each undoable. ORC-bearing paragraphs stay read-only.
				if (_cache.ServiceLocator.ObjectRepository.GetObject(targetHvo) is IStText stText)
				{
					var anyText = stText.ParagraphsOS.OfType<IStTxtPara>()
						.Any(par => !string.IsNullOrWhiteSpace(par.Contents?.Text));
					if (!anyText && HideWhenEmpty(node))
						return;
					AddStructuredText(node, obj, depth, flid, stText);
					return;
				}

				AddReadOnlyRow(node, obj, depth, ResolveShortName(targetHvo));
			}

			/// <summary>
			/// Composes a reference-sequence or reference-collection field: an editable
			/// chooser-backed ReferenceVector when the targets can be enumerated, otherwise a
			/// read-only row of joined short names.
			/// </summary>
			private void WalkReferenceVectorField(ViewNode node, ICmObject obj, int depth, int flid)
			{
				var count = _sda.get_VecSize(obj.Hvo, flid);

				// A vector whose targets live in a possibility list becomes an editable
				// ReferenceVector row (legacy slice, trailing type-ahead add slot), even
				// when empty, so the field always offers the add affordance.
				if (obj.ReferenceTargetOwner(flid) is ICmPossibilityList list)
				{
					if (count == 0 && HideWhenEmpty(node))
						return;
					AddReferenceVector(node, obj, depth, flid, list, count);
					return;
				}

				// A vector whose targets are
				// entries/senses (the EntrySequenceReferenceSlice fields --
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

				// Virtual back-reference vectors (Subentries, VisibleComplexFormBackRefs):
				// the relationship is OWNED by the OTHER entry's LexEntryRef, so add/remove
				// route across objects exactly like the legacy
				// EntrySequenceReferenceLauncher.AddNewObjectsToProperty /
				// RemoveFromPropertyAt overrides. VariantFormEntryBackRefs stays read-only
				// (its legacy add is "insert a NEW variant entry", not a chooser-add of an
				// existing ref -- out of scope for this safe increment).
				var backRefKind = ResolveEditableBackRefKind(node, flid, obj);
				if (backRefKind != BackRefVectorKind.None)
				{
					if (count == 0 && HideWhenEmpty(node))
						return;
					AddBackRefReferenceVector(node, obj, depth, flid, count, backRefKind);
					return;
				}

				// Any remaining reference vector whose valid targets
				// can be enumerated (e.g. natural-class Segments -> phonemes, ad-hoc Others ->
				// allomorphs/morphemes) composes as an editable chooser-backed ReferenceVector,
				// matching the legacy reference-vector slice. Entry/sense (huge) vectors were
				// handled above; the candidate cap guards any other large set into read-only.
				var genericCandidates = SafeReferenceTargetCandidates(obj, flid);
				if (genericCandidates != null && genericCandidates.Count > 0)
				{
					if (count == 0 && HideWhenEmpty(node))
						return;
					AddGenericReferenceVector(node, obj, depth, flid, count, genericCandidates);
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
			}

			// A literal/"lit" slice (legacy MessageSlice) -- the slice's label/message text is
			// the
			// static content. Routed to DetailFieldKind.Literal so the view renders it as static text in the
			// value column rather than an (empty) editable row. The content is carried in the value slot so
			// the renderer shows the message even when there is no separate label column.
			private void AddLiteralRow(ViewNode node, ICmObject obj, int depth)
			{
				// The "lit" slice's label/message text IS the content (legacy
				// MessageSlice). Label column stays empty; message rides the value
				// slot so it renders ONCE, as static gray content -- not duplicated.
				var message = Localize(node.Label) ?? node.Field ?? string.Empty;
				AddField(new DetailField(StableId(node, obj), string.Empty,
					node.Field, node.WritingSystem, DetailFieldKind.Literal, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing,
					new List<DetailWsValue> { new DetailWsValue("", message) }, null, null,
					isEditable: false, indent: depth, objectHvo: obj.Hvo));
			}

			private void AddReadOnlyRow(ViewNode node, ICmObject obj, int depth, string display)
			{
				AddField(new DetailField(StableId(node, obj), Localize(node.Label) ?? node.Field,
					node.Field, node.WritingSystem, DetailFieldKind.Text, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing,
					new List<DetailWsValue> { new DetailWsValue("", display ?? string.Empty) }, null, null,
					isEditable: false, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, objectHvo: obj.Hvo));
			}

			// The editable multi-paragraph structured-text (StText) row (legacy StTextSlice).
			// Builds one DetailParagraph per StTxtPara (run-aware text +
			// per-paragraph named style; an ORC/lossy paragraph stays read-only/preserved) and
			// registers the four paragraph-CRUD setters that mutate the LCModel StText inside the open
			// fenced session -- text/style writes are one undo step (focus-loss autosave),
			// add/delete one
			// undo step (immediate commit + host re-show). The closures verify the StText/paragraph still
			// exists on each call (a Cancel can roll an insert/StText creation back under a still-shown view).
			private void AddStructuredText(ViewNode node, ICmObject obj, int depth, int flid, IStText stText)
			{
				var paragraphs = new List<DetailParagraph>();
				foreach (var par in stText.ParagraphsOS.OfType<IStTxtPara>())
				{
					var rich = DetailRichTextAdapter.FromTsString(par.Contents, _cache.WritingSystemFactory);
					paragraphs.Add(new DetailParagraph(rich, par.StyleName));
				}

				var stableId = StableId(node, obj);
				// The field is editable when the field itself is not hidden and EVERY paragraph round-trips
				// (no ORC/lossy paragraph). A lossy paragraph holds its own row read-only via CanEditText, but
				// the row's structural affordances (add/delete) stay available, so the field stays editable.
				var field = new DetailField(stableId, Localize(node.Label) ?? node.Field, node.Field,
					node.WritingSystem, DetailFieldKind.StructuredText, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, null, null,
					isEditable: true, indent: depth, menuId: node.MenuId, contextMenuId: node.ContextMenuId,
					hotlinksId: node.HotlinksId, objectHvo: obj.Hvo, paragraphs: paragraphs);
				field.AvailableParagraphStyles = ParagraphStyleNames();
				// The StText paragraph rows also expose the run-level character-style + ws pickers and
				// the per-run font display, so they get the same host-supplied lists as the single-WS rows.
				field.AvailableNamedStyles = CharacterStyleNames();
				field.AvailableWritingSystems = WritingSystemOptions();
				field.WritingSystemFonts = WritingSystemFonts();
				AddField(field);

				var stTextHvo = stText.Hvo;
				var defaultWs = _cache.DefaultAnalWs;
				var repo = _cache.ServiceLocator.GetInstance<IStTextRepository>();

				// Re-resolve the StText each call (it must survive a Cancel that rolled an edit back); null
				// when the StText was deleted out from under the still-shown view.
				IStText Live() => repo.TryGetObject(stTextHvo, out var live) ? live : null;

				HandlerFor(stableId).ParagraphText = (index, value) =>
				{
					var live = Live();
					if (live == null || value == null || index < 0)
						return false;
					var paras = live.ParagraphsOS;
					// Index may point one past the end of an StText whose paragraphs were never created
					// (the editor always shows >= 1 row); create empty paragraphs up to the index.
					while (paras.Count <= index)
						live.InsertNewTextPara(paras.Count, null);
					if (!(paras[index] is IStTxtPara para))
						return false;
					para.Contents = DetailRichTextAdapter.ToTsString(value, _cache.WritingSystemFactory, defaultWs);
					return true;
				};

				HandlerFor(stableId).ParagraphStyle = (index, styleName) =>
				{
					var live = Live();
					if (live == null || index < 0 || index >= live.ParagraphsOS.Count)
						return false;
					if (!(live.ParagraphsOS[index] is IStTxtPara para))
						return false;
					// LCModel forbids a null/empty paragraph StyleName; "clear" means revert to the default
					// paragraph style (legacy StVc seeds StText paragraphs with "Normal").
					para.StyleName = string.IsNullOrEmpty(styleName)
						? StyleServices.NormalStyleName
						: styleName;
					return true;
				};

				HandlerFor(stableId).ParagraphInsert = afterIndex =>
				{
					var live = Live();
					if (live == null)
						return false;
					// Insert AFTER the given index (a negative index inserts at the start); clamp
					// into range.
					var pos = afterIndex < 0 ? 0 : Math.Min(afterIndex + 1, live.ParagraphsOS.Count);
					live.InsertNewTextPara(pos, null);
					return true;
				};

				HandlerFor(stableId).ParagraphDelete = index =>
				{
					var live = Live();
					if (live == null || index < 0 || index >= live.ParagraphsOS.Count)
						return false;
					// The StText always keeps at least one paragraph, like the legacy editor.
					if (live.ParagraphsOS.Count <= 1)
						return false;
					live.ParagraphsOS.RemoveAt(index);
					return true;
				};
			}

			// The plugin-claimed row -- DetailFieldKind.Custom
			// with the normal label/indent/menu metadata, carrying a factory that closes over
			// (object, node, deferred edit context, cache). The factory runs in the view at render
			// time, so composing stays side-effect free and the edit context exists by then.
			private void AddPluginRow(ViewNode node, ICmObject obj, int depth, ISlicePlugin plugin)
			{
				// ONE plugin contract -- the build context bundles everything a
				// plugin can need (object, node, deferred edit-context accessor, cache); there is
				// no
				// service-aware marker type test.
				var context = new SlicePluginBuildContext(obj, node, _editContextAccessor, _cache);
				Func<Avalonia.Controls.Control> factory = () => plugin.BuildControl(context);
				AddField(new DetailField(StableId(node, obj), Localize(node.Label) ?? node.Field,
					node.Field, node.WritingSystem, DetailFieldKind.Custom, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, null, null,
					isEditable: true, indent: depth,
					menuId: node.MenuId, contextMenuId: node.ContextMenuId, hotlinksId: node.HotlinksId,
					objectHvo: obj.Hvo,
					controlFactory: factory));
			}

			private void WalkUnsupported(ViewNode node, ICmObject obj, int depth)
			{
				// The Unsupported worklist row still binds its object and slice menus so a right-click
				// keeps the legacy per-object commands (Field Visibility / Move Field / Help), and so the
				// row is addressable by its object (menu targeting, override editing) like any other row.
				AddField(new DetailField(StableId(node, obj), Localize(node.Label) ?? node.Field,
					node.Field, node.WritingSystem, DetailFieldKind.Unsupported, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, null, null, null,
					isEditable: false, indent: depth, menuId: node.MenuId, contextMenuId: node.ContextMenuId,
					objectHvo: obj.Hvo));
			}

			// Viewing parity: empty always-visible object/sequence fields show the
			// legacy ghost add-prompt as a WATERMARK on an editable row -- clicking in clears the
			// prompt, and typing creates the missing object inside the fenced session (the legacy
			// ghost-slice create-on-edit path), routing the text into the layout's ghost field
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
				AddField(new DetailField(stableId, label, node.Field,
					node.WritingSystem, DetailFieldKind.Text, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing,
					new List<DetailWsValue> { new DetailWsValue(ghost?.WsAbbrev ?? "", string.Empty, wsTag: ghost?.WsTag) },
					null, null, isEditable: ghost != null, indent: depth,
					menuId: node.MenuId, hotlinksId: node.HotlinksId, objectHvo: obj.Hvo,
					ghostPrompt: prompt));

				if (ghost != null)
					HandlerFor(stableId).Text = ghost.Setter;
			}

			private sealed class GhostCreation
			{
				public string WsAbbrev;
				public string WsTag; // unique IETF tag (ws.Id), the edit-routing identity
				public Func<string, string, bool> Setter;
			}

			// The create-on-edit half of the ghost path: resolve the owning field's destination class
			// (the layout's ghostClass when the model class is abstract; MoStemAllomorph for MoForm,
			// matching legacy CreateAllomorph; Gloss-on-LexSense when no ghost field is authored) and
			// build a setter that creates the object inside the open session -- cancel rolls the
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

				// With no resolvable ghost field AND no init method, typing could
				// only MakeNewObject a bare object while the typed text silently vanished
				// (nothing
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
						// The closure outlives the edit session: a Cancel rolls MakeNewObject
						// back, so a later edit via the same still-visible view must not write
						// to the deleted hvo -- verify it exists, re-create if not.
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
							// The shared 3-way text write dispatch.
							WriteTextProp(createdHvo, ghostFlid, ws.Handle,
								(CellarPropertyType)_mdc.GetFieldType(ghostFlid), value);
						}
						// Invoke the layout's ghostInitMethod by reflection on the newly created
						// object, after the typed text lands -- exactly GhostStringSliceView.
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
					// Ghost add-prompt for an empty always-visible object field.
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
						// summary text (ShortName = gloss) in the header line. The number
						// is the domain's own LexSenseOutline (the dictionary/bulk-edit outline), so
						// the entry pane cannot diverge from the other views.
						itemLabel = ($"{sense.LexSenseOutline.Text}  {item.ShortName}").TrimEnd();
					}
					else
					{
						itemLabel = $"{sectionLabel} {i + 1}";
					}

					// The item's own layout carries the slice menu (e.g. the sense's
					// HeavySummary part ref binds mnuDataTree-Sense in LexSense.fwlayout) -- the
					// sequence node itself usually has none.
					var itemBinding = ResolveItemMenuBinding(node, item);
					AddField(new DetailField($"{StableId(node, obj)}/item{i}",
						itemLabel, node.Field, null, DetailFieldKind.Header,
						EditorClassification.GroupingNone, null, null, HostRouting.Inherit,
						null, null, null, isEditable: false, indent: depth + 1,
						isCollapsible: true, isInitiallyExpanded: expanded,
						menuId: itemBinding.MenuId ?? node.MenuId,
						hotlinksId: itemBinding.HotlinksId ?? node.HotlinksId,
						objectHvo: item.Hvo));
					DescendInto(node, item, depth + 1);
				}
			}

			// First root-level menu/hotlinks binding of the item's compiled layout (compile
			// results are memoized per (class, layout), and the binding itself is memoized per
			// compose state).
			private (string MenuId, string HotlinksId) ResolveItemMenuBinding(ViewNode node, ICmObject item)
			{
				var layoutName = string.IsNullOrEmpty(node.TargetLayout) ? "Normal" : node.TargetLayout;
				if (_itemMenuBindings.TryGetValue((item.ClassID, layoutName), out var cached))
					return cached;

				var compiled = CompileForObjectWithOverrides(item, layoutName);
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

			// An embedded formatted view (legacy jtview / ViewSlice over an XmlView). The jtview's
			// param/layout names a layout to render for the SAME object inline; we compile that layout and
			// walk its fields at depth+1, exactly as DescendInto walks a descended object's layout. The
			// visited-set guards against a layout that (directly or transitively) re-enters itself. When the
			// nested layout is empty/unresolvable, degrade to the legacy read-only ShortName row so the
			// field never silently vanishes.
			private void WalkEmbeddedView(ViewNode node, ICmObject obj, int depth)
			{
				var layoutName = node.TargetLayout;
				if (string.IsNullOrEmpty(layoutName))
				{
					AddReadOnlyRow(node, obj, depth, obj.ShortName ?? string.Empty);
					return;
				}

				if (!_visited.Add((obj.Hvo, layoutName)))
				{
					// Already composing this (object, layout) higher in the stack -- a cyclic
					// jtview nest.
					AddReadOnlyRow(node, obj, depth, obj.ShortName ?? string.Empty);
					return;
				}

				try
				{
					var compiled = CompileForObjectWithOverrides(obj, layoutName);
					if (compiled != null && compiled.Roots.Count > 0)
					{
						EnterModel(compiled);
						foreach (var child in compiled.Roots)
							Walk(child, obj, depth + 1);
						ExitModel();
					}
					else
					{
						AddReadOnlyRow(node, obj, depth, obj.ShortName ?? string.Empty);
					}
				}
				finally
				{
					_visited.Remove((obj.Hvo, layoutName));
				}
			}

			private void DescendInto(ViewNode node, ICmObject target, int depth)
			{
				var layoutName = string.IsNullOrEmpty(node.TargetLayout) ? "Normal" : node.TargetLayout;
				if (!_visited.Add((target.Hvo, layoutName)))
					return;

				var compiled = CompileForObjectWithOverrides(target, layoutName);
				if (compiled != null && compiled.Roots.Count > 0)
				{
					// Rows from the descended model are stamped with ITS (class,
					// layout), so the menu-button commands on a sense/allomorph row target
					// that layout's override file, not the entry's.
					EnterModel(compiled);
					foreach (var child in compiled.Roots)
						Walk(child, target, depth + 1);
					ExitModel();
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

		// Mirrors legacy's GetMagicWsIdFromName/GetWritingSystemList; override falls back to all.
		internal static IReadOnlyList<CoreWritingSystemDefinition> ApplyVisibleWritingSystems(
			IReadOnlyList<CoreWritingSystemDefinition> systems, IReadOnlyList<string> visible)
		{
			if (visible == null || visible.Count == 0 || systems == null || systems.Count == 0)
				return systems;

			var byId = systems.ToDictionary(ws => ws.Id, System.StringComparer.OrdinalIgnoreCase);
			var result = new List<CoreWritingSystemDefinition>();
			var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
			foreach (var spec in visible)
			{
				if (byId.TryGetValue(spec, out var ws) && seen.Add(ws.Id))
					result.Add(ws);
			}
			return result.Count > 0 ? result : systems;
		}

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
		/// Compiles the layout for an object's class, walking base classes the way legacy
		/// DataTree
		/// does (e.g. MoStemAllomorph -> MoForm) for both layout lookup and part resolution.
		/// Memoized per (starting class, layout) for the lifetime of the loaded sources.
		/// </summary>
		/// <summary>
		/// Resolve the layout-choice GUID for a record whose detail layout is type-selected via
		/// a <c>layoutChoiceField</c> (e.g. RnGenericRec/Normal keyed on the record's <c>Type</c> possibility).
		/// Returns the chosen possibility's GUID string, or null when there is no choice field / no value /
		/// the field is not an atomic object reference -- mirroring legacy DataTree, which then
		/// falls back to
		/// the choiceGuid-less layout variant.
		/// </summary>
		internal static string ResolveLayoutChoiceGuid(LcmCache cache, ICmObject obj, string layoutChoiceField)
		{
			if (obj == null || string.IsNullOrEmpty(layoutChoiceField))
				return null;
			try
			{
				var mdc = (IFwMetaDataCacheManaged)cache.DomainDataByFlid.MetaDataCache;
				var flid = mdc.GetFieldId2(obj.ClassID, layoutChoiceField, true);
				if (flid == 0)
					return null;
				var targetHvo = cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid);
				if (targetHvo == 0 || !cache.ServiceLocator.IsValidObjectId(targetHvo))
					return null;
				return cache.ServiceLocator.GetObject(targetHvo).Guid.ToString();
			}
			catch (Exception)
			{
				// A non-atomic field (or any metadata mismatch) just means "no choice" ->
				// choiceGuid-less layout.
				return null;
			}
		}

		internal static ViewDefinitionModel CompileForObject(LcmCache cache, ICmObject obj, string layoutName)
			=> CompileForObject(cache, obj, layoutName, null, null);

		internal static ViewDefinitionModel CompileForObject(LcmCache cache, ICmObject obj, string layoutName,
			ViewDefinitionOverrideResolver overrides)
			=> CompileForObject(cache, obj, layoutName, null, overrides);

		/// <summary>
		/// Compiles (with the legacy base-class walk) and, when <paramref name="overrides"/> supplies a
		/// per-project patch for the resulting (class, layout), returns the patched model
		/// CRITICAL: the cache (<see cref="CompilerSources.CompiledModels"/>) holds
		/// the SHIPPED model only -- the override is applied on the way OUT to a fresh copy
		/// (<see cref="ViewDefinitionOverrideApplier.Apply"/> is pure), so a patched project never poisons
		/// the process-wide cache that other projects/classes read.
		/// </summary>
		internal static ViewDefinitionModel CompileForObject(LcmCache cache, ICmObject obj, string layoutName,
			string choiceGuid, ViewDefinitionOverrideResolver overrides)
		{
			var sources = GetSources();
			if (sources == null)
				return null;

			var shipped = sources.CompiledModels.GetOrAdd((obj.ClassID, layoutName, choiceGuid ?? string.Empty),
				key => CompileForClass(cache, key.ClassId, key.LayoutName, key.ChoiceGuid, sources));
			if (shipped == null || overrides == null)
				return shipped;

			// The compiled model's ClassName is the class where the layout was actually found (possibly a
			// base class of obj.ClassID); key the override by that, matching how the patch was authored.
			var patch = overrides(shipped.ClassName, shipped.LayoutName);
			return patch == null || patch.IsEmpty
				? shipped
				: ViewDefinitionOverrideApplier.Apply(shipped, patch);
		}

		private static ViewDefinitionModel CompileForClass(LcmCache cache, int classId, string layoutName,
			string choiceGuid, CompilerSources sources)
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
				// Pick the choiceGuid-matching variant (exact match, else the choiceGuid-less
				// fallback, else first) so a record's layoutChoiceField selects the right layout instead of the
				// document-first one.
				if (sources.LayoutIndex.TryGetValue((className, "detail", layoutName), out var variants))
				{
					layout = LayoutSourceLoader.SelectLayoutForChoice(variants, choiceGuid);
					if (layout != null)
						break;
				}
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
				// Which directories are searched, and in what precedence, is PartsInventory's
				// call; only resolving a subdirectory to a path stays here. A test scope may
				// substitute the whole path (OverrideSearchPath).
				var searchPath = s_searchPathOverride
					?? PartsInventory.SearchPath(FwDirectoryFinder.GetCodeSubDirectory);
				var partsXml = PartsInventory.LoadMergedPartsXml(searchPath);
				if (partsXml == null)
				{
					// Never a silent permanent failure -- log, fall back to the
					// 3-field first slice for THIS compose, and retry next time (GetSources).
					SIL.Reporting.Logger.WriteEvent(
						"DetailComposer: no merged parts XML under '" + string.Join("', '", searchPath)
						+ "'; falling back to the first slice (will retry on the next compose).");
					return null;
				}

				var layoutFiles = PartsInventory.LoadLayoutFiles(searchPath);
				return new CompilerSources
				{
					PartsXml = partsXml,
					LayoutIndex = LayoutSourceLoader.IndexLayoutsByChoice(layoutFiles)
				};
			}
			catch (Exception e)
			{
				// Never a silent permanent failure -- log, fall back to the
				// 3-field first slice for THIS compose, and retry next time (GetSources).
				SIL.Reporting.Logger.WriteError(
					"DetailComposer: failed to load layout sources; "
					+ "falling back to the first slice for this compose.", e);
				return null;
			}
		}
	}
}
