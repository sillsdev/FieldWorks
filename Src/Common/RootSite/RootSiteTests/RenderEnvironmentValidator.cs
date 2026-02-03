// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SIL.FieldWorks.Common.RootSites.RenderBenchmark
{
	/// <summary>
	/// Validates that the rendering environment is deterministic for pixel-perfect comparisons.
	/// Checks fonts, DPI, theme settings, and other factors that affect rendering output.
	/// </summary>
	public class RenderEnvironmentValidator
	{
		/// <summary>
		/// Gets the current environment settings.
		/// </summary>
		public EnvironmentSettings CurrentSettings { get; private set; }

		/// <summary>
		/// Initializes a new instance and captures current environment settings.
		/// </summary>
		public RenderEnvironmentValidator()
		{
			CurrentSettings = CaptureCurrentSettings();
		}

		/// <summary>
		/// Gets a hash of the current environment settings.
		/// </summary>
		/// <returns>A SHA256 hash string of the environment settings.</returns>
		public string GetEnvironmentHash()
		{
			var settingsJson = Newtonsoft.Json.JsonConvert.SerializeObject(CurrentSettings);
			using (var sha256 = SHA256.Create())
			{
				var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(settingsJson));
				return Convert.ToBase64String(hashBytes);
			}
		}

		/// <summary>
		/// Validates that the current environment matches the expected hash.
		/// </summary>
		/// <param name="expectedHash">The expected environment hash.</param>
		/// <returns>True if the environment matches; otherwise, false.</returns>
		public bool Validate(string expectedHash)
		{
			if (string.IsNullOrEmpty(expectedHash))
				return true; // No validation required

			return GetEnvironmentHash() == expectedHash;
		}

		/// <summary>
		/// Gets a detailed comparison between current and expected environment.
		/// </summary>
		/// <param name="expectedSettings">The expected settings to compare against.</param>
		/// <returns>A list of differences found.</returns>
		public List<EnvironmentDifference> Compare(EnvironmentSettings expectedSettings)
		{
			var differences = new List<EnvironmentDifference>();

			if (expectedSettings == null)
				return differences;

			if (CurrentSettings.DpiX != expectedSettings.DpiX || CurrentSettings.DpiY != expectedSettings.DpiY)
			{
				differences.Add(new EnvironmentDifference
				{
					Setting = "DPI",
					Expected = $"{expectedSettings.DpiX}x{expectedSettings.DpiY}",
					Actual = $"{CurrentSettings.DpiX}x{CurrentSettings.DpiY}"
				});
			}

			if (CurrentSettings.FontSmoothing != expectedSettings.FontSmoothing)
			{
				differences.Add(new EnvironmentDifference
				{
					Setting = "FontSmoothing",
					Expected = expectedSettings.FontSmoothing.ToString(),
					Actual = CurrentSettings.FontSmoothing.ToString()
				});
			}

			if (CurrentSettings.ClearTypeEnabled != expectedSettings.ClearTypeEnabled)
			{
				differences.Add(new EnvironmentDifference
				{
					Setting = "ClearType",
					Expected = expectedSettings.ClearTypeEnabled.ToString(),
					Actual = CurrentSettings.ClearTypeEnabled.ToString()
				});
			}

			if (CurrentSettings.ThemeName != expectedSettings.ThemeName)
			{
				differences.Add(new EnvironmentDifference
				{
					Setting = "Theme",
					Expected = expectedSettings.ThemeName,
					Actual = CurrentSettings.ThemeName
				});
			}

			if (CurrentSettings.TextScaleFactor != expectedSettings.TextScaleFactor)
			{
				differences.Add(new EnvironmentDifference
				{
					Setting = "TextScaleFactor",
					Expected = expectedSettings.TextScaleFactor.ToString(CultureInfo.InvariantCulture),
					Actual = CurrentSettings.TextScaleFactor.ToString(CultureInfo.InvariantCulture)
				});
			}

			return differences;
		}

		/// <summary>
		/// Refreshes the current environment settings.
		/// </summary>
		public void Refresh()
		{
			CurrentSettings = CaptureCurrentSettings();
		}

		private EnvironmentSettings CaptureCurrentSettings()
		{
			var settings = new EnvironmentSettings();

			// Capture DPI settings
			using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
			{
				settings.DpiX = (int)graphics.DpiX;
				settings.DpiY = (int)graphics.DpiY;
			}

			// Capture font smoothing
			settings.FontSmoothing = GetFontSmoothing();
			settings.ClearTypeEnabled = GetClearTypeEnabled();

			// Capture theme
			settings.ThemeName = GetCurrentTheme();

			// Capture text scale factor
			settings.TextScaleFactor = GetTextScaleFactor();

			// Capture screen info
			var screen = Screen.PrimaryScreen;
			settings.ScreenWidth = screen.Bounds.Width;
			settings.ScreenHeight = screen.Bounds.Height;

			// Capture culture
			settings.CultureName = CultureInfo.CurrentCulture.Name;

			return settings;
		}

		private bool GetFontSmoothing()
		{
			try
			{
				bool smoothing = false;
				SystemParametersInfo(SPI_GETFONTSMOOTHING, 0, ref smoothing, 0);
				return smoothing;
			}
			catch
			{
				return false;
			}
		}

		private bool GetClearTypeEnabled()
		{
			try
			{
				int type = 0;
				SystemParametersInfo(SPI_GETFONTSMOOTHINGTYPE, 0, ref type, 0);
				return type == FE_FONTSMOOTHINGCLEARTYPE;
			}
			catch
			{
				return false;
			}
		}

		private string GetCurrentTheme()
		{
			try
			{
				using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
				{
					if (key != null)
					{
						var appsUseLightTheme = key.GetValue("AppsUseLightTheme");
						if (appsUseLightTheme != null)
						{
							return (int)appsUseLightTheme == 1 ? "Light" : "Dark";
						}
					}
				}
			}
			catch
			{
				// Ignore errors reading theme
			}
			return "Unknown";
		}

		private double GetTextScaleFactor()
		{
			try
			{
				using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Accessibility"))
				{
					if (key != null)
					{
						var textScaleFactor = key.GetValue("TextScaleFactor");
						if (textScaleFactor != null)
						{
							return (int)textScaleFactor / 100.0;
						}
					}
				}
			}
			catch
			{
				// Ignore errors
			}
			return 1.0;
		}

		#region Native methods
		private const int SPI_GETFONTSMOOTHING = 0x004A;
		private const int SPI_GETFONTSMOOTHINGTYPE = 0x200A;
		private const int FE_FONTSMOOTHINGCLEARTYPE = 0x0002;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SystemParametersInfo(int uiAction, int uiParam, ref bool pvParam, int fWinIni);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool SystemParametersInfo(int uiAction, int uiParam, ref int pvParam, int fWinIni);
		#endregion
	}

	/// <summary>
	/// Captures the current rendering environment settings.
	/// </summary>
	public class EnvironmentSettings
	{
		/// <summary>Gets or sets the horizontal DPI.</summary>
		public int DpiX { get; set; }

		/// <summary>Gets or sets the vertical DPI.</summary>
		public int DpiY { get; set; }

		/// <summary>Gets or sets whether font smoothing is enabled.</summary>
		public bool FontSmoothing { get; set; }

		/// <summary>Gets or sets whether ClearType is enabled.</summary>
		public bool ClearTypeEnabled { get; set; }

		/// <summary>Gets or sets the current theme name.</summary>
		public string ThemeName { get; set; }

		/// <summary>Gets or sets the text scale factor.</summary>
		public double TextScaleFactor { get; set; }

		/// <summary>Gets or sets the primary screen width.</summary>
		public int ScreenWidth { get; set; }

		/// <summary>Gets or sets the primary screen height.</summary>
		public int ScreenHeight { get; set; }

		/// <summary>Gets or sets the culture name.</summary>
		public string CultureName { get; set; }
	}

	/// <summary>
	/// Represents a difference between expected and actual environment settings.
	/// </summary>
	public class EnvironmentDifference
	{
		/// <summary>Gets or sets the setting name.</summary>
		public string Setting { get; set; }

		/// <summary>Gets or sets the expected value.</summary>
		public string Expected { get; set; }

		/// <summary>Gets or sets the actual value.</summary>
		public string Actual { get; set; }

		/// <inheritdoc/>
		public override string ToString()
		{
			return $"{Setting}: expected '{Expected}', got '{Actual}'";
		}
	}
}
