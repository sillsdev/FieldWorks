//#define DumpCleanedMode
// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: M3ParserTransformer.cs
// Responsibility: John Hatton

using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Given an XML file representing an instance of a M3 grammar model,
	/// transforms it into the format needed by a parser.
	/// </summary>
	internal class M3ToHCTransformer : M3ToParserTransformerBase
	{
		private XslCompiledTransform m_inputTransform;
		private readonly string m_database;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToHCTransformer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToHCTransformer(string database, string dataDir)
			: base(dataDir)
		{
			m_database = database;
		}

		private XslCompiledTransform InputTransform
		{
			get
			{
				if (m_inputTransform == null)
					m_inputTransform = CreateTransform("FxtM3ParserToHCInput.xsl");
				return m_inputTransform;
			}
		}

		public void MakeHCFiles(XDocument model)
		{
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "HCInput.xml")))
				InputTransform.Transform(model.CreateNavigator(), null, writer);

			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "gram.txt")))
				GrammarTransform.Transform(model.CreateNavigator(), null, writer);

			// TODO: Putting this here is not necessarily efficient because it happens every time
			//       the parser is run.  It would be more efficient to run this only when the user
			//       is trying a word.  But we need the "model" to apply this transform and it is
			//       available here, so we're doing this for now.
			using (var writer = new StreamWriter(Path.Combine(Path.GetTempPath(), m_database + "XAmpleWordGrammarDebugger.xsl")))
				GrammarDebuggingTransform.Transform(model.CreateNavigator(), null, writer);
		}
	}
}
