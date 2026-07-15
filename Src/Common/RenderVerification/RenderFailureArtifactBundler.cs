// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SIL.FieldWorks.Common.RenderVerification
{
	/// <summary>
	/// Bundles render/parity verification FAILURE artifacts into a single CI-discoverable folder so a
	/// failed snapshot comparison is diagnosable from the build output (task 2.5 of
	/// lexical-edit-avalonia-migration). Given a failed <see cref="RenderBaselineVerificationResult"/>
	/// it copies the received image, received metadata, and diff image into one folder and writes a
	/// <c>failure-summary.json</c> describing the test and the pixel-diff metrics.
	///
	/// It is defensive: missing source files (e.g. a missing-baseline failure has no diff) are skipped,
	/// and writing a bundle never throws into the calling test. Use it on the failure path of a render
	/// test before asserting, so the artifacts exist regardless of the assertion outcome.
	/// </summary>
	public static class RenderFailureArtifactBundler
	{
		/// <summary>
		/// Writes a failure-artifact bundle for a failed verification and returns the bundle folder path.
		/// Returns null when <paramref name="result"/> is null or passed (nothing to bundle).
		/// </summary>
		/// <param name="result">The failed verification result.</param>
		/// <param name="testClassName">Owning test class (for the bundle name and summary).</param>
		/// <param name="testMethodName">Owning test method (for the bundle name and summary).</param>
		/// <param name="scenarioId">Scenario identifier (for the bundle name and summary).</param>
		/// <param name="outputRoot">
		/// Optional root folder for failure bundles. When null, a <c>_RenderFailures</c> folder next to
		/// the received/baseline artifact is used so the location is always writable.
		/// </param>
		public static string BundleFailureArtifacts(
			RenderBaselineVerificationResult result,
			string testClassName,
			string testMethodName,
			string scenarioId,
			string outputRoot = null)
		{
			if (result == null || result.Passed)
			{
				return null;
			}

			try
			{
				var baseDir = outputRoot ?? Path.Combine(DeriveBaseDirectory(result), "_RenderFailures");
				var runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
				var bundleName = Sanitize($"{testClassName}_{testMethodName}_{scenarioId}");
				var bundleFolder = Path.Combine(baseDir, runId, bundleName);
				Directory.CreateDirectory(bundleFolder);

				CopyIfPresent(result.ReceivedPath, Path.Combine(bundleFolder, "actual.png"));
				CopyIfPresent(result.ReceivedMetadataPath, Path.Combine(bundleFolder, "actual-metadata.json"));
				CopyIfPresent(result.DiffPath, Path.Combine(bundleFolder, "diff.png"));
				CopyIfPresent(result.DiffMetadataPath, Path.Combine(bundleFolder, "diff-metadata.json"));

				if (!string.IsNullOrEmpty(result.VerifiedPath))
				{
					File.WriteAllText(
						Path.Combine(bundleFolder, "expected-image-path.txt"),
						result.VerifiedPath);
				}

				File.WriteAllText(
					Path.Combine(bundleFolder, "failure-summary.json"),
					BuildFailureSummaryJson(result, testClassName, testMethodName, scenarioId, bundleFolder));

				return bundleFolder;
			}
			catch (Exception ex)
			{
				// Bundling is best-effort diagnostics; never mask the real test failure.
				Console.Error.WriteLine($"RenderFailureArtifactBundler failed: {ex.Message}");
				return null;
			}
		}

		private static string DeriveBaseDirectory(RenderBaselineVerificationResult result)
		{
			foreach (var path in new[] { result.ReceivedPath, result.VerifiedPath, result.DiffPath })
			{
				if (!string.IsNullOrEmpty(path))
				{
					var dir = Path.GetDirectoryName(path);
					if (!string.IsNullOrEmpty(dir))
					{
						return dir;
					}
				}
			}

			return Path.GetTempPath();
		}

		private static void CopyIfPresent(string source, string destination)
		{
			if (!string.IsNullOrEmpty(source) && File.Exists(source))
			{
				File.Copy(source, destination, overwrite: true);
			}
		}

		private static string Sanitize(string value)
		{
			var sb = new StringBuilder(value.Length);
			foreach (var c in value)
			{
				sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 ? '_' : c);
			}

			return sb.ToString();
		}

		private static string BuildFailureSummaryJson(
			RenderBaselineVerificationResult result,
			string testClassName,
			string testMethodName,
			string scenarioId,
			string bundleFolder)
		{
			var diff = result.DiffSummary;
			var fields = new List<string>
			{
				JsonField("testClassName", testClassName),
				JsonField("testMethodName", testMethodName),
				JsonField("scenarioId", scenarioId),
				JsonField("capturedAtUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)),
				JsonField("failureKind", DetermineFailureKind(result)),
				JsonField("failureMessage", result.FailureMessage ?? ""),
				JsonField("bundleFolder", bundleFolder),
				JsonField("expectedImagePath", result.VerifiedPath ?? ""),
				JsonField("actualImagePath", result.ReceivedPath ?? ""),
				JsonField("diffImagePath", result.DiffPath ?? "")
			};

			if (diff != null)
			{
				fields.Add(JsonRaw("differentPixelCount", diff.DifferentPixelCount.ToString(CultureInfo.InvariantCulture)));
				fields.Add(JsonRaw("diffRegionWidth", diff.DiffRegionWidth.ToString(CultureInfo.InvariantCulture)));
				fields.Add(JsonRaw("diffRegionHeight", diff.DiffRegionHeight.ToString(CultureInfo.InvariantCulture)));
			}

			return "{" + string.Join(",", fields) + "}";
		}

		private static string DetermineFailureKind(RenderBaselineVerificationResult result)
		{
			if (result.DiffSummary != null && result.DiffSummary.DifferentPixelCount > 0)
			{
				return "pixel-mismatch";
			}

			if (string.IsNullOrEmpty(result.VerifiedPath) || !File.Exists(result.VerifiedPath))
			{
				return "missing-baseline";
			}

			return "verification-failed";
		}

		private static string JsonField(string name, string value)
			=> $"\"{name}\":\"{Escape(value)}\"";

		private static string JsonRaw(string name, string rawValue)
			=> $"\"{name}\":{rawValue}";

		private static string Escape(string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return "";
			}

			return value
				.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("\r", "\\r")
				.Replace("\n", "\\n")
				.Replace("\t", "\\t");
		}
	}
}
