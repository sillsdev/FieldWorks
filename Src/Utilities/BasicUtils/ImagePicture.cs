// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IPicture.cs
// Responsibility: Linux Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SIL.Utils.ComTypes;

namespace SIL.Utils
{
	/// <summary/>
	[ComImport()]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("CA9AAF91-4C34-4c6a-8E07-97C1A7B3486A")]
	public interface IComDisposable
	{

		/// <summary> </summary>
		[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void Dispose();
	}

	/// <summary>
	/// A IPicture Implementation using C# image class (mainly for use on Linux)
	/// </summary>
	/// <remarks>NOTE: only the methods that are currently used are implemented.</remarks>
	//[ComImport()]
	[Guid("1bd4d91c-124b-11de-96cb-0019dbf4566e"),
	ClassInterface(ClassInterfaceType.None),
	TypeLibType(TypeLibTypeFlags.FCanCreate)]
	public class ImagePicture : IPicture, IDisposable, IPictureDisp, IComDisposable
	{
		/// <summary>
		/// Contains the image this is Rendered.
		/// </summary>
		protected Image m_img;

		// TODO-Linux: Currently using fixed dpi this is not ideal as images will be wrong size
		// on monitors with different dpis.
		private int dpiX = 96;
		private int dpiY = 96;

		/// <summary>
		/// Whether the ImagePicture is owned by native or managed code, to specify
		/// who should dispose of it.
		/// </summary>
		public bool ReferenceOwnedByNative { get; set; }

		/// <summary>
		/// Construct a ImagePicture from a C# Image object
		/// </summary>
		public static ImagePicture FromImage(Image img)
		{
			ImagePicture ret = new ImagePicture();
			ret.m_img = (Image)img.Clone();

			return ret;
		}

		/// <summary>
		/// Construct a  ImagePicture from an array of bytes
		/// </summary>
		public static ImagePicture ImageBytes(byte[] pbData, int cbData)
		{
			MemoryStream s = new MemoryStream(pbData, 0, cbData);
			ImagePicture ret = new ImagePicture();
			ret.m_img = Image.FromStream(s);
			return ret;
		}

		/// <summary/>
		public ImagePicture()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="ImagePicture"/> is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~ImagePicture()
		{
			Dispose(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="fDisposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing)
			{
				if (m_img != null)
					m_img.Dispose();
			}
			m_img = null;
		}

		#region IDisposable Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IComDisposable.Dispose()
		{
			if (ReferenceOwnedByNative == false)
				return;

			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void IDisposable.Dispose()
		{
			if (ReferenceOwnedByNative == true)
				return;

			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the horizontal dpi.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DpiX
		{
			get { return dpiX; }
			set { dpiX = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the vertical dpi.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DpiY
		{
			get { return dpiY; }
			set { dpiY = value; }
		}

		#region IPicture Members

		/// <summary> return the width in himetric</summary>
		public int Width
		{
			get
			{
				return new HiMetric(m_img.Width, dpiX).Value;
			}
		}

		/// <summary> return the height in himetric</summary>
		public int Height
		{
			get
			{
				return new HiMetric(m_img.Height, dpiY).Value;
			}
		}

		/// <summary></summary>
		/// prcWBounds not used
		public void Render(
			IntPtr hdc,
			int x,
			int y,
			int cx,
			int cy,
			int xSrc,
			int ySrc,
			int cxSrc,
			int cySrc,
			IntPtr prcWBounds)
		{
			using (Graphics gr = Graphics.FromHdc(hdc))
			{

				// Sometimes width or height is negative and we are expected to draw backwards (which DrawImage doesn't seem to do)
				if (cySrc < 0)
				{
					cySrc = Math.Abs(cySrc);
					ySrc -= cySrc;
				}

				if (cxSrc < 0)
				{
					cxSrc = Math.Abs(cxSrc);
					xSrc -= cxSrc;
				}

				gr.DrawImage(m_img, new Rectangle(x, y, cx, cy),
							 new Rectangle(
									  new HiMetric(xSrc).GetPixels(dpiX),
									  new HiMetric(ySrc).GetPixels(dpiY),
									  new HiMetric(cxSrc).GetPixels(dpiX),
									  new HiMetric(cySrc).GetPixels(dpiY)), GraphicsUnit.Pixel);
			}
		}

		/// <summary/>
		public int Attributes
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary/>
		public int CurDC
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary/>
		public int Handle
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary/>
		public bool KeepOriginalFormat
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		/// <summary/>
		public void PictureChanged()
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public void Render(int hdc, int x, int y, int cx, int cy, int xSrc, int ySrc, int cxSrc, int cySrc, IntPtr prcWBounds)
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public void SaveAsFile(IntPtr pstm, bool fSaveMemCopy, out int pcbSize)
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public void SelectPicture(int hdcIn, out int phdcOut, out int phbmpOut)
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public void SetHdc(int hdc)
		{
			throw new NotImplementedException();
		}

		/// <summary/>
		public short Type
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary/>
		public int hPal
		{
			get { throw new NotImplementedException(); }
		}
		/// <summary/>
		public void put_hPal(int val)
		{
			throw new NotImplementedException();
		}

		#endregion
	}

}
