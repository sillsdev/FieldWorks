// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// An immutable snapshot of the XML source needed to compile one view definition. Taking this
	/// snapshot up front (rather than reading live <c>Inventory</c>/<c>PropertyTable</c> state during an
	/// off-thread compile) means compilation works from immutable inputs only.
	/// </summary>
	public sealed class ViewDefinitionSourceSnapshot
	{
		private readonly XElement _layoutElement;
		private readonly ViewDefinitionIdentity _requestedIdentity;
		private readonly ViewDefinitionIdentity _resolvedIdentity;
		private readonly Lazy<string> _fingerprint;

		/// <summary>Creates a snapshot without an explicit requested layout identity.</summary>
		public ViewDefinitionSourceSnapshot(string className, string layoutType, string layoutXml,
			string partsXml, IReadOnlyDictionary<string, string> baseClassMap)
			: this(className, layoutType, layoutXml, partsXml, baseClassMap, null, null)
		{
		}

		/// <summary>Creates a snapshot with optional requested layout identity
		/// metadata.</summary>
		public ViewDefinitionSourceSnapshot(string className, string layoutType, string layoutXml, string partsXml,
			IReadOnlyDictionary<string, string> baseClassMap = null,
			string requestedLayoutName = null, string requestedChoiceGuid = null)
		{
			ClassName = className ?? "";
			LayoutType = string.IsNullOrEmpty(layoutType) ? "detail" : layoutType;
			LayoutXml = layoutXml ?? "";
			PartsXml = partsXml ?? "";
			_layoutElement = XElement.Parse(LayoutXml);
			BaseClassMap = CopyBaseClassMap(baseClassMap);

			var resolvedLayoutName = (string)_layoutElement.Attribute("name") ?? "";
			var resolvedClassName = (string)_layoutElement.Attribute("class") ?? ClassName;
			var resolvedLayoutType = (string)_layoutElement.Attribute("type") ?? LayoutType;
			var resolvedChoiceGuid = (string)_layoutElement.Attribute("choiceGuid");
			RequestedLayoutName = requestedLayoutName ?? resolvedLayoutName;
			RequestedChoiceGuid = requestedChoiceGuid;
			ResolvedLayoutName = resolvedLayoutName;
			ResolvedClassName = resolvedClassName;
			ResolvedLayoutType = resolvedLayoutType;
			ResolvedChoiceGuid = resolvedChoiceGuid;
			_requestedIdentity = new ViewDefinitionIdentity(ClassName, LayoutType,
				RequestedLayoutName, RequestedChoiceGuid);
			_resolvedIdentity = new ViewDefinitionIdentity(ResolvedClassName, ResolvedLayoutType,
				ResolvedLayoutName, ResolvedChoiceGuid);
			CustomFieldsExpanded = false;
			_fingerprint = new Lazy<string>(ComputeFingerprintCore,
				LazyThreadSafetyMode.ExecutionAndPublication);
		}

		/// <summary>Creates a snapshot with optional requested layout identity and expansion
		/// metadata.</summary>
		public ViewDefinitionSourceSnapshot(string className, string layoutType, string layoutXml, string partsXml,
			IReadOnlyDictionary<string, string> baseClassMap, string requestedLayoutName,
			string requestedChoiceGuid, bool customFieldsExpanded)
			: this(className, layoutType, layoutXml, partsXml, baseClassMap, requestedLayoutName,
				requestedChoiceGuid)
		{
			CustomFieldsExpanded = customFieldsExpanded;
		}

		public string ClassName { get; }

		public string LayoutType { get; }

		/// <summary>The single <c>&lt;layout&gt;</c> element source.</summary>
		public string LayoutXml { get; }

		/// <summary>The <c>&lt;PartInventory&gt;</c> (or <c>&lt;bin&gt;</c>) source.</summary>
		public string PartsXml { get; }

		/// <summary>Optional subclass -> base class chain used for part-ref resolution
		/// fallback.</summary>
		public IReadOnlyDictionary<string, string> BaseClassMap { get; }

		/// <summary>The layout name parsed from <see cref="LayoutXml"/>.</summary>
		public string LayoutName => ResolvedLayoutName;

		/// <summary>The resolved class parsed from the selected layout XML.</summary>
		public string ResolvedClassName { get; }

		/// <summary>The resolved type parsed from the selected layout XML.</summary>
		public string ResolvedLayoutType { get; }

		/// <summary>The requested layout name before class/default fallback.</summary>
		public string RequestedLayoutName { get; }

		/// <summary>The requested nullable layout choice before variant fallback.</summary>
		public string RequestedChoiceGuid { get; }

		/// <summary>The effective layout name parsed from <see cref="LayoutXml"/>.</summary>
		public string ResolvedLayoutName { get; }

		/// <summary>The effective nullable layout choice parsed from <see
		/// cref="LayoutXml"/>.</summary>
		public string ResolvedChoiceGuid { get; }

		/// <summary>The complete identity requested from the inventory.</summary>
		public ViewDefinitionIdentity RequestedIdentity => _requestedIdentity;

		/// <summary>The complete identity selected by the inventory, including base-class
		/// fallback.</summary>
		public ViewDefinitionIdentity ResolvedIdentity => _resolvedIdentity;

		/// <summary>The effective nullable layout choice parsed from <see
		/// cref="LayoutXml"/>.</summary>
		public string ChoiceGuid => ResolvedChoiceGuid;

		/// <summary>Whether custom fields have already been expanded into this snapshot's
		/// layout.</summary>
		public bool CustomFieldsExpanded { get; private set; }

		/// <summary>Computes a stable content fingerprint over the layout and parts source text.</summary>
		public string ComputeFingerprint() => _fingerprint.Value;

		private string ComputeFingerprintCore()
		{
			using (var sha = SHA256.Create())
			{
				var baseMapText = BaseClassMap == null
					? ""
					: string.Join(";", BaseClassMap.OrderBy(p => NormalizeIdentity(p.Key), StringComparer.Ordinal)
						.Select(p => NormalizeIdentity(p.Key) + ">" + NormalizeIdentity(p.Value)));
				var identityText = string.Join("\n", new[]
				{
					NormalizeIdentity(ClassName), NormalizeIdentity(LayoutType),
					NormalizeIdentity(RequestedLayoutName), NormalizeIdentity(RequestedChoiceGuid),
					NormalizeIdentity(ResolvedClassName), NormalizeIdentity(ResolvedLayoutType),
					NormalizeIdentity(ResolvedLayoutName), NormalizeIdentity(ResolvedChoiceGuid)
				});
				var bytes = Encoding.UTF8.GetBytes(
					identityText + "\n" + LayoutXml + "\n" + PartsXml + "\n" + baseMapText);
				var hash = sha.ComputeHash(bytes);
				var sb = new StringBuilder(hash.Length * 2);
				foreach (var b in hash)
				{
					sb.Append(b.ToString("x2"));
				}

				return sb.ToString();
			}
		}

		/// <summary>Builds the cache key for this snapshot.</summary>
		public ViewDefinitionCacheKey ToKey()
			=> new ViewDefinitionCacheKey(_requestedIdentity.ClassName, _requestedIdentity.LayoutName,
				_requestedIdentity.LayoutType, _requestedIdentity.ChoiceGuid, _fingerprint.Value);

		private static string NormalizeIdentity(string value)
			=> value == null ? "<null>" : value.ToUpperInvariant();

		private static IReadOnlyDictionary<string, string> CopyBaseClassMap(
			IReadOnlyDictionary<string, string> baseClassMap)
		{
			if (baseClassMap == null)
				return null;

			var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var pair in baseClassMap)
				copy.Add(pair.Key, pair.Value);
			return new ReadOnlyDictionary<string, string>(copy);
		}

		internal XElement CreateLayoutElement() => new XElement(_layoutElement);
	}

	/// <summary>A thread-safe cache of compiled view definitions keyed by content fingerprint.</summary>
	public interface IViewDefinitionCache
	{
		bool TryGet(ViewDefinitionCacheKey key, out ViewDefinitionModel model);

		ViewDefinitionModel GetOrAdd(ViewDefinitionCacheKey key, Func<ViewDefinitionModel> factory);

		void Invalidate(ViewDefinitionCacheKey key);

		void InvalidateAll();

		int Count { get; }
	}

	/// <summary>Simple thread-safe FIFO-bounded cache.</summary>
	public sealed class ViewDefinitionCache : IViewDefinitionCache
	{
		/// <summary>Default maximum number of compiled definitions retained by a cache.</summary>
		public const int DefaultCapacity = 256;

		private sealed class CacheEntry
		{
			public CacheEntry(Lazy<ViewDefinitionModel> value, LinkedListNode<ViewDefinitionCacheKey> orderNode)
			{
				Value = value;
				OrderNode = orderNode;
			}

			public Lazy<ViewDefinitionModel> Value { get; }

			public LinkedListNode<ViewDefinitionCacheKey> OrderNode { get; }
		}

		private readonly object _gate = new object();
		private readonly Dictionary<ViewDefinitionCacheKey, CacheEntry> _map
			= new Dictionary<ViewDefinitionCacheKey, CacheEntry>();
		private readonly LinkedList<ViewDefinitionCacheKey> _insertionOrder
			= new LinkedList<ViewDefinitionCacheKey>();

		/// <summary>Creates a cache with the default maximum capacity.</summary>
		public ViewDefinitionCache() : this(DefaultCapacity)
		{
		}

		/// <summary>Creates a cache that retains at most <paramref name="capacity"/>
		/// entries.</summary>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not
		/// positive.</exception>
		public ViewDefinitionCache(int capacity)
		{
			if (capacity <= 0)
				throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
					"The view definition cache capacity must be positive.");

			Capacity = capacity;
		}

		/// <summary>The maximum number of compiled definitions retained by this cache.</summary>
		public int Capacity { get; }

		public bool TryGet(ViewDefinitionCacheKey key, out ViewDefinitionModel model)
		{
			CacheEntry entry;
			lock (_gate)
			{
				if (!_map.TryGetValue(key, out entry) || !entry.Value.IsValueCreated)
				{
					model = null;
					return false;
				}
			}

			try
			{
				model = entry.Value.Value;
				return true;
			}
			catch
			{
				RemoveFailedEntry(key, entry);
				model = null;
				return false;
			}
		}

		public ViewDefinitionModel GetOrAdd(ViewDefinitionCacheKey key, Func<ViewDefinitionModel> factory)
		{
			CacheEntry entry;
			lock (_gate)
			{
				if (!_map.TryGetValue(key, out entry))
				{
					if (factory == null)
						throw new ArgumentNullException(nameof(factory));

					if (_map.Count >= Capacity)
						EvictOldest();

					var value = new Lazy<ViewDefinitionModel>(factory,
						LazyThreadSafetyMode.ExecutionAndPublication);
					var orderNode = _insertionOrder.AddLast(key);
					entry = new CacheEntry(value, orderNode);
					_map[key] = entry;
				}
			}

			try
			{
				return entry.Value.Value;
			}
			catch
			{
				RemoveFailedEntry(key, entry);
				throw;
			}
		}

		public void Invalidate(ViewDefinitionCacheKey key)
		{
			lock (_gate)
			{
				if (_map.TryGetValue(key, out var entry))
				{
					_map.Remove(key);
					_insertionOrder.Remove(entry.OrderNode);
				}
			}
		}

		public void InvalidateAll()
		{
			lock (_gate)
			{
				_map.Clear();
				_insertionOrder.Clear();
			}
		}

		public int Count
		{
			get
			{
				lock (_gate)
				{
					return _map.Count;
				}
			}
		}

		private void EvictOldest()
		{
			var oldest = _insertionOrder.First;
			if (oldest == null)
				return;

			_insertionOrder.RemoveFirst();
			_map.Remove(oldest.Value);
		}

		private void RemoveFailedEntry(ViewDefinitionCacheKey key, CacheEntry entry)
		{
			lock (_gate)
			{
				if (_map.TryGetValue(key, out var current) && ReferenceEquals(current, entry))
				{
					_map.Remove(key);
					_insertionOrder.Remove(entry.OrderNode);
				}
			}
		}
	}

	/// <summary>
	/// Compiles <see cref="ViewDefinitionSourceSnapshot"/>s into <see cref="ViewDefinitionModel"/>s via
	/// the <see cref="XmlLayoutImporter"/>, caching by content fingerprint and supporting cancellable
	/// off-thread compilation over immutable snapshots.
	/// </summary>
	public sealed class ViewDefinitionCompiler
	{
		private readonly IViewDefinitionImporter _importer;
		private readonly IViewDefinitionCache _cache;

		public ViewDefinitionCompiler(IViewDefinitionImporter importer = null, IViewDefinitionCache cache = null)
		{
			_importer = importer ?? new XmlLayoutImporter();
			_cache = cache ?? new ViewDefinitionCache();
		}

		public IViewDefinitionCache Cache => _cache;

		/// <summary>Compiles synchronously, returning a cached result when the fingerprint matches.</summary>
		public ViewDefinitionModel Compile(ViewDefinitionSourceSnapshot snapshot)
		{
			var key = snapshot.ToKey();
			return _cache.GetOrAdd(key, () => CompileCore(snapshot, CancellationToken.None));
		}

		/// <summary>
		/// Compiles off-thread over the immutable snapshot. Honors cancellation and returns the cached
		/// result when available.
		/// </summary>
		public Task<ViewDefinitionModel> CompileAsync(ViewDefinitionSourceSnapshot snapshot, CancellationToken cancellationToken)
		{
			var key = snapshot.ToKey();
			if (_cache.TryGet(key, out var cached))
			{
				return Task.FromResult(cached);
			}

			return Task.Run(() =>
			{
				cancellationToken.ThrowIfCancellationRequested();
				return _cache.GetOrAdd(key, () => CompileCore(snapshot, cancellationToken));
			}, cancellationToken);
		}

		private ViewDefinitionModel CompileCore(ViewDefinitionSourceSnapshot snapshot, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var layout = snapshot.CreateLayoutElement();
			var parts = new DictionaryPartResolver(XElement.Parse(snapshot.PartsXml), snapshot.BaseClassMap);
			cancellationToken.ThrowIfCancellationRequested();
			var imported = _importer.Import(layout, parts, snapshot.ClassName);
			return imported.WithLayoutIdentities(snapshot.RequestedIdentity, snapshot.ResolvedIdentity);
		}
	}
}
