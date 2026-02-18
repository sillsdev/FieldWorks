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

namespace SIL.HCSynthByGloss
{
	class HCSynthByGloss
	{
		static void Main(string[] args)
		{
			bool doTracing = false;
			bool showTracing = false;
			int argCount = args.Count();
			if (argCount != 6 || args[0] != "-h" || args[2] != "-g" || args[4] != "-o")
			{
				if (argCount == 7 && args[6] == "-t")
				{
					doTracing = true;
				}
				else if (argCount == 8 && args[6] == "-t" && args[7] == "-s")
				{
					doTracing = true;
					showTracing = true;
				}
				else
				{
					Console.WriteLine("Usage:");
					Console.WriteLine(
						"HCSynthByGloss -h HC.xml_file -g gloss_file -o output (-t (-s))"
					);
					Console.WriteLine("\t-t = turn on tracing");
					Console.WriteLine(
						"\t-s = show the tracing result in the system default web browser; -s is only valid when also using -t"
					);
					Environment.Exit(1);
				}
			}
			if (!File.Exists(args[1]))
			{
				Console.WriteLine("Could not find file '" + args[1] + "'.");
				Environment.Exit(2);
			}
			if (!File.Exists(args[3]))
			{
				Console.WriteLine("Could not find file '" + args[3] + "'.");
				Environment.Exit(3);
			}

			var dll = new HCSynthByGlossDll(args[5]);
			dll.HcXmlFile = args[1];
			dll.GlossFile = args[3];
			dll.DoTracing = doTracing;
			dll.ShowTracing = showTracing;
			var result = dll.Process();
			Console.WriteLine("Processing result: " + result + ".");
		}
	}
}
