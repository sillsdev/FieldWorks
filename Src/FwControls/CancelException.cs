// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Useful little exception class that clients can throw as part of their processing the
	/// Progress Dialog's cancel event.
	/// </summary>
	public class CancelException : Exception
	{
		/// <summary />
		public CancelException(string msg)
			: base(msg)
		{
		}
	}
}