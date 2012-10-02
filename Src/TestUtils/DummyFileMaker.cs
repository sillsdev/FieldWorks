// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyFileMaker.cs
// Responsibility: Edge
//
// <remarks>
// Creates and deletes dummy files for testing.
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for DummyFileMaker.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFileMaker : IFWDisposable
	{
		private string m_fileName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyFileMaker"/> class.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="fUseTempPath"></param>
		/// ------------------------------------------------------------------------------------
		public DummyFileMaker(string fileName, bool fUseTempPath) :
			this((fUseTempPath)? Path.Combine(Path.GetTempPath(), fileName) : fileName)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyFileMaker"/> class.
		/// </summary>
		/// <param name="fileName">Full path of the dummy file</param>
		/// ------------------------------------------------------------------------------------
		public DummyFileMaker(string fileName)
		{
			if (fileName != null && fileName != string.Empty)
			{
				m_fileName = fileName;

				if (!File.Exists(fileName))
				{
					FileStream stream = File.Create(fileName);
					if (stream != null)
						stream.Close();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the filename (including absolute or relative path).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Filename
		{
			get
			{
				CheckDisposed();

				return m_fileName;
			}
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

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
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~DummyFileMaker()
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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_fileName != null && m_fileName != string.Empty)
				{
					if (File.Exists(m_fileName))
						File.Delete(m_fileName);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fileName = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
}
