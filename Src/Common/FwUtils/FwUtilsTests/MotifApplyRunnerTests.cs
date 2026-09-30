// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	public class MotifApplyRunnerTests
	{
		[Test]
		public void ResolverPrefersMotifDirectoryEnvironmentVariable()
		{
			var resolver = new MotifExecutableResolver(
				() => @"C:\Motif Dev",
				() => @"C:\Motif Registered\motif.exe",
				path => path == @"C:\Motif Dev\motif.exe" || path == @"C:\Motif Registered\motif.exe");

			Assert.That(resolver.FindExecutable(), Is.EqualTo(@"C:\Motif Dev\motif.exe"));
		}

		[Test]
		public void ResolverUsesRegisteredCliPathWhenEnvironmentIsMissing()
		{
			const string registered = @"C:\Users\linguist\AppData\Local\SIL.Motif\current\motif.exe";
			var resolver = new MotifExecutableResolver(
				() => null,
				() => registered,
				path => path == registered);

			Assert.That(resolver.FindExecutable(), Is.EqualTo(registered));
		}

		[Test]
		public void ResolverFallsBackToRegistryWhenEnvironmentDirectoryHasNoExecutable()
		{
			var resolver = new MotifExecutableResolver(
				() => @"C:\Motif Dev",
				() => @"C:\Motif Registered\motif.exe",
				path => path == @"C:\Motif Registered\motif.exe");

			Assert.That(resolver.FindExecutable(), Is.EqualTo(@"C:\Motif Registered\motif.exe"));
		}

		[Test]
		public void ResolverIgnoresRegisteredCliPathThatNoLongerExists()
		{
			var resolver = new MotifExecutableResolver(
				() => null, () => @"C:\Motif Removed\motif.exe", path => false);

			Assert.That(resolver.FindExecutable(), Is.Null);
		}

		[Test]
		public void ResolverReturnsNullWhenMotifIsNotInstalled()
		{
			var resolver = new MotifExecutableResolver(() => null, () => null, path => false);

			Assert.That(resolver.FindExecutable(), Is.Null);
		}

		[Test]
		public void ApplyQuotesProjectPathAndCapturesBothOutputStreams()
		{
			ProcessStartInfo startInfo = null;
			var runner = CreateRunner(
				new MotifProcessOutput(true, 0, "{\"ok\":true,\"applied\":true}", ""),
				info => startInfo = info);

			var result = runner.Apply(@"C:\Users\linguist\My Project\Field Data.fwdata");

			Assert.That(result.Outcome, Is.EqualTo(MotifApplyOutcome.Applied));
			Assert.That(startInfo.FileName, Is.EqualTo(@"C:\Motif\motif.exe"));
			Assert.That(startInfo.Arguments, Is.EqualTo("apply --all-pending --project \"C:\\Users\\linguist\\My Project\\Field Data.fwdata\" --json"));
			Assert.That(startInfo.UseShellExecute, Is.False);
			Assert.That(startInfo.RedirectStandardOutput, Is.True);
			Assert.That(startInfo.RedirectStandardError, Is.True);
		}

		[Test]
		public void ApplyReadsOptionalSummaryAndIgnoresUnknownJsonFields()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 0,
				"{\"ok\":true,\"applied\":true,\"summary\":\"2 changes applied\",\"futureField\":7}", ""));

			var result = runner.Apply(@"C:\Project\My Project.fwdata");

			Assert.That(result.Outcome, Is.EqualTo(MotifApplyOutcome.Applied));
			Assert.That(result.Summary, Is.EqualTo("2 changes applied"));
		}

		[Test]
		public void ApplyTreatsSuccessfulNoOpAsNoChangesWithoutReceipt()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 0,
				"{\"ok\":true,\"applied\":false,\"receipt\":null,\"futureField\":true}", ""));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.NoChanges));
		}

		[Test]
		public void ExitTwoIsRefusal()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 2,
				"", "{\"ok\":false,\"code\":\"apply.change-no-longer-fits\"}"));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.Refused));
		}

		[Test]
		public void ExitThreeIsBusy()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 3, "", "not json"));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.Busy));
		}

		[Test]
		public void ExitFourRequiresReconciliation()
		{
			var invocationCount = 0;
			var runner = CreateRunner(new MotifProcessOutput(true, 4, "", "not json"),
				_ => invocationCount++);

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.ReconciliationRequired));
			Assert.That(invocationCount, Is.EqualTo(1));
		}

		[Test]
		public void SuccessfulExitWithFalseOkIsAmbiguous()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 0,
				"{\"ok\":false,\"applied\":true}", ""));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.Ambiguous));
		}

		[Test]
		public void SuccessfulExitWithoutAppliedFieldIsAmbiguous()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 0, "{\"ok\":true}", ""));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.Ambiguous));
		}

		[Test]
		public void EmptyStandardOutputIsAmbiguous()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 0, "", ""));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.Ambiguous));
		}

		[Test]
		public void ProcessRunnerExceptionIsAmbiguous()
		{
			var resolver = new MotifExecutableResolver(
				() => @"C:\Motif", () => null, path => path == @"C:\Motif\motif.exe");
			var runner = new MotifApplyRunner(resolver,
				_ => throw new System.InvalidOperationException("process failed"));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.Ambiguous));
		}

		[Test]
		public void ProcessThatDidNotStartIsCouldNotStart()
		{
			var runner = CreateRunner(new MotifProcessOutput(false, null, "", ""));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.CouldNotStart));
		}

		[Test]
		public void RunProcessDrainsLargeStandardOutputAndStandardError()
		{
			var commandProcessor = Path.Combine(
				System.Environment.GetFolderPath(System.Environment.SpecialFolder.System), "cmd.exe");
			var startInfo = new ProcessStartInfo
			{
				FileName = commandProcessor,
				Arguments = "/d /c \"for /L %i in (1,1,4096) do @echo 12345678901234567890 & " +
					"@echo 12345678901234567890 1>&2\"",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			var output = MotifApplyRunner.RunProcess(startInfo);

			Assert.That(output.Started, Is.True);
			Assert.That(output.ExitCode, Is.EqualTo(0));
			Assert.That(output.StandardOutput.Length, Is.GreaterThan(64 * 1024));
			Assert.That(output.StandardError.Length, Is.GreaterThan(64 * 1024));
		}

		[Test]
		public void ReconciliationCodeRequiresReloadEvenWhenExitCodeIsTwo()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 2, "",
				"{\"ok\":false,\"code\":\"apply.reconciliation-needed\"}"));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.ReconciliationRequired));
		}

		[TestCase(1)]
		[TestCase(5)]
		public void OtherExitCodesAreAmbiguous(int exitCode)
		{
			var runner = CreateRunner(new MotifProcessOutput(true, exitCode, "", "not json"));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.Ambiguous));
		}

		[Test]
		public void MalformedSuccessJsonIsAmbiguous()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 0, "{broken", ""));

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.Ambiguous));
		}

		[Test]
		public void JsonFailureEnvelopeCanBeReadFromStandardError()
		{
			var runner = CreateRunner(new MotifProcessOutput(true, 4, "",
				"{\"ok\":false,\"code\":\"apply.reconciliation-needed\",\"extra\":{\"ignored\":true}}"));

			var result = runner.Apply(@"C:\Project\My Project.fwdata");

			Assert.That(result.Outcome, Is.EqualTo(MotifApplyOutcome.ReconciliationRequired));
			Assert.That(result.ErrorCode, Is.EqualTo("apply.reconciliation-needed"));
		}

		[Test]
		public void MissingExecutableDoesNotCreateProcess()
		{
			var processCalled = false;
			var resolver = new MotifExecutableResolver(() => null, () => null, path => false);
			var runner = new MotifApplyRunner(resolver, info =>
			{
				processCalled = true;
				return new MotifProcessOutput(true, 0, "", "");
			});

			Assert.That(runner.Apply(@"C:\Project\My Project.fwdata").Outcome,
				Is.EqualTo(MotifApplyOutcome.NotInstalled));
			Assert.That(processCalled, Is.False);
		}

		private static MotifApplyRunner CreateRunner(MotifProcessOutput output,
			System.Action<ProcessStartInfo> inspectStartInfo = null)
		{
			var resolver = new MotifExecutableResolver(
				() => @"C:\Motif",
				() => null,
				path => path == @"C:\Motif\motif.exe");
			return new MotifApplyRunner(resolver, info =>
			{
				if (inspectStartInfo != null)
					inspectStartInfo(info);
				return output;
			});
		}
	}
}
