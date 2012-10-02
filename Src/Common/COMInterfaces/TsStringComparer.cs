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
// File: TsStringComparer.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Compare two ITsStrings.
	/// </summary>
	/// <remarks>This class does not check the writing systems of the strings to compare but
	/// takes the ICU locale passed in to the constructor to create a collation engine.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class TsStringComparer : IComparer, IFWDisposable, IIcuCleanupCallback
	{
		private ILgCollatingEngine m_collatingEngine;
		private bool m_fCollatingEngineIsOpen;
		private readonly string m_icuLocale;
		private IIcuCleanupManager m_IcuCleanupManager;

		#region Constructors

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsStringComparer"/> class.
		/// </summary>
		/// <remarks>This version of the constructor uses .NET to compare two ITsStrings.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public TsStringComparer()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TsStringComparer"/> class.
		/// </summary>
		/// <param name="icuLocale">The icu locale that is used to create a collating engine.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public TsStringComparer(string icuLocale)
		{
			m_collatingEngine = LgIcuCollatorClass.Create();
			m_IcuCleanupManager = IcuCleanupManagerClass.Create();
			m_IcuCleanupManager.RegisterCleanupCallback(this);
			m_icuLocale = icuLocale;
		}
		#endregion

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
		/// TsStringComparer is reclaimed by garbage collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		~TsStringComparer()
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
		protected void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (m_collatingEngine != null && m_fCollatingEngineIsOpen)
					m_collatingEngine.Close();

				if (m_IcuCleanupManager != null)
					m_IcuCleanupManager.UnregisterCleanupCallback(this);
			}

			m_collatingEngine = null;
			m_IcuCleanupManager = null;

			m_fDisposed = true;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the collating engine.
		/// </summary>
		/// <value>The collating engine.</value>
		/// ------------------------------------------------------------------------------------
		private ILgCollatingEngine CollatingEngine
		{
			get
			{
				if (m_collatingEngine == null)
					return null;

				if (!m_fCollatingEngineIsOpen)
				{
					m_collatingEngine.Open(IcuLocale);
					m_fCollatingEngineIsOpen = true;
				}

				return m_collatingEngine;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the icu locale.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string IcuLocale
		{
			get { return m_icuLocale; }
		}

		#region IComparer Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two objects and returns a value indicating whether one is less than, equal
		/// to, or greater than the other.
		/// </summary>
		/// <param name="x">The first object to compare.</param>
		/// <param name="y">The second object to compare.</param>
		/// <returns>
		/// Value Condition
		/// Less than zero = x is less than y.
		/// Zero = x equals y.
		/// Greater than zero = x is greater than y.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">Neither x nor y implements the
		/// <see cref="T:System.IComparable"/> interface.-or- x and y are of different types and
		/// neither one can handle comparisons with the other. </exception>
		/// ------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			if ((!(x is ITsString || x is string) || !(y is ITsString || y is string)) &&
				x != null && y != null)
			{
				throw new ArgumentException();
			}

			string xString = (x is ITsString) ? ((ITsString)x).Text : x as string;
			string yString = (y is ITsString) ? ((ITsString)y).Text : y as string;
			if (xString == string.Empty)
				xString = null;
			if (yString == string.Empty)
				yString = null;

			if (xString == null && yString == null)
				return 0;

			if (xString == null)
				return -1;

			if (yString == null)
				return 1;

			int ret;
			if (CollatingEngine != null)
				ret = CollatingEngine.Compare(xString, yString, LgCollatingOptions.fcoDefault);
			else
				ret = xString.CompareTo(yString);
			return ret;
		}

		#endregion

		#region IIcuCleanupCallback Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Informs the implementor that a cleanup has been performed. The receiver should NOT
		/// recreate the object immediately, as that will invalidate the purpose of the cleanup.
		/// It should only be recreated when actually needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IIcuCleanupCallback.DoneCleanup()
		{
			// In case we have a handle open we want to close it now (and reopen it later when
			// we need it). If we don't re-open it later we'd access invalid memory.
			if (m_collatingEngine != null && m_fCollatingEngineIsOpen)
				m_collatingEngine.Close();
			m_fCollatingEngineIsOpen = false;
		}

		#endregion
	}
}
