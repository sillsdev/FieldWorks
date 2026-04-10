using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace SIL.FieldWorks.Common.RenderVerification
{
	public static class RenderSnapshotVerifier
	{
		private const string UpdateBaselinesEnvVar = "FW_UPDATE_RENDER_BASELINES";
		private const string FontQualityEnvVar = "FW_FONT_QUALITY";
		private const int MaxAllowedPixelDifferences = 4;
		private const string DeterministicRenderFontFamily = "Segoe UI";
		private const int DpiAwarenessInvalid = -1;
		private const int DpiAwarenessUnaware = 0;
		private const int DpiAwarenessSystemAware = 1;
		private const int DpiAwarenessPerMonitorAware = 2;

		[DllImport("user32.dll")]
		private static extern IntPtr GetThreadDpiAwarenessContext();

		[DllImport("user32.dll")]
		private static extern int GetAwarenessFromDpiAwarenessContext(IntPtr dpiContext);

		public static string GetSourceFileDirectory([CallerFilePath] string sourceFile = "")
		{
			return Path.GetDirectoryName(sourceFile);
		}

		public static RenderBaselineVerificationResult Verify(Bitmap actualBitmap, string directory, string name, string scenarioId)
		{
			if (actualBitmap == null)
				throw new ArgumentNullException(nameof(actualBitmap));
			if (string.IsNullOrEmpty(directory))
				throw new ArgumentException("A snapshot directory is required.", nameof(directory));
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("A snapshot name is required.", nameof(name));
			if (string.IsNullOrEmpty(scenarioId))
				throw new ArgumentException("A scenario identifier is required.", nameof(scenarioId));

			string snapshotBasePath = Path.Combine(directory, name);
			string verifiedPath = snapshotBasePath + ".verified.png";
			string verifiedMetadataPath = snapshotBasePath + ".verified.json";
			string receivedPath = snapshotBasePath + ".received.png";
			string receivedMetadataPath = snapshotBasePath + ".received.json";
			string diffPath = snapshotBasePath + ".diff.png";
			string diffMetadataPath = snapshotBasePath + ".diff.json";

			var currentArtifact = CreateCurrentArtifact(actualBitmap, scenarioId, name, receivedPath, receivedMetadataPath);
			RefreshVerifiedBaselineIfRequested(actualBitmap, currentArtifact, verifiedPath, verifiedMetadataPath);

			DeleteIfPresent(diffPath);
			DeleteIfPresent(diffMetadataPath);

			if (!File.Exists(verifiedPath))
			{
				actualBitmap.Save(receivedPath, ImageFormat.Png);
				SaveJson(receivedMetadataPath, currentArtifact.Metadata);

				return new RenderBaselineVerificationResult
				{
					Passed = false,
					FailureMessage = BuildMissingBaselineMessage(scenarioId, verifiedPath, currentArtifact),
					VerifiedPath = verifiedPath,
					VerifiedMetadataPath = verifiedMetadataPath,
					ReceivedPath = receivedPath,
					ReceivedMetadataPath = receivedMetadataPath,
					DiffPath = diffPath,
					DiffMetadataPath = diffMetadataPath
				};
			}

			using (var expectedBitmap = new Bitmap(verifiedPath))
			{
				var savedArtifact = LoadSavedArtifact(expectedBitmap, verifiedPath, verifiedMetadataPath);
				var diffSummary = CompareBitmaps(expectedBitmap, actualBitmap);
				if (diffSummary.DifferentPixelCount <= MaxAllowedPixelDifferences)
				{
					DeleteIfPresent(receivedPath);
					DeleteIfPresent(receivedMetadataPath);
					DeleteIfPresent(diffPath);
					DeleteIfPresent(diffMetadataPath);
					return new RenderBaselineVerificationResult
					{
						Passed = true,
						VerifiedPath = verifiedPath,
						VerifiedMetadataPath = verifiedMetadataPath,
						ReceivedPath = receivedPath,
						ReceivedMetadataPath = receivedMetadataPath,
						DiffPath = diffPath,
						DiffMetadataPath = diffMetadataPath,
						DiffSummary = diffSummary
					};
				}

				using (var diffBitmap = CreateDiffBitmap(expectedBitmap, actualBitmap))
				{
					diffBitmap.Save(diffPath, ImageFormat.Png);
				}

				actualBitmap.Save(receivedPath, ImageFormat.Png);
				SaveJson(receivedMetadataPath, currentArtifact.Metadata);

				var comparisonReport = new RenderSnapshotComparisonReport
				{
					ScenarioId = scenarioId,
					SnapshotName = name,
					AllowedDifferentPixelCount = MaxAllowedPixelDifferences,
					SavedBaseline = savedArtifact,
					CurrentRun = currentArtifact,
					Diff = diffSummary,
					Differences = BuildDifferences(savedArtifact, currentArtifact)
				};
				SaveJson(diffMetadataPath, comparisonReport);

				return new RenderBaselineVerificationResult
				{
					Passed = false,
					FailureMessage = BuildFailureMessage(scenarioId, diffPath, diffMetadataPath, comparisonReport),
					VerifiedPath = verifiedPath,
					VerifiedMetadataPath = verifiedMetadataPath,
					ReceivedPath = receivedPath,
					ReceivedMetadataPath = receivedMetadataPath,
					DiffPath = diffPath,
					DiffMetadataPath = diffMetadataPath,
					DiffSummary = diffSummary
				};
			}
		}

		private static RenderSnapshotArtifact CreateCurrentArtifact(
			Bitmap actualBitmap,
			string scenarioId,
			string snapshotName,
			string receivedPath,
			string receivedMetadataPath)
		{
			return new RenderSnapshotArtifact
			{
				ArtifactKind = "current",
				ImagePath = receivedPath,
				MetadataPath = receivedMetadataPath,
				ImageWidth = actualBitmap.Width,
				ImageHeight = actualBitmap.Height,
				MetadataAvailable = true,
				Metadata = CaptureMetadata(actualBitmap, scenarioId, snapshotName)
			};
		}

		private static RenderSnapshotArtifact LoadSavedArtifact(Bitmap expectedBitmap, string verifiedPath, string verifiedMetadataPath)
		{
			var artifact = new RenderSnapshotArtifact
			{
				ArtifactKind = "saved baseline",
				ImagePath = verifiedPath,
				MetadataPath = verifiedMetadataPath,
				ImageWidth = expectedBitmap.Width,
				ImageHeight = expectedBitmap.Height
			};

			var fileInfo = new FileInfo(verifiedPath);
			if (fileInfo.Exists)
				artifact.FileLastWriteUtc = fileInfo.LastWriteTimeUtc;

			artifact.Metadata = LoadMetadata(verifiedMetadataPath, out var loadError);
			artifact.MetadataAvailable = artifact.Metadata != null;
			artifact.MetadataLoadError = loadError;
			return artifact;
		}

		private static RenderSnapshotMetadata CaptureMetadata(Bitmap actualBitmap, string scenarioId, string snapshotName)
		{
			var validator = new RenderEnvironmentValidator();
			return new RenderSnapshotMetadata
			{
				ScenarioId = scenarioId,
				SnapshotName = snapshotName,
				CapturedAtUtc = DateTime.UtcNow,
				ImageWidth = actualBitmap.Width,
				ImageHeight = actualBitmap.Height,
				MachineName = Environment.MachineName,
				OsVersion = Environment.OSVersion.VersionString,
				EnvironmentHash = validator.GetEnvironmentHash(),
				Environment = validator.CurrentSettings,
				DpiAwareness = GetDpiAwarenessDescription(),
				FontQuality = Environment.GetEnvironmentVariable(FontQualityEnvVar) ?? string.Empty,
				DeterministicFontFamily = DeterministicRenderFontFamily,
				DeterministicFontInstalled = IsFontInstalled(DeterministicRenderFontFamily)
			};
		}

		private static void RefreshVerifiedBaselineIfRequested(
			Bitmap bitmap,
			RenderSnapshotArtifact currentArtifact,
			string verifiedPath,
			string verifiedMetadataPath)
		{
			if (!string.Equals(Environment.GetEnvironmentVariable(UpdateBaselinesEnvVar), "1", StringComparison.Ordinal))
				return;

			bitmap.Save(verifiedPath, ImageFormat.Png);
			SaveJson(verifiedMetadataPath, CloneMetadata(currentArtifact.Metadata));
		}

		private static RenderSnapshotMetadata CloneMetadata(RenderSnapshotMetadata metadata)
		{
			if (metadata == null)
				return null;

			return new RenderSnapshotMetadata
			{
				ScenarioId = metadata.ScenarioId,
				SnapshotName = metadata.SnapshotName,
				CapturedAtUtc = metadata.CapturedAtUtc,
				ImageWidth = metadata.ImageWidth,
				ImageHeight = metadata.ImageHeight,
				MachineName = metadata.MachineName,
				OsVersion = metadata.OsVersion,
				EnvironmentHash = metadata.EnvironmentHash,
				Environment = metadata.Environment == null
					? null
					: new EnvironmentSettings
					{
						DpiX = metadata.Environment.DpiX,
						DpiY = metadata.Environment.DpiY,
						FontSmoothing = metadata.Environment.FontSmoothing,
						ClearTypeEnabled = metadata.Environment.ClearTypeEnabled,
						ThemeName = metadata.Environment.ThemeName,
						TextScaleFactor = metadata.Environment.TextScaleFactor,
						ScreenWidth = metadata.Environment.ScreenWidth,
						ScreenHeight = metadata.Environment.ScreenHeight,
						CultureName = metadata.Environment.CultureName
					},
				DpiAwareness = metadata.DpiAwareness,
				FontQuality = metadata.FontQuality,
				DeterministicFontFamily = metadata.DeterministicFontFamily,
				DeterministicFontInstalled = metadata.DeterministicFontInstalled
			};
		}

		private static List<string> BuildDifferences(RenderSnapshotArtifact savedArtifact, RenderSnapshotArtifact currentArtifact)
		{
			var differences = new List<string>();
			AddDifference(differences, "image", FormatImageSize(savedArtifact.ImageWidth, savedArtifact.ImageHeight), FormatImageSize(currentArtifact.ImageWidth, currentArtifact.ImageHeight));

			if (!savedArtifact.MetadataAvailable || currentArtifact.Metadata == null)
				return differences;

			AddDifference(differences, "environmentHash", Shorten(savedArtifact.Metadata.EnvironmentHash), Shorten(currentArtifact.Metadata.EnvironmentHash));
			AddDifference(differences, "DPI", FormatDpi(savedArtifact.Metadata.Environment), FormatDpi(currentArtifact.Metadata.Environment));
			AddDifference(differences, "screen", FormatScreen(savedArtifact.Metadata.Environment), FormatScreen(currentArtifact.Metadata.Environment));
			AddDifference(differences, "textScale", FormatTextScale(savedArtifact.Metadata.Environment), FormatTextScale(currentArtifact.Metadata.Environment));
			AddDifference(differences, "dpiAwareness", savedArtifact.Metadata.DpiAwareness, currentArtifact.Metadata.DpiAwareness);
			AddDifference(differences, "fontSmoothing", FormatBoolean(savedArtifact.Metadata.Environment != null && savedArtifact.Metadata.Environment.FontSmoothing), FormatBoolean(currentArtifact.Metadata.Environment != null && currentArtifact.Metadata.Environment.FontSmoothing));
			AddDifference(differences, "clearType", FormatBoolean(savedArtifact.Metadata.Environment != null && savedArtifact.Metadata.Environment.ClearTypeEnabled), FormatBoolean(currentArtifact.Metadata.Environment != null && currentArtifact.Metadata.Environment.ClearTypeEnabled));
			AddDifference(differences, "theme", savedArtifact.Metadata.Environment != null ? savedArtifact.Metadata.Environment.ThemeName : string.Empty, currentArtifact.Metadata.Environment != null ? currentArtifact.Metadata.Environment.ThemeName : string.Empty);
			AddDifference(differences, "culture", savedArtifact.Metadata.Environment != null ? savedArtifact.Metadata.Environment.CultureName : string.Empty, currentArtifact.Metadata.Environment != null ? currentArtifact.Metadata.Environment.CultureName : string.Empty);
			AddDifference(differences, "FW_FONT_QUALITY", savedArtifact.Metadata.FontQuality, currentArtifact.Metadata.FontQuality);
			AddDifference(differences, "deterministicFontInstalled", FormatBoolean(savedArtifact.Metadata.DeterministicFontInstalled), FormatBoolean(currentArtifact.Metadata.DeterministicFontInstalled));
			return differences;
		}

		private static void AddDifference(List<string> differences, string label, string savedValue, string currentValue)
		{
			if (string.Equals(savedValue ?? string.Empty, currentValue ?? string.Empty, StringComparison.Ordinal))
				return;

			differences.Add(string.Format(CultureInfo.InvariantCulture, "{0} saved={1} current={2}", label, savedValue ?? "(null)", currentValue ?? "(null)"));
		}

		private static string BuildMissingBaselineMessage(string scenarioId, string verifiedPath, RenderSnapshotArtifact currentArtifact)
		{
			var builder = new StringBuilder();
			builder.AppendFormat(CultureInfo.InvariantCulture,
				"Missing verified render baseline for '{0}'. Review and accept {1} as the new baseline.",
				scenarioId,
				currentArtifact.ImagePath);
			builder.AppendLine();
			builder.AppendLine(FormatArtifactLine("Current run", currentArtifact));
			builder.AppendFormat(CultureInfo.InvariantCulture,
				"Expected baseline location: {0}. Current metadata: {1}.",
				verifiedPath,
				currentArtifact.MetadataPath);
			return builder.ToString();
		}

		private static string BuildFailureMessage(
			string scenarioId,
			string diffPath,
			string diffMetadataPath,
			RenderSnapshotComparisonReport report)
		{
			var builder = new StringBuilder();
			builder.AppendFormat(CultureInfo.InvariantCulture,
				"Render output for '{0}' differed from baseline by {1} pixels; {2} or fewer differences are allowed.",
				scenarioId,
				report.Diff.DifferentPixelCount,
				report.AllowedDifferentPixelCount);
			builder.AppendLine();
			builder.AppendLine(FormatArtifactLine("Saved baseline", report.SavedBaseline));
			builder.AppendLine(FormatArtifactLine("Current run", report.CurrentRun));
			builder.AppendFormat(CultureInfo.InvariantCulture,
				"Diff composition: inBounds={0}; savedOnly={1}; currentOnly={2}; region={3}.",
				report.Diff.InBoundsPixelDifferences,
				report.Diff.ExpectedOnlyPixelDifferences,
				report.Diff.ActualOnlyPixelDifferences,
				FormatDiffRegion(report.Diff));
			builder.AppendLine();
			if (report.Differences.Count > 0)
			{
				builder.Append("Key differences: ");
				builder.Append(string.Join("; ", report.Differences));
				builder.AppendLine();
			}
			builder.AppendFormat(CultureInfo.InvariantCulture,
				"Artifacts: diff={0}; received={1}; currentMetadata={2}; comparison={3}.",
				diffPath,
				report.CurrentRun.ImagePath,
				report.CurrentRun.MetadataPath,
				diffMetadataPath);
			return builder.ToString();
		}

		private static string FormatArtifactLine(string label, RenderSnapshotArtifact artifact)
		{
			var builder = new StringBuilder();
			builder.Append(label);
			builder.Append(": image=");
			builder.Append(FormatImageSize(artifact.ImageWidth, artifact.ImageHeight));
			if (artifact.FileLastWriteUtc.HasValue)
			{
				builder.Append("; lastWriteUtc=");
				builder.Append(artifact.FileLastWriteUtc.Value.ToString("O", CultureInfo.InvariantCulture));
			}
			if (!artifact.MetadataAvailable || artifact.Metadata == null)
			{
				builder.Append("; metadata unavailable");
				if (!string.IsNullOrEmpty(artifact.MetadataLoadError))
				{
					builder.Append(" (");
					builder.Append(artifact.MetadataLoadError);
					builder.Append(")");
				}
				return builder.ToString();
			}

			builder.Append("; captured=");
			builder.Append(artifact.Metadata.CapturedAtUtc.ToString("O", CultureInfo.InvariantCulture));
			builder.Append("; envHash=");
			builder.Append(Shorten(artifact.Metadata.EnvironmentHash));
			builder.Append("; DPI=");
			builder.Append(FormatDpi(artifact.Metadata.Environment));
			builder.Append("; screen=");
			builder.Append(FormatScreen(artifact.Metadata.Environment));
			builder.Append("; textScale=");
			builder.Append(FormatTextScale(artifact.Metadata.Environment));
			builder.Append("; dpiAwareness=");
			builder.Append(artifact.Metadata.DpiAwareness);
			builder.Append("; fontSmoothing=");
			builder.Append(FormatBoolean(artifact.Metadata.Environment != null && artifact.Metadata.Environment.FontSmoothing));
			builder.Append("; clearType=");
			builder.Append(FormatBoolean(artifact.Metadata.Environment != null && artifact.Metadata.Environment.ClearTypeEnabled));
			builder.Append("; theme=");
			builder.Append(artifact.Metadata.Environment != null ? artifact.Metadata.Environment.ThemeName : string.Empty);
			builder.Append("; culture=");
			builder.Append(artifact.Metadata.Environment != null ? artifact.Metadata.Environment.CultureName : string.Empty);
			builder.Append("; FW_FONT_QUALITY=");
			builder.Append(string.IsNullOrEmpty(artifact.Metadata.FontQuality) ? "(unset)" : artifact.Metadata.FontQuality);
			builder.Append("; font='");
			builder.Append(artifact.Metadata.DeterministicFontFamily);
			builder.Append("' installed=");
			builder.Append(FormatBoolean(artifact.Metadata.DeterministicFontInstalled));
			builder.Append("; machine=");
			builder.Append(artifact.Metadata.MachineName);
			builder.Append("; os=");
			builder.Append(artifact.Metadata.OsVersion);
			return builder.ToString();
		}

		private static string FormatDiffRegion(RenderPixelDiffSummary diffSummary)
		{
			if (!diffSummary.MinX.HasValue || !diffSummary.MinY.HasValue || !diffSummary.MaxX.HasValue || !diffSummary.MaxY.HasValue)
				return "none";

			return string.Format(
				CultureInfo.InvariantCulture,
				"x={0}..{1}, y={2}..{3}, size={4}x{5}",
				diffSummary.MinX.Value,
				diffSummary.MaxX.Value,
				diffSummary.MinY.Value,
				diffSummary.MaxY.Value,
				diffSummary.DiffRegionWidth,
				diffSummary.DiffRegionHeight);
		}

		private static string FormatImageSize(int width, int height)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", width, height);
		}

		private static string FormatDpi(EnvironmentSettings settings)
		{
			if (settings == null)
				return "unknown";
			return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", settings.DpiX, settings.DpiY);
		}

		private static string FormatScreen(EnvironmentSettings settings)
		{
			if (settings == null)
				return "unknown";
			return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", settings.ScreenWidth, settings.ScreenHeight);
		}

		private static string FormatTextScale(EnvironmentSettings settings)
		{
			if (settings == null)
				return "unknown";
			return settings.TextScaleFactor.ToString("0.###", CultureInfo.InvariantCulture);
		}

		private static string FormatBoolean(bool value)
		{
			return value ? "true" : "false";
		}

		private static string Shorten(string value)
		{
			if (string.IsNullOrEmpty(value))
				return "unknown";
			return value.Length <= 12 ? value : value.Substring(0, 12);
		}

		private static string GetDpiAwarenessDescription()
		{
			try
			{
				var context = GetThreadDpiAwarenessContext();
				int awareness = GetAwarenessFromDpiAwarenessContext(context);
				switch (awareness)
				{
					case DpiAwarenessUnaware:
						return "Unaware";
					case DpiAwarenessSystemAware:
						return "SystemAware";
					case DpiAwarenessPerMonitorAware:
						return "PerMonitorAware";
					case DpiAwarenessInvalid:
						return "Invalid";
					default:
						return string.Format(CultureInfo.InvariantCulture, "Unknown({0})", awareness);
				}
			}
			catch (DllNotFoundException)
			{
				return "Unavailable";
			}
			catch (EntryPointNotFoundException)
			{
				return "Unavailable";
			}
			catch
			{
				return "Unknown";
			}
		}

		private static bool IsFontInstalled(string fontFamily)
		{
			try
			{
				using (var installedFonts = new InstalledFontCollection())
				{
					foreach (var family in installedFonts.Families)
					{
						if (string.Equals(family.Name, fontFamily, StringComparison.OrdinalIgnoreCase))
							return true;
					}
				}
			}
			catch
			{
			}

			return false;
		}

		private static RenderSnapshotMetadata LoadMetadata(string metadataPath, out string loadError)
		{
			loadError = null;
			if (!File.Exists(metadataPath))
				return null;

			try
			{
				return JsonConvert.DeserializeObject<RenderSnapshotMetadata>(File.ReadAllText(metadataPath, Encoding.UTF8));
			}
			catch (Exception ex)
			{
				loadError = ex.GetType().Name + ": " + ex.Message;
				return null;
			}
		}

		private static void SaveJson<T>(string path, T value)
		{
			var directory = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			File.WriteAllText(
				path,
				JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore
				}),
				Encoding.UTF8);
		}

		private static void DeleteIfPresent(string path)
		{
			if (File.Exists(path))
				File.Delete(path);
		}

		private static RenderPixelDiffSummary CompareBitmaps(Bitmap expectedBitmap, Bitmap actualBitmap)
		{
			int maxWidth = Math.Max(expectedBitmap.Width, actualBitmap.Width);
			int maxHeight = Math.Max(expectedBitmap.Height, actualBitmap.Height);
			var summary = new RenderPixelDiffSummary();

			for (int y = 0; y < maxHeight; y++)
			{
				for (int x = 0; x < maxWidth; x++)
				{
					bool expectedInBounds = x < expectedBitmap.Width && y < expectedBitmap.Height;
					bool actualInBounds = x < actualBitmap.Width && y < actualBitmap.Height;

					if (!expectedInBounds || !actualInBounds)
					{
						summary.DifferentPixelCount++;
						if (expectedInBounds)
							summary.ExpectedOnlyPixelDifferences++;
						else if (actualInBounds)
							summary.ActualOnlyPixelDifferences++;
						UpdateDiffBounds(summary, x, y);
						continue;
					}

					if (expectedBitmap.GetPixel(x, y) == actualBitmap.GetPixel(x, y))
						continue;

					summary.DifferentPixelCount++;
					summary.InBoundsPixelDifferences++;
					UpdateDiffBounds(summary, x, y);
				}
			}

			if (summary.MinX.HasValue && summary.MaxX.HasValue)
				summary.DiffRegionWidth = summary.MaxX.Value - summary.MinX.Value + 1;
			if (summary.MinY.HasValue && summary.MaxY.HasValue)
				summary.DiffRegionHeight = summary.MaxY.Value - summary.MinY.Value + 1;

			return summary;
		}

		private static void UpdateDiffBounds(RenderPixelDiffSummary summary, int x, int y)
		{
			if (!summary.MinX.HasValue || x < summary.MinX.Value)
				summary.MinX = x;
			if (!summary.MaxX.HasValue || x > summary.MaxX.Value)
				summary.MaxX = x;
			if (!summary.MinY.HasValue || y < summary.MinY.Value)
				summary.MinY = y;
			if (!summary.MaxY.HasValue || y > summary.MaxY.Value)
				summary.MaxY = y;
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
	}

	public sealed class RenderBaselineVerificationResult
	{
		public bool Passed { get; set; }
		public string FailureMessage { get; set; }
		public string VerifiedPath { get; set; }
		public string VerifiedMetadataPath { get; set; }
		public string ReceivedPath { get; set; }
		public string ReceivedMetadataPath { get; set; }
		public string DiffPath { get; set; }
		public string DiffMetadataPath { get; set; }
		public RenderPixelDiffSummary DiffSummary { get; set; }
	}

	public sealed class RenderSnapshotMetadata
	{
		public string ScenarioId { get; set; }
		public string SnapshotName { get; set; }
		public DateTime CapturedAtUtc { get; set; }
		public int ImageWidth { get; set; }
		public int ImageHeight { get; set; }
		public string MachineName { get; set; }
		public string OsVersion { get; set; }
		public string EnvironmentHash { get; set; }
		public EnvironmentSettings Environment { get; set; }
		public string DpiAwareness { get; set; }
		public string FontQuality { get; set; }
		public string DeterministicFontFamily { get; set; }
		public bool DeterministicFontInstalled { get; set; }
	}

	public sealed class RenderSnapshotArtifact
	{
		public string ArtifactKind { get; set; }
		public string ImagePath { get; set; }
		public string MetadataPath { get; set; }
		public int ImageWidth { get; set; }
		public int ImageHeight { get; set; }
		public DateTime? FileLastWriteUtc { get; set; }
		public bool MetadataAvailable { get; set; }
		public string MetadataLoadError { get; set; }
		public RenderSnapshotMetadata Metadata { get; set; }
	}

	public sealed class RenderSnapshotComparisonReport
	{
		public string ScenarioId { get; set; }
		public string SnapshotName { get; set; }
		public int AllowedDifferentPixelCount { get; set; }
		public RenderSnapshotArtifact SavedBaseline { get; set; }
		public RenderSnapshotArtifact CurrentRun { get; set; }
		public RenderPixelDiffSummary Diff { get; set; }
		public List<string> Differences { get; set; } = new List<string>();
	}

	public sealed class RenderPixelDiffSummary
	{
		public int DifferentPixelCount { get; set; }
		public int InBoundsPixelDifferences { get; set; }
		public int ExpectedOnlyPixelDifferences { get; set; }
		public int ActualOnlyPixelDifferences { get; set; }
		public int? MinX { get; set; }
		public int? MinY { get; set; }
		public int? MaxX { get; set; }
		public int? MaxY { get; set; }
		public int DiffRegionWidth { get; set; }
		public int DiffRegionHeight { get; set; }
	}
}