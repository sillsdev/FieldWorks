using SIL.LCModel.Core.WritingSystems;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIL.PcPatrFLEx
{
	public class FLExFormUtilities
	{
		public static Font CreateFont(CoreWritingSystemDefinition wsDef)
		{
			float fontSize = (wsDef.DefaultFontSize == 0) ? 10 : wsDef.DefaultFontSize;
			var fStyle = FontStyle.Regular;
			if (wsDef.DefaultFontFeatures.Contains("Bold"))
			{
				fStyle |= FontStyle.Bold;
			}
			if (wsDef.DefaultFontFeatures.Contains("Italic"))
			{
				fStyle |= FontStyle.Italic;
			}
			return new Font(wsDef.DefaultFontName, fontSize, fStyle);
		}

		public static void DetermineIndexOfBinInExecutablesPath(out string rootdir, out int indexOfBinInPath)
		{
			Uri uriBase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
			rootdir = Path.GetDirectoryName(Uri.UnescapeDataString(uriBase.AbsolutePath));
			indexOfBinInPath = rootdir.LastIndexOf("bin");
		}


	}
}
