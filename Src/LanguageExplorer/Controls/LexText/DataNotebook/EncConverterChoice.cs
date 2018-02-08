// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.LCModel.Core.WritingSystems;
using SIL.Xml;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	public class EncConverterChoice
	{
		private string m_sConverter;
		private readonly CoreWritingSystemDefinition m_ws;

		/// <summary>
		/// Constructor using an XmlNode from the settings file.
		/// </summary>
		public EncConverterChoice(XmlNode xnConverter, WritingSystemManager wsManager)
			: this(XmlUtils.GetMandatoryAttributeValue(xnConverter, "ws"), XmlUtils.GetOptionalAttributeValue(xnConverter, "converter", null), wsManager)
		{
		}

		/// <summary>
		/// Constructor using the writing system identifier and Converter name explicitly.
		/// </summary>
		public EncConverterChoice(string sWs, string sConverter, WritingSystemManager wsManager)
		{
			m_sConverter = sConverter;
			if (string.IsNullOrEmpty(m_sConverter))
			{
				m_sConverter = Sfm2Xml.STATICS.AlreadyInUnicode;
			}
			wsManager.GetOrSet(sWs, out m_ws);
		}

		/// <summary>
		/// Get the identifier for the writing system.
		/// </summary>
		public CoreWritingSystemDefinition WritingSystem => m_ws;

		/// <summary>
		/// Get the encoding converter name for the writing system.
		/// </summary>
		public string ConverterName
		{
			get { return m_sConverter; }
			set
			{
				m_sConverter = value;
				if (string.IsNullOrEmpty(m_sConverter))
				{
					m_sConverter = Sfm2Xml.STATICS.AlreadyInUnicode;
				}
				Converter = null;
			}
		}

		/// <summary>
		/// Get/set the actual encoding converter for the writing system (may be null).
		/// </summary>
		public ECInterfaces.IEncConverter Converter { get; set; }

		/// <summary>
		/// Get the name of the writing system.
		/// </summary>
		public string Name => m_ws.DisplayLabel;
	}
}