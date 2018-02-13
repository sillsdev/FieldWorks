// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// A ParaNodeinfo assumes that all the context strings are the contents of
	/// StTxtParas. It assumes the length of all key strings is constant, typically
	/// the length of the header node string. The offsets are supplied as a
	/// parameter to the constructor. Context editing is off by default, but
	/// may be turned on.
	/// </summary>
	internal class ParaNodeInfo : INodeInfo
	{
		int[] m_startOffsets;
		int m_keyLength;

		public ParaNodeInfo(int flidList, int[] startOffsets, int keyLength)
		{
			m_startOffsets = startOffsets;
			m_keyLength = keyLength;
			ListFlid = flidList;
		}

		/// <summary>
		/// This constructor looks up the headword of the slice to get the key length.
		/// </summary>
		public ParaNodeInfo(int flidList, int[] startOffsets, ISilDataAccess sda, int hvoHeadObj, int flid)
		{
			ListFlid = flidList;
			var str = sda.get_StringProp(hvoHeadObj, flid);
			m_keyLength = str.Length;
			m_startOffsets = startOffsets;
		}
		/// <summary>
		/// A flid that can be used to obtain from the cache an object sequence
		/// property of the HVO for the slice, giving the list of objects
		/// each of which produces one line in the cache.
		/// </summary>
		public int ListFlid { get; }

		/// <summary>
		/// This implementation assumes it is dealing with contents of paragraphs.
		/// </summary>
		public int ContextStringFlid(int ihvoContext, int hvoContext)
		{
			return StTxtParaTags.kflidContents;
		}

		/// <summary>
		/// Alternative context string (not used in this impl)
		/// </summary>
		public ITsString ContextString(int ihvoContext, int hvoContext)
		{
			return null;
		}

		/// <summary>
		/// True to allow the context string to be edited. It is assumed that
		/// the context strings are real properties and the cache will handle
		/// persisting the changes. Ignored if ContextStringFlid returns 0.
		/// </summary>
		public bool AllowContextEditing { get; set; } = false;

		/// <summary>
		/// Obtains the offset in the context string where the interesting word
		/// appears, given the HVO and position of the context object.
		/// </summary>
		public int ContextStringStartOffset(int ihvoContext, int hvoContext)
		{
			return m_startOffsets[ihvoContext];
		}
		/// <summary>
		/// Obtains the length of the interesting word in the context string,
		/// given the HVO and position of the context object.
		/// </summary>
		public int ContextStringLength(int ihvoContext, int hvoContext)
		{
			return m_keyLength;
		}
	}
}