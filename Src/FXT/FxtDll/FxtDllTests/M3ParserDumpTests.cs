// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: M3ParserDumpTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;

using NUnit.Framework;

using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.FXT
{
#if WANTTESTPORT //(FLEx) Need to port these tests to the new FDO
	/// <summary>
	/// Summary description for M3ParserDumpTests.
	/// </summary>
		[TestFixture]
		[Category("ByHand")]
		public class M3ParserDumpTests : FxtTestBase
		{
			/// <summary>
			/// Location of M3 Parser FXT file
			/// </summary>
			protected string m_sFxtParserPath = Path.Combine(DirectoryFinder.FlexFolder,
																		  Path.Combine("Configuration",
																		  Path.Combine("Grammar", "FXTs")))),
															 "M3Parser.fxt");

			/// -----------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="SimpleTests"/> class.
			/// </summary>
			/// -----------------------------------------------------------------------------------
			public M3ParserDumpTests():base()
			{
			}
			[Test]
			public void TLPParser()
			{
				m_filters = new IFilterStrategy[]{new ConstraintFilterStrategy()};
				string sAnswerFile = Path.Combine(m_sExpectedResultsPath, "TLPParser.xml");
				DoDump("TestLangProj", "TLP M3Parser dump", m_sFxtParserPath, sAnswerFile, true, true);
			}
		}
#endif
}
