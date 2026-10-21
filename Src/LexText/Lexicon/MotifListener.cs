// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks.LexText;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>Handles Tools > Apply Motif changes for the open project.</summary>
	[MediatorDispose]
	internal sealed class MotifListener : IxCoreColleague, IDisposable
	{
		private readonly MotifApplyRunner _runner;
		private Mediator _mediator;
		private PropertyTable _propertyTable;

		/// <summary>Creates the listener XCore registers from Main.xml.</summary>
		public MotifListener()
			: this(new MotifApplyRunner())
		{
		}

		/// <summary>Creates a listener that runs Motif through the given runner.</summary>
		internal MotifListener(MotifApplyRunner runner)
		{
			_runner = runner ?? throw new ArgumentNullException(nameof(runner));
		}

		#region IxCoreColleague Implementation

		/// <summary/>
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		/// <summary/>
		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			_mediator = mediator;
			_propertyTable = propertyTable;
			mediator.AddColleague(this);
		}

		/// <summary/>
		public int Priority => (int)ColleaguePriority.Medium;

		/// <summary/>
		public bool ShouldNotCall => IsDisposed;

		#endregion

		/// <summary>Controls whether the Motif apply command is available.</summary>
		/// <param name="parameters">The XCore command parameters.</param>
		/// <param name="display">The command's display properties.</param>
		/// <returns>True when the command has been handled.</returns>
		public bool OnDisplayApplyMotifChanges(object parameters, ref UIItemDisplayProperties display)
		{
			display.Visible = true;
			display.Enabled = IsMotifAvailable();
			return true;
		}

		/// <summary>Applies approved pending Motif changes to the open project.</summary>
		/// <param name="commandObject">The XCore command object.</param>
		/// <returns>True when the command has been handled.</returns>
		public bool OnApplyMotifChanges(object commandObject)
		{
			CheckDisposed();

			Application.DoEvents();
			// Reopening the project disposes this listener and its window, so everything used
			// after the workflow starts is captured here.
			var propertyTable = _propertyTable;
			var owner = propertyTable.GetValue<Form>("window");
			var cache = propertyTable.GetValue<LcmCache>("cache");
			var app = propertyTable.GetValue<LexTextApp>("App");
			var runner = _runner;

			var precheckMessage = MotifApplyWorkflow.GetPrecheckMessage(IsMotifAvailable(),
				IsFieldWorksBusy(propertyTable, cache));
			if (precheckMessage != MotifApplyMessage.None)
			{
				ShowMotifApplyMessage(owner, precheckMessage);
				return true;
			}

			if (MessageBox.Show(owner, LexEdStrings.ksApplyMotifConfirmation,
				LexEdStrings.ksApplyMotifChangesTitle, MessageBoxButtons.OKCancel,
				MessageBoxIcon.Question) != DialogResult.OK)
				return true;

			var projectFilePath = Path.GetFullPath(ProjectLifecycle.GetFullProjectFileName(cache));
			// Resolving Motif before the project is unlocked means a missing or misconfigured
			// install never leaves the project unlocked.
			var motifCommand = runner.Prepare(projectFilePath);
			if (motifCommand == null)
			{
				ShowMotifApplyMessage(owner, MotifApplyMessage.NotInstalled);
				return true;
			}

			ProjectLifecycle.StopParser(propertyTable);
			var workflow = new MotifApplyWorkflow(
				() => ProjectLifecycle.ValidatePendingEditorData(propertyTable),
				() => ProjectLifecycle.SaveAndUnlockProject(cache),
				() => runner.Run(motifCommand),
				() => ProjectLifecycle.RefreshCacheWindowAndAll(app, projectFilePath),
				() => ProjectLockingService.LockCurrentProject(cache),
				() => File.Exists(projectFilePath));
			MotifApplyWorkflowResult workflowResult;
			using (new WaitCursor())
				workflowResult = workflow.Execute();

			ShowMotifApplyMessage(workflowResult.ReopenedWindow ?? owner, workflowResult.Message,
				workflowResult.ApplyResult?.Summary, workflowResult.Error, projectFilePath);
			return true;
		}

		// Checked afresh each time, so installing or removing Motif while FieldWorks is open
		// shows the next time the Tools menu opens.
		private bool IsMotifAvailable()
		{
			return _runner.IsAvailable;
		}

		private static bool IsFieldWorksBusy(PropertyTable propertyTable, LcmCache cache)
		{
			var bridgeListener = propertyTable.GetValue<FLExBridgeListener>("FLExBridgeListener");
			return (bridgeListener != null && bridgeListener.IsTransferInProgress) ||
				SharedBackendServices.AreMultipleApplicationsConnected(cache);
		}

		private static void ShowMotifApplyMessage(Form owner, MotifApplyMessage message, string summary = null,
			Exception error = null, string projectFilePath = null)
		{
			var errorText = error?.Message ?? string.Empty;
			string content;
			MessageBoxIcon icon;
			switch (message)
			{
				case MotifApplyMessage.Applied:
					content = LexEdStrings.ksApplyMotifSuccess;
					icon = MessageBoxIcon.Information;
					break;
				case MotifApplyMessage.AppliedWithSummary:
					content = string.Format(LexEdStrings.ksApplyMotifSuccessWithSummary, summary);
					icon = MessageBoxIcon.Information;
					break;
				case MotifApplyMessage.NoChanges:
					content = LexEdStrings.ksApplyMotifNoChanges;
					icon = MessageBoxIcon.Information;
					break;
				case MotifApplyMessage.Refused:
					content = LexEdStrings.ksApplyMotifRefused;
					icon = MessageBoxIcon.Warning;
					break;
				case MotifApplyMessage.MotifBusy:
					content = LexEdStrings.ksMotifBusy;
					icon = MessageBoxIcon.Warning;
					break;
				case MotifApplyMessage.FieldWorksBusy:
					content = LexEdStrings.ksApplyMotifBusy;
					icon = MessageBoxIcon.Warning;
					break;
				case MotifApplyMessage.NotInstalled:
					content = LexEdStrings.ksMotifNotFound;
					icon = MessageBoxIcon.Warning;
					break;
				case MotifApplyMessage.CouldNotStart:
					content = LexEdStrings.ksMotifCouldNotStart;
					icon = MessageBoxIcon.Error;
					break;
				case MotifApplyMessage.Reconciliation:
					content = LexEdStrings.ksApplyMotifReconciliation;
					icon = MessageBoxIcon.Error;
					break;
				case MotifApplyMessage.TimedOut:
					content = LexEdStrings.ksApplyMotifTimedOut;
					icon = MessageBoxIcon.Error;
					break;
				case MotifApplyMessage.PreparationFailed:
					content = string.Format(LexEdStrings.ksApplyMotifPreparationFailed, errorText);
					icon = MessageBoxIcon.Error;
					break;
				case MotifApplyMessage.MotifStillRunning:
					content = LexEdStrings.ksApplyMotifStillRunning;
					icon = MessageBoxIcon.Error;
					break;
				case MotifApplyMessage.MissingProjectFile:
					content = string.Format(LexEdStrings.ksApplyMotifMissingProjectFile, projectFilePath,
						Path.ChangeExtension(projectFilePath, "bak"));
					icon = MessageBoxIcon.Error;
					break;
				case MotifApplyMessage.RecoveryFailed:
					content = string.Format(LexEdStrings.ksApplyMotifRecoveryFailed, errorText);
					icon = MessageBoxIcon.Error;
					break;
				default:
					return;
			}

			MessageBox.Show(owner == null || owner.IsDisposed ? null : owner, content,
				LexEdStrings.ksApplyMotifChangesTitle, MessageBoxButtons.OK, icon);
		}

		#region IDisposable implementation

		/// <summary/>
		public void Dispose()
		{
			if (IsDisposed)
				return;

			_mediator?.RemoveColleague(this);
			_mediator = null;
			_propertyTable = null;
			IsDisposed = true;
		}

		/// <summary>Throws an ObjectDisposedException after the listener is disposed.</summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		/// <summary>Whether the listener has been disposed.</summary>
		public bool IsDisposed { get; private set; }

		#endregion
	}
}
