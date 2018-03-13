// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Private marker class so we can catch the specific problem of seeking an HVO for an unknown
	/// (probably deleted object) guid.
	/// </summary>
	internal class InvalidObjectGuidException : ApplicationException
	{
	}
}