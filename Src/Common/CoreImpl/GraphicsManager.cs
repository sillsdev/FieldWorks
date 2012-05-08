// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: GraphicsManager.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Used to keep track of references to a VwGraphics object. Also handles the setting of the
	/// HDC in the VwGraphics.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification="m_parent is a reference")]
	public class GraphicsManager : IFWDisposable
	{
		private volatile int m_cactInitGraphics = 0;
		private Graphics m_graphics;
		/// <summary></summary>
		protected IVwGraphicsWin32 m_vwGraphics;
		private Control m_parent;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:GraphicsManager"/> class.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// ------------------------------------------------------------------------------------
		public GraphicsManager(Control parent)
		{
			if (parent == null)
				throw new ArgumentNullException("parent");
			m_parent = parent;
		}

		#region Disposed stuff

		private bool m_fDisposed;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the public property for knowing if the object has been disposed of yet
		/// </summary>
		/// <remarks>This property is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed
		{
			get { return m_fDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// GraphicsManager is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~GraphicsManager()
		{
			Dispose(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this before doing anything else.
		/// </summary>
		/// <remarks>This method is thread safe.</remarks>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(
					string.Format("'{0}' in use after being disposed.", GetType().Name));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + "******************");
			if (disposing)
			{
				Debug.Assert(m_cactInitGraphics == 0, "We should release the HDC before disposing");
				if (m_graphics != null)
					m_graphics.Dispose();
			}

			m_graphics = null;
			m_vwGraphics = null;
			m_parent = null;
			m_fDisposed = true;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the VwGraphics object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwGraphics VwGraphics
		{
			get { CheckDisposed(); return m_vwGraphics; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the graphics object has a DC. If it already has, increment a count,
		/// so we know when to really free the DC.
		/// </summary>
		/// <param name="zoom">The zoom percentage.</param>
		/// ------------------------------------------------------------------------------------
		public void Init(float zoom)
		{
			Init(0, 0, zoom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the graphics object has a DC. If it already has, increment a count,
		/// so we know when to really free the DC.
		/// </summary>
		/// <param name="dpix">The DPI in the x direction.</param>
		/// <param name="dpiy">The DPI in the y direction.</param>
		/// ------------------------------------------------------------------------------------
		public void Init(int dpix, int dpiy)
		{
			Init(dpix, dpiy, 1.0f);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the graphics object has a DC. If it already has, increment a count,
		/// so we know when to really free the DC.
		/// </summary>
		/// <param name="dpix">The DPI in the x direction.</param>
		/// <param name="dpiy">The DPI in the y direction.</param>
		/// <param name="zoom">The zoom percentage.</param>
		/// ------------------------------------------------------------------------------------
		private void Init(int dpix, int dpiy, float zoom)
		{
			CheckDisposed();

			if (m_cactInitGraphics == 0)
			{
				// We are asking for a VwGraphics but haven't been given a DC. Make one.
				// (ReleaseHdc is called in Uninit!)
				// TODO: we might want a graphics appropriate for our printer.
				m_graphics = m_parent.CreateGraphics();
				if (m_vwGraphics == null)
					m_vwGraphics = VwGraphicsWin32Class.Create();

				if (m_graphics != null)
				{
					if (dpix <= 0)
						dpix = (int) (m_graphics.DpiX*zoom);
					if (dpiy <= 0)
						dpiy = (int) (m_graphics.DpiY*zoom);
					IntPtr hdc = m_graphics.GetHdc();
					m_vwGraphics.Initialize(hdc);
				}
				else
				{
					// we think m_graphics should never be null. But it has happened (see e.g. LTB-708).
					// So provide a desperate fall-back.
					if (dpix <= 0)
						dpix = 96;
					if (dpiy <= 0)
						dpiy = 96;
					SIL.Utils.Logger.WriteEvent(String.Format("WARNING: failed to create m_graphics in GraphicsManager.Init({0}, {1}, {2})", dpix, dpiy, zoom));
				}
				m_vwGraphics.XUnitsPerInch = dpix;
				m_vwGraphics.YUnitsPerInch = dpiy;
			}
			m_cactInitGraphics++;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Uninitialize the graphics object by releasing the DC.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void Uninit()
		{
			CheckDisposed();

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
					IntPtr hdc = m_vwGraphics.GetDeviceContext();
					m_vwGraphics.ReleaseDC();
					if (hdc != IntPtr.Zero)
						m_graphics.ReleaseHdc(hdc);

					m_graphics.Dispose();
					m_graphics = null;
				}
			}
		}
	}
}
