namespace TypedCollectionBuilder {
	using System;
	using System.Collections;
	using System.ComponentModel;


	public class CollGenCommand {

		// assumes all same case.
		static bool ArgumentMatch(string arg, string formal) {
			if (arg[0] != '/' && arg[0] != '-') {
				return false;
			}
			arg = arg.Substring(1);
			return (arg == formal || (arg.Length == 1 && arg[0] == formal[0]));
		}


	public static void Main(string[] args) {

			TypedCollectionGenerator x = new TypedCollectionGenerator();

		x.Language = "cs";
			x.FileName = string.Empty;
			x.CollectionTypeName = string.Empty;
			x.NameSpace = string.Empty;
			x.AddValidation = false;
			x.GenerateEnum = false;
			x.GenerateComments = false;

			for (int i = 0; i < args.Length; i++) {
				string arg = args[i];
				string value = String.Empty;

				int colonPos = arg.IndexOf(":");
				if (colonPos != -1) {
					value = arg.Substring(colonPos + 1);
					arg = arg.Substring(0, colonPos);
				}

				arg = arg.ToLower();

				if (ArgumentMatch(arg, "?") || ArgumentMatch(arg, "help")) {
					WriteHelp();
					return;
				}
				else if (ArgumentMatch(arg, "type")) {
					x.CollectionTypeName = value;
				}
				else if (ArgumentMatch(arg, "namespace")) {
					x.NameSpace = value;
				}
				else if (ArgumentMatch(arg, "outputfile")) {
					x.FileName = value;
				}
				else if (ArgumentMatch(arg, "language")) {
					x.Language = value;
				}
				else if (ArgumentMatch(arg, "enum")) {
					if (value == "nested") {
						x.GenerateEnum = true;
						x.GenerateEnumAsNested = true;
					}
					else if (value == "separate") {
						x.GenerateEnum = true;
						x.GenerateEnumAsNested = false;
					}
					else if (value == "none") {
						x.GenerateEnum = false;
					}
					else {
						throw new Exception("invalid command line argument: " + args[i]);
					}
				}
				else if (ArgumentMatch(arg, "comments")) {
					x.GenerateComments = true;
				}
				else {
					throw new Exception("invalid command line argument: " + args[i]);
				}
			}

			if (x.CollectionTypeName.Length == 0 || x.NameSpace.Length == 0) {
				WriteHelp();
				return;
			}

			if (x.FileName.Length == 0) {
				x.FileName = x.CollectionTypeName + "Collection";
			}

			// Output to file
			WriteHeader();
			string path = Environment.CurrentDirectory + "\\";
		x.Generate(path);
			Console.WriteLine("Collection " + x.FileName + " generated.");
			Console.WriteLine();
	}

		private static void WriteHeader() {
			Console.WriteLine("Microsoft (R) Collection Generator utility");
			Console.WriteLine("[.NET Version " + System.Environment.Version.ToString() + "]");
			Console.WriteLine("Copyright (C) Microsoft Corp 2000. All rights reserved.");
			Console.WriteLine();
		}

		static void WriteHelp() {
			WriteHeader();

			Console.WriteLine("Usage:");
			Console.WriteLine();
			Console.WriteLine("collgen /t:<type name> /n:<namespace> [/o:<outputfile>] [/l:<language>] [/e:<enum>] [/c]");

			Console.WriteLine();
			Console.WriteLine("More help on command-line options:");
			Console.WriteLine("/?, /h[elp]            Prints this message.");
			Console.WriteLine("/t:<type>              The type name to generate the class for.");
			Console.WriteLine("/n:<namespace>         The namespace the type lives in.");
			Console.WriteLine("/o:<outputfile>        The filename to create without extension");
			Console.WriteLine("                       defaults to standard output.");
			Console.WriteLine("/l:<language>          Language to generate code in.");
			Console.WriteLine("                       Choose from 'cs'(default), 'vb' or 'jscript'.");
			Console.WriteLine("/e:<enum>              Strongly Typed Enumerator.");
			Console.WriteLine("                       Choose from 'none'(default), 'separate' or 'nested'.");
			Console.WriteLine("/c:                    Generate Doc Comments.");
			Console.WriteLine();
		}

	}
}
