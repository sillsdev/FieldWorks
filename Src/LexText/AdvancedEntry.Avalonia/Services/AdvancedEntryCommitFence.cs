using System;
using System.Reflection;
using System.Threading;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Services;

/// <summary>
/// Installs an <see cref="IActionHandler"/> proxy that blocks <c>Commit()</c> and (most)
/// <c>EndUndoTask()</c> calls while an Advanced Entry edit session is active.
///
/// This is a defense-in-depth guardrail to ensure that cancel/crash paths don't
/// accidentally persist changes due to unrelated code paths committing.
/// </summary>
internal sealed class AdvancedEntryCommitFence : IDisposable
{
	private static readonly AsyncLocal<int> s_allowUnsafeDepth = new();

	private readonly LcmCache _cache;
	private readonly IActionHandler _original;
	private readonly IActionHandler _installed;
	private readonly bool _isInstalled;
	private bool _disposed;

	public AdvancedEntryCommitFence(LcmCache cache)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		_original = _cache.DomainDataByFlid.GetActionHandler();

		_installed = CommitFenceActionHandlerProxy.Create(
			_original,
			ShouldAllowCall,
			GetUndoDepth
		);

		try
		{
			_cache.DomainDataByFlid.SetActionHandler(_installed);
			_isInstalled = true;
		}
		catch (NotSupportedException)
		{
			// Some cache/data-access implementations (notably memory-only) don't support swapping
			// the action handler. In that scenario, we fall back to a no-op fence.
			_isInstalled = false;
		}
	}

	public void Dispose()
	{
		if (_disposed)
			return;
		_disposed = true;

		if (_isInstalled)
		{
			try
			{
				_cache.DomainDataByFlid.SetActionHandler(_original);
			}
			catch (NotSupportedException)
			{
				// Ignore: if swapping isn't supported, there is nothing to restore.
			}
		}
	}

	public void RunUnfenced(Action action)
	{
		if (action is null)
			throw new ArgumentNullException(nameof(action));

		s_allowUnsafeDepth.Value++;
		try
		{
			action();
		}
		finally
		{
			s_allowUnsafeDepth.Value--;
		}
	}

	private bool ShouldAllowCall(string methodName)
	{
		if (s_allowUnsafeDepth.Value > 0)
			return true;

		return false;
	}

	private int GetUndoDepth() => _cache.ActionHandlerAccessor.CurrentDepth;

	private class CommitFenceActionHandlerProxy : DispatchProxy
	{
		private IActionHandler? _inner;
		private Func<string, bool>? _allow;
		private Func<int>? _getUndoDepth;

		public static IActionHandler Create(
			IActionHandler inner,
			Func<string, bool> allow,
			Func<int> getUndoDepth
		)
		{
			var proxy = Create<IActionHandler, CommitFenceActionHandlerProxy>();
			((CommitFenceActionHandlerProxy)(object)proxy).Initialize(inner, allow, getUndoDepth);
			return proxy;
		}

		private void Initialize(
			IActionHandler inner,
			Func<string, bool> allow,
			Func<int> getUndoDepth
		)
		{
			_inner = inner;
			_allow = allow;
			_getUndoDepth = getUndoDepth;
		}

		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
		{
			if (targetMethod is null)
				throw new ArgumentNullException(nameof(targetMethod));
			if (_inner is null || _allow is null || _getUndoDepth is null)
				throw new InvalidOperationException("Proxy not initialized.");

			var name = targetMethod.Name;

			// Commit is the persistence boundary. Block unless explicitly allowed.
			if (string.Equals(name, nameof(IActionHandler.Commit), StringComparison.Ordinal))
			{
				if (!_allow(name))
					throw new InvalidOperationException("Commit is blocked while Advanced Entry edit session is active.");
			}

			// Prevent other code paths from ending the outer undo task.
			if (string.Equals(name, nameof(IActionHandler.EndUndoTask), StringComparison.Ordinal))
			{
				var depth = _getUndoDepth();
				var wouldEndOuterTask = depth <= 1;
				if (wouldEndOuterTask && !_allow(name))
					throw new InvalidOperationException("EndUndoTask is blocked while Advanced Entry edit session is active.");
			}

			return targetMethod.Invoke(_inner, args);
		}
	}
}
