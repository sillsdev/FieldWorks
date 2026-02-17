// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExtraViewsInterfacesTests.cs
// Responsibility: Linux team
//
// <remarks>
// </remarks>

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using NUnit.Framework;

namespace SIL.FieldWorks.Common.ViewsInterfaces
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


	/// <summary>
	/// Historical/experimental test that depended on Mono-specific COM interfaces.
	/// The referenced ILgWritingSystemFactoryBuilder interface no longer exists.
	/// </summary>
	[TestFixture]
	[Ignore("Obsolete: ILgWritingSystemFactoryBuilder interface no longer exists (historical Mono-specific experiment).")]
	public class ReleaseComObjectTests
	{
		[Test]
		public void ObsoleteInterface_Disabled()
		{
			Assert.Ignore(
				"Obsolete: ILgWritingSystemFactoryBuilder interface no longer exists; " +
				"legacy COM-release test is kept for reference only."
			);
		}
	}
}
