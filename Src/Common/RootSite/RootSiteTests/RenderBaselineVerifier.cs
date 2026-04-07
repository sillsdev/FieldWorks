using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;

namespace SIL.FieldWorks.Common.RootSites
{
	internal static class RenderBaselineVerifier
	{
		private const string UpdateBaselinesEnvVar = "FW_UPDATE_RENDER_BASELINES";
		private const int MaxAllowedPixelDifferences = 4;
		private const int PerChannelTolerance = 8;
		private const int AlphaTolerance = 8;

		internal static string GetSourceFileDirectory([CallerFilePath] string sourceFile = "")
		{
			return Path.GetDirectoryName(sourceFile);
		}

		internal static RenderBaselineVerificationResult Verify(Bitmap actualBitmap, string directory, string name, string scenarioId)
		{
			string verifiedPath = Path.Combine(directory, $"{name}.verified.png");
			string receivedPath = Path.Combine(directory, $"{name}.received.png");
			string diffPath = Path.Combine(directory, $"{name}.diff.png");

			RefreshVerifiedBaselineIfRequested(actualBitmap, verifiedPath);

			if (File.Exists(diffPath))
				File.Delete(diffPath);

			if (!File.Exists(verifiedPath))
			{
				actualBitmap.Save(receivedPath, ImageFormat.Png);
				return new RenderBaselineVerificationResult
				{
					Passed = false,
					FailureMessage = $"Missing verified render baseline for '{scenarioId}'. Review and accept {receivedPath} as the new baseline.",
					VerifiedPath = verifiedPath,
					DiffPath = diffPath
				};
			}

			using (var expectedBitmap = new Bitmap(verifiedPath))
			{
				int differentPixelCount = CountDifferentPixels(expectedBitmap, actualBitmap);
				if (differentPixelCount <= MaxAllowedPixelDifferences)
				{
					DeleteIfPresent(receivedPath);
					DeleteIfPresent(diffPath);
					return new RenderBaselineVerificationResult
					{
						Passed = true,
						VerifiedPath = verifiedPath,
						DiffPath = diffPath
					};
				}

				using (var diffBitmap = CreateDiffBitmap(expectedBitmap, actualBitmap))
				{
					diffBitmap.Save(diffPath, ImageFormat.Png);
				}

				actualBitmap.Save(receivedPath, ImageFormat.Png);

				return new RenderBaselineVerificationResult
				{
					Passed = false,
					FailureMessage =
						$"Render output for '{scenarioId}' differed from baseline by {differentPixelCount} pixels; " +
						$"{MaxAllowedPixelDifferences} or fewer differences are allowed. See {diffPath}.",
					VerifiedPath = verifiedPath,
					DiffPath = diffPath
				};
			}
		}

		private static void RefreshVerifiedBaselineIfRequested(Bitmap bitmap, string verifiedPath)
		{
			if (!string.Equals(Environment.GetEnvironmentVariable(UpdateBaselinesEnvVar), "1", StringComparison.Ordinal))
				return;

			bitmap.Save(verifiedPath, ImageFormat.Png);
		}

		private static void DeleteIfPresent(string path)
		{
			if (File.Exists(path))
				File.Delete(path);
		}

		private static int CountDifferentPixels(Bitmap expectedBitmap, Bitmap actualBitmap)
		{
			int maxWidth = Math.Max(expectedBitmap.Width, actualBitmap.Width);
			int maxHeight = Math.Max(expectedBitmap.Height, actualBitmap.Height);
			int differentPixelCount = 0;

			for (int y = 0; y < maxHeight; y++)
			{
				for (int x = 0; x < maxWidth; x++)
				{
					bool expectedInBounds = x < expectedBitmap.Width && y < expectedBitmap.Height;
					bool actualInBounds = x < actualBitmap.Width && y < actualBitmap.Height;

					if (!expectedInBounds || !actualInBounds)
					{
						differentPixelCount++;
						continue;
					}

					var expected = expectedBitmap.GetPixel(x, y);
					var actual = actualBitmap.GetPixel(x, y);

					if (!PixelsAreEquivalent(expected, actual))
						differentPixelCount++;
				}
			}

			return differentPixelCount;
		}

		private static Bitmap CreateDiffBitmap(Bitmap expectedBitmap, Bitmap actualBitmap)
		{
			int maxWidth = Math.Max(expectedBitmap.Width, actualBitmap.Width);
			int maxHeight = Math.Max(expectedBitmap.Height, actualBitmap.Height);
			var diffBitmap = new Bitmap(maxWidth, maxHeight);

			for (int y = 0; y < maxHeight; y++)
			{
				for (int x = 0; x < maxWidth; x++)
				{
					Color expected = x < expectedBitmap.Width && y < expectedBitmap.Height
						? expectedBitmap.GetPixel(x, y)
						: Color.White;
					Color actual = x < actualBitmap.Width && y < actualBitmap.Height
						? actualBitmap.GetPixel(x, y)
						: Color.White;

					diffBitmap.SetPixel(x, y, CreateDiffPixel(expected, actual));
				}
			}

			return diffBitmap;
		}

		private static Color CreateDiffPixel(Color expected, Color actual)
		{
			return Color.FromArgb(
				255,
				ScaleDiffChannel(expected.R, actual.R),
				ScaleDiffChannel(expected.G, actual.G),
				ScaleDiffChannel(expected.B, actual.B));
		}

		private static int ScaleDiffChannel(int expected, int actual)
		{
			return Math.Min(255, Math.Abs(expected - actual) * 4);
		}

		private static bool PixelsAreEquivalent(Color expected, Color actual)
		{
			return Math.Abs(expected.A - actual.A) <= AlphaTolerance &&
				Math.Abs(expected.R - actual.R) <= PerChannelTolerance &&
				Math.Abs(expected.G - actual.G) <= PerChannelTolerance &&
				Math.Abs(expected.B - actual.B) <= PerChannelTolerance;
		}
	}

	internal sealed class RenderBaselineVerificationResult
	{
		internal bool Passed { get; set; }
		internal string FailureMessage { get; set; }
		internal string VerifiedPath { get; set; }
		internal string DiffPath { get; set; }
	}
}