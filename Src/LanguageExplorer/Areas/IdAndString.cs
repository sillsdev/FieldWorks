// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Helper class for storing a T value and String value together
	/// </summary>
	internal class IdAndString<T>
	{
		internal IdAndString(T id, string name)
		{
			Id = id;
			Name = name;
		}
		public override string ToString() { return Name; }
		// read only properties
		internal T Id { get; }
		internal string Name { get; }
	}
}