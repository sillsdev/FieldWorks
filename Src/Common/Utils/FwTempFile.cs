// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2002' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwTempFile.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;


namespace SIL.FieldWorks.Common.Utils
{
	/// <summary>
	/// Manages the creation and automatic disposal of the temporary file.
	/// </summary>
	/// <remarks>
	/// Be careful to close all readers on the file before the file is disposed of.
	/// If this is not possible to ensure this (e.g. you give the past two in other process or thread),
	/// then be sure to call Detach()before either disposing of the object before allowing the
	/// object to go out of scope, in which case it will be disposed of when it is garbage collected.
	/// Of course, then the temporary file will not be automatically deleted.
	/// </remarks>
	public class FwTempFile : IFWDisposable
	{
		private string m_path;
		private TextWriter m_writer;
		private bool m_isDetached = false;

		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		public FwTempFile()
		{
			Initialize(null);
		}

		/// <summary>
		/// Provides a temporary file with the specified extension.
		/// </summary>
		/// <param name="extension">Extension with the preceding ".". Example ".htm"</param>
		public FwTempFile(string extension)
		{
			Initialize(extension);
		}


		/// <summary>
		/// Initialize the instance.
		/// </summary>
		/// <param name="extension">Extension with the preceding ".". Example ".htm"</param>
		protected void Initialize(string extension)
		{
			m_path = System.IO.Path.GetTempFileName();
			if (extension != null)
			{
				// I (JohnH) could not figure out how to just rename the file,
				// so I just delete and re-create it.
				System.IO.File.Delete(m_path);
				m_path = System.IO.Path.ChangeExtension(m_path, extension);
				m_writer = File.CreateText(m_path);
			}
			else
				m_writer = File.CreateText(m_path);//OpenText() sounds better, but it gives me the wrong class

		}
		#endregion Construction

		/// <summary>
		/// Call this when you do not want the temporary file deleted when this object is disposed of.
		/// </summary>
		public void Detach()
		{
			CheckDisposed();
			m_isDetached = true;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FwTempFile()
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
				if (m_writer != null)
					m_writer.Close();
				if (!m_isDetached)
				{
					// Leaving the file open without Detach()ing is to be avoided,
					// but it can be beyond the programs reasonable controll (e.g. what if
					// another app opens it or holds it open longer than expected).
					// Therefore, it's not worth making the user think we "crashed".  So just log it in Debug mode.
					try
					{
						File.Delete(m_path);
					}
					catch
					{
						Debug.WriteLine("Warning: Temp file could not be deleted.");
					}
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_path = null;
			m_writer = null;

			m_isDisposed = true;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets the text writer.
		/// </summary>
		public TextWriter Writer
		{
			get
			{
				CheckDisposed();
				return m_writer;
			}
		}

		/// <summary>
		/// Get the pathname to the temporary file.
		/// </summary>
		public string Path
		{
			get
			{
				CheckDisposed();
				return m_path;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Closes the file and returns the full path to the file.
		/// </summary>
		/// <returns>The path to the temporary file.</returns>
		public string CloseAndGetPath()
		{
			CheckDisposed();
			m_writer.Close();
			return m_path;
		}



		/// <summary>
		/// Closes the file and sends its contents to the Console.
		/// </summary>
		public void DumpToConsole()
		{
			CheckDisposed();
			try
			{
				FileStream reader = File.OpenRead(CloseAndGetPath());
				int c = reader.ReadByte();
				while(c > 0)
				{
					Console.Write((char)c);
					c = reader.ReadByte();
				}
				reader.Close();
			}
			catch
			{
				Console.WriteLine("Could not read the temp file.");
			}
		}
		/// <summary>
		/// Create a temporary file; close it; and return the path to it.
		/// </summary>
		/// <param name="extension">File extension to use for the temporary file</param>
		/// <returns>full path to the newly created temporary file</returns>
		public static string CreateTempFileAndGetPath(string extension)
		{
			FwTempFile tmpFile = new FwTempFile(extension);
			return tmpFile.CloseAndGetPath();
		}
		#endregion
	}

	/// <summary>
	/// Creates an HTML temporary file.
	/// </summary>
	public class FwTempHtmlFile : FwTempFile
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public FwTempHtmlFile() : base(".htm")
		{
		}
	}


}
