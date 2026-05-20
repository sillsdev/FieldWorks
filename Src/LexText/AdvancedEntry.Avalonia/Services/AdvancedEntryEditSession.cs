using System;
using System.Diagnostics;
using SIL.LCModel;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Services;

/// <summary>
/// Controls an Advanced Entry edit session using a long-lived LCModel undo task.
///
/// Rules:
/// - Begin starts an outer undo task (depth must be 0).
/// - Save ends the undo task and explicitly commits.
/// - Cancel/Dispose rolls back to depth 0 and does not commit.
/// </summary>
public sealed class AdvancedEntryEditSession : IDisposable
{
	private readonly LcmCache _cache;
	private readonly AdvancedEntryCommitFence _commitFence;
	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
	private bool _completed;
	private bool _saved;

	public AdvancedEntryEditSession(LcmCache cache, string undoText, string redoText)
	{
		_cache = cache ?? throw new ArgumentNullException(nameof(cache));

		var actionHandler = _cache.ActionHandlerAccessor;
		if (actionHandler.CurrentDepth != 0)
		{
			throw new InvalidOperationException(
				$"Cannot begin Advanced Entry edit session when an undo task is already active. CurrentDepth={actionHandler.CurrentDepth}.");
		}

		_commitFence = new AdvancedEntryCommitFence(_cache);

		actionHandler.BeginUndoTask(undoText, redoText);
		AdvancedEntryTrace.Info("AdvancedEntry edit session started");
	}

	public TimeSpan Duration => _stopwatch.Elapsed;

	public void Save()
	{
		EnsureNotCompleted();

		try
		{
			_commitFence.RunUnfenced(() =>
			{
				_cache.ActionHandlerAccessor.EndUndoTask();
				_cache.ActionHandlerAccessor.Commit();
			});
			_saved = true;
			AdvancedEntryTrace.Info($"AdvancedEntry edit session saved (Duration={Duration.TotalMilliseconds:0} ms)");
		}
		catch (Exception ex)
		{
			AdvancedEntryTrace.Error("AdvancedEntry edit session save failed; rolling back", ex);
			SafeRollback();
			throw;
		}
		finally
		{
			_commitFence.Dispose();
			_completed = true;
		}
	}

	public void Cancel()
	{
		EnsureNotCompleted();
		SafeRollback();
		_completed = true;
		_commitFence.Dispose();
		AdvancedEntryTrace.Info($"AdvancedEntry edit session canceled (Duration={Duration.TotalMilliseconds:0} ms)");
	}

	public void Dispose()
	{
		if (_completed)
			return;

		// Default-safe behavior: if the session goes out of scope without Save, roll it back.
		try
		{
			SafeRollback();
		}
		catch (Exception ex)
		{
			AdvancedEntryTrace.Error("AdvancedEntry edit session rollback failed during dispose", ex);
		}
		finally
		{
			_commitFence.Dispose();
			_completed = true;
		}
	}

	private void EnsureNotCompleted()
	{
		if (_completed)
			throw new InvalidOperationException("The edit session has already completed.");
	}

	private void SafeRollback()
	{
		if (_saved)
			return;

		var actionHandler = _cache.ActionHandlerAccessor;
		if (actionHandler.CurrentDepth == 0)
			return;

		// Roll back all nested/outer work to a clean state.
		actionHandler.Rollback(0);
	}
}
