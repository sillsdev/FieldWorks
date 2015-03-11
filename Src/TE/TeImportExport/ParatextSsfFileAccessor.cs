// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ParatextSsfFileAccessor.cs
// Responsibility: TE Team

using System.IO;
using System.Text;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	#region class ParatextSsfFileAccessor
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ParatextSsfFileAccessor reads and writes a Paratext-style ssf file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ParatextSsfFileAccessor
	{
		/// <summary>Constant for Unicode encoding in Paratext</summary>
		public const string kUnicodeEncoding = "65001";

		#region Member Data
		private readonly FdoCache m_cache;
		private readonly FilteredScrBooks m_filter;
		private string m_wsName;
		private string m_fileScheme;
		private string m_sPostPart;
		private string m_sPrePart;
		private string m_LtoR;
		private string m_projPath;
		private string m_versification;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParatextSsfFileAccessor"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filter">book filter indicating which books are being exported</param>
		/// ------------------------------------------------------------------------------------
		public ParatextSsfFileAccessor(FdoCache cache, FilteredScrBooks filter)
		{
			m_cache = cache;
			m_filter = filter;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Computes the settings.
		/// </summary>
		/// <param name="format">The format.</param>
		/// <param name="projPath">The proj path.</param>
		/// <param name="ws">The ws.</param>
		/// ------------------------------------------------------------------------------------
		private void ComputeSettings(FileNameFormat format, string projPath, int ws)
		{
			m_projPath = projPath.Trim();
			CoreWritingSystemDefinition wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
			m_wsName = wsObj.DisplayLabel;
			m_fileScheme = format.ParatextFileScheme;
			m_sPostPart = format.m_fileSuffix + "." + format.m_fileExtension;
			m_sPrePart = format.m_filePrefix;
			m_LtoR = (wsObj.RightToLeftScript ? "F" : "T");
			m_versification =
				((int)m_cache.LangProject.TranslatedScriptureOA.Versification).ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the Paratext ssf file.
		/// </summary>
		/// <param name="ssfFileName">Name of the SSF file.</param>
		/// <param name="format">the prefix, scheme, suffix, extension</param>
		/// <param name="sShortName">The short name of the Paratext project</param>
		/// <param name="styleSheetFile">The style sheet file.</param>
		/// <param name="projPath">The path where the project is located</param>
		/// <param name="ws">The HVO of the writing system for the current export.</param>
		/// <returns>Name of the Language whose LDS file should be updated</returns>
		/// ------------------------------------------------------------------------------------
		public string UpdateSsfFile(string ssfFileName, FileNameFormat format, string sShortName,
			string styleSheetFile, string projPath, int ws)
		{
			XmlDocument ssfContents;
			using (StreamReader sr = new StreamReader(ssfFileName))
			{
				ssfContents = UpdateSsfFile(sr, format, sShortName, styleSheetFile, projPath, ws);
			}

			// Set settings so that XmlWriter produces the XML file in the pseudo-XML format
			// that Paratext expects (no whitespace except line breaks, no XML declaration)
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.CheckCharacters = false;
			settings.Encoding = new UTF8Encoding(false);
			settings.Indent = true;
			settings.IndentChars = string.Empty;
			settings.OmitXmlDeclaration = true;

			using (XmlWriter writer = XmlWriter.Create(ssfFileName, settings))
			{
				ssfContents.Save(writer);
			}
			return ssfContents.SelectSingleNode("ScriptureText/Language").InnerText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates new contents for an existing ssf file.
		/// </summary>
		/// <param name="ssf">Stream for Paratext SSF that already exists.</param>
		/// <param name="format">the prefix, scheme, suffix, extension</param>
		/// <param name="sShortName">The short name of the Paratext project</param>
		/// <param name="styleSheetFile">The style sheet file.</param>
		/// <param name="projPath">The path where the project is located</param>
		/// <param name="ws">The HVO of the writing system for the current export.</param>
		/// <returns>An XML document containing the settings for the Paratext project</returns>
		/// ------------------------------------------------------------------------------------
		public XmlDocument UpdateSsfFile(TextReader ssf, FileNameFormat format,
			string sShortName, string styleSheetFile, string projPath, int ws)
		{
			ComputeSettings(format, projPath, ws);

			XmlDocument project = new XmlDocument();
			project.InnerXml = ssf.ReadToEnd();

			XmlNode scriptureTextNode = project.SelectSingleNode("ScriptureText");

			XmlNode styleSheetNode = scriptureTextNode.SelectSingleNode("StyleSheet");
			if (styleSheetNode == null)
				scriptureTextNode.InnerXml += "<StyleSheet>" + styleSheetFile + "</StyleSheet>";

			XmlNode booksPresent, directory, encoding, fileNameForm, fileNamePostPart,
				fileNamePrePart, namingNode;

			bool fRequiredElementsMissing = false;
			fRequiredElementsMissing |= !CheckNode(scriptureTextNode, "BooksPresent", out booksPresent);
			fRequiredElementsMissing |= !CheckNode(scriptureTextNode, "Directory", out directory);
			fRequiredElementsMissing |= !CheckNode(scriptureTextNode, "Encoding", out encoding);
			fRequiredElementsMissing |= !CheckNode(scriptureTextNode, "FileNameForm", out fileNameForm);
			fRequiredElementsMissing |= !CheckNode(scriptureTextNode, "FileNamePostPart", out fileNamePostPart);
			fRequiredElementsMissing |= !CheckNode(scriptureTextNode, "FileNamePrePart", out fileNamePrePart);
			namingNode = scriptureTextNode.SelectSingleNode("Naming");

			if (namingNode != null && namingNode.OuterXml != NamingNode)
			{
				scriptureTextNode.RemoveChild(namingNode);
				namingNode = null;
			}

			if (namingNode == null)
			{
				fRequiredElementsMissing = true;
				scriptureTextNode.InnerXml += NamingNode;
			}

			// If the project is the same...
			if (!fRequiredElementsMissing &&
				directory.InnerText == m_projPath &&
				encoding.InnerText == kUnicodeEncoding &&
				fileNameForm.InnerText == m_fileScheme &&
				fileNamePostPart.InnerText == m_sPostPart &&
				fileNamePrePart.InnerText == m_sPrePart)
			{
				// Need to add the books being exported to the existing list of books in the project.
				booksPresent.InnerText = MergeBooksPresent(booksPresent.InnerText);
			}
			else
			{
				// If any required elements were missing, we need to re-get all the nodes since the
				// ones we got before refer to the old XML.
				if (fRequiredElementsMissing)
				{
					booksPresent = scriptureTextNode.SelectSingleNode("BooksPresent");
					directory = scriptureTextNode.SelectSingleNode("Directory");
					encoding = scriptureTextNode.SelectSingleNode("Encoding");
					fileNameForm = scriptureTextNode.SelectSingleNode("FileNameForm");
					fileNamePostPart = scriptureTextNode.SelectSingleNode("FileNamePostPart");
					fileNamePrePart = scriptureTextNode.SelectSingleNode("FileNamePrePart");
					namingNode = scriptureTextNode.SelectSingleNode("Naming");
				}

				// set up the default values.
				booksPresent.InnerText = BooksPresent;
				directory.InnerText = m_projPath;
				encoding.InnerText = kUnicodeEncoding;
				fileNameForm.InnerText = m_fileScheme;
				fileNamePostPart.InnerText = m_sPostPart;
				fileNamePrePart.InnerText = m_sPrePart;
				namingNode.Attributes.GetNamedItem("PostPart").Value = m_sPostPart;
				namingNode.Attributes.GetNamedItem("BookNameForm").Value = m_fileScheme;
			}

			XmlNode languageNode = scriptureTextNode.SelectSingleNode("Language");
			if (languageNode == null)
				scriptureTextNode.InnerXml += "<Language>" + m_wsName + "</Language>";
			else
				languageNode.InnerText = m_wsName;

			XmlNode leftToRight = scriptureTextNode.SelectSingleNode("LeftToRight");
			if (leftToRight != null)
				leftToRight.InnerText = m_LtoR;

			// Technically, the Versification node isn't required. We only want to write
			// the versification if it doesn't already exist in the project file.
			XmlNode versification = scriptureTextNode.SelectSingleNode("Versification");
			if (versification == null)
			{
				scriptureTextNode.InnerXml += "<Versification>" + m_versification +
					"</Versification>";
			}

			return project;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks for the given node. Adds it to the inner XML of the ScriptureText node if it
		/// is missing (value will be set later)
		/// </summary>
		/// <param name="scriptureTextNode">The ScriptureText node.</param>
		/// <param name="nodeName">Name of the node to look for.</param>
		/// <param name="node">The node if found (null if we had to add it).</param>
		/// <returns><c>true</c> if the requested node was found; <c>false</c> if we had to
		/// create it.</returns>
		/// ------------------------------------------------------------------------------------
		private bool CheckNode(XmlNode scriptureTextNode, string nodeName, out XmlNode node)
		{
			node = scriptureTextNode.SelectSingleNode(nodeName);
			if (node == null)
			{
				scriptureTextNode.InnerXml += "<" + nodeName + "></" + nodeName + ">";
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Merges the books present.
		/// </summary>
		/// <param name="existingList">The existing list of books present (a sequence of 1s and
		/// 0s whose position in the list correspond to the books in the canon, followed by
		/// a sequence corresponding to the deuterocanon).</param>
		/// <returns>A merged list that includes all the existing books plus those present in
		/// the filter.</returns>
		/// ------------------------------------------------------------------------------------
		public string MergeBooksPresent(string existingList)
		{
			char[] booksPresent = BooksPresent.ToCharArray();
			for (int i = 0; i < booksPresent.Length; i++)
			{
				if (existingList[i] == '1')
					booksPresent[i] = '1';
			}
			return new string(booksPresent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of books present (a sequence of 1s and 0s whose position in the list\
		/// correspond to the books in the canon, followed by a sequence corresponding to the
		/// deuterocanon).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BooksPresent
		{
			get
			{
				StringBuilder bldr = new StringBuilder(99);
				bldr.Insert(0, "0", 99);
				foreach (int bookId in m_filter.BookIds)
					bldr[bookId - 1] = '1';
				return bldr.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Naming tag with all the attributes set correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string NamingNode
		{
			get
			{
				return "<Naming " +
				((m_sPrePart != string.Empty) ? "PrePart=\"" + m_sPrePart + "\" " : string.Empty) +
				"PostPart=\"" + m_sPostPart + "\" BookNameForm=\"" + m_fileScheme + "\"></Naming>";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Naming tag with all the attributes set correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string NamingNodeInnerXml
		{
			get
			{
				return "<Naming " +
				((m_sPrePart != string.Empty) ? "PrePart=\"" + m_sPrePart + "\" " : string.Empty) +
				"PostPart=\"" + m_sPostPart + "\" BookNameForm=\"" + m_fileScheme + "\"></Naming>";
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the ssf file.
		/// </summary>
		/// <param name="format">the prefix, scheme, suffix, extension</param>
		/// <param name="sShortName">The short name of the Paratext project</param>
		/// <param name="styleSheetFile">The style sheet file.</param>
		/// <param name="projPath">The path where the project is located</param>
		/// <param name="writer">The file writer.</param>
		/// <param name="ws">The HVO of the writing system for the current export.</param>
		/// ------------------------------------------------------------------------------------
		public void SaveSsfFile(FileNameFormat format, string sShortName, string styleSheetFile,
			string projPath, FileWriter writer, int ws)
		{
			ComputeSettings(format, projPath, ws);
			writer.WriteLine("<ScriptureText>");
			writer.WriteLine("<BooksPresent>" + BooksPresent + "</BooksPresent>");
			writer.WriteLine("<Copyright></Copyright>");
			writer.WriteLine("<Directory>" + m_projPath + "</Directory>");
			writer.WriteLine("<Editable>T</Editable>");
			writer.WriteLine("<Encoding>" + kUnicodeEncoding + "</Encoding>");
			writer.WriteLine("<FileNameForm>" + m_fileScheme + "</FileNameForm>");
			writer.WriteLine("<FileNamePostPart>" + m_sPostPart + "</FileNamePostPart>");
			writer.WriteLine("<FileNamePrePart>" + m_sPrePart + "</FileNamePrePart>");
			writer.WriteLine("<FullName>" + m_cache.ProjectId.Name + "</FullName>");
			writer.WriteLine("<Language>" + m_wsName + "</Language>");
			writer.WriteLine("<LeftToRight>" + m_LtoR + "</LeftToRight>");
			writer.WriteLine("<Name>" + sShortName + "</Name>");
			writer.WriteLine("<StyleSheet>" + styleSheetFile + "</StyleSheet>");
			writer.WriteLine("<Versification>" + m_versification + "</Versification>");
			writer.WriteLine(NamingNode);
			writer.WriteLine("</ScriptureText>");
		}
	}
	#endregion
}
