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
