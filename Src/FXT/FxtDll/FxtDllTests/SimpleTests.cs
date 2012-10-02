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
// File: SimpleTests.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using System.Diagnostics;

using NUnit.Framework;

namespace SIL.FieldWorks.Common.FXT
{
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
			@"fxt\fxtdll\fxtdlltests");
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
}
