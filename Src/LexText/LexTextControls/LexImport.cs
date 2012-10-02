// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LexImport.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LexImport
	{
		private string m_sPhase2XSLT;
		private string m_sPhase3XSLT;
		private string m_sPhase4XSLT;
		private string m_sBuildPhase2XSLT;
		private bool m_fCancel;
		private FdoCache m_cache;
		private string m_sTempDir;
		private string m_sPhase4Output;
		private bool m_fDisplayImportReport;
		private string m_sPhase1HtmlReport;
		private string m_sPhase1FileName;

		public delegate void ErrorHandler(object sender, string message, string caption);
		public event ErrorHandler Error;

		public static readonly string s_sPhase1FileName = "Phase1Output.xml";
		public static readonly string s_sPhase2FileName = "Phase2Output.xml";
		public static readonly string s_sPhase3FileName = "Phase3Output.xml";
		public static readonly string s_sPhase4FileName = "Phase4Output.xml";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:LexImport"/> class.
		/// </summary>
		/// <param name="cache">The FDO cache.</param>
		/// <param name="tempDir">The temp dir.</param>
		/// <param name="transformDir">The transform dir.</param>
		/// ------------------------------------------------------------------------------------
		public LexImport(FdoCache cache, string tempDir, string transformDir)
		{
			m_cache = cache;
			m_sTempDir = tempDir;

			// XSLT files
			m_sPhase2XSLT = Path.Combine(tempDir, "Phase2.xsl"); // needs to be in temp dir also sense it's created
			m_sPhase3XSLT = Path.Combine(transformDir, "Phase3.xsl");
			m_sPhase4XSLT = Path.Combine(transformDir, "Phase4.xsl");
			m_sBuildPhase2XSLT = Path.Combine(transformDir, "BuildPhase2XSLT.xsl");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user presses the cancel button on the dialog.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// ------------------------------------------------------------------------------------
		public void OnProgressDlgCancel(object sender)
		{
			m_fCancel = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// does the import
		/// </summary>
		/// <param name="dlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) runToCompletion, 2) last step,
		/// 3) start phase, 4) database file name, 5) number of entries, 6) true to display
		/// the import report, 7) name of phase 1 HTML report file, 8) name of phase 1 file.
		/// </param>
		/// <returns><c>true</c> if import was successful, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		public object Import(IAdvInd4 dlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 8);
			bool runToCompletion = (bool)parameters[0];
			int lastStep = (int)parameters[1];
			int startPhase = (int)parameters[2];
			string databaseFileName = (string)parameters[3];
			int cEntries = (int)parameters[4];
			m_fDisplayImportReport = (bool)parameters[5];
			m_sPhase1HtmlReport = (string)parameters[6];
			m_sPhase1FileName = (string)parameters[7];

			string sErrorMsg = LexTextControls.ksTransformProblem_X;
			bool fAttemptedXml = false;
			bool fXmlOk = false;
			string processedInputFile = databaseFileName;
			string sPhase1Output = Path.Combine(m_sTempDir, s_sPhase1FileName);
			string sPhase2Output = Path.Combine(m_sTempDir, s_sPhase2FileName);
			string sPhase3Output = Path.Combine(m_sTempDir, s_sPhase3FileName);
			m_sPhase4Output = Path.Combine(m_sTempDir, s_sPhase4FileName);

			try
			{
				// if starting with a phase file, rename the phase file with the input file
				switch (startPhase)
				{
					case 1: sPhase1Output = databaseFileName; break;
					case 2: sPhase2Output = databaseFileName; break;
					case 3: sPhase3Output = databaseFileName; break;
					case 4: m_sPhase4Output = databaseFileName; break;
					default: break;	// no renaming needed
				}

				IAdvInd ai = dlg as IAdvInd;

				if (startPhase < 2)
				{
					dlg.Title = String.Format(LexTextControls.ksImportingXEntriesFromY,
						cEntries, processedInputFile);
					dlg.Message = String.Format(LexTextControls.ksPhase1ofX_Preview, lastStep);
					sErrorMsg = LexTextControls.ksTransformProblemPhase1_X;
					DoTransform(m_sBuildPhase2XSLT, sPhase1Output, m_sPhase2XSLT);
				}
				ai.Step(10);
				if (m_fCancel)
					return false;

				sErrorMsg = LexTextControls.ksTransformProblemPhase2_X;
				dlg.Message = String.Format(LexTextControls.ksPhase2ofX, lastStep);
				if (startPhase < 2)
					DoTransform(m_sPhase2XSLT, sPhase1Output, sPhase2Output);
				ai.Step(10);
				if (m_fCancel)
					return false;

				sErrorMsg = LexTextControls.ksTransformProblemPhase3_X;
				dlg.Message = String.Format(LexTextControls.ksPhase3ofX, lastStep);
				if (startPhase < 3)
					DoTransform(m_sPhase3XSLT, sPhase2Output, sPhase3Output);
				ai.Step(10);
				if (m_fCancel)
					return false;

				sErrorMsg = LexTextControls.ksTransformProblemPhase4_X;
				dlg.Message = String.Format(LexTextControls.ksPhase4ofX, lastStep);
				if (startPhase < 4)
					DoTransform(m_sPhase4XSLT, sPhase3Output, m_sPhase4Output);
				ai.Step(20);
				if (m_fCancel)
					return false;

				if (runToCompletion)
				{
					sErrorMsg = LexTextControls.ksXmlParsingProblem5_X;
					dlg.Message = LexTextControls.ksPhase5of5_LoadingData;
					IFwXmlData2 fxd = FwXmlDataClass.Create();
					fxd.Open(m_cache.ServerName, m_cache.DatabaseName);
					int hvoOwner = m_cache.LangProject.LexDbOA.Hvo;
					int flid = (int)LexDb.LexDbTags.
						kflidEntries;
					if (m_fCancel)
						return false;
					ai.Step(1);
					// There's no way to cancel from here on out.
					if (dlg is ProgressDialogWithTask)
						((ProgressDialogWithTask)dlg).CancelButtonVisible = false;
					fAttemptedXml = true;
					if (startPhase == 4 && processedInputFile.Length == 0)
						processedInputFile = m_sPhase4Output;
					fxd.put_BaseImportDirectory(processedInputFile.Substring(0,
						processedInputFile.LastIndexOfAny(new char[2] { '\\', '/' })));
					fxd.ImportXmlObject(m_sPhase4Output, hvoOwner, flid, ai);
					fXmlOk = true;
					sErrorMsg = LexTextControls.ksLogFileProblem5_X;
					ProcessLogFile(processedInputFile, startPhase);
					return true;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error: " + ex.Message);

				ReportError(string.Format(sErrorMsg, ex.Message), LexTextControls.ksUnhandledError);

				if (fAttemptedXml && !fXmlOk)
				{
					// We want to see the log file even (especially) if the Xml blows up.
					ProcessLogFile(processedInputFile, startPhase);
				}
			}

			return false;
		}

		private void DoTransform(string xsl, string xml, string output)
		{
			//Create the XslTransform and load the stylesheet.
			XslCompiledTransform xslt = new XslCompiledTransform();
			xslt.Load(xsl, System.Xml.Xsl.XsltSettings.TrustedXslt, null);

			//Load the XML data file.
			XPathDocument doc = new XPathDocument(xml);

			//Create an XmlTextWriter to output to the console.
			XmlTextWriter writer = new XmlTextWriter(output, System.Text.Encoding.UTF8);
			writer.Formatting = System.Xml.Formatting.Indented;

			//Transform the file.
			xslt.Transform(doc, null, writer);
			writer.Close();
		}

		internal static string GetHtmlJavaScript()
		{
			// create a string for storing the jscript html code for showing the link
			string sRootDir = DirectoryFinder.FWCodeDirectory;
			if (!sRootDir.EndsWith("\\"))
				sRootDir += "\\";

			string sNewLine = System.Environment.NewLine;
			string zedit = "\"" + sRootDir + "zedit.exe\"";
			zedit = zedit.Replace(@"\", @"\\");
			string script = @"<script>" + sNewLine +
				@"function exec (command) {" + sNewLine +
				@"command = '" + zedit + " ' + command" + sNewLine +
				@"//alert(command) // look at the command" + sNewLine +
				@"if (document.layers && navigator.javaEnabled()) {" + sNewLine +
				@"window._command = command;" + sNewLine +
				@"window.oldOnError = window.onerror;" + sNewLine +
				@"window.onerror = function (err) {" + sNewLine +
				@"if (err.indexOf (""User didn't grant"") != -1) {" + sNewLine +
				@"alert('command execution of ' + window._command + ' disallowed by user.'); " + sNewLine +
				@"return true;" + sNewLine +
				@"}" + sNewLine +
				@"else return false;" + sNewLine +
				@"}" + sNewLine +
				@"netscape.security.PrivilegeManager.enablePrivilege('UniversalExecAccess');" + sNewLine +
				@"java.lang.Runtime.getRuntime().exec(command);" + sNewLine +
				@"window.onerror = window.oldOnError;" + sNewLine +
				@"}" + sNewLine +
				@"else if (document.all) {" + sNewLine +
				@"window.oldOnError = window.onerror;" + sNewLine +
				@"window._command = command;" + sNewLine +
				@"window.onerror = function (err) {" + sNewLine +
				@"if (err.indexOf('utomation') != -1) {" + sNewLine +
				@"alert('command execution of ' + window._command + ' disallowed by user.'); " + sNewLine +
				@"return true;" + sNewLine +
				@"}" + sNewLine +
				@"else return false;" + sNewLine +
				@"};" + sNewLine +
				@"var wsh = new ActiveXObject('WScript.Shell');" + sNewLine +
				@"if (wsh)" + sNewLine +
				@"wsh.Run(command);" + sNewLine +
				@"window.onerror = window.oldOnError;" + sNewLine +
				@"}" + sNewLine +
				@"}" + sNewLine +
				@"</script>" + sNewLine;
			return script;
		}

		private void ProcessLogFile(string processedInputFile, int startPhase)
		{
			string inputFileName = processedInputFile;
			if (startPhase > 0)
			{
				inputFileName = m_sPhase1FileName;
				inputFileName = inputFileName.Replace("1", startPhase.ToString());
			}
			ProcessPhase4Log(inputFileName);

			if (m_fDisplayImportReport)
			{
				string sHtmlFile = Path.Combine(m_sTempDir, "ImportLog.htm");
				Process.Start(sHtmlFile);
			}
		}

		private void ProcessPhase4Log(string inputFileName)
		{
			string sLogFile = m_sPhase4Output;
			int ich = m_sPhase4Output.LastIndexOf('.');
			if (ich != -1)
				sLogFile = m_sPhase4Output.Remove(ich, sLogFile.Length - ich);
			sLogFile += "-Import.log";
			StreamReader sr;
			try
			{
				sr = File.OpenText(sLogFile);
			}
			catch
			{
				sr = null;
			}
			if (sr == null)
				return;
			string sHtmlFile = Path.Combine(m_sTempDir, "ImportLog.htm");
			StreamWriter sw = File.CreateText(sHtmlFile);
			sw.WriteLine("<html>");
			sw.WriteLine("<head>");
			string sHeadInfo = String.Format(LexTextControls.ksImportLogForX, inputFileName);
			sw.WriteLine(String.Format("  <title>{0}</title>", sHeadInfo));
			// add the script
			string script = GetHtmlJavaScript();
			sw.WriteLine(script);
			// done adding the java script for jumping to errors
			sw.WriteLine("</head>");
			sw.WriteLine("<body>");
			sw.WriteLine(String.Format("<h2>{0}</h2>", sHeadInfo));
			sw.WriteLine(String.Format("<h3>{0}</h3>", LexTextControls.ksMessagesFromPreview));
			sw.WriteLine(m_sPhase1HtmlReport);
			string sInput;
			// LT-1901 : make a first pass through the log file and put all "Warning:" errors together
			bool bWarningFound = false;	// none yet
			string sWarning_ = LexTextControls.ksWarning_;	// localized version of "Warning:"
			while ((sInput = sr.ReadLine()) != null)
			{
				ich = sInput.IndexOf(sWarning_);
				if (ich == -1)
					ich = sInput.IndexOf("Warning:");	// in case warning message not localized...
				if (ich != -1)
				{
					if (!bWarningFound)	// first time put out the header
					{
						bWarningFound = true;
						sw.WriteLine(String.Format("<p><h3>{0}</h3>",
							LexTextControls.ksMayBeBugInImport));
						sw.WriteLine("<ul>");
					}

					// Need to quote any occurrences of <, >, or & in the message text.
					sInput = sInput.Replace("&", "&amp;");
					sInput = sInput.Replace("<", "&lt;");
					sInput = sInput.Replace(">", "&gt;");
					sw.WriteLine("<li>" + sInput);
				}
			}
			if (sr != null)
			{
				sr.Close();
				sr = File.OpenText(sLogFile);
			}

			if (bWarningFound)
				sw.WriteLine("</ul>");

			sw.WriteLine(String.Format("<p><h3>{0}</h3>",
				LexTextControls.ksMessagesFromLoading));
			string sTiming = null;
			sw.WriteLine("<ul>");
			// These next few strings should not be localized, because the C++ code that
			// generates them is not localized.
			string sMoreTimesFrag = LexTextControls.ksMoreTimesFrag;
			string sLoadingTookFrag = LexTextControls.ksLoadingTookFrag;
			string sCreatedAnEntryFor = "Created an entry for \"";
			string sToSatisfyCrossRef = "\" to satisfy a cross reference.";
			while ((sInput = sr.ReadLine()) != null)
			{
				// warning msgs were already handled, so don't show them again
				ich = sInput.IndexOf(sWarning_);
				if (ich == -1)
					ich = sInput.IndexOf("Warning:");	// in case warning message not localized...
				if (ich != -1)
					continue;
				ich = sInput.IndexOf(m_sPhase4Output + ":");
				if (ich != -1)
				{
					int ichNumber = ich + m_sPhase4Output.Length + 1;
					ich = sInput.IndexOf(": ", ichNumber);
					if (ich != -1)
					{
						string sNumber = sInput.Substring(ichNumber, ich - ichNumber);
						// Need to quote any occurrences of <, >, or & in the message text.
						sInput = sInput.Remove(0, ich + 2);
						sInput = sInput.Replace("&", "&amp;");
						sInput = sInput.Replace("<", "&lt;");
						sInput = sInput.Replace(">", "&gt;");
						if (sInput.StartsWith(sCreatedAnEntryFor) && sInput.Contains(sToSatisfyCrossRef))
						{
							int hvo;
							if (Int32.TryParse(sNumber, out hvo))
							{
								// Convert text between ichBegin and ichEnd into a link to the Flex entry.
								string sLinkRef = LinkRef(hvo);
								if (sLinkRef != null)
								{
									int ichBegin = sInput.IndexOf('"') + 1;
									int ichEnd = sInput.IndexOf(sToSatisfyCrossRef);
									Debug.Assert(ichBegin > 0 && ichEnd > ichBegin);
									sInput = sInput.Insert(ichEnd, "</a>");
									sInput = sInput.Insert(ichBegin, String.Format("<a href=\"{0}\">", sLinkRef));
								}
							}
						}
						sw.WriteLine("<li>" + sInput);
					}
				}
				else
				{
					ich = sInput.IndexOf("    [");
					if (ich == 0)
					{
						ich = sInput.IndexOf("][");
						if (ich >= 0)
							ich = sInput.IndexOf("][", ich + 2);
						if (ich >= 0)
							ich = sInput.IndexOf("]", ich + 2);
						if (ich >= 0)
						{
							sInput = sInput.Remove(2, ich - 1);	// leave 2 spaces at beginning of line.
							sInput = sInput.Replace("&", "&amp;");
							sInput = sInput.Replace("<", "&lt;");
							sInput = sInput.Replace(">", "&gt;");
							sw.WriteLine(sInput);			// merge with preceding line for list element.
							continue;
						}
					}
					ich = sInput.IndexOf(sMoreTimesFrag);
					if (ich == -1)			// in case msg not localized...
						ich = sInput.IndexOf(" more times in the XML file]");
					if (ich != -1)
					{
						sw.WriteLine("<li>" + sInput);
					}
					else
					{
						ich = sInput.IndexOf(sLoadingTookFrag);
						if (ich == -1)		// in case msg not localized...
							ich = sInput.IndexOf("Loading the XML file into the database took");
						if (ich != -1)
							sTiming = sInput;
					}
				}
			}
			sr.Close();
			sw.WriteLine("</ul>");
			if (sTiming != null)
				sw.WriteLine("<p>" + sTiming);
			sw.WriteLine("</body>");
			sw.WriteLine("</html>");
			sw.Close();
		}

		internal string LinkRef(int hvo)
		{
			Guid guid = m_cache.GetGuidFromId(hvo);
			if (guid == Guid.Empty)
				return null;
			FdoUi.FwLink link = FdoUi.FwLink.Create("lexiconEdit", guid,
				m_cache.ServerName, m_cache.DatabaseName);
			return link.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reports an error.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="caption">The caption.</param>
		/// ------------------------------------------------------------------------------------
		private void ReportError(string message, string caption)
		{
			if (Error != null)
				Error(this, message, caption);
		}
	}
}
