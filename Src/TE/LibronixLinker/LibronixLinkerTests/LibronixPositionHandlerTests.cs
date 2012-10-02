// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LibronixPositionHandlerTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE.LibronixLinker
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests methods of the LibronixLinker class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LibronixPositionHandlerTests: BaseTest
	{
		private LibronixPositionHandler m_LibronixPositionHandler;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_LibronixPositionHandler = new LibronixPositionHandler();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			m_LibronixPositionHandler.Dispose();
			base.Dispose(disposing);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Invalid references passed to ConvertToBcv return -1
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ConvertToBcv_Null()
		{
			Assert.AreEqual(-1, m_LibronixPositionHandler.ConvertToBcv(null));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Invalid references passed to ConvertToBcv return -1
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ConvertToBcv_InvalidFormat()
		{
			Assert.AreEqual(-1, m_LibronixPositionHandler.ConvertToBcv("1.1.1"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Invalid references passed to ConvertToBcv return -1
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ConvertToBcv_Apcrypha()
		{
			Assert.AreEqual(-1, m_LibronixPositionHandler.ConvertToBcv("bible.40.1.1"));
			Assert.AreEqual(-1, m_LibronixPositionHandler.ConvertToBcv("bible.88.1.1"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Valid reference should return a BCV reference
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ConvertToBcv_OT()
		{
			Assert.AreEqual( 1001001, m_LibronixPositionHandler.ConvertToBcv("bible.1.1.1"));
			Assert.AreEqual(39001001, m_LibronixPositionHandler.ConvertToBcv("bible.39.1.1"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Valid reference should return a BCV reference
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ConvertToBcv_NT()
		{
			Assert.AreEqual(40001001, m_LibronixPositionHandler.ConvertToBcv("bible.61.1.1"));
			Assert.AreEqual(66001001, m_LibronixPositionHandler.ConvertToBcv("bible.87.1.1"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Valid BCV reference returns the corresponding Libronix reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertFromBcv_OT()
		{
			Assert.AreEqual("bible.1.1.1",  m_LibronixPositionHandler.ConvertFromBcv( 1001001));
			Assert.AreEqual("bible.39.1.1", m_LibronixPositionHandler.ConvertFromBcv(39001001));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Valid BCV reference returns the corresponding Libronix reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConvertFromBcv_NT()
		{
			Assert.AreEqual("bible.61.1.1", m_LibronixPositionHandler.ConvertFromBcv(40001001));
			Assert.AreEqual("bible.87.1.1", m_LibronixPositionHandler.ConvertFromBcv(66001001));
		}
	}
}
