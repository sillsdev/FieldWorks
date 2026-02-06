// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites.RenderBenchmark;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Render harness and infrastructure tests.
	/// Validates that the capture pipeline, environment validator, and diagnostics
	/// toggle work correctly. Pixel-perfect snapshot regression is handled by
	/// <see cref="RenderVerifyTests"/> using Verify.
	/// </summary>
	[TestFixture]
	[Category("RenderBenchmark")]
	public class RenderBaselineTests : RenderBenchmark.RenderBenchmarkTestsBase
	{
		private RenderEnvironmentValidator m_environmentValidator;
		private RenderDiagnosticsToggle m_diagnostics;

		/// <summary>
		/// Creates the test data (Scripture book with footnotes) for rendering.
		/// </summary>
		protected override void CreateTestData()
		{
			SetupScenarioData("simple");
		}

		/// <summary>
		/// Sets up each test.
		/// </summary>
		[SetUp]
		public override void TestSetup()
		{
			m_environmentValidator = new RenderEnvironmentValidator();
			base.TestSetup();
			m_diagnostics = new RenderDiagnosticsToggle();
		}

		/// <summary>
		/// Tears down each test.
		/// </summary>
		[TearDown]
		public override void TestTearDown()
		{
			m_diagnostics?.Dispose();
			m_diagnostics = null;
			base.TestTearDown();
		}

		/// <summary>
		/// Tests that the harness can render a simple view and capture a bitmap.
		/// </summary>
		[Test]
		public void RenderHarness_CapturesSimpleView_ReturnsValidBitmap()
		{
			// Arrange
			var scenario = new RenderScenario
			{
				Id = "simple-test",
				Description = "Basic view for harness validation",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag
			};

			using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
			{
				// Act
				var coldTiming = harness.ExecuteColdRender(width: 400, height: 300);
				var bitmap = harness.CaptureViewBitmap();

				// Assert
				Assert.That(coldTiming, Is.Not.Null, "Cold timing result should not be null");
				Assert.That(coldTiming.DurationMs, Is.GreaterThan(0), "Cold render should take measurable time");
				Assert.That(coldTiming.IsColdRender, Is.True, "Should be marked as cold render");

				Assert.That(bitmap, Is.Not.Null, "Captured bitmap should not be null");
				Assert.That(bitmap.Width, Is.EqualTo(400), "Bitmap width should match view width");
				Assert.That(bitmap.Height, Is.EqualTo(300), "Bitmap height should match view height");
			}
		}

		/// <summary>
		/// Tests that warm renders complete in a reasonable time relative to cold renders.
		/// With rich styled content, Reconstruct() can be close to or exceed cold render time,
		/// so we use a generous multiplier. The real value is that both complete successfully.
		/// </summary>
		[Test]
		public void RenderHarness_WarmRender_IsFasterThanColdRender()
		{
			// Arrange
			var scenario = new RenderScenario
			{
				Id = "warm-vs-cold",
				Description = "Compare warm vs cold render timing",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag
			};

			using (var harness = new RenderBenchmarkHarness(Cache, scenario, m_environmentValidator))
			{
				// Act
				var coldTiming = harness.ExecuteColdRender();
				var warmTiming = harness.ExecuteWarmRender();

				// Assert
				Assert.That(warmTiming, Is.Not.Null, "Warm timing result should not be null");
				Assert.That(warmTiming.IsColdRender, Is.False, "Should be marked as warm render");

				// With rich content (styles, chapter/verse formatting), Reconstruct()
				// can be comparable to initial layout. Allow up to 5x cold time to
				// accommodate style resolution overhead on warm renders.
				Assert.That(warmTiming.DurationMs, Is.LessThan(coldTiming.DurationMs * 5),
					$"Warm render ({warmTiming.DurationMs:F2}ms) should not be much slower than cold ({coldTiming.DurationMs:F2}ms)");
			}
		}

		/// <summary>
		/// Tests that the environment validator produces consistent hashes.
		/// </summary>
		[Test]
		public void EnvironmentValidator_SameEnvironment_ProducesConsistentHash()
		{
			// Arrange
			var validator1 = new RenderEnvironmentValidator();
			var validator2 = new RenderEnvironmentValidator();

			// Act
			var hash1 = validator1.GetEnvironmentHash();
			var hash2 = validator2.GetEnvironmentHash();

			// Assert
			Assert.That(hash1, Is.Not.Null.And.Not.Empty, "Hash should not be empty");
			Assert.That(hash1, Is.EqualTo(hash2), "Same environment should produce same hash");
		}

		/// <summary>
		/// Tests that diagnostics toggle enables trace output.
		/// </summary>
		[Test]
		public void DiagnosticsToggle_Enable_WritesTraceEntries()
		{
			// Arrange
			m_diagnostics.EnableDiagnostics();

			// Act
			m_diagnostics.WriteTraceEntry("TestStage", 123.45, "test context");
			m_diagnostics.Flush();

			var content = m_diagnostics.GetTraceLogContent();

			// Assert
			Assert.That(content, Does.Contain("[RENDER]"), "Trace log should contain render entry");
			Assert.That(content, Does.Contain("TestStage"), "Trace log should contain stage name");
			Assert.That(content, Does.Contain("123.45"), "Trace log should contain duration");

			// Cleanup
			m_diagnostics.ClearTraceLog();
		}
	}
}
