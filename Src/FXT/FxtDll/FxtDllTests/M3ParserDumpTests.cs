// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: M3ParserDumpTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

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
