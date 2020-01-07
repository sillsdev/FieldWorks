// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal interface INodeInfo
	{
		/// <summary>
		/// A flid that can be used to obtain from the cache an object sequence
		/// property of the HVO for the slice, giving the list of objects
		/// each of which produces one line in the cache.
		/// </summary>
		int ListFlid { get; }
		/// <summary>
		/// For each context object, this returns a flid that may be displayed as
		/// the context for that object. If this returns 0, the following method
		/// is used instead to obtain the context string. It is passed both the
		/// position and actual hvo of the context object.
		/// </summary>
		int ContextStringFlid(int ihvoContext, int hvoContext);
		/// <summary>
		/// Alternative context string (used if ContextStringFlid returns 0)
		/// </summary>
		ITsString ContextString(int ihvoContext, int hvoContext);
		/// <summary>
		/// True to allow the context string to be edited. It is assumed that
		/// the context strings are real properties and the cache will handle
		/// persisting the changes. Ignored if ContextStringFlid returns 0.
		/// </summary>
		bool AllowContextEditing { get; }
		/// <summary>
		/// Obtains the offset in the context string where the interesting word
		/// appears, given the HVO and position of the context object.
		/// </summary>
		int ContextStringStartOffset(int ihvoContext, int hvoContext);
		/// <summary>
		/// Obtains the length of the interesting word in the context string,
		/// given the HVO and position of the context object.
		/// </summary>
		int ContextStringLength(int ihvoContext, int hvoContext);
	}
}