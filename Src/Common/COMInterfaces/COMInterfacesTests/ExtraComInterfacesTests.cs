// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExtraComInterfacesTests.cs
// Responsibility: Linux team
//
// <remarks>
// </remarks>

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using NUnit.Framework;

namespace SIL.FieldWorks.Common.COMInterfaces
{

	/// <summary>Dummy implementation of IStream to pass to methods that require one.</summary>
	public class MockIStream :  IStream
	{
		#region IStream Members

		/// <summary/>
		public void Clone(out IStream ppstm)
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public void Commit(int grfCommitFlags) { }

		/// <summary/>
		public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public void LockRegion(long libOffset, long cb, int dwLockType)	{}

		/// <summary/>
		public void Read(byte[] pv, int cb, IntPtr pcbRead)
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public void Revert() {}

		/// <summary/>
		public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition) { }

		/// <summary/>
		public void SetSize(long libNewSize) { }

		/// <summary/>
		public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg,
			int grfStatFlag)
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public void UnlockRegion(long libOffset, long cb, int dwLockType) {	}

		/// <summary/>
		public void Write(byte[] pv, int cb, IntPtr pcbWritten) { }

		#endregion
	}


	/// <summary/>
	[TestFixture]
	[Ignore("experimental Mono dependent")]
	public class ReleaseComObjectTests // can't derive from BaseTest because of dependencies
	{
		/// <summary/>
		[Test]
		[ExpectedException(typeof(COMException))]
		public void ComRelease()
		{
			ILgWritingSystemFactoryBuilder lefBuilder = LgWritingSystemFactoryBuilderClass.Create();
			ILgWritingSystemFactoryBuilder myref = lefBuilder;
			Assert.AreEqual(true, Marshal.IsComObject(lefBuilder), "#1");
			Assert.AreEqual(0, Marshal.ReleaseComObject(lefBuilder), "#2");
			lefBuilder = null;
			myref.ShutdownAllFactories();
		}
	}
}
