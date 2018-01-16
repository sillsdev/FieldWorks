// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// The three 'weights' of objects for detail views:
	/// HeavyWeight objects get a thick rule above them;
	/// Normal objects don't.
	/// (Is there a distinction for lightweight? They're supposed to be almost non-detectable.)
	/// (Since this is an indication of whether an object starts at the top of a field, another
	/// case is that it's just a field.)
	/// (This is not fully utliized or implemented yet. Only the heavy option is distinguished
	/// from normal to produce the heavy rule.)
	/// </summary>
	internal enum ObjectWeight
	{
		heavy,
		normal,
		light,
		field
	}
}