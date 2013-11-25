// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SimpleTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.IO;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.FXT
{
#if WANTTESTPORT //(FLEx) Need to port these tests to the new FDO
	/// <summary>
	/// Summary description for SimpleTests.
	/// </summary>
	[TestFixture]
	[Category("ByHand")]
	public class QuickTests : FxtTestBase
	{
		/// <summary>
		/// Location of simple test FXT files
		/// </summary>
		protected string m_sFxtSimpleTestPath = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FwSourceDirectory,
															Path.Combine("FXT",
															Path.Combine("FxtDll", "FxtDllTests")));
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleTests"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public QuickTests()
		{
		}
		[Test]
		[Category("ByHand")]
		public void SimpleXml()
		{
			string sFxtPath = Path.Combine(m_sFxtSimpleTestPath, "SimpleGuids.fxt");
			string sAnswerFile = Path.Combine(m_sExpectedResultsPath, "TLPSimpleGuidsAnswer.xml");
			DoDump ("TestLangProj", "Simple with Guids", sFxtPath, sAnswerFile, true, true);
		}
		[Test]
		public void SimpleWebPage()
		{
			string sFxtPath = Path.Combine(m_sFxtSimpleTestPath, "WebPageSample.xhtml");
			string sAnswerFile = Path.Combine(m_sExpectedResultsPath, "TLPWebPageSampleAnswer.xhtml");
			DoDump ("TestLangProj", "WebPageSample", sFxtPath, sAnswerFile,false,true);
		}
	}
#endif
}
