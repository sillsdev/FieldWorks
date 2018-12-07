// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// This subclass is used to estimate the total width (and max height) of all text that the display shows.
	/// Currently it only supports a rough approximation by picking one font to use throughout.
	/// Could be enhanced to consider changes in text properties.
	/// Could be much more easily made to do the right thing if we had a valid base VwEnv.
	/// </summary>
	internal class StringMeasureEnv : CollectorEnv
	{
		/// <summary />
		protected Font m_font;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:StringMeasureEnv"/> class.
		/// </summary>
		/// <param name="baseEnv">The base env.</param>
		/// <param name="sda">Date access to get prop values etc.</param>
		/// <param name="hvoRoot">The root object to display, if m_baseEnv is null.
		/// If baseEnv is not null, hvoRoot is ignored.</param>
		/// <param name="graphics">To use to measure string</param>
		/// <param name="font">To use to measure strings</param>
		public StringMeasureEnv(IVwEnv baseEnv, ISilDataAccess sda, int hvoRoot, Graphics graphics, System.Drawing.Font font)
			: base(baseEnv, sda, hvoRoot)
		{
			GraphicsObj = graphics;
			m_font = font;
		}

		/// <summary>
		/// Gets the result width.
		/// </summary>
		public int Width { get; protected set; }

		/// <summary>
		/// Gets the System.Drawing.Graphics object.
		/// </summary>
		protected Graphics GraphicsObj { get; }

		/// <summary>
		/// Accumulate a string into our result, by adding its width
		/// </summary>
		public override void AddResultString(string s)
		{
			base.AddResultString (s);
			if (s == null)
			{
				return;
			}
			AddStringWidth(s);
		}

		/// <summary>
		/// update total pixel width for display
		/// </summary>
		protected virtual void AddStringWidth(string s)
		{
			Width += Convert.ToInt32(GraphicsObj.MeasureString(s, m_font).Width);
		}
	}
}