// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LanguageExplorer
{
	/// <summary>
	/// A set that collects objects for later disposal
	/// </summary>
	internal class DisposableObjectsSet<T> : IDisposable where T : class
	{
		/// <summary />
		protected readonly HashSet<IDisposable> _objectsToDispose = new HashSet<IDisposable>();

		#region Disposable stuff
#if DEBUG
		/// <summary />
		~DisposableObjectsSet()
		{
			Dispose(false);
		}
#endif

		/// <summary />
		private bool IsDisposed { get; set; }

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary />
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// dispose managed and unmanaged objects
				DisposeAllObjects();
			}

			IsDisposed = true;
		}
		#endregion

		/// <summary>
		/// Disposes all objects in the set, but doesn't dispose the set itself.
		/// </summary>
		private void DisposeAllObjects()
		{
			foreach (var disposable in _objectsToDispose)
			{
				disposable.Dispose();
			}

			_objectsToDispose.Clear();
		}

		/// <summary>
		/// Registers an object for later disposal. If the object is already registered or
		/// doesn't implement IDisposable nothing happens.
		/// </summary>
		internal void Add(T obj)
		{
			if (!(obj is IDisposable disposable))
			{
				return;
			}
			_objectsToDispose.Add(disposable);
		}

		/// <summary>
		/// Returns <c>true</c> if obj is contained in the set.
		/// </summary>
		internal bool Contains(T obj)
		{
			return _objectsToDispose.Any() && obj is IDisposable disposable && _objectsToDispose.Contains(disposable);
		}

		/// <summary>
		/// Removes an object from the set of objects that need to be disposed.
		/// If the object doesn't implement IDisposable or isn't registered
		/// for disposal, nothing happens.
		/// </summary>
		internal bool Remove(T obj)
		{
			return obj is IDisposable disposable && _objectsToDispose.Remove(disposable);
		}
	}
}