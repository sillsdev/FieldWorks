using System.Xml.Xsl;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Base class for transforming an M3 model to files needed by a parser
	/// </summary>
	internal abstract class M3ToParserTransformerBase
	{
		private readonly string m_dataDir;

		private XslCompiledTransform m_grammarTransform;
		private XslCompiledTransform m_grammarDebuggingTransform;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToParserTransformerBase"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected M3ToParserTransformerBase(string dataDir)
		{
			m_dataDir = dataDir;
		}

		protected XslCompiledTransform GrammarTransform
		{
			get
			{
				if (m_grammarTransform == null)
					m_grammarTransform = CreateTransform("FxtM3ParserToToXAmpleGrammar");
				return m_grammarTransform;
			}
		}

		protected XslCompiledTransform GrammarDebuggingTransform
		{
			get
			{
				if (m_grammarDebuggingTransform == null)
					m_grammarDebuggingTransform = CreateTransform("FxtM3ParserToXAmpleWordGrammarDebuggingXSLT");
				return m_grammarDebuggingTransform;
			}
		}

		protected XslCompiledTransform CreateTransform(string xslName)
		{
			return XmlUtils.CreateTransform(xslName, "ApplicationTransforms");
		}
	}
}
