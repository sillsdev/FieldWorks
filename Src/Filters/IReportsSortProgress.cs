// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Interface implemented by RecordSorter if it can send percent done messages.
	/// </summary>
	public interface IReportsSortProgress
	{
		Action<int> SetPercentDone { get; set;}
	}
}