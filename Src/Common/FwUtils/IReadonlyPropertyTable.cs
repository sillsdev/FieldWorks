// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface for read only property tables, where even the default value, is not stored in the implementation.
	/// </summary>
	public interface IReadonlyPropertyTable : IPropertyRetriever
	{ }
}