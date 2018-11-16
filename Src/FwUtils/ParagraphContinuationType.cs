// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	[Flags]
	public enum ParagraphContinuationType
	{
		/// <summary />
		None = 0,
		/// <summary />
		RequireAll = 1,
		/// <summary />
		RequireInnermost = 2,
		/// <summary />
		RequireOutermost = 4,
	}
}