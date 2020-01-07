// Copyright (c) 2018-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;

namespace LanguageExplorer
{
	internal static class BitmapExtensions
	{
		internal static Image SetBackgroundColor(this Bitmap me, Color transparentColor)
		{
			me.MakeTransparent(transparentColor);
			return me;
		}
	}
}
