// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	public interface IConcSliceInfo
	{
		/// <summary>
		/// This gets the object that the slice is mainly about. It may return
		/// zero, if the slice is not 'about' any particular object.
		/// </summary>
		int Hvo { get; }
		/// <summary>
		/// This returns a flid for a string property (of Hvo) that may be displayed as
		/// the contents of the slice. If this of Hvo returns 0, the following method
		/// is used instead to obtain the contents string.
		/// </summary>
		int ContentStringFlid { get; }
		/// <summary>
		/// Alternative context string (used if ContextStringFlid returns 0)
		/// </summary>
		ITsString ContentString { get; }
		/// <summary>
		/// True to allow the content string to be edited. It is assumed that
		/// the content strings are real properties and the cache will handle
		/// persisting the changes. Ignored if Hvo or ContentStringFlid returns 0.
		/// </summary>
		bool AllowContentEditing { get; }
		/// <summary>
		/// True to make this a context slice. In this case, the start offset
		/// and length methods are used to display the contents as a context string.
		/// </summary>
		bool DisplayAsContext { get; }
		/// <summary>
		/// Obtains the offset in the context string where the interesting word
		/// appears, given the HVO and position of the context object.
		/// </summary>
		int ContextStringStartOffset { get; }
		/// <summary>
		/// Obtains the length of the interesting word in the context string,
		/// given the HVO and position of the context object.
		/// </summary>
		int ContextStringLength { get; }
		/// <summary>
		/// This allows the client to supply a view constructor to control what
		/// appears in the slice. If it returns null, one of two standard views
		/// is used, depending on DisplayAsContext.
		/// </summary>
		IVwViewConstructor Vc { get; }
		/// <summary>
		/// Returns the number of children. Typically zero for context slices.
		/// </summary>
		int Count { get; }
		/// <summary>
		/// Returns the indicated child (0-based addressing). The index passed
		/// will be in the range 0..Count-1.
		/// </summary>
		IConcSliceInfo ChildAt(int index);
	}
}