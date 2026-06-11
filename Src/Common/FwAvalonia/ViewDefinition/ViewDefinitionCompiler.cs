// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
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
	/// off-thread compile) satisfies task 4.6: compilation works from immutable inputs only.
	/// </summary>
	public sealed class ViewDefinitionSourceSnapshot
	{
		public ViewDefinitionSourceSnapshot(string className, string layoutType, string layoutXml, string partsXml)
		{
			ClassName = className ?? "";
			LayoutType = string.IsNullOrEmpty(layoutType) ? "detail" : layoutType;
			LayoutXml = layoutXml ?? "";
			PartsXml = partsXml ?? "";
		}

		public string ClassName { get; }

		public string LayoutType { get; }

		/// <summary>The single <c>&lt;layout&gt;</c> element source.</summary>
		public string LayoutXml { get; }

		/// <summary>The <c>&lt;PartInventory&gt;</c> (or <c>&lt;bin&gt;</c>) source.</summary>
		public string PartsXml { get; }

		/// <summary>The layout name parsed from <see cref="LayoutXml"/>.</summary>
		public string LayoutName => (string)XElement.Parse(LayoutXml).Attribute("name") ?? "";

		/// <summary>Computes a stable content fingerprint over the layout and parts source text.</summary>
		public string ComputeFingerprint()
		{
			using (var sha = SHA256.Create())
			{
				var bytes = Encoding.UTF8.GetBytes(ClassName + "\n" + LayoutType + "\n" + LayoutXml + "\n" + PartsXml);
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
			=> new ViewDefinitionCacheKey(ClassName, LayoutName, LayoutType, ComputeFingerprint());
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

	/// <summary>Simple thread-safe dictionary-backed cache.</summary>
	public sealed class ViewDefinitionCache : IViewDefinitionCache
	{
		private readonly object _gate = new object();
		private readonly Dictionary<ViewDefinitionCacheKey, ViewDefinitionModel> _map
			= new Dictionary<ViewDefinitionCacheKey, ViewDefinitionModel>();

		public bool TryGet(ViewDefinitionCacheKey key, out ViewDefinitionModel model)
		{
			lock (_gate)
			{
				return _map.TryGetValue(key, out model);
			}
		}

		public ViewDefinitionModel GetOrAdd(ViewDefinitionCacheKey key, Func<ViewDefinitionModel> factory)
		{
			lock (_gate)
			{
				if (_map.TryGetValue(key, out var existing))
				{
					return existing;
				}
			}

			// Compile outside the lock so a slow compile does not block other keys.
			var created = factory();

			lock (_gate)
			{
				if (_map.TryGetValue(key, out var raced))
				{
					return raced;
				}

				_map[key] = created;
				return created;
			}
		}

		public void Invalidate(ViewDefinitionCacheKey key)
		{
			lock (_gate)
			{
				_map.Remove(key);
			}
		}

		public void InvalidateAll()
		{
			lock (_gate)
			{
				_map.Clear();
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
			var layout = XElement.Parse(snapshot.LayoutXml);
			var parts = new DictionaryPartResolver(XElement.Parse(snapshot.PartsXml));
			cancellationToken.ThrowIfCancellationRequested();
			return _importer.Import(layout, parts);
		}
	}
}
