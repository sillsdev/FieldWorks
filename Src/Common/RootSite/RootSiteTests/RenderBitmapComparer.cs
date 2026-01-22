// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Provides pixel-perfect bitmap comparison for render validation.
	/// Uses zero-tolerance comparison by default for deterministic validation.
	/// </summary>
	public class RenderBitmapComparer
	{
		/// <summary>
		/// Gets or sets the tolerance threshold for pixel differences (0-255).
		/// Default is 0 for pixel-perfect comparison.
		/// </summary>
		public int Tolerance { get; set; } = 0;

		/// <summary>
		/// Gets or sets whether to generate a diff image highlighting mismatches.
		/// </summary>
		public bool GenerateDiffImage { get; set; } = true;

		/// <summary>
		/// Compares two bitmaps for pixel-perfect equality.
		/// </summary>
		/// <param name="expected">The expected/baseline bitmap.</param>
		/// <param name="actual">The actual/rendered bitmap.</param>
		/// <returns>A comparison result with match status and mismatch details.</returns>
		public BitmapComparisonResult Compare(Bitmap expected, Bitmap actual)
		{
			if (expected == null)
				throw new ArgumentNullException(nameof(expected));
			if (actual == null)
				throw new ArgumentNullException(nameof(actual));

			var result = new BitmapComparisonResult
			{
				ExpectedWidth = expected.Width,
				ExpectedHeight = expected.Height,
				ActualWidth = actual.Width,
				ActualHeight = actual.Height
			};

			// Check dimensions first
			if (expected.Width != actual.Width || expected.Height != actual.Height)
			{
				result.IsMatch = false;
				result.MismatchReason = $"Dimension mismatch: expected {expected.Width}x{expected.Height}, got {actual.Width}x{actual.Height}";
				return result;
			}

			// Use fast pixel comparison via LockBits
			var mismatchCount = ComparePixels(expected, actual, out Bitmap diffImage);

			result.IsMatch = mismatchCount == 0;
			result.MismatchedPixelCount = mismatchCount;
			result.TotalPixelCount = expected.Width * expected.Height;
			result.MismatchPercentage = (double)mismatchCount / result.TotalPixelCount * 100;
			result.DiffImage = diffImage;

			if (!result.IsMatch)
			{
				result.MismatchReason = $"Pixel mismatch: {mismatchCount:N0} pixels differ ({result.MismatchPercentage:F2}%)";
			}

			return result;
		}

		/// <summary>
		/// Compares a rendered bitmap against a baseline snapshot file.
		/// </summary>
		/// <param name="baselineSnapshotPath">Path to the baseline PNG file.</param>
		/// <param name="actual">The actual rendered bitmap.</param>
		/// <returns>A comparison result.</returns>
		public BitmapComparisonResult CompareToBaseline(string baselineSnapshotPath, Bitmap actual)
		{
			if (string.IsNullOrEmpty(baselineSnapshotPath))
				throw new ArgumentNullException(nameof(baselineSnapshotPath));

			if (!File.Exists(baselineSnapshotPath))
			{
				return new BitmapComparisonResult
				{
					IsMatch = false,
					MismatchReason = $"Baseline snapshot not found: {baselineSnapshotPath}"
				};
			}

			using (var expected = new Bitmap(baselineSnapshotPath))
			{
				return Compare(expected, actual);
			}
		}

		/// <summary>
		/// Saves a comparison result's diff image to disk.
		/// </summary>
		/// <param name="result">The comparison result.</param>
		/// <param name="outputPath">The output path for the diff image.</param>
		public void SaveDiffImage(BitmapComparisonResult result, string outputPath)
		{
			if (result?.DiffImage == null)
				return;

			var directory = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			result.DiffImage.Save(outputPath, ImageFormat.Png);
		}

		private int ComparePixels(Bitmap expected, Bitmap actual, out Bitmap diffImage)
		{
			int width = expected.Width;
			int height = expected.Height;
			int mismatchCount = 0;

			diffImage = GenerateDiffImage ? new Bitmap(width, height, PixelFormat.Format32bppArgb) : null;

			// Lock bits for fast access
			var expectedData = expected.LockBits(
				new Rectangle(0, 0, width, height),
				ImageLockMode.ReadOnly,
				PixelFormat.Format32bppArgb);

			var actualData = actual.LockBits(
				new Rectangle(0, 0, width, height),
				ImageLockMode.ReadOnly,
				PixelFormat.Format32bppArgb);

			BitmapData diffData = null;
			if (diffImage != null)
			{
				diffData = diffImage.LockBits(
					new Rectangle(0, 0, width, height),
					ImageLockMode.WriteOnly,
					PixelFormat.Format32bppArgb);
			}

			try
			{
				int stride = expectedData.Stride;
				int bytes = Math.Abs(stride) * height;

				byte[] expectedPixels = new byte[bytes];
				byte[] actualPixels = new byte[bytes];
				byte[] diffPixels = diffData != null ? new byte[bytes] : null;

				Marshal.Copy(expectedData.Scan0, expectedPixels, 0, bytes);
				Marshal.Copy(actualData.Scan0, actualPixels, 0, bytes);

				for (int i = 0; i < bytes; i += 4)
				{
					// BGRA format
					bool match = Math.Abs(expectedPixels[i] - actualPixels[i]) <= Tolerance &&
								 Math.Abs(expectedPixels[i + 1] - actualPixels[i + 1]) <= Tolerance &&
								 Math.Abs(expectedPixels[i + 2] - actualPixels[i + 2]) <= Tolerance &&
								 Math.Abs(expectedPixels[i + 3] - actualPixels[i + 3]) <= Tolerance;

					if (!match)
					{
						mismatchCount++;
					}

					if (diffPixels != null)
					{
						if (match)
						{
							// Copy original pixel with reduced alpha for context
							diffPixels[i] = (byte)(actualPixels[i] / 2);
							diffPixels[i + 1] = (byte)(actualPixels[i + 1] / 2);
							diffPixels[i + 2] = (byte)(actualPixels[i + 2] / 2);
							diffPixels[i + 3] = 128;
						}
						else
						{
							// Highlight mismatch in red
							diffPixels[i] = 0;       // B
							diffPixels[i + 1] = 0;   // G
							diffPixels[i + 2] = 255; // R
							diffPixels[i + 3] = 255; // A
						}
					}
				}

				if (diffData != null && diffPixels != null)
				{
					Marshal.Copy(diffPixels, 0, diffData.Scan0, bytes);
				}
			}
			finally
			{
				expected.UnlockBits(expectedData);
				actual.UnlockBits(actualData);
				if (diffImage != null && diffData != null)
				{
					diffImage.UnlockBits(diffData);
				}
			}

			return mismatchCount;
		}
	}

	/// <summary>
	/// Represents the result of a bitmap comparison.
	/// </summary>
	public class BitmapComparisonResult
	{
		/// <summary>Gets or sets whether the bitmaps match.</summary>
		public bool IsMatch { get; set; }

		/// <summary>Gets or sets the reason for mismatch (if any).</summary>
		public string MismatchReason { get; set; }

		/// <summary>Gets or sets the number of mismatched pixels.</summary>
		public int MismatchedPixelCount { get; set; }

		/// <summary>Gets or sets the total pixel count.</summary>
		public int TotalPixelCount { get; set; }

		/// <summary>Gets or sets the mismatch percentage.</summary>
		public double MismatchPercentage { get; set; }

		/// <summary>Gets or sets the expected bitmap width.</summary>
		public int ExpectedWidth { get; set; }

		/// <summary>Gets or sets the expected bitmap height.</summary>
		public int ExpectedHeight { get; set; }

		/// <summary>Gets or sets the actual bitmap width.</summary>
		public int ActualWidth { get; set; }

		/// <summary>Gets or sets the actual bitmap height.</summary>
		public int ActualHeight { get; set; }

		/// <summary>Gets or sets the diff image highlighting mismatches.</summary>
		public Bitmap DiffImage { get; set; }
	}
}
