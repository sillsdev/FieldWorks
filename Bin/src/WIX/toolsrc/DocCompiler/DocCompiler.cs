//-------------------------------------------------------------------------------------------------
// <copyright file="DocCompiler.cs" company="Microsoft">
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
// Compiles various things into documentation.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.DocCompiler
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using System.Text;
	using System.Xml;
	using System.Xml.Schema;

	/// <summary>
	/// Compiles various things into documentation.
	/// </summary>
	public class DocCompiler
	{
		internal const string DocCompilerNamespace = "http://schemas.microsoft.com/wix/2005/DocCompiler";

		private string hhcFile;
		private XmlNamespaceManager namespaceManager;
		private string versionNumber;
		private string outputDir;
		private string outputFileName;
		private bool showHelp;
		private string tocFile;
		private bool chm = false;
		private bool web = false;

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// <param name="args">The command line arguments.</param>
		/// <returns>The error code for the application.</returns>
		[STAThread]
		public static int Main(string[] args)
		{
			DocCompiler docCompiler = new DocCompiler();
			return docCompiler.Run(args);
		}

		/// <summary>
		/// Run the application.
		/// </summary>
		/// <param name="args">The command line arguments.</param>
		/// <returns>The error code for the application.</returns>
		private int Run(string[] args)
		{
			try
			{
				this.ParseCommandline(args);

				// get the assemblies
				Assembly docCompilerAssembly = Assembly.GetExecutingAssembly();

				if (this.showHelp)
				{
					Console.WriteLine("Microsoft (R) Documentation Compiler version {0}", docCompilerAssembly.GetName().Version.ToString());
					Console.WriteLine("Copyright (C) Microsoft Corporation. All rights reserved.");
					Console.WriteLine();
					Console.WriteLine(" usage: DocCompiler [-?] [-c:hhc.exe | -w] [-v version] tableOfContents.xml output");
					Console.WriteLine();
					Console.WriteLine("    c - path to HTML Help Compiler to create output CHM");
					Console.WriteLine("    w - creates Web HTML Manual to output directory");
					Console.WriteLine("   -v       set version information for elements");

					return 0;
				}

				// ensure the directory containing the html files exists
				Directory.CreateDirectory(Path.Combine(this.outputDir, "html"));

				// load the schema
				XmlReader schemaReader = null;
				XmlSchemaCollection schemas = null;
				try
				{
					schemaReader = new XmlTextReader(docCompilerAssembly.GetManifestResourceStream("Microsoft.Tools.DocCompiler.Xsd.docCompiler.xsd"));
					schemas = new XmlSchemaCollection();
					schemas.Add(DocCompilerNamespace, schemaReader);
				}
				finally
				{
					schemaReader.Close();
				}

				// load the table of contents
				XmlTextReader reader = null;
				try
				{
					reader = new XmlTextReader(this.tocFile);
					XmlValidatingReader validatingReader = new XmlValidatingReader(reader);
					validatingReader.Schemas.Add(schemas);

					// load the xml into a DOM
					XmlDocument doc = new XmlDocument();
					doc.Load(validatingReader);

					// create a namespace manager
					this.namespaceManager = new XmlNamespaceManager(doc.NameTable);
					this.namespaceManager.AddNamespace("doc", DocCompilerNamespace);
					this.namespaceManager.PushScope();

					this.ProcessCopyFiles(doc);
					this.ProcessTopics(doc);
					this.ProcessSchemas(doc);
					if (this.chm)
					{
						this.CompileChm(doc);
					}
					if (this.web)
					{
						this.BuildWeb(doc);
					}
				}
				finally
				{
					if (reader != null)
					{
						reader.Close();
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("DocCompiler.exe : fatal error DCMP0001: {0}", e.Message);
				Console.WriteLine();
				Console.WriteLine("Stack Trace:");
				Console.WriteLine(e.StackTrace);

				if (e is NullReferenceException)
				{
					throw;
				}

				return 1;
			}

			return 0;
		}

		/// <summary>
		/// Process files to copy.
		/// </summary>
		/// <param name="doc">The documentation compiler xml document.</param>
		private void ProcessCopyFiles(XmlDocument doc)
		{
			XmlNodeList copyFileNodes = doc.SelectNodes("//doc:CopyFile", this.namespaceManager);

			foreach (XmlElement copyFileElement in copyFileNodes)
			{
				string sourceFile = copyFileElement.GetAttribute("Source");
				string destinationFile = copyFileElement.GetAttribute("Destination");

				destinationFile = Path.Combine(this.outputDir, destinationFile);

				File.Copy(sourceFile, destinationFile, true);

				// remove the read-only attribute
				File.SetAttributes(destinationFile, File.GetAttributes(destinationFile) & ~FileAttributes.ReadOnly);
			}
		}

		/// <summary>
		/// Process the xml schemas.
		/// </summary>
		/// <param name="doc">The documentation compiler xml document.</param>
		private void ProcessSchemas(XmlDocument doc)
		{
			XmlNodeList schemaNodes = doc.SelectNodes("//doc:XmlSchema", this.namespaceManager);

			XmlSchemaCompiler schemaCompiler = new XmlSchemaCompiler(this.outputDir, this.versionNumber);
			schemaCompiler.CompileSchemas(schemaNodes);
		}

		/// <summary>
		/// Process the topics.
		/// </summary>
		/// <param name="doc">The documentation compiler xml document.</param>
		private void ProcessTopics(XmlDocument doc)
		{
			XmlNodeList topicNodes = doc.SelectNodes("//doc:Topic", this.namespaceManager);

			foreach (XmlElement topicElement in topicNodes)
			{
				string sourceFile = topicElement.GetAttribute("SourceFile");

				if (sourceFile.Length > 0)
				{
					string htmlDir = Path.Combine(this.outputDir, "html");
					string destinationFile = Path.Combine(htmlDir, Path.GetFileName(sourceFile));

					// save the relative path to the destination file
					topicElement.SetAttribute("DestinationFile", Path.Combine("html", Path.GetFileName(sourceFile)));

					File.Copy(sourceFile, destinationFile, true);

					// remove the read-only attribute
					File.SetAttributes(destinationFile, File.GetAttributes(destinationFile) & ~FileAttributes.ReadOnly);
				}
			}
		}

		/// <summary>
		/// Compile the documentation into a chm file.
		/// </summary>
		/// <param name="doc">The documentation compiler xml document.</param>
		private void CompileChm(XmlDocument doc)
		{
			XmlElement defaultTopicNode = (XmlElement)doc.SelectSingleNode("//doc:Topic", this.namespaceManager);
			string defaultTopicFile = defaultTopicNode.GetAttribute("DestinationFile");
			string defaultTopicTitle = defaultTopicNode.GetAttribute("Title");

			// create the project file
			string projectFile = Path.Combine(this.outputDir, "project.hhp");
			using (StreamWriter sw = File.CreateText(projectFile))
			{
				sw.WriteLine("[OPTIONS]");
				sw.WriteLine("Compatibility=1.1 or later");
				sw.WriteLine(String.Format("Compiled file={0}", this.outputFileName));
				sw.WriteLine("Contents file=toc.hhc");
				sw.WriteLine("Index file=idx.hhk");
				sw.WriteLine("Default Window=Main");
				sw.WriteLine(String.Format("Default topic={0}", defaultTopicFile));
				sw.WriteLine("Display compile progress=No");
				sw.WriteLine("Error log file=log.txt");
				sw.WriteLine("Full-text search=Yes");
				sw.WriteLine("Language=0x409 English (United States)");
				sw.WriteLine(String.Format("Title={0}", defaultTopicTitle));
				sw.WriteLine("");
				sw.WriteLine("[WINDOWS]");
				sw.WriteLine("Main=,\"toc.hhc\",\"idx.hhk\",\"{0}\",\"{0}\",,,,,0x63520,,0x384e,,,,,,,,0", defaultTopicFile);
			}

			// create the index file
			string indexFile = Path.Combine(this.outputDir, "idx.hhk");
			using (StreamWriter sw = File.CreateText(indexFile))
			{
				sw.WriteLine("<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML//EN\">");
				sw.WriteLine("<HTML>");
				sw.WriteLine("<HEAD>");
				sw.WriteLine("<META NAME=\"GENERATOR\" CONTENT=\"A tool\">");
				sw.WriteLine("</HEAD>");
				sw.WriteLine("<BODY>");
				sw.WriteLine("<OBJECT TYPE=\"text/site properties\">");
				sw.WriteLine("	<PARAM NAME=\"FrameName\" VALUE=\"TEXT\">");
				sw.WriteLine("</OBJECT>");
				sw.WriteLine("<UL>");

				XmlNodeList topicNodes = doc.SelectNodes("//doc:Topic", this.namespaceManager);
				foreach (XmlElement topicElement in topicNodes)
				{
					string title = topicElement.GetAttribute("Title");
					string destinationFile = topicElement.GetAttribute("DestinationFile");

					if (destinationFile.Length > 0)
					{
						sw.WriteLine("\t<LI> <OBJECT type=\"text/sitemap\">");
						sw.WriteLine(String.Format("\t\t<param name=\"Keyword\" value=\"{0}\">", title));
						sw.WriteLine(String.Format("\t\t<param name=\"Name\" value=\"{0}\">", title));
						sw.WriteLine(String.Format("\t\t<param name=\"Local\" value=\"{0}\">", destinationFile));
						sw.WriteLine("\t\t</OBJECT>");
					}
				}

				sw.WriteLine("</UL>");
				sw.WriteLine("</BODY>");
				sw.WriteLine("</HTML>");
			}

			// create the table of contents file
			string tocFile = Path.Combine(this.outputDir, "toc.hhc");
			using (StreamWriter sw = File.CreateText(tocFile))
			{
				sw.WriteLine("<!DOCTYPE HTML PUBLIC \"-//IETF//DTD HTML//EN\">");
				sw.WriteLine("<HTML>");
				sw.WriteLine("<HEAD>");
				sw.WriteLine("<meta name=\"GENERATOR\" content=\"Microsoft&reg; HTML Help Workshop 4.1\">");
				sw.WriteLine("<!-- Sitemap 1.0 -->");
				sw.WriteLine("</HEAD><BODY>");
				sw.WriteLine("<OBJECT type=\"text/site properties\">");
				sw.WriteLine("\t<param name=\"ImageType\" value=\"Folder\">");
				sw.WriteLine("</OBJECT>");
				sw.WriteLine("<UL>");

				XmlNodeList topicNodes = doc.SelectNodes("//doc:TableOfContents/doc:Topic", this.namespaceManager);
				foreach (XmlNode topicNode in topicNodes)
				{
					this.WriteTopic((XmlElement)topicNode, sw);
				}

				sw.WriteLine("</UL>");
				sw.WriteLine("</BODY></HTML>");
			}

			// call the help compiler
			Process hhcProcess = new Process();
			hhcProcess.StartInfo.FileName = this.hhcFile;
			hhcProcess.StartInfo.Arguments = String.Concat("\"", projectFile, "\"");
			hhcProcess.StartInfo.CreateNoWindow = true;
			hhcProcess.StartInfo.UseShellExecute = false;
			hhcProcess.StartInfo.RedirectStandardOutput = true;
			hhcProcess.Start();

			// wait for the process to terminate
			hhcProcess.WaitForExit();

			// check for errors
			if (hhcProcess.ExitCode != 1)
			{
				throw new InvalidOperationException("The help compiler failed.");
			}
		}

		/// <summary>
		/// Write a single topic and its children to the HTMLHelp table of contents file.
		/// </summary>
		/// <param name="topicElement">The topic element to write.</param>
		/// <param name="sw">Writer for the table of contents.</param>
		private void WriteTopic(XmlElement topicElement, StreamWriter sw)
		{
			string destinationFile = topicElement.GetAttribute("DestinationFile");
			string title = topicElement.GetAttribute("Title");

			sw.WriteLine("\t<LI> <OBJECT type=\"text/sitemap\">");
			sw.WriteLine(String.Format("\t\t<param name=\"Name\" value=\"{0}\">", title));
			if (destinationFile.Length > 0)
			{
				sw.WriteLine(String.Format("\t\t<param name=\"Local\" value=\"{0}\">", destinationFile));
			}
			sw.WriteLine("\t\t</OBJECT>");

			XmlNodeList topicNodes = topicElement.SelectNodes("doc:Topic", this.namespaceManager);
			if (topicNodes.Count > 0)
			{
				sw.WriteLine("<UL>");
			}

			foreach (XmlNode topicNode in topicNodes)
			{
				this.WriteTopic((XmlElement)topicNode, sw);
			}

			if (topicNodes.Count > 0)
			{
				sw.WriteLine("</UL>");
			}
		}

		/// <summary>
		/// Build web manual pages.
		/// </summary>
		/// <param name="doc">The documentation compiler xml document.</param>
		private void BuildWeb(XmlDocument doc)
		{
			XmlNodeList topicNodes = doc.SelectNodes("//doc:TableOfContents/doc:Topic", this.namespaceManager);

			ArrayList parentTopics = new ArrayList();

			foreach (XmlNode topicNode in topicNodes)
			{
				this.WriteWebTopic((XmlElement)topicNode, parentTopics);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="topicElement">The topic element to write.</param>
		/// <param name="parentTopics"></param>
		private void WriteWebTopic(XmlElement topicElement, ArrayList parentTopics)
		{
			string file = Path.Combine(this.outputDir, topicElement.GetAttribute("DestinationFile"));
			string title = topicElement.GetAttribute("Title");

			if (file == "")
				return;

			string outputFile = Path.Combine(this.outputDir, Path.ChangeExtension(file, ".xml"));
			XmlTextWriter w = new XmlTextWriter(outputFile, Encoding.UTF8);

			w.WriteStartElement("ManualPage");
			w.WriteAttributeString("File", file);
			w.WriteAttributeString("Title", title);

			IEnumerator parentTopicEnumerator = parentTopics.GetEnumerator();
			if (parentTopicEnumerator.MoveNext())
			{
				w.WriteStartElement("ParentTopics");
				this.WriteWebParentTopic(parentTopicEnumerator, w);
				w.WriteEndElement();
			}

			w.WriteEndElement();
			w.Close();

			XmlNodeList topicNodes = topicElement.SelectNodes("doc:Topic", this.namespaceManager);
			if (topicNodes.Count > 0)
			{
				parentTopics.Add(topicElement);

				foreach (XmlNode topicNode in topicNodes)
				{
					this.WriteWebTopic((XmlElement)topicNode, parentTopics);
				}

				parentTopics.RemoveAt(parentTopics.Count - 1);
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="topicEnumerator"></param>
		/// <param name="w"></param>
		private void WriteWebParentTopic(IEnumerator topicEnumerator, XmlWriter w)
		{
			XmlElement topicElement = (XmlElement)topicEnumerator.Current;

			string destinationFile = Path.GetFileName(topicElement.GetAttribute("DestinationFile"));
			string title = topicElement.GetAttribute("Title");

			w.WriteStartElement("Topic");
			w.WriteAttributeString("File", destinationFile);
			w.WriteAttributeString("Title", title);

			if (topicEnumerator.MoveNext())
			{
				WriteWebParentTopic(topicEnumerator, w);
			}

			w.WriteEndElement();
		}

		/// <summary>
		/// Parse the command line arguments.
		/// </summary>
		/// <param name="args">Command line arguments.</param>
		private void ParseCommandline(string[] args)
		{
			foreach (string arg in args)
			{
				if (arg.StartsWith("-") || arg.StartsWith("/"))
				{
					if (arg.Length > 1)
					{
						switch (arg[1])
						{
							case '?':
								this.showHelp = true;
								break;
							case 'c':
								this.hhcFile = arg.Substring(3);
								this.chm = true;
								break;
							case 'v':
								this.versionNumber = arg.Substring(3);
								break;
							case 'w':
								this.web = true;
								break;
							default:
								throw new ArgumentException(String.Format("Unrecognized commandline parameter '{0}'.", arg));
						}
					}
					else
					{
						throw new ArgumentException(String.Format("Unrecognized commandline parameter '{0}'.", arg));
					}
				}
				else if (this.tocFile == null)
				{
					this.tocFile = arg;
				}
				else if (this.outputFileName == null)
				{
					if (this.chm)
					{
						this.outputFileName = Path.GetFileName(arg);
						this.outputDir = Path.GetDirectoryName(arg);
					}
					if (this.web)
					{
						this.outputDir = arg;
					}
				}
				else
				{
					throw new ArgumentException(String.Format("Unrecognized argument '{0}'.", arg));
				}
			}

			// check for missing mandatory arguments
			if (!this.showHelp && (this.outputDir == null || (!this.chm && !this.web) || (this.chm && this.hhcFile == null)))
			{
				throw new ArgumentException("Missing mandatory argument.");
			}
		}
	}
}
