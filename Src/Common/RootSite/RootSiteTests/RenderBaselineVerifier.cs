using System.Drawing;
using System.Runtime.CompilerServices;
using SIL.FieldWorks.Common.RenderVerification;

namespace SIL.FieldWorks.Common.RootSites
{
	internal static class RenderBaselineVerifier
	{
		internal static string GetSourceFileDirectory([CallerFilePath] string sourceFile = "")
		{
			return RenderSnapshotVerifier.GetSourceFileDirectory(sourceFile);
		}

		internal static RenderBaselineVerificationResult Verify(Bitmap actualBitmap, string directory, string name, string scenarioId)
		{
			return RenderSnapshotVerifier.Verify(actualBitmap, directory, name, scenarioId);
		}
	}
}