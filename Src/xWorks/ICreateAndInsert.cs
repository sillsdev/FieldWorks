// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Interface for creating method objects that can be passed into DoCreateAndInsert
	/// in order to create an object, insert them into our list, and adjust CurrentIndex in one operation.
	/// </summary>
	/// <typeparam name="TObject"></typeparam>
	public interface ICreateAndInsert<out TObject>
		where TObject : ICmObject
	{
		TObject Create();
	}
}