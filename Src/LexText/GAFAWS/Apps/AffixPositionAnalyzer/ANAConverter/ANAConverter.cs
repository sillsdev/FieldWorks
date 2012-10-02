// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2007' company='SIL International'>
//    Copyright (c) 2007, SIL International. All Rights Reserved.
// </copyright>
//
// File: ANAConverter.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Implementation of ANAGAFAWSConverter.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.Xml.Xsl;

using SIL.WordWorks.GAFAWS;

namespace SIL.WordWorks.GAFAWS.ANAConverter
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Converts an Ample ANA file into an XML document suitable for input to GAFAWSAnalysis.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class ANAGAFAWSConverter : GafawsProcessor, IGAFAWSConverter
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------

		public ANAGAFAWSConverter()
		{
			ANAObject.Reset();
		}

		#region IGAFAWSConverter implementation

		/// <summary>
		/// Do whatever it takes to convert the input this processor knows about.
		/// </summary>
		public void Convert()
		{
			using (ANAConverterDlg dlg = new ANAConverterDlg())
			{
				dlg.ShowDialog();
				if (dlg.DialogResult == DialogResult.OK)
				{
					string outputPathname = null;
					string parametersPathname = null;
					try
					{
						parametersPathname = dlg.ParametersPathname;
						string anaPathname = dlg.ANAPathname;
						using (StreamReader reader = new StreamReader(anaPathname)) // Client to catch any exception.
						{
							ANARecord record = null;
							string line = reader.ReadLine();
							ANARecord.SetParameters(parametersPathname);
							ANAObject.DataLayer = m_gd;

							// Sanity checks.
							if (line == null)
								ThrowFileLoadException(reader, anaPathname, "ANA File is empty");

							while (!line.StartsWith("\\a"))
							{
								line = line.Trim();
								if ((line != "") || ((line = reader.ReadLine()) == null))
									ThrowFileLoadException(reader, anaPathname, "Does not appear to be an ANA file.");
							}

							while (line != null)
							{
								switch (line.Split()[0])
								{
									case "\\a":
										{
											if (record != null)
												record.Convert();
											record = new ANARecord(line.Substring(3));
											break;
										}
									case "\\w":
										{
											record.ProcessWLine(line.Substring(3));
											break;
										}
									case "\\u":
										{
											record.ProcessOtherLine(LineType.kUnderlyingForm, line.Substring(3));
											break;
										}
									case "\\d":
										{
											record.ProcessOtherLine(LineType.kDecomposition, line.Substring(3));
											break;
										}
									case "\\cat":
										{
											record.ProcessOtherLine(LineType.kCategory, line.Substring(5));
											break;
										}
									default:
										// Eat this line.
										break;
								}
								line = reader.ReadLine();
							}
							Debug.Assert(record != null);
							record.Convert(); // Process last record.
						}

						// Main processing.
						PositionAnalyzer anal = new PositionAnalyzer();
						anal.Process(m_gd);

						// Do any post-analysis processing here, if needed.
						// End of any optional post-processing.

						// Save, so it can be transformed.
						outputPathname = GetOutputPathname(anaPathname);
						m_gd.SaveData(outputPathname);

						// Transform.
						XslCompiledTransform trans = new XslCompiledTransform();
						try
						{
							trans.Load(XSLPathname);
						}
						catch
						{
							MessageBox.Show("Could not load the XSL file.", "Information");
							return;
						}

						string htmlOutput = Path.GetTempFileName() + ".html";
						try
						{
							trans.Transform(outputPathname, htmlOutput);
						}
						catch
						{
							MessageBox.Show("Could not transform the input file.", "Information");
							return;
						}
						Process.Start(htmlOutput);
					}
					finally
					{
						if (parametersPathname != null && File.Exists(parametersPathname))
							File.Delete(parametersPathname);
						if (outputPathname != null && File.Exists(outputPathname))
							File.Delete(outputPathname);
					}
				}
			}

			// Reset m_gd, in case it gets called for another file.
			m_gd = GAFAWSData.Create();
		}

		/// <summary>
		/// Gets the name of the converter that is suitable for display in a list
		/// of other converts.
		/// </summary>
		public string Name
		{
			get { return "ANA converter"; }
		}

		/// <summary>
		/// Gets a description of the converter that is suitable for display.
		/// </summary>
		public string Description
		{
			get { return "Prepare a CARLA ANA file for processing."; }
		}

		/// <summary>
		/// Gets the pathname of the XSL file used to turn the XML into HTML.
		/// </summary>
		public string XSLPathname
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(
					Assembly.GetExecutingAssembly().CodeBase),
					"AffixPositionChart_ANA.xsl");
			}
		}

		#endregion IGAFAWSConverter implementation

		/// <summary>
		/// Close the reader, and throw a FileLoadException.
		/// </summary>
		/// <param name="reader">The reader to close.</param>
		/// <param name="pathInput">Input pathname for invalid file.</param>
		/// <param name="message">The message to use in the exception.</param>
		private void ThrowFileLoadException(StreamReader reader, string pathInput, string message)
		{
			reader.Close();
			throw new FileLoadException(message, pathInput);
		}

	}
}
