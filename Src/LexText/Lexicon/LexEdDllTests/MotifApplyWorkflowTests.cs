// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.XWorks.LexEd;

namespace LexEdDllTests
{
	[TestFixture]
	public class MotifApplyWorkflowTests
	{
		[Test]
		public void ExecuteCommitsEditsAndUnlocksBeforeOneMotifInvocation()
		{
			var actions = new List<string>();
			var runCount = 0;
			var workflow = CreateWorkflow(actions, () =>
			{
				actions.Add("run-motif");
				runCount++;
				return CreateResult(0, "{\"ok\":true,\"applied\":true}");
			});

			var result = workflow.Execute();

			Assert.That(actions, Is.EqualTo(new[] { "validate-edits", "save-and-unlock", "run-motif", "reload" }));
			Assert.That(runCount, Is.EqualTo(1));
			Assert.That(result.Reloaded, Is.True);
			Assert.That(result.Relocked, Is.False);
			Assert.That(result.Message, Is.EqualTo(MotifApplyMessage.Applied));
		}

		[TestCase(MotifApplyOutcome.Applied, 0, "{\"ok\":true,\"applied\":true}", true, (int)MotifApplyMessage.Applied, true, true)]
		[TestCase(MotifApplyOutcome.NoChanges, 0, "{\"ok\":true,\"applied\":false}", false, (int)MotifApplyMessage.NoChanges, true, true)]
		[TestCase(MotifApplyOutcome.Refused, 2, "", false, (int)MotifApplyMessage.Refused, true, true)]
		[TestCase(MotifApplyOutcome.Busy, 3, "", false, (int)MotifApplyMessage.MotifBusy, true, true)]
		[TestCase(MotifApplyOutcome.NotInstalled, 0, "", false, (int)MotifApplyMessage.NotInstalled, true, false)]
		[TestCase(MotifApplyOutcome.CouldNotStart, -1, "", false, (int)MotifApplyMessage.CouldNotStart, false, true)]
		[TestCase(MotifApplyOutcome.ReconciliationRequired, 4, "", true, (int)MotifApplyMessage.Reconciliation, true, true)]
		[TestCase(MotifApplyOutcome.Ambiguous, 8, "", true, (int)MotifApplyMessage.Reconciliation, true, true)]
		public void ExecuteSelectsMessageForEachOutcome(MotifApplyOutcome expectedOutcome,
			int exitCode, string json, bool reload, int expectedMessage,
			bool processStarted, bool executableAvailable)
		{
			var actions = new List<string>();
			var workflow = CreateWorkflow(actions,
				() => CreateResult(exitCode, json, processStarted, executableAvailable));

			var result = workflow.Execute();

			Assert.That(result.ApplyResult.Outcome, Is.EqualTo(expectedOutcome));
			Assert.That(result.Reloaded, Is.EqualTo(reload));
			Assert.That(result.Relocked, Is.EqualTo(!reload));
			Assert.That(result.Message, Is.EqualTo((MotifApplyMessage)expectedMessage));
			Assert.That(actions, Does.Contain(reload ? "reload" : "relock"));
		}

		[Test]
		public void RelockFailureReloadsAndPreservesMotifOutcomeMessage()
		{
			var actions = new List<string>();
			var lockError = new InvalidOperationException("lock failed");
			var workflow = CreateWorkflow(actions, () =>
			{
				actions.Add("run-motif");
				return CreateResult(2, "");
			}, relock: () =>
			{
				actions.Add("relock");
				throw lockError;
			});

			var result = workflow.Execute();

			Assert.That(actions, Is.EqualTo(new[]
				{ "validate-edits", "save-and-unlock", "run-motif", "relock", "reload" }));
			Assert.That(result.Reloaded, Is.True);
			Assert.That(result.Relocked, Is.False);
			Assert.That(result.ApplyResult.Outcome, Is.EqualTo(MotifApplyOutcome.Refused));
			Assert.That(result.Message, Is.EqualTo(MotifApplyMessage.Refused));
			Assert.That(result.Error, Is.SameAs(lockError));
		}

		[Test]
		public void ReloadFailureIsReportedInsteadOfThrown()
		{
			var reloadError = new InvalidOperationException("reload failed");
			var workflow = CreateWorkflow(new List<string>(),
				() => CreateResult(0, "{\"ok\":true,\"applied\":true}"),
				reload: () => throw reloadError,
				relock: () => Assert.Fail("Applied outcome must reload instead of relocking."));

			var result = workflow.Execute();

			Assert.That(result.Message, Is.EqualTo(MotifApplyMessage.RecoveryFailed));
			Assert.That(result.Reloaded, Is.False);
			Assert.That(result.Error, Is.SameAs(reloadError));
		}

		[Test]
		public void RelockAndReloadFailureIsReportedInsteadOfThrown()
		{
			var reloadError = new InvalidOperationException("reload failed");
			var workflow = CreateWorkflow(new List<string>(), () => CreateResult(2, ""),
				reload: () => throw reloadError,
				relock: () => throw new InvalidOperationException("lock failed"));

			var result = workflow.Execute();

			Assert.That(result.Message, Is.EqualTo(MotifApplyMessage.RecoveryFailed));
			Assert.That(result.Relocked || result.Reloaded, Is.False);
			Assert.That(result.Error, Is.SameAs(reloadError));
		}

