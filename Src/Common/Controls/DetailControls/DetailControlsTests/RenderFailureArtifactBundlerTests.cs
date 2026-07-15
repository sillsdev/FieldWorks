// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.RenderVerification;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Tests for <see cref="RenderFailureArtifactBundler"/> (task 2.5): on a failed render/parity
	/// verification, the bundler must gather the available artifacts and a summary into one
	/// CI-discoverable folder, and must be a no-op for a passing result.
	/// </summary>
	[TestFixture]
	public class RenderFailureArtifactBundlerTests
	{
		private string m_workDir;

		[SetUp]
		public void SetUp()
		{
			m_workDir = Path.Combine(Path.GetTempPath(), "FwRenderBundlerTests", Path.GetRandomFileName());
			Directory.CreateDirectory(m_workDir);
		}

		[TearDown]
		public void TearDown()
		{
			try
			{
				if (Directory.Exists(m_workDir))
				{
					Directory.Delete(m_workDir, recursive: true);
				}
			}
			catch
			{
				// Best-effort cleanup.
			}
		}

		[Test]
		public void BundleFailureArtifacts_PassingResult_ReturnsNull()
		{
			var result = new RenderBaselineVerificationResult { Passed = true };
			var folder = RenderFailureArtifactBundler.BundleFailureArtifacts(
				result, "MyTests", "MyMethod", "scenario");
			Assert.That(folder, Is.Null, "A passing verification has nothing to bundle.");
		}

		[Test]
		public void BundleFailureArtifacts_NullResult_ReturnsNull()
		{
			Assert.That(
				RenderFailureArtifactBundler.BundleFailureArtifacts(null, "C", "M", "s"),
				Is.Null);
		}

		[Test]
		public void BundleFailureArtifacts_PixelMismatch_CopiesArtifactsAndWritesSummary()
		{
			// Arrange: simulate a pixel-mismatch failure with received + diff images present.
			var received = Path.Combine(m_workDir, "snap.received.png");
			var receivedMeta = Path.Combine(m_workDir, "snap.received.json");
			var diff = Path.Combine(m_workDir, "snap.diff.png");
			File.WriteAllText(received, "fake-png-bytes");
			File.WriteAllText(receivedMeta, "{}");
			File.WriteAllText(diff, "fake-diff-bytes");

			var outputRoot = Path.Combine(m_workDir, "out");
			var result = new RenderBaselineVerificationResult
			{
				Passed = false,
				FailureMessage = "pixels differ",
				VerifiedPath = Path.Combine(m_workDir, "snap.verified.png"),
				ReceivedPath = received,
				ReceivedMetadataPath = receivedMeta,
				DiffPath = diff,
				DiffSummary = new RenderPixelDiffSummary
				{
					DifferentPixelCount = 42,
					DiffRegionWidth = 10,
					DiffRegionHeight = 5
				}
			};

			// Act
			var folder = RenderFailureArtifactBundler.BundleFailureArtifacts(
				result, "DataTreeRenderTests", "DataTreeRender_simple", "simple", outputRoot);

			// Assert: bundle folder under the provided root, with copied artifacts and a summary.
			Assert.That(folder, Is.Not.Null);
			Assert.That(Directory.Exists(folder), Is.True);
			Assert.That(folder, Does.StartWith(outputRoot));
			Assert.That(File.Exists(Path.Combine(folder, "actual.png")), Is.True);
			Assert.That(File.Exists(Path.Combine(folder, "actual-metadata.json")), Is.True);
			Assert.That(File.Exists(Path.Combine(folder, "diff.png")), Is.True);
			Assert.That(File.Exists(Path.Combine(folder, "expected-image-path.txt")), Is.True);

			var summaryPath = Path.Combine(folder, "failure-summary.json");
			Assert.That(File.Exists(summaryPath), Is.True);
			var summary = File.ReadAllText(summaryPath);
			Assert.That(summary, Does.Contain("\"failureKind\":\"pixel-mismatch\""));
			Assert.That(summary, Does.Contain("\"differentPixelCount\":42"));
			Assert.That(summary, Does.Contain("\"scenarioId\":\"simple\""));
		}

		[Test]
		public void BundleFailureArtifacts_MissingBaseline_StillBundlesSummary()
		{
			// Arrange: missing-baseline failure (only a received image, no diff, baseline absent).
			var received = Path.Combine(m_workDir, "snap.received.png");
			File.WriteAllText(received, "fake-png-bytes");

			var outputRoot = Path.Combine(m_workDir, "out");
			var result = new RenderBaselineVerificationResult
			{
				Passed = false,
				FailureMessage = "no baseline",
				VerifiedPath = Path.Combine(m_workDir, "does-not-exist.verified.png"),
				ReceivedPath = received
			};

			// Act
			var folder = RenderFailureArtifactBundler.BundleFailureArtifacts(
				result, "DataTreeRenderTests", "DataTreeRender_simple", "simple", outputRoot);

			// Assert: bundle exists; diff is absent; summary classifies it as missing-baseline.
			Assert.That(folder, Is.Not.Null);
			Assert.That(File.Exists(Path.Combine(folder, "actual.png")), Is.True);
			Assert.That(File.Exists(Path.Combine(folder, "diff.png")), Is.False, "No diff for a missing baseline.");
			var summary = File.ReadAllText(Path.Combine(folder, "failure-summary.json"));
			Assert.That(summary, Does.Contain("\"failureKind\":\"missing-baseline\""));
		}
	}
}
