// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DummyFileMaker.cs
// Responsibility: Edge
//
// <remarks>
// Creates and deletes dummy files for testing.
// </remarks>

using System;
using System.IO;
using System.Text;
using SIL.Utils;

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
			if (!string.IsNullOrEmpty(fileName))
			{
				m_fileName = fileName;

				if (!FileUtils.FileExists(fileName))
				{
					string dirName = Path.GetDirectoryName(m_fileName);
					if (!Directory.Exists(dirName))
						Directory.CreateDirectory(Path.GetDirectoryName(m_fileName));
					TextWriter stream = FileUtils.OpenFileForWrite(fileName, Encoding.ASCII);
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
		private bool m_isDisposed;

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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (!string.IsNullOrEmpty(m_fileName))
				{
					if (FileUtils.FileExists(m_fileName))
						FileUtils.Delete(m_fileName);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fileName = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
}
