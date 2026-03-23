using System;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	[SetUpFixture]
	public sealed class RenderTestAssemblySetup
	{
		private const int DpiAwarenessContextUnaware = -1;
		private const string DeterministicRenderFontFamily = "Segoe UI";

		[DllImport("User32.dll")]
		private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

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
		}
	}
}