// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Xsl;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LanguageExplorer.Areas;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application.ApplicationServices;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary />
	public class LexImport
	{
		private string m_sPhase2XSLT;
		private string m_sPhase3XSLT;
		private string m_sPhase4XSLT;
		private string m_sBuildPhase2XSLT;
		private LcmCache m_cache;
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

		/// <summary />
		public LexImport(LcmCache cache, string tempDir, string transformDir)
		{
			m_cache = cache;
			m_sTempDir = tempDir;

			// XSLT files
			m_sPhase2XSLT = Path.Combine(tempDir, "Phase2.xsl"); // needs to be in temp dir also sense it's created
			m_sPhase3XSLT = Path.Combine(transformDir, "Phase3.xsl");
			m_sPhase4XSLT = Path.Combine(transformDir, "Phase4.xsl");
			m_sBuildPhase2XSLT = Path.Combine(transformDir, "BuildPhase2XSLT.xsl");
		}

		/// <summary>
		/// does the import
		/// </summary>
		/// <param name="dlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: 1) runToCompletion, 2) last step,
		/// 3) start phase, 4) database file name, 5) number of entries, 6) true to display
		/// the import report, 7) name of phase 1 HTML report file, 8) name of phase 1 file,
		/// 9) true to create entries for missing link targets.
		/// </param>
		/// <returns><c>true</c> if import was successful, otherwise <c>false</c>.</returns>
		public object Import(IThreadedProgress dlg, object[] parameters)
		{
			Debug.Assert(parameters.Length == 9);
			var runToCompletion = (bool)parameters[0];
			var lastStep = (int)parameters[1];
			var startPhase = (int)parameters[2];
			var databaseFileName = (string)parameters[3];
			var cEntries = (int)parameters[4];
			m_fDisplayImportReport = (bool)parameters[5];
			m_sPhase1HtmlReport = (string)parameters[6];
			m_sPhase1FileName = (string)parameters[7];
			var fCreateMissingLinks = (bool) parameters[8];

			var sErrorMsg = LexTextControls.ksTransformProblem_X;
			var fAttemptedXml = false;
			var processedInputFile = databaseFileName;
			var sPhase1Output = Path.Combine(m_sTempDir, s_sPhase1FileName);
			var sPhase2Output = Path.Combine(m_sTempDir, s_sPhase2FileName);
			var sPhase3Output = Path.Combine(m_sTempDir, s_sPhase3FileName);
			m_sPhase4Output = Path.Combine(m_sTempDir, s_sPhase4FileName);

			XmlImportData xid = null;
			try
			{
				// if starting with a phase file, rename the phase file with the input file
				switch (startPhase)
				{
					case 1:
						sPhase1Output = databaseFileName;
						break;
					case 2:
						sPhase2Output = databaseFileName;
						break;
					case 3:
						sPhase3Output = databaseFileName;
						break;
					case 4:
						m_sPhase4Output = databaseFileName;
						break;
					default:
						break; // no renaming needed
				}

				if (startPhase < 2)
				{
					dlg.Title = string.Format(LexTextControls.ksImportingXEntriesFromY, cEntries, processedInputFile);
					dlg.Message = string.Format(LexTextControls.ksPhase1ofX_Preview, lastStep);
					sErrorMsg = LexTextControls.ksTransformProblemPhase1_X;
					DoTransform(m_sBuildPhase2XSLT, sPhase1Output, m_sPhase2XSLT);
				}
				dlg.Step(10);
				if (dlg.Canceled)
				{
					return false;
				}

				sErrorMsg = LexTextControls.ksTransformProblemPhase2_X;
				dlg.Message = string.Format(LexTextControls.ksPhase2ofX, lastStep);
				if (startPhase < 2)
				{
					DoTransform(m_sPhase2XSLT, sPhase1Output, sPhase2Output);
				}
				dlg.Step(10);
				if (dlg.Canceled)
				{
					return false;
				}

				sErrorMsg = LexTextControls.ksTransformProblemPhase3_X;
				dlg.Message = string.Format(LexTextControls.ksPhase3ofX, lastStep);
				if (startPhase < 3)
				{
					DoTransform(m_sPhase3XSLT, sPhase2Output, sPhase3Output);
				}
				dlg.Step(10);
				if (dlg.Canceled)
				{
					return false;
				}

				sErrorMsg = LexTextControls.ksTransformProblemPhase4_X;
				dlg.Message = string.Format(LexTextControls.ksPhase4ofX, lastStep);
				if (startPhase < 4)
				{
					DoTransform(m_sPhase4XSLT, sPhase3Output, m_sPhase4Output);
				}
				dlg.Step(20);
				if (dlg.Canceled)
				{
					return false;
				}

				if (runToCompletion)
				{
					sErrorMsg = LexTextControls.ksXmlParsingProblem5_X;
					dlg.Message = LexTextControls.ksPhase5of5_LoadingData;
					if (dlg.Canceled)
					{
						return false;
					}
					dlg.Step(1);
					// There's no way to cancel from here on out.
					dlg.AllowCancel = false;
					xid = new XmlImportData(m_cache, fCreateMissingLinks);
					fAttemptedXml = true;
					if (startPhase == 4 && processedInputFile.Length == 0)
					{
						processedInputFile = m_sPhase4Output;
					}
					xid.ImportData(m_sPhase4Output, dlg);
					sErrorMsg = LexTextControls.ksLogFileProblem5_X;
					ProcessLogFile(processedInputFile, startPhase, xid);
					return true;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Error: " + ex.Message);

				ReportError(string.Format(sErrorMsg, ex.Message), LexTextControls.ksUnhandledError);

				if (fAttemptedXml)
				{
					// We want to see the log file even (especially) if the Xml blows up.
					ProcessLogFile(processedInputFile, startPhase, xid);
				}
			}

			return false;
		}

		private static void DoTransform(string xsl, string xml, string output)
		{
			// Create the XslTransform and load the stylesheet.
			var xslt = new XslCompiledTransform();
			xslt.Load(xsl, XsltSettings.TrustedXslt, null);

			// Create an XmlReader for input to the transform.
			// Create an XmlTextWriter to output the result of the transform.
			using (var reader = XmlReader.Create(xml))
			using (var writer = new XmlTextWriter(output, System.Text.Encoding.UTF8))
			{
				// Do NOT set writer.Formatting to Formatting.Indented. It can insert spurious white space, for example,
				// when the first child of a Custom element in phase 2 is an InFieldMarker, it inserts
				// a newline before it, which becomes part of the content of the string, with bad consequences
				// (LT-LT-13607)
				// Transform the file.
				xslt.Transform(reader, writer);
				writer.Close();
			}
		}

		internal static string GetHtmlJavaScript()
		{
			// create a string for storing the jscript html code for showing the link
			var sRootDir = FwDirectoryFinder.CodeDirectory;
			if (!sRootDir.EndsWith("\\"))
			{
				sRootDir += "\\";
			}

			// TODO-Linux: this isn't portable this way
			var sNewLine = Environment.NewLine;
			var zedit = "\"" + sRootDir + "zedit.exe\"";
			zedit = zedit.Replace(@"\", @"\\");
			var script = "<script>" + sNewLine +
				"function exec (command) {" + sNewLine +
				@"command = '" + zedit + " ' + command" + sNewLine +
				"//alert(command) // look at the command" + sNewLine +
				"if (document.layers && navigator.javaEnabled()) {" + sNewLine +
				"window._command = command;" + sNewLine +
				"window.oldOnError = window.onerror;" + sNewLine +
				"window.onerror = function (err) {" + sNewLine +
				@"if (err.indexOf (""User didn't grant"") != -1) {" + sNewLine +
				@"alert('command execution of ' + window._command + ' disallowed by user.'); " + sNewLine +
				"return true;" + sNewLine +
				"}" + sNewLine +
				"else return false;" + sNewLine +
				"}" + sNewLine +
				@"netscape.security.PrivilegeManager.enablePrivilege('UniversalExecAccess');" + sNewLine +
				"java.lang.Runtime.getRuntime().exec(command);" + sNewLine +
				"window.onerror = window.oldOnError;" + sNewLine +
				"}" + sNewLine +
				"else if (document.compatMode) {" + sNewLine +
				"window.oldOnError = window.onerror;" + sNewLine +
				"window._command = command;" + sNewLine +
				"window.onerror = function (err) {" + sNewLine +
				@"if (err.indexOf('utomation') != -1) {" + sNewLine +
				@"alert('command execution of ' + window._command + ' disallowed by user.'); " + sNewLine +
				"return true;" + sNewLine +
				"}" + sNewLine +
				"else return false;" + sNewLine +
				"};" + sNewLine +
				@"var wsh = new ActiveXObject('WScript.Shell');" + sNewLine +
				"if (wsh)" + sNewLine +
				"wsh.Run(command);" + sNewLine +
				"window.onerror = window.oldOnError;" + sNewLine +
				"}" + sNewLine +
				"}" + sNewLine +
				"</script>" + sNewLine;
			return script;
		}

		private void ProcessLogFile(string processedInputFile, int startPhase, XmlImportData xid)
		{
			string inputFileName = processedInputFile;
			if (startPhase > 0)
			{
				inputFileName = m_sPhase1FileName;
				inputFileName = inputFileName.Replace("1", startPhase.ToString());
			}
			ProcessPhase4Log(inputFileName, xid);

			if (m_fDisplayImportReport)
			{
				var sHtmlFile = Path.Combine(m_sTempDir, "ImportLog.htm");
				Process.Start(sHtmlFile);
			}
		}

		private void ProcessPhase4Log(string inputFileName, XmlImportData xid)
		{
			var sLogFile = m_sPhase4Output;
			var ich = m_sPhase4Output.LastIndexOf('.');
			if (ich != -1)
			{
				sLogFile = m_sPhase4Output.Remove(ich, sLogFile.Length - ich);
			}
			sLogFile += "-Import.log";
			StreamReader sr;
			try
			{
				sr = File.OpenText(sLogFile);
			}
			catch
			{
				return;
			}
			try
			{
				var sHtmlFile = Path.Combine(m_sTempDir, "ImportLog.htm");
				using (var sw = File.CreateText(sHtmlFile))
				{
					sw.WriteLine("<html>");
					sw.WriteLine("<head>");
					sw.WriteLine("<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\"/>");
					var sHeadInfo = string.Format(LexTextControls.ksImportLogForX, inputFileName);
					sw.WriteLine($"  <title>{sHeadInfo}</title>");
					// add the script
					var script = GetHtmlJavaScript();
					sw.WriteLine(script);
					// done adding the java script for jumping to errors
					sw.WriteLine("</head>");
					sw.WriteLine("<body>");
					sw.WriteLine($"<h2>{sHeadInfo}</h2>");
					sw.WriteLine($"<h3>{LexTextControls.ksMessagesFromPreview}</h3>");
					sw.WriteLine(m_sPhase1HtmlReport);
					string sInput;
					// LT-1901 : make a first pass through the log file and put all "Warning:" errors together
					var bWarningFound = false;	// none yet
					var sWarning_ = LexTextControls.ksWarning_;	// localized version of "Warning:"
					var sInfo_ = LexTextControls.ksInfo_;		// localized version of "Info:"
					while ((sInput = sr.ReadLine()) != null)
					{
						ich = sInput.IndexOf(sWarning_);
						if (ich == -1)
						{
							ich = sInput.IndexOf("Warning:");	// in case warning message not localized...
						}
						if (ich != -1)
						{
							if (!bWarningFound)	// first time put out the header
							{
								bWarningFound = true;
								sw.WriteLine($"<p><h3>{LexTextControls.ksMayBeBugInImport}</h3>");
								sw.WriteLine("<ul>");
							}

							// Need to quote any occurrences of <, >, or & in the message text.
							sInput = sInput.Replace("&", "&amp;");
							sInput = sInput.Replace("<", "&lt;");
							sInput = sInput.Replace(">", "&gt;");
							sw.WriteLine("<li>" + sInput);
						}
					}
					sr.Dispose();
					sr = File.OpenText(sLogFile);

					if (bWarningFound)
					{
						sw.WriteLine("</ul>");
					}

					sw.WriteLine($"<p><h3>{LexTextControls.ksMessagesFromLoading}</h3>");
					string sTiming = null;
					sw.WriteLine("<ul>");

					var sPath = m_sPhase4Output.Replace("\\", "\\\\");
					var rgsCreated = xid.CreatedForMessages;
					var rgxMsgs = new List<Regex>();
					foreach (var sMsg in rgsCreated)
					{
						var sRegex = "^" + sMsg + "$";
						sRegex = sRegex.Replace("{0}", sPath);
						sRegex = sRegex.Replace("{1}", "[0-9]+");
						sRegex = sRegex.Replace("{2}", "[^\"]+");
						var xMsg = new Regex(sRegex);
						rgxMsgs.Add(xMsg);
					}

					var sElapsedTimeMsg = xid.ElapsedTimeMsg;
					ich = sElapsedTimeMsg.IndexOf("{0:F1}");
					Debug.Assert(ich >= 0);
					sElapsedTimeMsg = sElapsedTimeMsg.Substring(0, ich);
					// Print the Info: messages together, save everything else for a later loop.
					var rgsNotInfo = new List<string>();
					while ((sInput = sr.ReadLine()) != null)
					{
						// warning msgs were already handled, so don't show them again
						ich = sInput.IndexOf(sWarning_);
						if (ich == -1)
						{
							ich = sInput.IndexOf("Warning:");	// in case warning message not localized...
						}

						if (ich != -1)
						{
							continue;
						}
						ich = sInput.IndexOf(m_sPhase4Output + ":");
						if (ich != -1)
						{
							var ichNumber = ich + m_sPhase4Output.Length + 1;
							ich = sInput.IndexOf(": ", ichNumber);
							if (ich != -1)
							{
								// Need to quote any occurrences of <, >, or & in the message text.
								var sOutput = sInput.Remove(0, ich + 2);
								sOutput = sOutput.Replace("&", "&amp;");
								sOutput = sOutput.Replace("<", "&lt;");
								sOutput = sOutput.Replace(">", "&gt;");
								if (AnyMsgMatches(rgxMsgs, sInput))
								{
									var sNumber = sInput.Substring(ichNumber, ich - ichNumber);
									int hvo;
									if (int.TryParse(sNumber, out hvo))
									{
										// Convert text between ichBegin and ichEnd into a link to the Flex entry.
										var sLinkRef = LinkRef(hvo);
										if (sLinkRef != null)
										{
											var ichBegin = sOutput.IndexOf('"') + 1;
											var ichEnd = sOutput.IndexOf('"', ichBegin);
											Debug.Assert(ichBegin > 0 && ichEnd > ichBegin);
											sOutput = sOutput.Insert(ichEnd, "</a>");
											sOutput = sOutput.Insert(ichBegin, $"<a href=\"{sLinkRef}\">");
										}
									}
								}
								sOutput = sOutput.Insert(0, "<li>");
								if (sOutput.IndexOf(sInfo_) >= 0 || sOutput.IndexOf("Info:") >= 0)
								{
									sw.WriteLine(sOutput);
								}
								else
								{
									rgsNotInfo.Add(sOutput);
								}
							}
						}
						else
						{
							ich = sInput.IndexOf(sElapsedTimeMsg);
							if (ich != -1)
							{
								sTiming = sInput;
							}
						}
					}
					sr.Close();
					foreach (var notInfo in rgsNotInfo)
					{
						sw.WriteLine(notInfo);
					}
					sw.WriteLine("</ul>");
					if (sTiming != null)
					{
						sw.WriteLine("<p>" + sTiming);
					}
					sw.WriteLine("</body>");
					sw.WriteLine("</html>");
					sw.Close();
				}
			}
			finally
			{
				sr.Dispose();
			}
		}

		private static bool AnyMsgMatches(List<Regex> rgxCreated, string sInput)
		{
			foreach (var xMsg in rgxCreated)
			{
				if (xMsg.IsMatch(sInput))
				{
					return true;
				}
			}
			return false;
		}

		internal string LinkRef(int hvo)
		{
			var repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			if (!repo.IsValidObjectId(hvo))
			{
				return null;
			}
			return new FwLinkArgs(AreaServices.LexiconEditMachineName, repo.GetObject(hvo).Guid).ToString();
		}

		/// <summary>
		/// Reports an error.
		/// </summary>
		private void ReportError(string message, string caption)
		{
			Error?.Invoke(this, message, caption);
		}
	}
}
