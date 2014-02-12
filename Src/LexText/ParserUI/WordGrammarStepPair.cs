namespace SIL.FieldWorks.LexText.Controls
{
	public class WordGrammarStepPair
	{
		protected string m_sXmlFile;
		protected string m_sHtmlFile;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="sXmlFile">Xml file</param>
		/// <param name="sHtmlFile">Html file</param>
		public WordGrammarStepPair(string sXmlFile, string sHtmlFile)
		{
			m_sXmlFile = sXmlFile;
			m_sHtmlFile = sHtmlFile;
		}
		/// <summary>
		/// Gete/set XmlFile
		/// </summary>
		public string XmlFile
		{
			get
			{
				return m_sXmlFile;
			}
			set
			{
				m_sXmlFile = value;
			}
		}
		/// <summary>
		/// Gete/set HtmlFile
		/// </summary>
		public string HtmlFile
		{
			get
			{
				return m_sHtmlFile;
			}
			set
			{
				m_sHtmlFile = value;
			}
		}

	}
}
