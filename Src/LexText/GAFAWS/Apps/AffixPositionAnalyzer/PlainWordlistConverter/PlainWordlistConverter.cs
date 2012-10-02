// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2007' company='SIL International'>
//    Copyright (c) 2007, SIL International. All Rights Reserved.
// </copyright>
//
// File: PlainWordlistConverter.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Converts a plain text analyzed wordlist for use by GAFAWS.
// The list will be one analyzed word per line, and follow this format:
// p1-p2-<stem>-s1-s2
// Prefixes or suffixes are optional, but the boundary <stem> is required.
// Optional whitespace can separate affixes and the stem.
// A hyphen is required to mark boundaries between other affixes and the stem.
// Technically, the optional whitespace can be on either side of the hyphen,
// or on both sides of it.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Xsl;

using SIL.WordWorks.GAFAWS;

namespace SIL.WordWorks.GAFAWS.PlainWordlistConverter
{
	public class PlainWordlistConverter : GafawsProcessor, IGAFAWSConverter
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		internal PlainWordlistConverter()
		{
		}

		#region IGAFAWSConverter implementation

		/// <summary>
		/// Do whatever it takes to convert the input this processor knows about.
		/// </summary>
		public void Convert()
		{
			string outputPathname = null;

			OpenFileDialog openFileDlg = new OpenFileDialog();

			openFileDlg.InitialDirectory = "c:\\";
			openFileDlg.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
			openFileDlg.FilterIndex = 2;
			openFileDlg.Multiselect = false;

			if (openFileDlg.ShowDialog() == DialogResult.OK)
			{
				string sourcePathname = openFileDlg.FileName;
				if (File.Exists(sourcePathname))
				{
					// Try to convert it.
					using (StreamReader reader = new StreamReader(sourcePathname))
					{
						string line = reader.ReadLine();
						Dictionary<string, bool> dictPrefixes = new Dictionary<string, bool>();
						Dictionary<string, bool> dictStems = new Dictionary<string, bool>();
						Dictionary<string, bool> dictSuffixes = new Dictionary<string, bool>();
						while (line != null)
						{
							line = line.Trim();
							if (line != String.Empty)
							{
								int openAngleLocation = line.IndexOf("<", 0);
								if (openAngleLocation < 0)
									continue;
								int closeAngleLocation = line.IndexOf(">", openAngleLocation + 1);
								if (closeAngleLocation < 0)
									continue;
								WordRecord wrdRec = new WordRecord();
								m_gd.WordRecords.Add(wrdRec);

								// Handle prefixes, if any.
								string prefixes = null;
								if (openAngleLocation > 0)
									prefixes = line.Substring(0, openAngleLocation);
								if (prefixes != null)
								{
									if (wrdRec.Prefixes == null)
										wrdRec.Prefixes = new List<Affix>();
									foreach (string prefix in prefixes.Split('-'))
									{
										if (prefix != null && prefix != "")
										{
											Affix afx = new Affix();
											afx.MIDREF = prefix;
											wrdRec.Prefixes.Add(afx);
											if (!dictPrefixes.ContainsKey(prefix))
											{
												m_gd.Morphemes.Add(new Morpheme(MorphemeType.prefix, prefix));
												dictPrefixes.Add(prefix, true);
											}
										}
									}
								}

								// Handle stem.
								string sStem = null;
								// Stem has content, so use it.
								sStem = line.Substring(openAngleLocation + 1, closeAngleLocation - openAngleLocation - 1);
								if (sStem.Length == 0)
									sStem = "stem";
								Stem stem = new Stem();
								stem.MIDREF = sStem;
								wrdRec.Stem = stem;
								if (!dictStems.ContainsKey(sStem))
								{
									m_gd.Morphemes.Add(new Morpheme(MorphemeType.stem, sStem));
									dictStems.Add(sStem, true);
								}

								// Handle suffixes, if any.
								string suffixes = null;
								if (line.Length > closeAngleLocation + 2)
									suffixes = line.Substring(closeAngleLocation + 1);
								if (suffixes != null)
								{
									if (wrdRec.Suffixes == null)
										wrdRec.Suffixes = new List<Affix>();
									foreach (string suffix in suffixes.Split('-'))
									{
										if (suffix != null && suffix != "")
										{
											Affix afx = new Affix();
											afx.MIDREF = suffix;
											wrdRec.Suffixes.Add(afx);
											if (!dictSuffixes.ContainsKey(suffix))
											{
												m_gd.Morphemes.Add(new Morpheme(MorphemeType.suffix, suffix));
												dictSuffixes.Add(suffix, true);
											}
										}
									}
								}
							}
							line = reader.ReadLine();
						}

						// Main processing.
						PositionAnalyzer anal = new PositionAnalyzer();
						anal.Process(m_gd);

						// Do any post-analysis processing here, if needed.
						// End of any optional post-processing.

						// Save, so it can be transformed.
						outputPathname = GetOutputPathname(sourcePathname);
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
						finally
						{
							if (outputPathname != null && File.Exists(outputPathname))
								File.Delete(outputPathname);
						}
						Process.Start(htmlOutput);
					} // end 'using'
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
			get { return "Wordlist converter"; }
		}

		/// <summary>
		/// Gets a description of the converter that is suitable for display.
		/// </summary>
		public string Description
		{
			get { return "Prepare a wordlist for processing.\r\nThe list will follow this pattern:\r\np1-p2-<stem>-s1-s2\r\nAffixes are optional, but the stem/root is not. The content between the stem markers (< and >) is up to the user."; }
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
					"AffixPositionChart_PWL.xsl");
			}
		}

		#endregion IGAFAWSConverter implementation

	}
}
