// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Saves and unlocks the project, runs Motif once, then either relocks the project or
	/// reopens it from disk. Once the project is unlocked, every failure is returned as a
	/// result instead of thrown.
	/// </summary>
	internal sealed class MotifApplyWorkflow
	{
		private readonly Action _validatePendingEdits;
		private readonly Action _saveAndUnlockProject;
		private readonly Func<MotifApplyResult> _runMotif;
		private readonly Func<Form> _reloadProject;
		private readonly Action _relockProject;
		private readonly Func<bool> _projectFileExists;

		internal MotifApplyWorkflow(Action validatePendingEdits, Action saveAndUnlockProject,
			Func<MotifApplyResult> runMotif, Func<Form> reloadProject, Action relockProject,
			Func<bool> projectFileExists)
		{
			_validatePendingEdits = validatePendingEdits ?? throw new ArgumentNullException(nameof(validatePendingEdits));
			_saveAndUnlockProject = saveAndUnlockProject ?? throw new ArgumentNullException(nameof(saveAndUnlockProject));
			_runMotif = runMotif ?? throw new ArgumentNullException(nameof(runMotif));
			_reloadProject = reloadProject ?? throw new ArgumentNullException(nameof(reloadProject));
			_relockProject = relockProject ?? throw new ArgumentNullException(nameof(relockProject));
			_projectFileExists = projectFileExists ?? throw new ArgumentNullException(nameof(projectFileExists));
		}

		internal MotifApplyWorkflowResult Execute()
		{
			_validatePendingEdits();
			try
			{
				_saveAndUnlockProject();
			}
			catch (Exception error)
			{
				return new MotifApplyWorkflowResult(null, false, false, MotifApplyMessage.PreparationFailed,
					null, error);
			}

			MotifApplyResult result;
			try
			{
				result = _runMotif() ?? Ambiguous();
			}
			catch (Exception error)
			{
				Logger.WriteError("Running Motif failed", error);
				result = Ambiguous();
			}

			// Relocking or reopening while Motif may still write the file would let the two
			// programs overwrite each other's changes.
			if (result.Outcome == MotifApplyOutcome.StillRunning)
				return new MotifApplyWorkflowResult(result, false, false, MotifApplyMessage.MotifStillRunning,
					null, null);

			bool projectFileExists;
			try
			{
				projectFileExists = _projectFileExists();
			}
			catch (Exception error)
			{
				return new MotifApplyWorkflowResult(result, false, false, MotifApplyMessage.RecoveryFailed,
					null, error);
			}
			if (!projectFileExists)
				return new MotifApplyWorkflowResult(result, false, false, MotifApplyMessage.MissingProjectFile,
					null, null);

			Exception relockError = null;
			if (!result.RequiresReload)
			{
				try
				{
					_relockProject();
					return new MotifApplyWorkflowResult(result, false, true, SelectMessage(result), null, null);
				}
				catch (Exception error)
				{
					relockError = error;
				}
			}

			try
			{
				var reopenedWindow = _reloadProject();
				return new MotifApplyWorkflowResult(result, true, false, SelectMessage(result),
					reopenedWindow, relockError);
			}
			catch (Exception error)
			{
				return new MotifApplyWorkflowResult(result, false, false, MotifApplyMessage.RecoveryFailed,
					null, error);
			}
		}

		internal static MotifApplyMessage GetPrecheckMessage(bool motifAvailable, bool fieldWorksBusy)
		{
			if (!motifAvailable)
				return MotifApplyMessage.NotInstalled;
			return fieldWorksBusy ? MotifApplyMessage.FieldWorksBusy : MotifApplyMessage.None;
		}

		private static MotifApplyResult Ambiguous()
		{
			return new MotifApplyResult(MotifApplyOutcome.Ambiguous, null, null, null);
		}

		private static MotifApplyMessage SelectMessage(MotifApplyResult result)
		{
			switch (result.Outcome)
			{
				case MotifApplyOutcome.Applied:
					return string.IsNullOrWhiteSpace(result.Summary)
						? MotifApplyMessage.Applied
						: MotifApplyMessage.AppliedWithSummary;
				case MotifApplyOutcome.NoChanges:
					return MotifApplyMessage.NoChanges;
				case MotifApplyOutcome.Refused:
					return MotifApplyMessage.Refused;
				case MotifApplyOutcome.Busy:
					return MotifApplyMessage.MotifBusy;
				case MotifApplyOutcome.NotInstalled:
					return MotifApplyMessage.NotInstalled;
				case MotifApplyOutcome.CouldNotStart:
					return MotifApplyMessage.CouldNotStart;
				case MotifApplyOutcome.TimedOut:
					return MotifApplyMessage.TimedOut;
				default:
					return MotifApplyMessage.Reconciliation;
			}
		}
	}

	internal enum MotifApplyMessage
	{
		None,
		Applied,
		AppliedWithSummary,
		NoChanges,
		Refused,
		MotifBusy,
		FieldWorksBusy,
		NotInstalled,
		CouldNotStart,
		Reconciliation,
		TimedOut,
		PreparationFailed,
		MotifStillRunning,
		MissingProjectFile,
		RecoveryFailed
	}

	internal sealed class MotifApplyWorkflowResult
	{
		internal MotifApplyWorkflowResult(MotifApplyResult applyResult, bool reloaded, bool relocked,
			MotifApplyMessage message, Form reopenedWindow, Exception error)
		{
			ApplyResult = applyResult;
			Reloaded = reloaded;
			Relocked = relocked;
			Message = message;
			ReopenedWindow = reopenedWindow;
			Error = error;
		}

		/// <summary>Motif's classified result, or null when Motif was not started.</summary>
		internal MotifApplyResult ApplyResult { get; }
		internal bool Reloaded { get; }
		internal bool Relocked { get; }
		internal MotifApplyMessage Message { get; }
		/// <summary>The main window that replaced the original one, or null when the project was
		/// not reopened.</summary>
		internal Form ReopenedWindow { get; }
		/// <summary>The failure that determined the message, or null when there was
		/// none.</summary>
		internal Exception Error { get; }
	}
}
