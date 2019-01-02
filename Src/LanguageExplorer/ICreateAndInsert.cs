// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for creating method objects that can be passed into DoCreateAndInsert
	/// in order to create an object, insert them into our list, and adjust CurrentIndex in one operation.
	/// </summary>
	public interface ICreateAndInsert<out TObject>
		where TObject : ICmObject
	{
		TObject Create();
	}
}