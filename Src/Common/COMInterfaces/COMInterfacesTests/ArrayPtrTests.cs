// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ArrayPtrTests.cs
// Authorship History: MarkS
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Runtime.InteropServices;
using SIL.Utils;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// <summary>
	/// Tests class ArrayPtr
	/// </summary>
	[TestFixture]
	public class ArrayPtrTests
	{
		/// <summary/>
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			// Set stub for messagebox so that we don't pop up a message box when running tests.
			MessageBoxUtils.Manager.SetMessageBoxAdapter(new MessageBoxStub());
		}

		/// <summary></summary>
		[Test]
		public void Basic()
		{
			using (var array = new ArrayPtr())
			{
				Assert.IsNotNull(array);
			}
		}

		/// <summary></summary>
		[Test]
		public void DisposingArrayPtrOfNativeOwnedMemoryDoesNotFree()
		{
			var intptr = Marshal.AllocCoTaskMem(10);
			using (var array = new ArrayPtr(intptr))
			{
				Marshal.WriteByte(intptr, (byte)123);
			}

			byte b = Marshal.ReadByte(intptr);
			Assert.AreEqual((byte)123, b, "native-owned memory should not have been freed");
			Marshal.FreeCoTaskMem(intptr);
		}

		/// <remarks>Reading memory after it has been freed is not guaranteed to have
		/// consistent results.</remarks>
		[Test]
		[Ignore("By design this test doesn't produce consistent results")]
		public void DisposingArrayPtrOfOwnMemoryDoesFree_1()
		{
			IntPtr arrayIntPtr;
			using (var array = new ArrayPtr(10))
			{
				arrayIntPtr = array.IntPtr;
				Marshal.WriteByte(arrayIntPtr, (byte)123);
			}
			byte b = Marshal.ReadByte(arrayIntPtr);
			Assert.AreEqual((byte)0, b, "Owned memory should have been freed");
		}

		/// <remarks>Reading memory after it has been freed is not guaranteed to have
		/// consistent results.</remarks>
		[Test]
		[Ignore("By design this test doesn't produce consistent results")]
		public void DisposingArrayPtrOfOwnMemoryDoesFree_2()
		{
			IntPtr arrayIntPtr;
			using (var array = new ArrayPtr())
			{
				array.Resize(10);

				arrayIntPtr = array.IntPtr;
				Marshal.WriteByte(arrayIntPtr, (byte)123);
			}
			byte b = Marshal.ReadByte(arrayIntPtr);
			Assert.AreEqual((byte)0, b, "Owned memory should have been freed");
		}

		/// <summary></summary>
		[Test]
		[ExpectedException(typeof(ApplicationException))]
		public void CannotResizeIfExternalMemory()
		{
			var intptr = Marshal.AllocCoTaskMem(10);
			using (var array = new ArrayPtr(intptr))
			{
				array.Resize(12);
			}
		}

		/// <summary></summary>
		[Test]
		public void CanResizeIfOwnMemory()
		{
			int originalSize = 10;
			using (var array = new ArrayPtr(originalSize))
			{
				Assert.AreEqual(originalSize, array.Size);

				int newSize = 12;
				array.Resize(newSize);
				Assert.AreEqual(newSize, array.Size);
			}
		}

		/// <summary></summary>
		[Test]
		public void DoNotOwnExternalMemory()
		{
			var intptr = Marshal.AllocCoTaskMem(10);
			using (var array = new ArrayPtr(intptr))
			{
				try
				{
					Assert.AreEqual(false, array.OwnMemory, "An ArrayPtr with externally allocated memory does not own its memory");
				}
				finally
				{
					Marshal.FreeCoTaskMem(intptr);
				}
			}
		}

		/// <summary></summary>
		[Test]
		public void DoOwnMemory_1()
		{
			using (var array = new ArrayPtr(10))
				Assert.AreEqual(true, array.OwnMemory, "Should own memory");
		}

		/// <summary></summary>
		[Test]
		public void DoOwnMemory_2()
		{
			using (var array = new ArrayPtr())
			{
				Assert.AreEqual(true, array.OwnMemory, "Should own memory");
				array.Resize(10);
				Assert.AreEqual(true, array.OwnMemory, "Should still own memory after resize");
			}
		}
	}
}
