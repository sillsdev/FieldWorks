// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// A Keyed object is one from which a key string can be retrieved.
	/// It is typically used to sort the objects.
	/// </summary>
	public interface IKeyedObject
	{
		string Key
		{
			get;
		}
	}
}