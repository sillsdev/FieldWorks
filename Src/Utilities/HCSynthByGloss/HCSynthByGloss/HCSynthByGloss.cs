// Copyright (c) 2022-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;
using HCSynthByGloss;
using SIL.Machine.Morphology;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.Machine.Translation;
using SIL.HCSynthByGloss;
using SIL.Utils;
using HCSynthByGlossLib;
using System.Globalization;
using System.Threading;

namespace SIL.HCSynthByGloss
{
	class HCSynthByGloss
	{
		static void Main(string[] args)
		{
			const int iLocale = 0;
			const int iArgH = 1;
			const int iHcFile = 2;
			const int iArgG = 3;
			const int iGlossFile = 4;
			const int iArgO = 5;
			const int iOutputFile = 6;
			const int iArgT = 7;
			const int iArgS = 8;

			bool doTracing = false;
			bool showTracing = false;
			int argCount = args.Count();
			if (argCount > 0 )
			{
				Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(args[iLocale]);
			}
			if (argCount != 7 || args[iArgH] != "-h" || args[iArgG] != "-g" || args[iArgO] != "-o")
			{
				if (argCount == 8 && args[iArgT] == "-t")
				{
					doTracing = true;
				}
				else if (argCount == 9 && args[iArgT] == "-t" && args[iArgS] == "-s")
				{
					doTracing = true;
					showTracing = true;
				}
				else
				{
					Console.WriteLine(HCSynthByGlossStrings.ksUsage);
					Console.WriteLine(HCSynthByGlossStrings.ksCommandLineUsage);
					Console.WriteLine(HCSynthByGlossStrings.ksTurnOnTracing);
					Console.WriteLine(HCSynthByGlossStrings.ksShowTracing);
					Environment.Exit(1);
				}
			}
			if (!File.Exists(args[iHcFile]))
			{
				Console.WriteLine(HCSynthByGlossStrings.ksCouldNotFindFile + args[iHcFile] + "'.");
				Environment.Exit(2);
			}
			if (!File.Exists(args[iGlossFile]))
			{
				Console.WriteLine(HCSynthByGlossStrings.ksCouldNotFindFile + args[iGlossFile] + "'.");
				Environment.Exit(3);
			}

			var dll = new HCSynthByGlossDll(args[iOutputFile]);
			dll.LocaleCode = args[iLocale];
			dll.SetHcXmlFile(args[iHcFile]);
			dll.SetGlossFile(args[iGlossFile]);
			dll.DoTracing = doTracing;
			dll.ShowTracing = showTracing;
			var result = dll.Process();
			Console.WriteLine(HCSynthByGlossStrings.ksProcessingResult + result + ".");
		}
	}
}
