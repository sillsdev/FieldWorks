// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ManagedPictureFactory.cs
// Responsibility:
// ---------------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using SIL.Utils.ComTypes;
using SIL.Utils;
namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// <summary>
	/// Implement IPictureFactory using ImagePicture class.
	/// This class Should NOT be used from C# or other managed code.
	/// </summary>
	[Guid("17a2e876-2968-11e0-8046-0019dbf4566e"),
	ClassInterface(ClassInterfaceType.None),
	TypeLibType(TypeLibTypeFlags.FCanCreate)]
	public class ManagedPictureFactory : IPictureFactory
	{
		/// <summary>
		/// Construct a IPicture from an array of bytes
		/// returned IPicture is tagged to be owned by NativeCode, which should dispose of it.
		/// </summary>
		public IPicture ImageFromBytes(byte[] pbData, int cbData)
		{
			var ret = ImagePicture.ImageBytes(pbData, cbData);
			ret.ReferenceOwnedByNative = true;
			return ret;
		}
	}
}
