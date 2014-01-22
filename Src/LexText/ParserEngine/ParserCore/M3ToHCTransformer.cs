//#define DumpCleanedMode
// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2003' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: M3ParserTransformer.cs
// Responsibility: John Hatton
// --------------------------------------------------------------------------------------------
using System.Xml;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Given an XML file representing an instance of a M3 grammar model,
	/// transforms it into the format needed by a parser.
	/// </summary>
	internal class M3ToHCTransformer : M3ToParserTransformerBase
	{

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ToHCTransformer"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ToHCTransformer(string database, string dataDir)
			: base(database, dataDir)
		{
		}

		public void MakeHCFiles(ref XmlDocument model)
		{
			TransformDomToFile("FxtM3ParserToHCInput.xsl", model, m_database + "HCInput.xml");

			TransformDomToFile("FxtM3ParserToToXAmpleGrammar.xsl", model, m_database + "gram.txt");

			// TODO: Putting this here is not necessarily efficient because it happens every time
			//       the parser is run.  It would be more efficient to run this only when the user
			//       is trying a word.  But we need the "model" to apply this transform and it is
			//       available here, so we're doing this for now.
			string sName = m_database + "XAmpleWordGrammarDebugger.xsl";
			TransformDomToFile("FxtM3ParserToXAmpleWordGrammarDebuggingXSLT.xsl", model, sName);
		}
	}
}
