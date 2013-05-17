// --------------------------------------------------------------------------------------------
// Copyright (C) 2010 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: fdoCache.cs
// Responsibility: Randy Regnier
// --------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.IOC;
using SIL.Utils;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of the IFWDisposable interface pattern for FdoCache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public sealed partial class FdoCache : IFWDisposable
	{
		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed;
		private bool m_fBeingDisposed;

		/// <summary>
		/// Occurs when the FDO cached is getting disposed. This event is called at the beginning
		/// of the Dispose(bool) method.
		/// </summary>
		public event EventHandler Disposing;

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FdoCache()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing && !m_fBeingDisposed)
			{
				// JohnT: not game to try setting m_isDisposed this early, but we are in danger of getting a recursive
				// call when disposing the service manager, at least. Seems safest to set a flag so we never do this
				// stuff twice, even if we are called recursively (or repeatedly, but we already guard against that).
				m_fBeingDisposed = true;

				RaiseDisposing();

				// No, since it's a C# implementation.
				//if (m_viewsCache != null)
				//{
				//    Marshal.ReleaseComObject(m_viewsCache);
				//}
				m_objectsBeingDeleted.Clear();
				CustomProperties.Clear();

				if (m_serviceLocator != null)
				{
					m_serviceLocator.WritingSystemManager.Save();

					ILgCharacterPropertyEngine cpe = m_serviceLocator.UnicodeCharProps;
					if (cpe != null)
						Marshal.ReleaseComObject(cpe);
				}

				var tsf = TsStrFactory;
				if (tsf != null)
				{
					// tsf is a global singleton,
					// so just do a simple release here.
					Marshal.ReleaseComObject(tsf);
				}

				// Do NOT do this! It's disposable but often a static from FieldWorks, we are NOT responsible to dispose it.
				//if (m_threadHelper != null)
				//    m_threadHelper.Dispose();

				// NOTE: this needs to be last since it calls FdoCache.Dispose() which
				// sets all member variables to null.
				// This will also dispose all Singletons which includes m_serviceLocator.GetInstance<IDataSetup>()
				var serviceLocatorWrapper = m_serviceLocator as StructureMapServiceLocatorWrapper;
				if (serviceLocatorWrapper != null)
					serviceLocatorWrapper.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			// Main data members.
			m_lgwsFactory = null;
			m_serviceLocator = null;
			m_threadHelper = null;

			m_isDisposed = true;
		}

		private void RaiseDisposing()
		{
			EventHandler handler = Disposing;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}
	}
}
