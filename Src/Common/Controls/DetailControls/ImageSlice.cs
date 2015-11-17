// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImageSlice.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// Implements a simple Image XDE editor.
// </remarks>

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

using SIL.Utils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for ImageSlice.
	/// </summary>
	public class ImageSlice : Slice
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="distFilesPath">the path to the distfiles Directory</param>
		/// <param name="relativeImagePath">they path to the image, relative to the distfiles Directory</param>
		/// <remarks>Will throw an exception if the image is not found.</remarks>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "PictureBox gets added to the Controls collection in the base class and disposed there")]
		public ImageSlice(string distFilesPath, string relativeImagePath) : base(new PictureBox())
		{
			string sPathname = System.IO.Path.Combine(distFilesPath, relativeImagePath);
			((PictureBox)this.Control).Image = Image.FromFile(FileUtils.ActualFilePath(sPathname));
			((PictureBox)this.Control).Height = ((PictureBox)this.Control).Image.Height;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Prevents memory leaks and also spurious continued locking of the file.
				var picBox = this.Control as PictureBox;
				if (picBox != null && picBox.Image != null)
				{
					picBox.Image.Dispose();
					picBox.Image = null;
				}
			}
			base.Dispose(disposing);
		}
//		// Overhaul Aug 05: want all Window backgrounds in Detail controls.
//		/// <summary>
//		/// This is passed the color that the XDE specified, if any, otherwise null.
//		/// Images are not editable, so use the inactive color.
//		/// </summary>
//		/// <param name="clr"></param>
//		public override void OverrideBackColor(String backColorName)
//		{
//			CheckDisposed();
//
//			if (this.Control == null)
//				return;
//			if (backColorName != null)
//				this.Control.BackColor = Color.FromName(backColorName);
//			else
//				this.Control.BackColor = System.Drawing.SystemColors.ControlLight;
//		}
	}
}
