// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Aggregates teardown work so a control disposes every handler/subscription it wired in one call.
	/// <see cref="Add(Action)"/> registers a teardown action; <see cref="Dispose"/> runs each once (in
	/// registration order) and is idempotent. <see cref="Count"/> reports the outstanding teardowns and
	/// drops to zero after disposal, so a control can surface it for recycling assertions.
	/// A local type: Avalonia 11 on net48 exposes no CompositeDisposable and the repo pulls in no
	/// System.Reactive, so this keeps the dependency surface unchanged.
	/// </summary>
	public sealed class CompositeDisposable : IDisposable
	{
		private readonly List<IDisposable> _disposables = new List<IDisposable>();
		private bool _disposed;

		/// <summary>The number of teardowns still registered; zero once disposed.</summary>
		public int Count => _disposables.Count;

		/// <summary>Registers a teardown action. Runs immediately if this composite is already disposed.</summary>
		public void Add(Action disposeAction)
		{
			if (disposeAction != null)
				Add(new ActionDisposable(disposeAction));
		}

		/// <summary>Registers a disposable. Disposes it immediately if this composite is already disposed.</summary>
		public void Add(IDisposable disposable)
		{
			if (disposable == null)
				return;
			if (_disposed)
			{
				disposable.Dispose();
				return;
			}
			_disposables.Add(disposable);
		}

		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			foreach (var disposable in _disposables)
				disposable.Dispose();
			_disposables.Clear();
		}

		private sealed class ActionDisposable : IDisposable
		{
			private Action _action;

			public ActionDisposable(Action action) => _action = action;

			public void Dispose()
			{
				var action = _action;
				_action = null;
				action?.Invoke();
			}
		}
	}
}
