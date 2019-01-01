// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Marker class so we can catch the specific problem of seeking an HVO for an unknown
	/// (probably deleted object) guid.
	/// </summary>
	internal class InvalidObjectGuidException : ApplicationException
	{
	}
}