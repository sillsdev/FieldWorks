// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews.Builders
{
	/// <summary>
	/// This class provides a fluent language way to specify units for dimensions that are required
	/// in millipoints. For example, Flow.FontSize(12.Points).
	/// </summary>
	public static class Units
	{
		public static int Points(this int points)
		{
			return points*1000;
		}
		public static int Points(this Double points)
		{
			return (int)Math.Round(points * 1000);
		}
	}
}
