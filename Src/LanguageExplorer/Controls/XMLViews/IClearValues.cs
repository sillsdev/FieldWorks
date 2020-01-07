// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This interface indicates a control or other object that may need to clear its values,
	/// typically as one stage of a Refresh that will ultimately restore current values.
	/// Typically this is because the old values are dummy ones that are no longer valid,
	/// and there is danger of an intermediate stage of the Refresh attempting to use them and crashing.
	/// </summary>
	public interface IClearValues
	{
		/// <summary>
		/// Clear values that might otherwise be reused before being fully reset.
		/// </summary>
		void ClearValues();
	}
}