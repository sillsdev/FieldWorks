// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
