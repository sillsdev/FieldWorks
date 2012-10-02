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
// File: SelInfoTests.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.Framework.SelInfo
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the compare methods of SelectionHelper.SelInfo
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class SelInfo_Compare : BaseTest
	{
		private SelectionHelper.SelInfo s1;
		private SelectionHelper.SelInfo s2;

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initalize the selection helper objects
		/// </summary>
		/// --------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			CheckDisposed();

			s1 = new SelectionHelper.SelInfo();
			s1.rgvsli = new SelLevInfo[2];
			s2 = new SelectionHelper.SelInfo();
			s2.rgvsli = new SelLevInfo[2];
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
			s1 = null;
			s2 = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Having different number of levels should throw an exception.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void NumberOfLevels()
		{
			CheckDisposed();

			Assert.IsFalse((s1 < s2));

			s2.rgvsli = new SelLevInfo[3];
			Assert.IsFalse((s1 < s2)); // exception
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Test different parent (top level) objects. If the tags are different we expect
		/// an exception.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void TopLevelParentObjects()
		{
			CheckDisposed();

			s1.rgvsli[1].ihvo = 1;
			s2.rgvsli[1].ihvo = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.rgvsli[1].ihvo = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.rgvsli[1].cpropPrevious = 1;
			s2.rgvsli[1].cpropPrevious = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.rgvsli[1].cpropPrevious = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.rgvsli[1].tag = 1;
			s2.rgvsli[1].tag = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s2.rgvsli[1].tag = 2;
			Assert.IsFalse((s1 < s2));	// exception
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Test different immediate parent objects. If the tags are different we expect
		/// an exception.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void ImmediateParentObjects()
		{
			CheckDisposed();

			// make the top level parent the same
			s1.rgvsli[1].ihvo = s2.rgvsli[1].ihvo = 1;
			s1.rgvsli[1].cpropPrevious = s2.rgvsli[1].cpropPrevious = 1;
			s1.rgvsli[1].tag = s2.rgvsli[1].tag = 1;

			s1.rgvsli[0].ihvo = 1;
			s2.rgvsli[0].ihvo = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.rgvsli[0].ihvo = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.rgvsli[0].cpropPrevious = 1;
			s2.rgvsli[0].cpropPrevious = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.rgvsli[0].cpropPrevious = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.rgvsli[0].tag = 1;
			s2.rgvsli[0].tag = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s2.rgvsli[0].tag = 2;
			Assert.IsFalse((s1 < s2));	// exception
		}


		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Test differing objects.
		/// </summary>
		/// --------------------------------------------------------------------------------
		[Test]
		public void Leafs()
		{
			CheckDisposed();

			// make the parents the same
			s1.rgvsli[1].ihvo = s2.rgvsli[1].ihvo = 1;
			s1.rgvsli[1].cpropPrevious = s2.rgvsli[1].cpropPrevious = 1;
			s1.rgvsli[1].tag = s2.rgvsli[1].tag = 1;

			s1.rgvsli[0].ihvo = s2.rgvsli[0].ihvo = 1;
			s1.rgvsli[0].cpropPrevious = s2.rgvsli[0].cpropPrevious = 1;
			s1.rgvsli[0].tag = s2.rgvsli[0].tag = 1;

			s1.ihvoRoot = 1;
			s2.ihvoRoot = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.ihvoRoot = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.cpropPrevious = 1;
			s2.cpropPrevious = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.cpropPrevious = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.ich = 1;
			s2.ich = 2;
			Assert.IsTrue(s1 < s2);
			Assert.IsTrue(s2 > s1);

			s2.ich = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			// we don't care about the rest of the properties, so we should always get false
			s1.fAssocPrev = true;
			s2.fAssocPrev = true;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s2.fAssocPrev = false;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.ws = 0;
			s2.ws = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));

			s1.ihvoEnd = 0;
			s2.ihvoEnd = 1;
			Assert.IsFalse((s1 < s2));
			Assert.IsFalse((s2 < s1));
			Assert.IsFalse((s1 > s2));
			Assert.IsFalse((s2 > s1));
		}
	}
}