		[Test]
		public void SaveFailureDoesNotStartMotif()
		{
			var actions = new List<string>();
			var saveError = new InvalidOperationException("save failed");
			var workflow = new MotifApplyWorkflow(
				() => actions.Add("validate-edits"),
				() => throw saveError,
				() =>
				{
					actions.Add("run-motif");
					return CreateResult(0, "{\"ok\":true,\"applied\":true}");
				},
				() => { actions.Add("reload"); return null; },
				() => actions.Add("relock"),
				() => true);

			var result = workflow.Execute();

			Assert.That(actions, Is.EqualTo(new[] { "validate-edits" }));
			Assert.That(result.Message, Is.EqualTo(MotifApplyMessage.PreparationFailed));
			Assert.That(result.ApplyResult, Is.Null);
			Assert.That(result.Error, Is.SameAs(saveError));
		}

		[Test]
		public void MotifRunFailureIsTreatedAsAmbiguousAndReloads()
		{
			var actions = new List<string>();
			var workflow = CreateWorkflow(actions, () => throw new InvalidOperationException("run failed"));

			var result = workflow.Execute();

			Assert.That(result.ApplyResult.Outcome, Is.EqualTo(MotifApplyOutcome.Ambiguous));
			Assert.That(result.Reloaded, Is.True);
			Assert.That(result.Message, Is.EqualTo(MotifApplyMessage.Reconciliation));
		}

		[Test]
		public void MissingMotifResultIsTreatedAsAmbiguousAndReloads()
		{
			var workflow = CreateWorkflow(new List<string>(), () => null);

			var result = workflow.Execute();

			Assert.That(result.ApplyResult.Outcome, Is.EqualTo(MotifApplyOutcome.Ambiguous));
			Assert.That(result.Reloaded, Is.True);
		}

		[Test]
		public void TimedOutMotifReloadsWithTimeoutMessage()
		{
			var actions = new List<string>();
			var workflow = CreateWorkflow(actions, () => CreateResult(
				new MotifProcessOutput(true, -1, "{\"ok\":true,\"applied\":true}", "", true, false)));

			var result = workflow.Execute();

			Assert.That(result.ApplyResult.Outcome, Is.EqualTo(MotifApplyOutcome.TimedOut));
			Assert.That(result.Reloaded, Is.True);
			Assert.That(result.Message, Is.EqualTo(MotifApplyMessage.TimedOut));
		}

		[Test]
		public void MotifThatCouldNotBeStoppedIsNeitherRelockedNorReloaded()
		{
			var actions = new List<string>();
			var workflow = CreateWorkflow(actions, () => CreateResult(
				new MotifProcessOutput(true, null, "", "", true, true)));

			var result = workflow.Execute();

			Assert.That(actions, Is.EqualTo(new[] { "validate-edits", "save-and-unlock" }));
			Assert.That(result.Message, Is.EqualTo(MotifApplyMessage.MotifStillRunning));
		}

		[Test]
		public void MissingProjectFileIsNotReopened()
		{
			var actions = new List<string>();
			var workflow = CreateWorkflow(actions, () => CreateResult(0, "{\"ok\":true,\"applied\":true}"),
				projectFileExists: () => false);

			var result = workflow.Execute();

			Assert.That(actions, Does.Not.Contain("reload"));
			Assert.That(result.Reloaded, Is.False);
			Assert.That(result.Message, Is.EqualTo(MotifApplyMessage.MissingProjectFile));
		}

		[Test]
		public void ReloadReturnsTheReopenedWindow()
		{
			using (var reopened = new Form())
			{
				var workflow = CreateWorkflow(new List<string>(),
					() => CreateResult(0, "{\"ok\":true,\"applied\":true}"), reload: () => reopened);

				Assert.That(workflow.Execute().ReopenedWindow, Is.SameAs(reopened));
			}
		}

		[TestCase(false, false, (int)MotifApplyMessage.NotInstalled)]
		[TestCase(true, true, (int)MotifApplyMessage.FieldWorksBusy)]
		[TestCase(true, false, (int)MotifApplyMessage.None)]
		[TestCase(false, true, (int)MotifApplyMessage.NotInstalled)]
		public void PrecheckSelectsTheExpectedMessage(bool motifAvailable, bool fieldWorksBusy,
			int expectedMessage)
		{
			Assert.That(MotifApplyWorkflow.GetPrecheckMessage(motifAvailable, fieldWorksBusy),
				Is.EqualTo((MotifApplyMessage)expectedMessage));
		}

		private static MotifApplyWorkflow CreateWorkflow(ICollection<string> actions,
			Func<MotifApplyResult> apply, Func<Form> reload = null, Action relock = null,
			Func<bool> projectFileExists = null)
		{
			return new MotifApplyWorkflow(
				() => actions.Add("validate-edits"),
				() => actions.Add("save-and-unlock"),
				apply,
				reload ?? (() => { actions.Add("reload"); return null; }),
				relock ?? (() => actions.Add("relock")),
				projectFileExists ?? (() => true));
		}

		private static MotifApplyResult CreateResult(int exitCode, string json,
			bool started = true, bool executableAvailable = true)
		{
			var resolver = new MotifExecutableResolver(
				() => executableAvailable ? @"C:\Motif" : null,
				() => null,
				path => executableAvailable && path == @"C:\Motif\motif.exe");
			var runner = new MotifApplyRunner(resolver,
				info => new MotifProcessOutput(started, exitCode, json, ""));
			return runner.Apply(@"C:\Project\Field Data.fwdata");
		}

		private static MotifApplyResult CreateResult(MotifProcessOutput output)
		{
			var resolver = new MotifExecutableResolver(
				() => @"C:\Motif", () => null, path => path == @"C:\Motif\motif.exe");
			return new MotifApplyRunner(resolver, info => output).Apply(@"C:\Project\Field Data.fwdata");
		}
	}
}
