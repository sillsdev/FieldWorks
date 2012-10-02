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
// File: UnitTest.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;

using NUnit.Framework;

namespace SIL.FieldWorks.Common.FXT
{
	/// <summary>
	/// Summary description for UnitTest.
	/// </summary>
	[TestFixture]
	public class UnitTest
	{
		FdoCache m_fdoCache;
		/// <summary>Time when <see cref="startClock"/> was called</summary>
		protected DateTime m_dtstart;
		/// <summary>Timespan between the call of <see cref="startClock"/> and
		/// <see cref="stopClock"/></summary>
		protected TimeSpan m_tsTimeSpan;

		private const string ksCmPossLangProj = "TestLangProj";


		/// <summary>
		/// start timing something.  Follow up with stopClock() and writeTimeSpan()
		/// </summary>
		protected void startClock()
		{
			m_dtstart = DateTime.Now;
		}
		/// <summary>
		/// stop timing something.  Follow up with writeTimeSpan ()
		/// </summary>
		protected void stopClock()
		{
			m_tsTimeSpan = new TimeSpan(DateTime.Now.Ticks - m_dtstart.Ticks);
		}
		/// <summary>
		/// write out a timespan in seconds and fractions of seconds to the debugger output window.
		/// </summary>
		/// <param name="sLabel"></param>
		protected void writeTimeSpan(string sLabel)
		{
			Console.WriteLine(sLabel + " " + m_tsTimeSpan.TotalSeconds.ToString() + " Seconds");
			Debug.WriteLine("");
			Debug.WriteLine("");
			Debug.WriteLine("");
			Debug.WriteLine(sLabel + " " + m_tsTimeSpan.TotalSeconds.ToString() + " Seconds");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UnitTest"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public UnitTest()
		{
		}

		[TestFixtureSetUp]
		public void FixtureInit()
		{
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("db", "ZPU");
			m_fdoCache = FdoCache.Create(cacheOptions);
		}

		[TestFixtureTearDown]
		public void FixtureCleanUp()
		{
			if (m_fdoCache != null)
			{
				m_fdoCache.Dispose();
				m_fdoCache = null;
			}
		}

		[Test]
		public void oneTwoThree()
		{
			DoDump ("1");
//			DoDump ("2");
/*			m_fdoCache.AssumeCacheFullyLoaded = true;
			DoDump ("3 no load");
			m_fdoCache.AssumeCacheFullyLoaded = false;
*/
		}

//		[Test]
//		public void oneTwo()
//		{
//			DoDump ("1");
//			m_fdoCache.AssumeCacheFullyLoaded = true;
//			DoDump ("2 no load");
//			m_fdoCache.AssumeCacheFullyLoaded = false;
//		}

		protected void DoDump (string s)
		{
			//string p = SIL.FieldWorks.Common.Utils.DirectoryFinder.FwSourceDirectory+@"\FXT\FxtDll\FxtDllTests\test3NoGloss.xml";
			string p = SIL.FieldWorks.Common.Utils.DirectoryFinder.GetFWCodeSubDirectory("WW") + "/M3Parser.fxt";
			XDumper dumper = new XDumper(m_fdoCache, p);
			CmObject lp= m_fdoCache.LangProject;
			startClock();
			dumper.Go(lp);
			stopClock();
			writeTimeSpan(s);
		}

	}
}
