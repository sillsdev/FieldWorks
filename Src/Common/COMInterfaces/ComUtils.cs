// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ComUtils.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Helper classes for use with COM interfaces. The structs are already defined in COM, but we
// re-define them so that we can provide conversion operators to/from .NET native types.
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// <summary>
	/// Redefine VwSelLevInfo struct, so that it can be serialized.
	/// </summary>
	[Serializable]
	public struct SelLevInfo
	{
		/// <summary>The tag</summary>
		public int tag;
		/// <summary>Number of previous occurences of the property</summary>
		public int cpropPrevious;
		/// <summary>Index of hvo (-1 for string property)</summary>
		public int ihvo;
		/// <summary> The actual hvo (only when reading info).</summary>
		public int hvo;
		/// <summary>
		/// If the property is a multitext one, gives the identifier of the alternative.
		/// Value is meaningless unless ihvo == -1.
		/// </summary>
		public int ws;
		/// <summary>
		/// If the property is a text (or multitext) one, gives the char index of the ORC
		/// that 'contains' the embedded object containing the selection.
		/// Value is meaningless unless ihvo == -1.
		/// </summary>
		public int ich;

		/// <summary>
		/// Get an array of SelLevInfo structs from the given selection.
		/// </summary>
		/// <param name="vwsel"></param>
		/// <param name="cvsli"></param>
		/// <param name="ihvoRoot"></param>
		/// <param name="tagTextProp"></param>
		/// <param name="cpropPrevious"></param>
		/// <param name="ichAnchor"></param>
		/// <param name="ichEnd"></param>
		/// <param name="ws"></param>
		/// <param name="fAssocPrev"></param>
		/// <param name="ihvoEnd"></param>
		/// <param name="ttp"></param>
		/// <returns></returns>
		public static SelLevInfo[] AllTextSelInfo(IVwSelection vwsel, int cvsli,
			out int ihvoRoot, out int tagTextProp, out int cpropPrevious, out int ichAnchor,
			out int ichEnd, out int ws, out bool fAssocPrev, out int ihvoEnd, out ITsTextProps ttp)
		{
			Debug.Assert(vwsel != null);

			using (ArrayPtr rgvsliPtr = MarshalEx.ArrayToNative(cvsli, typeof(SelLevInfo)))
			{
				vwsel.AllTextSelInfo(out ihvoRoot, cvsli, rgvsliPtr,
					out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttp);
				return (SelLevInfo[])MarshalEx.NativeToArray(rgvsliPtr, cvsli,
					typeof(SelLevInfo));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			return obj is SelLevInfo && ((SelLevInfo)obj) == this;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator == (SelLevInfo left, SelLevInfo right)
		{
			return (left.hvo == right.hvo && left.ich == right.ich && left.ihvo == right.ihvo &&
				left.tag == right.tag && left.ws == right.ws &&
				left.cpropPrevious == right.cpropPrevious);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool operator != (SelLevInfo left, SelLevInfo right)
		{
			return !(left == right);
		}
	}
}
