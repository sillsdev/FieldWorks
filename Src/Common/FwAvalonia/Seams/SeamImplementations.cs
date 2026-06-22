// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
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
