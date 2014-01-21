using System.IO;
using System.Xml;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Base class for transforming an M3 model to files needed by a parser
	/// </summary>
	abstract internal class M3ToParserTransformerBase
	{
		protected string m_outputDirectory;
		protected string m_database;
		protected readonly string m_appInstallDir;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToParserTransformerBase"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToParserTransformerBase(string database, string appInstallDir)
		{
			m_database = database;
			m_appInstallDir = appInstallDir;
			m_outputDirectory = Path.GetTempPath();
		}

		protected void TransformDomToFile(string transformName, XmlDocument inputDom, string outputName)
		{
			XmlUtils.TransformDomToFile(Path.Combine(m_appInstallDir + "/Language Explorer/Transforms/", transformName),
			inputDom, Path.Combine(m_outputDirectory, outputName));
		}
	}
}
