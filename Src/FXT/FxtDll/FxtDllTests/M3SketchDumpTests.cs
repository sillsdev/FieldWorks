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
// File: M3SketchDumpTests.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Utils;
using System.Diagnostics;
using NUnit.Framework;
using System.IO;

namespace SIL.FieldWorks.Common.FXT
{
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
		protected string m_sFxtSketchGenPath = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.GetFWCodeSubDirectory(@"Language Explorer\Configuration\Grammar\FXTs"),
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
			DoDump ("TestLangProj", "TLP M3Sketch dump", m_sFxtSketchGenPath, sAnswerFile, true, true);
		}
	}
}
