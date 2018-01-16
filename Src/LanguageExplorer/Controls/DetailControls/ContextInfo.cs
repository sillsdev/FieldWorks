// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This implementation of ConcSliceInfo is initialized with an Hvo, flid, offset,
	/// and length. It provides information about a context slice (leaf node).
	/// </summary>
	public class ContextInfo : IConcSliceInfo
	{
		public ContextInfo(int hvo, int flid, int offset, int length, bool fAllowEdit)
		{
			Hvo = hvo;
			ContentStringFlid = flid;
			ContextStringStartOffset = offset;
			ContextStringLength = length;
			AllowContentEditing = fAllowEdit;
		}
		/// <summary>
		/// This gets the object that the slice is mainly about, often an StTxtPara.
		/// </summary>
		public int Hvo { get; }

		/// <summary>
		/// This returns a flid for a string property (of Hvo) that may be displayed as
		/// the contents of the slice.
		/// </summary>
		public int ContentStringFlid { get; }

		/// <summary>
		/// Alternative context string (used if ContextStringFlid returns 0); hence
		/// not implemented for this class.
		/// </summary>
		public ITsString ContentString { get; } = null;

		/// <summary>
		/// True to allow the content string to be edited. It is assumed that
		/// the content strings are real properties and the cache will handle
		/// persisting the changes.
		/// </summary>
		public bool AllowContentEditing { get; }

		/// <summary>
		/// True to make this a context slice. In this case, the start offset
		/// and length methods are used to display the contents as a context string.
		/// </summary>
		public virtual bool DisplayAsContext { get; } = true;

		/// <summary>
		/// Obtains the offset in the context string where the interesting word
		/// appears, given the HVO and position of the context object.
		/// </summary>
		public int ContextStringStartOffset { get; }

		/// <summary>
		/// Obtains the length of the interesting word in the context string,
		/// given the HVO and position of the context object.
		/// </summary>
		public int ContextStringLength { get; }

		/// <summary>
		/// This allows the client to supply a view constructor to control what
		/// appears in the slice. As it returns null, the standard context VC is used
		/// </summary>
		public virtual IVwViewConstructor Vc { get; } = null;

		/// <summary>
		/// Returns the number of children. A context slice has none
		/// </summary>
		public int Count { get; } = 0;

		/// <summary>
		/// There are no children.
		/// </summary>
		public IConcSliceInfo ChildAt(int index)
		{
			Debug.Assert(false);
			return null;
		}
	}
}