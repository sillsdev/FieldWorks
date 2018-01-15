// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// A trivial interface implmented by IBulkEditSpecControls which can replace objects in
	/// the underlying list.
	/// </summary>
	public interface IGetReplacedObjects
	{
		/// <summary>
		/// Get the dictionary which maps from replaced objects to replacements (HVOs/object IDs).
		/// </summary>
		Dictionary<int, int> ReplacedObjects { get; }
	}
}