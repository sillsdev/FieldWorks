// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>Contains information about a selection</summary>
	[Serializable]
	public class SelInfo
	{
		/// <summary>Index of the root object</summary>
		public int ihvoRoot;
		/// <summary>Number of previous properties</summary>
		public int cpropPrevious;
		/// <summary>Character index</summary>
		public int ich;
		/// <summary>Writing system</summary>
		public int ws;
		/// <summary>The tag of the text property selected. </summary>
		public int tagTextProp;
		/// <summary>
		/// Text Props associated with the selection itself. This can be different
		/// from the properties of the text where the selection is. This allows a
		/// selection (most likely an insertion point) to have properties set that
		/// will be applied if the user starts typing.
		/// </summary>
		[NonSerialized]
		public ITsTextProps ttpSelProps;
		/// <summary>IP associated with characters before current position</summary>
		public bool fAssocPrev = true;
		/// <summary>Index of end HVO</summary>
		public int ihvoEnd = -1;
		/// <summary>Level information</summary>
		public SelLevInfo[] rgvsli = new SelLevInfo[0];

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new object of type <see cref="SelInfo"/>.
		/// </summary>
		/// --------------------------------------------------------------------------------
		public SelInfo()
		{
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="src">The source object</param>
		/// --------------------------------------------------------------------------------
		public SelInfo(SelInfo src)
		{
			if (src == null)
				return;

			tagTextProp = src.tagTextProp;
			ihvoRoot = src.ihvoRoot;
			cpropPrevious = src.cpropPrevious;
			ich = src.ich;
			ws = src.ws;
			fAssocPrev = src.fAssocPrev;
			ihvoEnd = src.ihvoEnd;
			rgvsli = new SelLevInfo[src.rgvsli.Length];
			Array.Copy(src.rgvsli, rgvsli, src.rgvsli.Length);
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Compares <paramref name="s1"/> with <paramref name="s2"/>
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns><c>true</c> if s1 is less than s2, otherwise <c>false</c></returns>
		/// <remarks>Both objects must have the same number of levels, otherwise
		/// an <see cref="ArgumentException"/> will be thrown.</remarks>
		/// --------------------------------------------------------------------------------
		public static bool operator <(SelInfo s1, SelInfo s2)
		{
			if (s1.rgvsli.Length != s2.rgvsli.Length)
				throw new ArgumentException("Number of levels differs");

			for (int i = s1.rgvsli.Length - 1; i >= 0; i--)
			{
				if (s1.rgvsli[i].tag != s2.rgvsli[i].tag)
					throw new ArgumentException("Differing tags");
				if (s1.rgvsli[i].ihvo > s2.rgvsli[i].ihvo)
					return false;
				else if (s1.rgvsli[i].ihvo == s2.rgvsli[i].ihvo)
				{
					if (s1.rgvsli[i].cpropPrevious > s2.rgvsli[i].cpropPrevious)
						return false;
					else if (s1.rgvsli[i].cpropPrevious < s2.rgvsli[i].cpropPrevious)
						return true;

				}
				else
					return true;
			}

			if (s1.ihvoRoot > s2.ihvoRoot)
				return false;
			else if (s1.ihvoRoot == s2.ihvoRoot)
			{
				if (s1.cpropPrevious > s2.cpropPrevious)
					return false;
				else if (s1.cpropPrevious == s2.cpropPrevious)
				{
					if (s1.ich >= s2.ich)
						return false;
				}
				else
					return true;
			}

			return true;
		}

		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Compares <paramref name="s1"/> with <paramref name="s2"/>
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns><c>true</c> if s1 is greater than s2, otherwise <c>false</c></returns>
		/// <remarks>Both objects must have the same number of levels, otherwise
		/// <c>false</c> will be returned.</remarks>
		/// --------------------------------------------------------------------------------
		public static bool operator >(SelInfo s1, SelInfo s2)
		{
			return s2 < s1;
		}
	}
}