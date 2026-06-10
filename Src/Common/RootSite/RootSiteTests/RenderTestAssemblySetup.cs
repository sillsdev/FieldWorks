using System;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.RootSites
{
	[SetUpFixture]
	public sealed class RenderTestAssemblySetup
	{
		private const int DpiAwarenessContextUnaware = -1;
		private const string DeterministicRenderFontFamily = "Segoe UI";

		// Segoe UI lacks Arabic on some Windows versions and its Arabic glyphs differ
		// between versions where present, so Arabic scenarios (e.g. rtl-script) are rendered
		// with this pinned, privately-loaded font for deterministic output across machines/CI.
		private const string ArabicRenderFontFamily = "Scheherazade New";
		private const string ArabicRenderFontFile = "ScheherazadeNew-Regular.ttf";
		private const int FR_PRIVATE = 0x10;

		private string m_loadedArabicFontPath;

		[DllImport("User32.dll")]
		private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

		[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
		private static extern int AddFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);

		[DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
		private static extern int RemoveFontResourceEx(string lpszFilename, uint fl, IntPtr pdv);

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			// Force grayscale antialiasing (ANTIALIASED_QUALITY=4) for deterministic
			// rendering across dev machines and CI (Windows Server 2025).
			// The native VwGraphics reads this env var when creating GDI fonts.
			Environment.SetEnvironmentVariable("FW_FONT_QUALITY", "4");

			try
			{
				SetProcessDpiAwarenessContext(DpiAwarenessContextUnaware);
			}
			catch (DllNotFoundException)
			{
			}
			catch (EntryPointNotFoundException)
			{
			}

			using (var installedFonts = new InstalledFontCollection())
			{
				bool hasDeterministicFont = installedFonts.Families.Any(
					family => string.Equals(family.Name, DeterministicRenderFontFamily, StringComparison.OrdinalIgnoreCase));
				TestContext.Progress.WriteLine(
					$"[RENDER-SETUP] DPI unaware requested. Font '{DeterministicRenderFontFamily}' installed={hasDeterministicFont}");
			}

			LoadArabicRenderFont();
		}

		[OneTimeTearDown]
		public void OneTimeTeardown()
		{
			if (m_loadedArabicFontPath != null)
			{
				RemoveFontResourceEx(m_loadedArabicFontPath, FR_PRIVATE, IntPtr.Zero);
				m_loadedArabicFontPath = null;
			}
		}

		/// <summary>
		/// Loads the pinned Arabic font privately into this process (GDI) so the Views engine
		/// can resolve it by family name without a machine-wide install. Fails fast if the font
		/// is missing rather than letting Windows silently fall back to a version-dependent font.
		/// </summary>
		private void LoadArabicRenderFont()
		{
			var fontPath = Path.Combine(
				TestContext.CurrentContext.TestDirectory, "TestData", "Fonts", ArabicRenderFontFile);

			if (!File.Exists(fontPath))
			{
				Assert.Fail(
					$"[RENDER-SETUP] Required Arabic render font '{ArabicRenderFontFile}' not found at {fontPath}. " +
					"It is downloaded by Build/PackageRestore.targets and copied to the test output; run a full build.");
			}

			int count = AddFontResourceEx(fontPath, FR_PRIVATE, IntPtr.Zero);
			if (count <= 0)
			{
				Assert.Fail($"[RENDER-SETUP] Failed to load Arabic render font from {fontPath} (AddFontResourceEx returned {count}).");
			}

			m_loadedArabicFontPath = fontPath;
			TestContext.Progress.WriteLine(
				$"[RENDER-SETUP] Loaded {count} face(s) of '{ArabicRenderFontFamily}' from {fontPath}");
		}
	}
}