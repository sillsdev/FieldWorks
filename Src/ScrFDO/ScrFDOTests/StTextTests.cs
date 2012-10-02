// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StTextTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using NUnit.Framework;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for StText class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StTextTests : ScrInMemoryFdoTestBase
	{
		private IScrSection m_section;

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
			m_section = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);

			m_scrInMemoryCache.AddSectionHeadParaToSection(m_section.Hvo, "Heading",
				ScrStyleNames.SectionHead);
			m_scrInMemoryCache.AddParaToMockedSectionContent(m_section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddParaToMockedSectionContent(m_section.Hvo, ScrStyleNames.NormalParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test moving all paragraphs from one StText to another.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveAllParagraphs()
		{
			CheckDisposed();

			int cHeadingParas = m_section.HeadingOA.ParagraphsOS.Count;
			int cContentParas = m_section.ContentOA.ParagraphsOS.Count;

			StText.MoveTextContents(m_section.HeadingOA, m_section.ContentOA, false);

			Assert.AreEqual(cContentParas + cHeadingParas, m_section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(0, m_section.HeadingOA.ParagraphsOS.Count);
		}
	}
}
