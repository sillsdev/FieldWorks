// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>The classified result of one Motif pending-changes invocation.</summary>
	public enum MotifApplyOutcome
	{
		/// <summary>No configured Motif executable was found.</summary>
		NotInstalled,
		/// <summary>The operating system did not start the executable.</summary>
		CouldNotStart,
		/// <summary>Motif applied the pending changes.</summary>
		Applied,
		/// <summary>Motif found no pending changes to apply.</summary>
		NoChanges,
		/// <summary>Motif refused the request without changing the project.</summary>
		Refused,
		/// <summary>The project could not be opened because another process is using
		/// it.</summary>
		Busy,
		/// <summary>The project may have changed and must be reopened.</summary>
		ReconciliationRequired,
		/// <summary>The result could not be classified safely.</summary>
		Ambiguous
	}

	/// <summary>The exit code and captured output from a Motif process.</summary>
	public sealed class MotifProcessOutput
	{
		/// <summary>Creates a process result for the runner.</summary>
		/// <param name="started">Whether the operating system started the process.</param>
		/// <param name="exitCode">The process exit code, or null if it did not exit.</param>
		/// <param name="standardOutput">Text captured from standard output.</param>
		/// <param name="standardError">Text captured from standard error.</param>
		public MotifProcessOutput(bool started, int? exitCode, string standardOutput, string standardError)
		{
			Started = started;
			ExitCode = exitCode;
			StandardOutput = standardOutput ?? string.Empty;
			StandardError = standardError ?? string.Empty;
		}

		/// <summary>Whether the operating system started the process.</summary>
		public bool Started { get; }
		/// <summary>The process exit code, or null if it did not exit.</summary>
		public int? ExitCode { get; }
		/// <summary>Text captured from standard output.</summary>
		public string StandardOutput { get; }
		/// <summary>Text captured from standard error.</summary>
		public string StandardError { get; }
	}

	/// <summary>The stable result FieldWorks uses to restore the project lifecycle.</summary>
	public sealed class MotifApplyResult
	{
		internal MotifApplyResult(MotifApplyOutcome outcome, int? exitCode, string errorCode,
			string summary)
		{
			Outcome = outcome;
			ExitCode = exitCode;
			ErrorCode = errorCode;
			Summary = summary;
		}

		/// <summary>The result category derived from the exit code and stable JSON
		/// fields.</summary>
		public MotifApplyOutcome Outcome { get; }
		/// <summary>The process exit code, when Motif started.</summary>
		public int? ExitCode { get; }
		/// <summary>The stable Motif failure code, when the JSON response provides one.</summary>
		public string ErrorCode { get; }
		/// <summary>The optional display summary supplied by Motif.</summary>
		public string Summary { get; }
		/// <summary>Whether FieldWorks must reload because the project may have
		/// changed.</summary>
		public bool RequiresReload => Outcome == MotifApplyOutcome.Applied ||
			Outcome == MotifApplyOutcome.ReconciliationRequired || Outcome == MotifApplyOutcome.Ambiguous;
	}

	/// <summary>Runs Motif once and classifies its stable process response.</summary>
	public sealed class MotifApplyRunner
	{
		private const string ReconciliationCode = "apply.reconciliation-needed";
		private readonly MotifExecutableResolver _executableResolver;
		private readonly Func<ProcessStartInfo, MotifProcessOutput> _runProcess;

		/// <summary>Creates a runner that starts Motif through the operating system.</summary>
		public MotifApplyRunner()
			: this(new MotifExecutableResolver(), RunProcess)
		{
		}

		/// <summary>Creates a runner with injectable executable discovery and process
		/// execution.</summary>
		/// <param name="executableResolver">Resolves the configured Motif executable.</param>
		/// <param name="runProcess">Starts one process and returns its exit code and captured
		/// streams.</param>
		public MotifApplyRunner(MotifExecutableResolver executableResolver,
			Func<ProcessStartInfo, MotifProcessOutput> runProcess)
		{
			_executableResolver = executableResolver ?? throw new ArgumentNullException(nameof(executableResolver));
			_runProcess = runProcess ?? throw new ArgumentNullException(nameof(runProcess));
		}

		/// <summary>Whether Motif is currently discoverable.</summary>
		public bool IsAvailable => _executableResolver.FindExecutable() != null;

		/// <summary>Runs one pending-changes apply for an absolute FieldWorks project
		/// path.</summary>
		/// <param name="projectFilePath">The path to the project's `.fwdata` file.</param>
		public MotifApplyResult Apply(string projectFilePath)
		{
			if (string.IsNullOrWhiteSpace(projectFilePath))
				throw new ArgumentException("A project file path is required.", nameof(projectFilePath));

			var executable = _executableResolver.FindExecutable();
			if (executable == null)
				return new MotifApplyResult(MotifApplyOutcome.NotInstalled, null, null, null);

			var project = Path.GetFullPath(projectFilePath);
			var startInfo = new ProcessStartInfo
			{
				FileName = executable,
				Arguments = "apply --all-pending --project " + QuoteArgument(project) + " --json",
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				StandardOutputEncoding = new UTF8Encoding(false),
				StandardErrorEncoding = new UTF8Encoding(false)
			};

			MotifProcessOutput output;
			try
			{
				output = _runProcess(startInfo);
			}
			catch
			{
				return new MotifApplyResult(MotifApplyOutcome.Ambiguous, null, null, null);
			}

			if (output == null)
				return new MotifApplyResult(MotifApplyOutcome.Ambiguous, null, null, null);
			if (!output.Started)
				return new MotifApplyResult(MotifApplyOutcome.CouldNotStart, null, null, null);

			var response = ReadResponse(output.StandardOutput) ?? ReadResponse(output.StandardError);
			var errorCode = response?.Code;
			if (errorCode == ReconciliationCode || output.ExitCode == 4)
				return new MotifApplyResult(MotifApplyOutcome.ReconciliationRequired,
					output.ExitCode, errorCode, response?.Summary);
			if (output.ExitCode == 2)
				return new MotifApplyResult(MotifApplyOutcome.Refused, output.ExitCode, errorCode, response?.Summary);
			if (output.ExitCode == 3)
				return new MotifApplyResult(MotifApplyOutcome.Busy, output.ExitCode, errorCode, response?.Summary);
			if (output.ExitCode != 0)
				return new MotifApplyResult(MotifApplyOutcome.Ambiguous, output.ExitCode, errorCode, response?.Summary);

			if (response?.Ok != true || !response.Applied.HasValue)
				return new MotifApplyResult(MotifApplyOutcome.Ambiguous, output.ExitCode, errorCode, response?.Summary);

			return new MotifApplyResult(response.Applied.Value ? MotifApplyOutcome.Applied : MotifApplyOutcome.NoChanges,
				output.ExitCode, errorCode, response.Summary);
		}

		private static string QuoteArgument(string argument)
		{
			return "\"" + argument.Replace("\"", "\\\"") + "\"";
		}

		private static MotifJsonResponse ReadResponse(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
				return null;

			try
			{
				var serializer = new DataContractJsonSerializer(typeof(MotifJsonResponse));
				using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
					return (MotifJsonResponse)serializer.ReadObject(stream);
			}
			catch (SerializationException)
			{
				return null;
			}
			catch (System.Xml.XmlException)
			{
				return null;
			}
		}

		internal static MotifProcessOutput RunProcess(ProcessStartInfo startInfo)
		{
			Process process;
			try
			{
				process = Process.Start(startInfo);
			}
			catch (Exception error) when (error is InvalidOperationException ||
				error is System.ComponentModel.Win32Exception || error is FileNotFoundException ||
				error is UnauthorizedAccessException)
			{
				return new MotifProcessOutput(false, null, null, null);
			}

			if (process == null)
				return new MotifProcessOutput(false, null, null, null);

			using (process)
			{
				var outputTask = process.StandardOutput.ReadToEndAsync();
				var errorTask = process.StandardError.ReadToEndAsync();
				process.WaitForExit();
				Task.WaitAll(outputTask, errorTask);
				return new MotifProcessOutput(true, process.ExitCode, outputTask.Result, errorTask.Result);
			}
		}

		[DataContract]
		private sealed class MotifJsonResponse
		{
			[DataMember(Name = "ok")]
			public bool? Ok { get; set; }
			[DataMember(Name = "applied")]
			public bool? Applied { get; set; }
			[DataMember(Name = "summary")]
			public string Summary { get; set; }
			[DataMember(Name = "code")]
			public string Code { get; set; }
		}
	}
}
