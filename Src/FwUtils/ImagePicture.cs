// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// A IPicture Implementation using C# image class (mainly for use on Linux)
	/// </summary>
	/// <remarks>NOTE: only the methods that are currently used are implemented.</remarks>
	[Guid("1bd4d91c-124b-11de-96cb-0019dbf4566e"), ClassInterface(ClassInterfaceType.None), TypeLibType(TypeLibTypeFlags.FCanCreate)]
	public sealed class ImagePicture : IPicture, IDisposable, IPictureDisp, IComDisposable
	{
		/// <summary>
		/// Contains the image this is Rendered.
		/// </summary>
		private Image m_img;

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
			var ret = new ImagePicture
			{
				m_img = (Image)img.Clone()
			};

			return ret;
		}

		/// <summary>
		/// Construct a  ImagePicture from an array of bytes
		/// </summary>
		public static ImagePicture ImageBytes(byte[] pbData, int cbData)
		{
			using (var s = new MemoryStream(pbData, 0, cbData))
			{
				var ret = new ImagePicture
				{
					m_img = Image.FromStream(s)
				};
				return ret;
			}
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="ImagePicture"/> is reclaimed by garbage collection.
		/// </summary>
		~ImagePicture()
		{
			Dispose(false);
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing)
			{
				m_img?.Dispose();
			}
			m_img = null;
		}

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		void IComDisposable.Dispose()
		{
			if (ReferenceOwnedByNative == false)
			{
				return;
			}
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void IDisposable.Dispose()
		{
			if (ReferenceOwnedByNative)
			{
				return;
			}

			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		/// <summary>
		/// Gets or sets the horizontal dpi.
		/// </summary>
		public int DpiX { get; set; } = 96;

		/// <summary>
		/// Gets or sets the vertical dpi.
		/// </summary>
		public int DpiY { get; set; } = 96;

		#region IPicture Members

		/// <summary> return the width in himetric</summary>
		public int Width => new HiMetric(m_img.Width, DpiX).Value;

		/// <summary> return the height in himetric</summary>
		public int Height => new HiMetric(m_img.Height, DpiY).Value;

		/// <summary />
		/// <remarks>prcWBounds not used</remarks>
		public void Render(IntPtr hdc, int x, int y, int cx, int cy, int xSrc, int ySrc, int cxSrc, int cySrc, IntPtr prcWBounds)
		{
			using (var gr = Graphics.FromHdc(hdc))
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

				gr.DrawImage(m_img, new Rectangle(x, y, cx, cy), new Rectangle(new HiMetric(xSrc).GetPixels(DpiX), new HiMetric(ySrc).GetPixels(DpiY), new HiMetric(cxSrc).GetPixels(DpiX), new HiMetric(cySrc).GetPixels(DpiY)), GraphicsUnit.Pixel);
			}
		}

		/// <summary />
		public int Attributes
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary />
		public int CurDC
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary />
		public int Handle
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary />
		public bool KeepOriginalFormat
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

		/// <summary />
		public void PictureChanged()
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void Render(int hdc, int x, int y, int cx, int cy, int xSrc, int ySrc, int cxSrc, int cySrc, IntPtr prcWBounds)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void SaveAsFile(IntPtr pstm, bool fSaveMemCopy, out int pcbSize)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void SelectPicture(int hdcIn, out int phdcOut, out int phbmpOut)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public void SetHdc(int hdc)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		public short Type
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary />
		public int hPal
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary />
		public void put_hPal(int val)
		{
			throw new NotSupportedException();
		}

		#endregion
	}
}