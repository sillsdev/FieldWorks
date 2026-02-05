// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites.RenderBenchmark;
using VerifyTests;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Snapshot tests using Verify (base package) for pixel-perfect render baseline validation.
	/// We use InnerVerifier directly because Verify.NUnit requires NUnit 4.x,
	/// and FieldWorks is pinned to NUnit 3.13.3.
	///
	/// Verify manages .verified.png files automatically — on first run it creates a
	/// .received.png that must be accepted (copied to .verified.png). Subsequent runs
	/// compare against the committed .verified.png baseline.
	/// </summary>
	[TestFixture]
	[Category("RenderBenchmark")]
	public class RenderVerifyTests : RenderBenchmarkTestsBase
	{
		/// <summary>
		/// Creates the test data (Scripture book with rich content) for rendering.
		/// </summary>
		protected override void CreateTestData()
		{
			SetupScenarioData("simple");
		}

		/// <summary>
		/// Verifies that the simple Scripture scenario renders consistently.
		/// On first run, creates the .received.png for acceptance.
		/// On subsequent runs, compares against the .verified.png baseline.
		/// </summary>
		[Test]
		public async Task SimpleScenario_MatchesVerifiedSnapshot()
		{
			// Arrange
			var scenario = new RenderScenario
			{
				Id = "simple",
				Description = "Baseline Scripture rendering with styles",
				RootObjectHvo = m_hvoRoot,
				RootFlid = m_flidContainingTexts,
				FragmentId = m_frag
			};

			using (var harness = new RenderBenchmarkHarness(Cache, scenario))
			{
				// Act — cold render to capture the first paint
				harness.ExecuteColdRender();
				using (var bitmap = harness.CaptureViewBitmap())
				{
					// Convert bitmap to PNG stream for Verify.
					// Note: InnerVerifier.VerifyStream disposes the stream, so we
					// do NOT wrap it in a using block here.
					var stream = new MemoryStream();
					bitmap.Save(stream, ImageFormat.Png);
					stream.Position = 0;

					// Use InnerVerifier directly (Verify.NUnit requires NUnit 4.x).
					// The directory is where .verified.png / .received.png files live.
					string directory = GetSourceFileDirectory();
					string name = "RenderVerifyTests.SimpleScenario_MatchesVerifiedSnapshot";
					using (var verifier = new InnerVerifier(directory, name))
					{
						await verifier.VerifyStream(stream, "png", null);
					}
				}
			}
		}

		/// <summary>
		/// Returns the directory containing this source file (for Verify file placement).
		/// Uses [CallerFilePath] to resolve at compile time.
		/// </summary>
		private static string GetSourceFileDirectory([CallerFilePath] string sourceFile = "")
		{
			return Path.GetDirectoryName(sourceFile);
		}
	}
}
