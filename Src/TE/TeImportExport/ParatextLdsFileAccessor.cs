// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParatextLdsFileAccessor.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ParatextLdsFileAccessor reads and writes a Paratext-style lds file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ParatextLdsFileAccessor
	{
		#region Member Data
		private FdoCache m_cache;
		private string m_fontName;
		private string m_fontSize;
		private string m_wsName;
		private string m_RtoL;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ParatextLdsFileAccessor"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public ParatextLdsFileAccessor(FdoCache cache)
		{
			m_cache = cache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Computes the settings.
		/// </summary>
		/// <param name="normalStyle">The normal style.</param>
		/// <param name="ws">The HVO of the vernacular writing system.</param>
		/// ------------------------------------------------------------------------------------
		private void ComputeSettings(UsfmStyEntry normalStyle, int ws)
		{
			FontInfo fontInfo = normalStyle.FontInfoForWs(ws);
			m_fontName = normalStyle.RealFontNameForWs(ws);
			m_fontSize = (fontInfo.m_fontSize.Value / 1000).ToString();

			LgWritingSystem lgws = new LgWritingSystem(m_cache, ws);
			m_wsName = lgws.Name.UserDefaultWritingSystem;
			m_RtoL = (lgws.RightToLeft ? "T" : "F");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the lds file which describes the writing system in Paratext.
		/// </summary>
		/// <param name="ldsFileName">Name of the LDS file.</param>
		/// <param name="normalStyle">The normal style.</param>
		/// <param name="ws">The HVO of the writing system for the current export.</param>
		/// <param name="fileWriterLDS">file writer for updating the Paratext LDS file</param>
		/// ------------------------------------------------------------------------------------
		public bool UpdateLdsFile(string ldsFileName, UsfmStyEntry normalStyle, int ws,
			FileWriter fileWriterLDS)
		{
			try
			{
				string ldsContents;
				using (StreamReader sr = new StreamReader(ldsFileName))
				{
					ldsContents = sr.ReadToEnd();
				}

				// Make backup of language file, if it doesn't exist.
				if (!File.Exists(ldsFileName + ".bak"))
					File.Copy(ldsFileName, ldsFileName + ".bak");
				// REVIEW: Best way to handle if the file cannot be copied?

				fileWriterLDS.Open(ldsFileName);
				UpdateLdsContents(ldsContents, normalStyle, ws, fileWriterLDS);
			}
			catch
			{
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the lds file which describes the writing system in Paratext.
		/// </summary>
		/// <param name="ldsContents">Contents of the existing LDS file.</param>
		/// <param name="normalStyle">The normal style.</param>
		/// <param name="ws">The writing system</param>
		/// <param name="writer">The file writer for the Paratext LDS file</param>
		/// ------------------------------------------------------------------------------------
		public void UpdateLdsContents(string ldsContents, UsfmStyEntry normalStyle,
			int ws, FileWriter writer)
		{
			ComputeSettings(normalStyle, ws);

			string [] lines = ldsContents.Split(new string[] { Environment.NewLine },
				StringSplitOptions.RemoveEmptyEntries);

			// Read lines of text from the file, updating it as necessary.
			bool fInGeneralSection = false;
			bool fInOtherSection = false;
			bool fHasGeneralSection = false;
			bool fHasCodePage = false;
			bool fHasFont = false;
			bool fHasFontSize = false;
			bool fHasName = false;
			bool fHasRTL = false;
			foreach (string line in lines)
			{
				if (line.StartsWith("[General]"))
				{
					if (fInOtherSection)
					{
						fInOtherSection = false;
						writer.WriteLine();
					}
					fInGeneralSection = true;
					fHasGeneralSection = true;
				}
				else if (line.StartsWith("["))
				{
					if (fInGeneralSection)
					{
						WriteGeneralSection(writer, ref fHasCodePage, ref fHasFont,
							ref fHasFontSize, ref fHasName, ref fHasRTL);

						fInGeneralSection = false;
						writer.WriteLine();
					}
					else if (fInOtherSection)
						writer.WriteLine();

					fInOtherSection = true;
				}

				if (fInGeneralSection)
				{
					string replaceString = string.Empty;

					// Check line for fields that must be updated
					if (line.StartsWith("codepage="))
					{
						replaceString = "codepage=65001";
						fHasCodePage = true;
					}
					else if (line.StartsWith("font="))
					{
						replaceString = "font=" + m_fontName;
						fHasFont = true;
					}
					else if (line.StartsWith("size="))
					{
						replaceString = "size=" + m_fontSize;
						fHasFontSize = true;
					}
					else if (line.StartsWith("name="))
					{
						replaceString = "name=" + m_wsName;
						fHasName = true;
					}
					else if (line.StartsWith("RTL="))
					{
						replaceString = "RTL=" + m_RtoL;
						fHasRTL = true;
					}
					if (replaceString != string.Empty)
						writer.WriteLine(replaceString); // Add updated line to file contents list
					else
						writer.WriteLine(line); // Add line, without changes, to file contents list
				}
				else
					writer.WriteLine(line);
			}

			if (!fHasGeneralSection)
			{
				if (fInOtherSection)
				{
					fInOtherSection = false;
					writer.WriteLine();
				}
				// General section not found. Write it.
				writer.WriteLine("[General]");
			}
			WriteGeneralSection(writer, ref fHasCodePage, ref fHasFont, ref fHasFontSize,
				ref fHasName, ref fHasRTL);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the contents of the general section.
		/// </summary>
		/// <param name="writer">The writer.</param>
		/// <param name="fHasCodePage">Indicates whether code page entry has been written.</param>
		/// <param name="fHasFont">Indicates whether font entry has been written.</param>
		/// <param name="fHasFontSize">Indicates whether font size entry has been written.</param>
		/// <param name="fHasName">Indicates whether name entry has been written.</param>
		/// <param name="fHasRTL">Indicates whether RTL entry has been written.</param>
		/// ------------------------------------------------------------------------------------
		private void WriteGeneralSection(FileWriter writer, ref bool fHasCodePage,
			ref bool fHasFont, ref bool fHasFontSize, ref bool fHasName, ref bool fHasRTL)
		{
			if (!fHasCodePage)
			{
				writer.WriteLine("codepage=65001");
				fHasCodePage = true;
			}
			if (!fHasFont)
			{
				writer.WriteLine("font=" + m_fontName);
				fHasFont = true;
			}
			if (!fHasFontSize)
			{
				writer.WriteLine("size=" + m_fontSize);
				fHasFontSize = true;
			}
			if (!fHasName)
			{
				writer.WriteLine("name=" + m_wsName);
				fHasName = true;
			}
			if (!fHasRTL)
			{
				writer.WriteLine("RTL=" + m_RtoL);
				fHasRTL = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// writes the contents of the LDS file.
		/// </summary>
		/// <param name="ldsFileName">name of the Paratext LDS to be saved.</param>
		/// <param name="normalStyle">The normal style.</param>
		/// <param name="ws">The HVO of the writing system for the current export.</param>
		/// <param name="fileWriterLds">file writer for the Paratext LDS file</param>
		/// ------------------------------------------------------------------------------------
		private void WriteLdsFileContents(string ldsFileName, UsfmStyEntry normalStyle, int ws,
			FileWriter fileWriterLds)
		{
			Debug.Assert(fileWriterLds != null);
			fileWriterLds.Open(ldsFileName);

			ComputeSettings(normalStyle, ws);

			fileWriterLds.WriteLine("[General]");
			fileWriterLds.WriteLine("codepage=65001");
			fileWriterLds.WriteLine("RTL=" + m_RtoL);
			fileWriterLds.WriteLine("font=" + m_fontName);
			fileWriterLds.WriteLine("name=" + m_wsName);
			fileWriterLds.WriteLine("size=" + m_fontSize);
			fileWriterLds.WriteLine(string.Empty, true);
			fileWriterLds.WriteLine("[Checking]");
			fileWriterLds.WriteLine(string.Empty, true);
			fileWriterLds.WriteLine("[Characters]");
			fileWriterLds.WriteLine(string.Empty, true);
			fileWriterLds.WriteLine("[Punctuation]");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes (creates or updates) a Paratext LDS file.
		/// </summary>
		/// <param name="ldsFileName">Name of the LDS file.</param>
		/// <param name="ws">The writing system which the LDS file describes.</param>
		/// <param name="normalStyle">The normal style.</param>
		/// ------------------------------------------------------------------------------------
		public void WriteParatextLdsFile(string ldsFileName, int ws, UsfmStyEntry normalStyle)
		{
			FileWriter fileWriterLDS = new FileWriter();
			try
			{
				WriteParatextLdsFile(ldsFileName, ws, normalStyle, fileWriterLDS);
			}
			finally
			{
				fileWriterLDS.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes (creates or updates) a Paratext LDS file.
		/// </summary>
		/// <param name="ldsFileName">Name of the LDS file.</param>
		/// <param name="ws">The writing system which the LDS file describes.</param>
		/// <param name="normalStyle">The normal style.</param>
		/// <param name="fileWriterLDS">The file writer used to write the LDS file.</param>
		/// ------------------------------------------------------------------------------------
		private void WriteParatextLdsFile(string ldsFileName, int ws, UsfmStyEntry normalStyle,
			FileWriter fileWriterLDS)
		{
			bool fUpdateSucceeded = false;
			// If the file describing the writing system exists, update values as needed.
			if (File.Exists(ldsFileName))
			{
				// Check to see if file is writable
				if ((File.GetAttributes(ldsFileName) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
					fUpdateSucceeded = UpdateLdsFile(ldsFileName, normalStyle, ws, fileWriterLDS);
				else
					return; // leave the existing read-only file unchanged
			}

			// If the lds file does not exist, or if there was a problem updating the lds file,
			// then a new lds file must be created.
			if (!fUpdateSucceeded)
			{
				// Create the LDS file in the settings directory
				WriteLdsFileContents(ldsFileName, normalStyle, ws, fileWriterLDS);
			}
		}
	}
}
