// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Used to keep track of references to a VwGraphics object. Also handles the setting of the
	/// HDC in the VwGraphics.
	/// </summary>
	public sealed class GraphicsManager : IDisposable
	{
		private volatile int m_cactInitGraphics;
		private Graphics m_graphics;
		private IVwGraphicsWin32 m_vwGraphics;
		private Control m_parent;

		/// <summary />
		public GraphicsManager(Control parent)
		{
			if (parent == null)
			{
				throw new ArgumentNullException(nameof(parent));
			}

			m_parent = parent;
		}

		#region Disposed stuff

		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		/// <remarks>This property is thread safe.</remarks>
		private bool IsDisposed { get; set; }

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// GraphicsManager is reclaimed by garbage collection.
		/// </summary>
		~GraphicsManager()
		{
			Dispose(false);
		}

		/// <summary />
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				Debug.Assert(m_cactInitGraphics == 0, "We should release the HDC before disposing");
				m_graphics?.Dispose();
			}

			m_graphics = null;
			m_vwGraphics = null;
			m_parent = null;
			IsDisposed = true;
		}
		#endregion

		/// <summary>
		/// Gets the VwGraphics object
		/// </summary>
		public IVwGraphics VwGraphics => m_vwGraphics;

		/// <summary>
		/// Make sure the graphics object has a DC. If it already has, increment a count,
		/// so we know when to really free the DC.
		/// </summary>
		public void Init(float zoomPercentage)
		{
			Init(0, 0, zoomPercentage);
		}

		/// <summary>
		/// Make sure the graphics object has a DC. If it already has, increment a count,
		/// so we know when to really free the DC.
		/// </summary>
		/// <param name="dpix">The DPI in the x direction.</param>
		/// <param name="dpiy">The DPI in the y direction.</param>
		public void Init(int dpix, int dpiy)
		{
			Init(dpix, dpiy, 1.0f);
		}

		/// <summary>
		/// Make sure the graphics object has a DC. If it already has, increment a count,
		/// so we know when to really free the DC.
		/// </summary>
		/// <param name="dpix">The DPI in the x direction.</param>
		/// <param name="dpiy">The DPI in the y direction.</param>
		/// <param name="zoomPercentage">The zoom percentage.</param>
		private void Init(int dpix, int dpiy, float zoomPercentage)
		{
			if (m_cactInitGraphics == 0)
			{
				// We are asking for a VwGraphics but haven't been given a DC. Make one.
				// (ReleaseHdc is called in Uninit!)
				// TODO: we might want a graphics appropriate for our printer.
				m_graphics = m_parent.CreateGraphics();
				if (m_vwGraphics == null)
				{
					m_vwGraphics = VwGraphicsWin32Class.Create();
				}
				if (m_graphics != null)
				{
					if (dpix <= 0)
					{
						dpix = (int)(m_graphics.DpiX * zoomPercentage);
					}
					if (dpiy <= 0)
					{
						dpiy = (int)(m_graphics.DpiY * zoomPercentage);
					}
					var hdc = m_graphics.GetHdc();
					m_vwGraphics.Initialize(hdc);
				}
				else
				{
					// we think m_graphics should never be null. But it has happened (see e.g. LTB-708).
					// So provide a desperate fall-back.
					if (dpix <= 0)
					{
						dpix = 96;
					}
					if (dpiy <= 0)
					{
						dpiy = 96;
					}
					Logger.WriteEvent($"WARNING: failed to create m_graphics in GraphicsManager.Init({dpix}, {dpiy}, {zoomPercentage})");
				}
				m_vwGraphics.XUnitsPerInch = dpix;
				m_vwGraphics.YUnitsPerInch = dpiy;
			}
			m_cactInitGraphics++;
		}

		/// <summary>
		/// Uninitialize the graphics object by releasing the DC.
		/// </summary>
		public void Uninit()
		{
			if (m_cactInitGraphics > 0)
			{
				Debug.Assert(m_vwGraphics != null);
				Debug.Assert(m_graphics != null);
				m_cactInitGraphics--;
				// JohnT: Should be redundant to test for m_graphics null, but see comments above
				// and LTB-708.
				if (m_cactInitGraphics == 0 && m_vwGraphics != null && m_graphics != null)
				{
					// We have released as often as we init'd. The device context must have been
					// made in InitGraphics. Release it.
					var hdc = m_vwGraphics.GetDeviceContext();
					m_vwGraphics.ReleaseDC();
					if (hdc != IntPtr.Zero)
					{
						m_graphics.ReleaseHdc(hdc);
					}
					m_graphics.Dispose();
					m_graphics = null;
				}
			}
		}
	}
}