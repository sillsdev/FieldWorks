// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LanguageDefinitionFactory.cs
// Responsibility:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	#region ILanguageDefinitionFactory interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Reads/Writes a LanguageDefinition from an XML file
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[Guid("A1F607AC-4621-4130-BD09-54D5A8C2768E")]
	public interface ILanguageDefinitionFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new Language Definition based on the information retrieved from the
		/// writing system and from ICU.
		/// </summary>
		/// <param name="ws">Writing system</param>
		/// ------------------------------------------------------------------------------------
		ILanguageDefinition Initialize(IWritingSystem ws);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes an existing Language Definition from an XML file. The file name
		/// corresponds to the ICU locale.
		/// </summary>
		/// <param name="wsf">Writing System factory</param>
		/// <param name="icuLocale">ICU locale</param>
		/// ------------------------------------------------------------------------------------
		ILanguageDefinition InitializeFromXml(ILgWritingSystemFactory wsf, string icuLocale);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the language definition
		/// </summary>
		/// ------------------------------------------------------------------------------------
		ILanguageDefinition LanguageDefinition
		{
			get;
			set;
		}
	}
	#endregion // ILanguageDefinitionFactory interface

	#region LanguageDefinitionFactory class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class stores all the information about a writing system from the data base and
	/// from ICU.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ProgId("FwCoreDlgs.LanguageDefinitionFactory")]
	// Key attribute to hide the "clutter" from System.Windows.Forms.Form
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("CBCD3100-6A78-4474-AD17-90CCAF9B1AA7")]
	[ComVisible(true)]
	public class LanguageDefinitionFactory: ILanguageDefinitionFactory
	{
		private ILanguageDefinition m_LanguageDefinition;
		private static ILgWritingSystemFactory m_wsf;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="LanguageDefinitionFactory"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LanguageDefinitionFactory()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and initializes a new instance of the LanguageDefinitionFactory class. The language
		/// 		/// definition is retrieved from ICU.
		/// </summary>
		/// <param name="ws">The writing system </param>
		/// ------------------------------------------------------------------------------------
		public LanguageDefinitionFactory(IWritingSystem ws): this()
		{
			Initialize(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the LanguageDefinitionFactory class. The language definition
		/// is deserialized from the XML file that corresponds to the given IcuCode.
		/// </summary>
		/// <param name="wsf">Writing system factory</param>
		/// <param name="icuCode">ICU code</param>
		/// ------------------------------------------------------------------------------------
		public LanguageDefinitionFactory(ILgWritingSystemFactory wsf, string icuCode)
		{
			InitializeFromXml(wsf, icuCode);
		}

		#region Serialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads a language definition from a file
		/// </summary>
		/// <param name="fileName">File name</param>
		/// <returns>Deserialized LanguageDefinition object</returns>
		/// ------------------------------------------------------------------------------------
		public void Deserialize(string fileName)
		{
			XmlReader reader = new XmlTextReader(fileName);
			Deserialize(reader);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="reader"></param>
		internal void Deserialize(XmlReader reader)
		{
			// This might fail if the working folder is not accessible. So, we will retry
			// once after setting the working folder to the temp folder.
			int retryCount = 0;
			while (true)
			{
				try
				{
					XmlSerializer xmlSerializer = new XmlSerializer(typeof(LanguageDefinition));

					m_LanguageDefinition = (LanguageDefinition)xmlSerializer.Deserialize(reader);
					if (m_wsf != null)
					{
						m_LanguageDefinition.WritingSystem.WritingSystemFactory = m_wsf;
						for (int i = 0; i < m_LanguageDefinition.CollationCount; i++)
						{
							m_LanguageDefinition.GetCollation(i).WritingSystemFactory = m_wsf;
						}
					}
					bool success = (m_LanguageDefinition as LanguageDefinition).LoadDefaultICUValues();
					break;
				}
				catch
				{
					if (retryCount++ >= 1)
						break;
					Directory.SetCurrentDirectory(Path.GetTempPath());
				}
			}
			reader.Close();
			// Capture the initial state of the new definition.
			if (m_LanguageDefinition != null)
				(m_LanguageDefinition as LanguageDefinition).FinishedInitializing();
		}
		#endregion



		#region ILanguageDefinitionFactory Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new Language Definition based on the information retrieved from the
		/// writing system and from ICU.
		/// </summary>
		/// <param name="ws">Writing system</param>
		/// ------------------------------------------------------------------------------------
		public ILanguageDefinition Initialize(IWritingSystem ws)
		{
			InitializeBasic(ws);
			(m_LanguageDefinition as LanguageDefinition).LoadDefaultICUValues();
			return m_LanguageDefinition;
		}

		private ILanguageDefinition InitializeBasic(IWritingSystem ws)
		{
			m_wsf = ws.WritingSystemFactory;
			m_LanguageDefinition = new LanguageDefinition(ws);
			return m_LanguageDefinition;
		}

		/// <summary>
		/// Initializes a new Language Definition with a new WritingSystem.
		/// </summary>
		/// <param name="wsf"></param>
		/// <returns></returns>
		public ILanguageDefinition CreateNew(ILgWritingSystemFactory wsf)
		{
			IWritingSystem writingSystem = WritingSystemClass.Create();
			writingSystem.WritingSystemFactory = wsf;
			return InitializeBasic(writingSystem);
		}


		/// <summary>
		/// Create a New LanguageDefinition and inherit its general data from the current language definition.
		/// </summary>
		/// <param name="wsf"></param>
		/// <param name="langDef"></param>
		/// <returns></returns>
		public ILanguageDefinition CreateNewFrom(ILgWritingSystemFactory wsf, LanguageDefinition langDef)
		{
			LanguageDefinition newLangDef = CreateNew(wsf) as LanguageDefinition;
			newLangDef.LocaleName = langDef.LocaleName;
			newLangDef.SetEthnologueCode(langDef.EthnoCode, langDef.LocaleName);
			newLangDef.LocaleAbbr = langDef.LocaleAbbr;
			newLangDef.ValidChars = langDef.ValidChars;
			newLangDef.MatchedPairs = langDef.MatchedPairs;
			newLangDef.PunctuationPatterns = langDef.PunctuationPatterns;
			newLangDef.CapitalizationInfo = langDef.CapitalizationInfo;
			newLangDef.QuotationMarks = langDef.QuotationMarks;
			newLangDef.LoadDefaultICUValues();
			newLangDef.FinishedInitializing();
			return newLangDef;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes an existing Language Definition from an XML file. The file name
		/// corresponds to the ICU locale.
		/// </summary>
		/// <param name="wsf">Writing System factory</param>
		/// <param name="icuLocale">ICU locale</param>
		/// ------------------------------------------------------------------------------------
		public ILanguageDefinition InitializeFromXml(ILgWritingSystemFactory wsf,
			string icuLocale)
		{
			m_wsf = wsf;
			string dataDirectory = Path.Combine(DirectoryFinder.FWDataDirectory, "languages");
			string filename = Path.Combine(dataDirectory, Path.ChangeExtension(icuLocale,
				"xml"));

			if (!File.Exists(filename))
				throw new FileNotFoundException();

			Deserialize(filename);

			return m_LanguageDefinition;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the language definition
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILanguageDefinition LanguageDefinition
		{
			get { return m_LanguageDefinition; }
			set { m_LanguageDefinition = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the writing system factory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static ILgWritingSystemFactory WritingSystemFactory
		{
			get { return m_wsf; }
			set { m_wsf = value; }
		}

		#endregion
	}
	#endregion // LanguageDefinitionFactory class
}
