//-------------------------------------------------------------------------------------------------
// <copyright file="XsdGen.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Generates a strongly-typed C# class from an XML schema (XSD).
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
	using System;
	using System.CodeDom;
	using System.CodeDom.Compiler;
	using System.Collections;
	using System.IO;
	using System.Xml;
	using System.Xml.Schema;
	using Microsoft.CSharp;
	using Microsoft.Tools.WindowsInstallerXml;

	/// <summary>
	/// Generates a strongly-typed C# class from an XML schema (XSD).
	/// </summary>
	public class XsdGen
	{
		private string xsdFile;
		private string outFile;
		private string outputNamespace;
		private bool showHelp;

		/// <summary>
		/// Constructor for the XsdGen class.
		/// </summary>
		/// <param name="args">Command-line arguments passed to the program.</param>
		private XsdGen(string[] args)
		{
			this.ParseCommandlineArgs(args);

			// show usage information
			if (this.showHelp)
			{
				Console.WriteLine("usage: XsdGen.exe <schema>.xsd <outputFile> <namespace>");
				return;
			}

			// ensure that the schema file exists
			if (!File.Exists(this.xsdFile))
			{
				throw new ApplicationException(String.Format("Schema file does not exist: '{0}'.", this.xsdFile));
			}

			XmlSchema document = null;
			using (StreamReader xsdFileReader = new StreamReader(this.xsdFile))
			{
				document = XmlSchema.Read(xsdFileReader, new ValidationEventHandler(this.ValidationHandler));
			}

			CodeCompileUnit codeCompileUnit = StronglyTypedClasses.Generate(document, this.outputNamespace);

			using (CSharpCodeProvider codeProvider = new CSharpCodeProvider())
			{
				ICodeGenerator generator = codeProvider.CreateGenerator();

				CodeGeneratorOptions options = new CodeGeneratorOptions();
				options.BlankLinesBetweenMembers = true;
				options.BracingStyle = "C";
				options.IndentString = "    ";

				using (StreamWriter csFileWriter = new StreamWriter(this.outFile))
				{
					generator.GenerateCodeFromCompileUnit(codeCompileUnit, csFileWriter, options);
				}
			}
		}

		/// <summary>
		/// Validation event handler.
		/// </summary>
		/// <param name="sender">Sender for the event.</param>
		/// <param name="e">Event args.</param>
		public void ValidationHandler(object sender, ValidationEventArgs e)
		{
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static int Main(string[] args)
		{
			try
			{
				XsdGen xsdGen = new XsdGen(args);
			}
			catch (Exception e)
			{
				Console.WriteLine("XsdGen.exe : fatal error MSF0000: {0}\r\n\r\nStack Trace:\r\n{1}", e.Message, e.StackTrace);
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// Parse the command line arguments.
		/// </summary>
		/// <param name="args">Command-line arguments.</param>
		private void ParseCommandlineArgs(string[] args)
		{
			if (3 > args.Length)
			{
				this.showHelp = true;
			}
			else
			{
				this.xsdFile = args[0];
				this.outFile = args[1];
				this.outputNamespace = args[2];
			}
		}
	}
}
