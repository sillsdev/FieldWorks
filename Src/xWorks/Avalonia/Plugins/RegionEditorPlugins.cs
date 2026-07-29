// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The one plugin contract for every remaining custom
	/// editor: builds an Avalonia control for (object, node, edit context) so a legacy dynamically
	/// loaded slice (<c>editor="Custom" class=...</c>) can render in-tree at the slice's real
	/// position instead of an unsupported row. Plugins are keyed by
	/// the <b>legacy layout identity</b> (<see cref="LegacyClassName"/>, the layout's `class=`
	/// attribute already carried on the typed node) — zero layout edits per migration.
	/// </summary>
	public interface IRegionEditorPlugin
	{
		/// <summary>
		/// The fully qualified legacy slice class this plugin claims (the layout `class=`
		/// attribute, e.g. <c>SIL.FieldWorks.XWorks.LexEd.MessageSlice</c>).
		/// </summary>
		string LegacyClassName { get; }

		/// <summary>
		/// Builds the Avalonia control that replaces the legacy slice for one composed row. Invoked
		/// lazily by the view (never during compose); the context carries the region's own edit
		/// context so plugin edits ride the same fenced session as every other row, plus the
		/// optional host services. This is the ONE plugin contract — there is no separate
		/// service-aware marker interface or overload.
		/// </summary>
		Control BuildControl(RegionEditorBuildContext context);
	}

	/// <summary>
	/// Everything the composer hands a plugin factory, bundled into one contract:
	/// the row's object and typed node, the region's edit context (resolved lazily through the
	/// composer's deferred accessor — the context object is created during compose, BEFORE the
	/// edit context exists; plugin factories run at render time, after), and the cache.
	/// </summary>
	public sealed class RegionEditorBuildContext
	{
		private readonly Func<IDetailEditContext> _editContextAccessor;

		public RegionEditorBuildContext(ICmObject target, ViewNode node,
			Func<IDetailEditContext> editContextAccessor, LcmCache cache)
		{
			Target = target;
			Node = node;
			_editContextAccessor = editContextAccessor;
			Cache = cache;
		}

		/// <summary>The composed row's own object (the slice's object in legacy terms).</summary>
		public ICmObject Target { get; }

		/// <summary>The row's typed view node (layout identity, field, label, menu bindings).</summary>
		public ViewNode Node { get; }

		/// <summary>The region's edit context, resolved on read (null until the region composed).</summary>
		public IDetailEditContext EditContext => _editContextAccessor?.Invoke();

		public LcmCache Cache { get; }
	}

	/// <summary>
	/// Maps legacy slice class names to their
	/// <see cref="IRegionEditorPlugin"/>. The composer consults <see cref="Resolve"/> per node
	/// while walking, FIRST in the resolution order (plugin → unsupported row).
	/// Thread-safe by immutable snapshot: registration copies under a lock, resolution reads the
	/// current snapshot without one, so a compose mid-registration sees a coherent table.
	/// </summary>
	public sealed class RegionEditorPluginRegistry
	{
		private readonly object _sync = new object();
		private volatile IReadOnlyDictionary<string, IRegionEditorPlugin> _snapshot
			= new Dictionary<string, IRegionEditorPlugin>(StringComparer.Ordinal);

		/// <summary>The process-wide registry the composer uses unless a caller supplies its own.</summary>
		public static RegionEditorPluginRegistry Default { get; } = CreateDefault();

		/// <summary>
		/// Registers a plugin for its <see cref="IRegionEditorPlugin.LegacyClassName"/>. A legacy
		/// class has exactly one owner: re-registering an already-claimed class throws, so two
		/// migrations cannot silently fight over a slice.
		/// </summary>
		public void Register(IRegionEditorPlugin plugin)
		{
			if (plugin == null)
				throw new ArgumentNullException(nameof(plugin));
			if (string.IsNullOrEmpty(plugin.LegacyClassName))
				throw new ArgumentException("A region editor plugin must claim a legacy class name (D1).",
					nameof(plugin));
			lock (_sync)
			{
				if (_snapshot.ContainsKey(plugin.LegacyClassName))
					throw new ArgumentException(
						$"'{plugin.LegacyClassName}' is already claimed by another plugin.", nameof(plugin));
				var next = new Dictionary<string, IRegionEditorPlugin>((IDictionary<string, IRegionEditorPlugin>)_snapshot,
					StringComparer.Ordinal)
				{
					[plugin.LegacyClassName] = plugin
				};
				_snapshot = next;
			}
		}

		/// <summary>The plugin claiming the legacy class, or null when the class is unclaimed.</summary>
		public IRegionEditorPlugin Resolve(string legacyClassName)
		{
			if (string.IsNullOrEmpty(legacyClassName))
				return null;
			return _snapshot.TryGetValue(legacyClassName, out var plugin) ? plugin : null;
		}

		/// <summary>The currently claimed legacy class names (a snapshot).</summary>
		public IReadOnlyCollection<string> RegisteredClassNames
		{
			get
			{
				var snapshot = _snapshot;
				return new List<string>(snapshot.Keys);
			}
		}

		private static RegionEditorPluginRegistry CreateDefault()
		{
			var registry = new RegionEditorPluginRegistry();
			RegisterBuiltins(registry);
			return registry;
		}

		// The builtin plugin list. The Reversal Entries slice
		// (ReversalIndexEntrySlice) composes as a native Avalonia editable multi-WS text field through
		// the plugin route. Every OTHER custom slice not absorbed by a composer route resolves to the
		// labeled Unsupported worklist row.
		internal static void RegisterBuiltins(RegionEditorPluginRegistry registry)
		{
			registry.Register(new ReversalIndexEntryPlugin());
		}
	}
}
