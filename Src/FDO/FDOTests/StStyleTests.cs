// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StStyleTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the StStyle class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class StStyleTests: InMemoryFdoTestBase
	{
		private StStyle m_style;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_style = new StStyle();
			Cache.LangProject.StylesOC.Add(m_style);
			m_style.Name = "Section Head Major";
			m_style.UserLevel = -1;
		}

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
			m_style = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test method to set the m_style to be in use.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetInUseTest_InUse()
		{
			CheckDisposed();

			m_style.InUse = true;
			Assert.IsTrue(m_style.InUse);
			Assert.IsTrue(m_style.UserLevel < 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test method to set the m_style to be in use.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetInUseTest_NotInUse()
		{
			CheckDisposed();

			m_style.InUse = false;
			Assert.IsFalse(m_style.InUse);
			Assert.IsTrue(m_style.UserLevel > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Asserts the properties of a run are set correctly to be a hyperlink for the given
		/// URL.
		/// </summary>
		/// <param name="props">The properties.</param>
		/// <param name="expectedWs">The expected writing system.</param>
		/// <param name="sUrl">The URL.</param>
		/// ------------------------------------------------------------------------------------
		public static void AssertHyperlinkPropsAreCorrect(ITsTextProps props, int expectedWs,
			string sUrl)
		{
			Assert.AreEqual(1, props.IntPropCount);
			int nDummy;
			Assert.AreEqual(expectedWs,
				props.GetIntPropValues((int)FwTextPropType.ktptWs, out nDummy));
			Assert.AreEqual(2, props.StrPropCount);
			Assert.AreEqual(StStyle.Hyperlink,
				props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			string sObjData = props.GetStrPropValue((int)FwTextPropType.ktptObjData);
			Assert.AreEqual(Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName), sObjData[0]);
			Assert.AreEqual(sUrl, sObjData.Substring(1));
		}
	}
}
