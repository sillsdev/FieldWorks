using System;
using System.Drawing.Text;
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

		[DllImport("User32.dll")]
		private static extern bool SetProcessDpiAwarenessContext(int dpiFlag);

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
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