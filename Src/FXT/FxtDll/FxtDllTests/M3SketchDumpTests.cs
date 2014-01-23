// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: M3SketchDumpTests.cs
// Responsibility: Andy Black

using System;
using System.Diagnostics;
using System.IO;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FXT
{
#if WANTTESTPORT //(FLEx) Need to port these tests to the new FDO
	/// <summary>
	/// Summary description for M3SketchDumpTests.
	/// </summary>
	[TestFixture]
	[Category("ByHand")]
	public class M3SketchDumpTests : FxtTestBase
	{
		/// <summary>
		/// Location of M3 Sketch Gen FXT file
		/// </summary>
		protected string m_sFxtSketchGenPath = Path.Combine(DirectoryFinder.FlexFolder,
																		 Path.Combine("Configuration",
																		 Path.Combine("Grammar", "FXTs")))),
															"M3SketchGen.fxt");
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleTests"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3SketchDumpTests() : base()
		{
		}
		[Test]
		public void TLPSketch()
		{
			string sAnswerFile = Path.Combine(m_sExpectedResultsPath, "TLPSketchGen.xml");
			DoDump (SIL.FieldWorks.Common.Utils.BasicUtils.GetTestLangProjDataBaseName(), "TLP M3Sketch dump", m_sFxtSketchGenPath, sAnswerFile, true, true);
		}
	}
#endif
}
