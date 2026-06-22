// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// Editor registry that resolves editor keys to handler tokens, with an optional fallback used for
	/// unregistered keys (the legacy slice path during migration). Framework-neutral: handlers are
	/// opaque objects so this can sit in front of either WinForms slices or Avalonia editors.
	/// </summary>
	public sealed class LexicalEditorRegistry : ILexicalEditorRegistry
	{
		private readonly Dictionary<string, object> _handlers = new Dictionary<string, object>(StringComparer.Ordinal);
		private readonly object _fallback;

		public LexicalEditorRegistry(object fallbackHandler = null)
		{
			_fallback = fallbackHandler;
		}

		public void Register(string editorKey, object handler)
		{
			if (string.IsNullOrEmpty(editorKey))
			{
				throw new ArgumentException("Editor key is required.", nameof(editorKey));
			}

			_handlers[editorKey] = handler ?? throw new ArgumentNullException(nameof(handler));
		}

		public object Resolve(string editorKey)
		{
			if (!string.IsNullOrEmpty(editorKey) && _handlers.TryGetValue(editorKey, out var handler))
			{
				return handler;
			}

			return _fallback;
		}

		public bool IsRegistered(string editorKey)
			=> !string.IsNullOrEmpty(editorKey) && _handlers.ContainsKey(editorKey);
	}

	/// <summary>
	/// In-memory property/state store seam. A faithful stand-in for the xCore PropertyTable surface the
	/// editors need (get/set/remove typed values), with no WinForms dependency.
	/// </summary>
	public sealed class InMemoryPropertyStateStore : IPropertyStateStore
	{
		private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.Ordinal);

		public bool TryGet<T>(string key, out T value)
		{
			if (_values.TryGetValue(key, out var raw) && raw is T typed)
			{
				value = typed;
				return true;
			}

			value = default;
			return false;
		}

		public void Set<T>(string key, T value)
		{
			_values[key] = value;
		}

		public bool Remove(string key) => _values.Remove(key);
	}

	/// <summary>
	/// UI scheduler that runs work synchronously. Used by non-view layers in tests and the preview host;
	/// the live app supplies an Avalonia-dispatcher-backed scheduler at the view edge.
	/// </summary>
	public sealed class ImmediateUiScheduler : IUiScheduler
	{
		public bool IsOnUiThread => true;

		public void Post(Action action) => action?.Invoke();
	}

	/// <summary>
	/// Region lifetime that disposes registered disposables exactly once, in reverse registration order.
	/// </summary>
	public sealed class RegionLifetime : IRegionLifetime
	{
		private readonly List<IDisposable> _disposables = new List<IDisposable>();

		public bool IsDisposed { get; private set; }

		public void Register(IDisposable disposable)
		{
			if (disposable == null)
			{
				throw new ArgumentNullException(nameof(disposable));
			}

			if (IsDisposed)
			{
				// Late registration after disposal: dispose immediately to avoid leaks.
				disposable.Dispose();
				return;
			}

			_disposables.Add(disposable);
		}

		public void Dispose()
		{
			if (IsDisposed)
			{
				return;
			}

			IsDisposed = true;
			for (var i = _disposables.Count - 1; i >= 0; i--)
			{
				_disposables[i].Dispose();
			}

			_disposables.Clear();
		}
	}
}
