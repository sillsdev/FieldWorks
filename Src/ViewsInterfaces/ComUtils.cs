// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
// Helper classes for use with COM interfaces. The structs are already defined in COM, but we
// re-define them so that we can provide conversion operators to/from .NET native types.
// </remarks>

using System;
using System.Diagnostics;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.ViewsInterfaces
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
		public static SelLevInfo[] AllTextSelInfo(IVwSelection vwsel, int cvsli,
			out int ihvoRoot, out int tagTextProp, out int cpropPrevious, out int ichAnchor,
			out int ichEnd, out int ws, out bool fAssocPrev, out int ihvoEnd, out ITsTextProps ttp)
		{
			Debug.Assert(vwsel != null);

			using (var rgvsliPtr = MarshalEx.ArrayToNative<SelLevInfo>(cvsli))
			{
				vwsel.AllTextSelInfo(out ihvoRoot, cvsli, rgvsliPtr,
					out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttp);
				return MarshalEx.NativeToArray<SelLevInfo>(rgvsliPtr, cvsli);
			}
		}

		/// <summary />
		public override bool Equals(object obj)
		{
			return obj is SelLevInfo selLevInfo && selLevInfo == this;
		}

		/// <summary />
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		/// <summary />
		public static bool operator == (SelLevInfo left, SelLevInfo right)
		{
			return left.hvo == right.hvo && left.ich == right.ich && left.ihvo == right.ihvo &&
				   left.tag == right.tag && left.ws == right.ws &&
				   left.cpropPrevious == right.cpropPrevious;
		}

		/// <summary />
		public static bool operator != (SelLevInfo left, SelLevInfo right)
		{
			return !(left == right);
		}
	}
}
