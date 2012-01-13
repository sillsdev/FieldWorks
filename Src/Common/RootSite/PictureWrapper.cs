// --------------------------------------------------------------------------------------------
#region Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PictureWrapper.cs
// Responsibility: FW Team
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SIL.Utils;
using SIL.Utils.ComTypes;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ---------------------------------------------------------------------------------------
	/// <remarks>
	/// A wrapper for the COM implementation of IPicture to hold a weak reference to image data
	/// </remarks>
	/// ---------------------------------------------------------------------------------------
	internal class PictureWrapper : IPicture
	{
		#region Data members
		private readonly string m_sPath;
		private int m_width;
		private int m_height;
		private WeakReference m_internalPicture;
		#endregion

		#region Constructor
		internal PictureWrapper(string path)
		{
			m_sPath = path;
		}
		#endregion

		#region Private helpers
		private IPicture Picture
		{
			get
			{
				IPicture picture = (m_internalPicture != null) ? m_internalPicture.Target as IPicture : null;
				if (picture == null)
					picture = LoadPicture();
				return picture;
			}
		}

		private IPicture LoadPicture()
		{
			IPicture picture;
			Image image = null;
			try
			{
				try
				{
					image = Image.FromFile(FileUtils.ActualFilePath(m_sPath));
				}
				catch
				{
					// unable to read image. set to default image that indicates an invalid image.
					image = SimpleRootSite.ImageNotFoundX;
				}
				try
				{
					picture = (IPicture)OLECvt.ToOLE_IPictureDisp(image);
				}
				catch
				{
					// conversion to OLE format from current image format is not supported (e.g. WMF file)
					// try to convert it to a bitmap and convert it to OLE format again.
					// TODO: deal with transparency
					// We could just do the following line (creating a new bitmap) instead of going
					// through a memory stream, but then we end up with an image that is too big.
					//image = new Bitmap(image, image.Size);
					using (MemoryStream imageStream = new MemoryStream())
					{
						image.Save(imageStream, ImageFormat.Png);
						image.Dispose();
						image = Image.FromStream(imageStream, true);
					}
					picture = (IPicture)OLECvt.ToOLE_IPictureDisp(image);
				}
				m_width = picture.Width;
				m_height = picture.Height;
				m_internalPicture = new WeakReference(picture);
			}
			finally
			{
				if (image != null)
					image.Dispose();
			}
			return picture;
		}
		#endregion

		#region Implementation of IPicture
		public void Render(IntPtr hdc, int x, int y, int cx, int cy, int xSrc, int ySrc, int cxSrc, int cySrc, IntPtr prcWBounds)
		{
			// See http://blogs.microsoft.co.il/blogs/sasha/archive/2008/07/28/finalizer-vs-application-a-race-condition-from-hell.aspx
			IPicture p = Picture;
			p.Render(hdc, x, y, cx, cy, xSrc, ySrc, cxSrc, cySrc, prcWBounds);
			GC.KeepAlive(p);
		}

		public void put_hPal(int val)
		{
			Picture.put_hPal(val);
		}

		public void SelectPicture(int hdcIn, out int phdcOut, out int phbmpOut)
		{
			Picture.SelectPicture(hdcIn, out phdcOut, out phbmpOut);
		}

		public void PictureChanged()
		{
			Picture.PictureChanged();
		}

		public void SaveAsFile(IntPtr pstm, bool fSaveMemCopy, out int pcbSize)
		{
			Picture.SaveAsFile(pstm, fSaveMemCopy, out pcbSize);
		}

		public void SetHdc(int hdc)
		{
			Picture.SetHdc(hdc);
		}

		public int Handle
		{
			get { throw new NotImplementedException("Not sure whether we could safely implement this simply since the underlying picture could be disposed before the caller finished using the handle."); /* return Picture.Handle; */ }
		}

		public int hPal
		{
			get { return Picture.hPal; }
		}

		public short Type
		{
			get { return Picture.Type; }
		}

		public int Width
		{
			get
			{
				if (m_internalPicture == null) // If this is null, the picture has never been loaded, so the width has not been set
					LoadPicture();
				return m_width;
			}
		}
		public int Height
		{
			get
			{
				if (m_internalPicture == null) // If this is null, the picture has never been loaded, so the height has not been set
					LoadPicture();
				return m_height;
			}
		}

		public int CurDC
		{
			get { throw new NotImplementedException("Not sure whether we could safely implement this simply since the underlying picture could be disposed before the caller finished using the return value."); /* return Picture.CurDC; */ }
		}

		public bool KeepOriginalFormat
		{
			get { return Picture.KeepOriginalFormat; }
			set { Picture.KeepOriginalFormat = value; }
		}

		public int Attributes
		{
			get { return Picture.Attributes; }
		}
		#endregion
	}
}
