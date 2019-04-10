// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Factory that creates an instance of ToolStripButton.
	/// </summary>
	internal static class ToolStripButtonFactory
	{
		internal static ToolStripButton CreateToolStripButton(EventHandler eventHandler, string buttonName, Image image, string tooltip = "")
		{
			throw new NotSupportedException("CreateToolStripButton");
			/*
			var newToolStripButton = new ToolStripButton(buttonName, image, eventHandler)
			{
				DisplayStyle = ToolStripItemDisplayStyle.Image
			};
			if (!string.IsNullOrWhiteSpace(tooltip))
			{
				newToolStripButton.ToolTipText = tooltip;
			}
			return newToolStripButton;
			*/
		}

		internal static ToolStripSeparator CreateToolStripSeparator()
		{
			throw new NotSupportedException("CreateToolStripButton");
			//return new ToolStripSeparator();
		}
	}
}