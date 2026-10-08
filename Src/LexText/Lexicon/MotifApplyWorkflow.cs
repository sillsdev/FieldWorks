// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	internal sealed class MotifApplyWorkflow
	{
		private readonly Action _validatePendingEdits;
		private readonly Action _saveAndUnlockProject;
		private readonly Func<MotifApplyResult> _runMotif;
		private readonly Action _reloadProject;
		private readonly Action _relockProject;

		internal MotifApplyWorkflow(Action validatePendingEdits, Action saveAndUnlockProject,
			Func<MotifApplyResult> runMotif, Action reloadProject, Action relockProject)
		{
			_validatePendingEdits = validatePendingEdits ?? throw new ArgumentNullException(nameof(validatePendingEdits));
			_saveAndUnlockProject = saveAndUnlockProject ?? throw new ArgumentNullException(nameof(saveAndUnlockProject));
			_runMotif = runMotif ?? throw new ArgumentNullException(nameof(runMotif));
			_reloadProject = reloadProject ?? throw new ArgumentNullException(nameof(reloadProject));
			_relockProject = relockProject ?? throw new ArgumentNullException(nameof(relockProject));
		}

		internal MotifApplyWorkflowResult Execute()
		{
			_validatePendingEdits();
			_saveAndUnlockProject();
			var result = _runMotif();
			var reloaded = false;
			var relocked = false;
			if (result.RequiresReload)
			{
				_reloadProject();
				reloaded = true;
			}
			else
			{
				try
				{
					_relockProject();
					relocked = true;
				}
				catch
				{
					_reloadProject();
					reloaded = true;
				}
			}

			return new MotifApplyWorkflowResult(result, reloaded, relocked, SelectMessage(result));
		}

		internal static MotifApplyMessage GetPrecheckMessage(bool motifAvailable, bool fieldWorksBusy)
		{
			if (!motifAvailable)
				return MotifApplyMessage.NotInstalled;
			return fieldWorksBusy ? MotifApplyMessage.FieldWorksBusy : MotifApplyMessage.None;
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
		Reconciliation
	}

	internal sealed class MotifApplyWorkflowResult
	{
		internal MotifApplyWorkflowResult(MotifApplyResult applyResult, bool reloaded, bool relocked,
			MotifApplyMessage message)
		{
			ApplyResult = applyResult;
			Reloaded = reloaded;
			Relocked = relocked;
			Message = message;
		}

		internal MotifApplyResult ApplyResult { get; }
		internal bool Reloaded { get; }
		internal bool Relocked { get; }
		internal MotifApplyMessage Message { get; }
	}
}
