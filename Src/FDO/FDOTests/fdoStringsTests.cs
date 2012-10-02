// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: fdoStringsTests.cs
// Responsibility: TomB
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for fdoStringsTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoStringsTests : InMemoryFdoTestBase
	{
		#region data members
		private MultiUnicodeAccessor m_multi;
		#endregion

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_multi = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Create a MultiUnicodeAccessor
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.CacheAccessor.CacheVecProp(Cache.LangProject.Hvo,
				(int)LangProject.LangProjectTags.kflidCurAnalysisWss,
				new int[]{InMemoryFdoCache.s_wsHvos.De, InMemoryFdoCache.s_wsHvos.Es}, 2);
			Cache.LangProject.CacheDefaultWritingSystems();
			m_multi = m_inMemoryCache.CreateArbitraryMultiUnicodeAccessor();
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has an
		/// alternative stored for the default analysis WS.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_DefaultAnalExists()
		{
			CheckDisposed();

			m_multi.AnalysisDefaultWritingSystem = "Hallo";
			m_multi.SetAlternative("Hola", Cache.LangProject.CurAnalysisWssRS[1].Hvo);
			m_multi.SetAlternative("YeeHaw", Cache.LangProject.DefaultUserWritingSystem);
			m_multi.SetAlternative("Hello", InMemoryFdoCache.s_wsHvos.En);
			Assert.AreEqual("Hallo", m_multi.BestAnalysisAlternative.Text);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has an
		/// alternative stored for the default analysis WS.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_EnglishExists()
		{
			CheckDisposed();

			m_multi.SetAlternative("Hello", InMemoryFdoCache.s_wsHvos.En);
			Assert.AreEqual("Hello", m_multi.BestAnalysisAlternative.Text);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has no
		/// alternatives stored.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_NoAlternativesExist()
		{
			CheckDisposed();

			Assert.AreEqual("***", m_multi.BestAnalysisAlternative.Text);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has an
		/// alternative stored analysis WS's other than the default.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_OtherAnalExists()
		{
			CheckDisposed();

			m_multi.SetAlternative("Hola", Cache.LangProject.CurAnalysisWssRS[1].Hvo);
			m_multi.SetAlternative("YeeHaw", Cache.LangProject.DefaultUserWritingSystem);
			m_multi.SetAlternative("Hello", InMemoryFdoCache.s_wsHvos.En);
			Assert.AreEqual("Hola", m_multi.BestAnalysisAlternative.Text);
		}

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Test of the GetBestAnalysisAlternative method when the MultiUnicodeAccessor has an
		/// alternative stored for the UI writing system, but none of the analysis WS's.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		[Test]
		public void TestGetBestAnalysisAlternative_UIExists()
		{
			CheckDisposed();

			m_multi.SetAlternative("YeeHaw", Cache.LangProject.DefaultUserWritingSystem);
			m_multi.SetAlternative("Hello", InMemoryFdoCache.s_wsHvos.Fr);
			Assert.AreEqual("YeeHaw", m_multi.BestAnalysisAlternative.Text);
		}
	}
}
