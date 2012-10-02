using System;
using Sfm2Xml;

namespace ConvertSFM
{
	/// <summary>
	/// Summary description for ConvertSFM.
	/// </summary>
	public class ConvertSFM
	{
		public static void Main(string [] args)
		{
			if (args.Length < 3 || args.Length > 6)
				throw new System.Exception("Usage: ConvertSFM.exe <input-file> <mapping-file> <output-file> [vern regional national]");	//  <log-file>");

			// parameters 4,5,6 are changes to the map file
			// 4: Vern     "code:key"  Ex; "en:encoding KEY"
			// 5: Regional "code:key"  Ex; "fr:encoding KEY"
			// 6: National "code:key"  Ex; "tg:encoding KEY"
			Sfm2Xml.Converter conv = new Sfm2Xml.Converter();
			string arg3 = args.Length==4 ? args[3] : "";
			string arg4 = args.Length==5 ? args[4] : "";
			string arg5 = args.Length==6 ? args[5] : "";

			conv.Convert(args[0], args[1], args[2], arg3, arg4, arg5);
		}
	}
}
