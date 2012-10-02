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
// File: TempSFFileMaker.cs
// Responsibility: TE Team
//
// <remarks>
// Class to manage creation (and retiring) of temp SF files
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TempSFFileMaker.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TempSFFileMaker : IFWDisposable
	{
		#region Data members
		private Set<string> m_files;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TempSFFileMaker"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TempSFFileMaker()
		{
			m_files = new Set<string>();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a temp file containing a single book
		/// </summary>
		/// <param name="sSILBookId">Three-letter SIL code for the book represented by the file
		/// being created</param>
		/// <param name="dataLines">Array of lines of SF text to write following the ID line.
		/// This text should not include line-break characters.</param>
		/// <returns>Name of file that was created</returns>
		/// ------------------------------------------------------------------------------------
		public string CreateFile(string sSILBookId, string[] dataLines)
		{
			CheckDisposed();

			return CreateFile(sSILBookId, dataLines, Encoding.ASCII, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a temp file containing a single book
		/// </summary>
		/// <param name="sSILBookId">Three-letter SIL code for the book represented by the file
		/// being created</param>
		/// <param name="dataLines">Array of lines of SF text to write following the ID line.
		/// This text should not include line-break characters.</param>
		/// <param name="encoding">The character encoding</param>
		/// <param name="fWriteByteOrderMark">Pass <c>true</c> to have the BOM written at the
		/// beginning of a Unicode file (ignored if encoding is ASCII).</param>
		/// <returns>Name of file that was created</returns>
		/// ------------------------------------------------------------------------------------
		public string CreateFile(string sSILBookId, string[] dataLines, Encoding encoding,
			bool fWriteByteOrderMark)
		{
			CheckDisposed();

			if (sSILBookId == null)
				throw new ArgumentNullException("sSILBookId");

			// Create a temporary file.
			string fileName = Path.GetTempFileName();

			BinaryWriter file = new BinaryWriter(new FileStream(fileName, FileMode.Create), encoding);
			m_files.Add(fileName);

			// write the byte order marker
			if (fWriteByteOrderMark && encoding != Encoding.ASCII)
				file.Write('\ufeff');

			// Write the id line to the file, IF it's length is > 0
			if (sSILBookId.Length > 0)
				file.Write(EncodeLine(@"\id " + sSILBookId, encoding));

			// write out the file contents
			if (dataLines != null)
			{
				foreach (string sLine in dataLines)
				{
					file.Write(EncodeLine(sLine, encoding));
				}
			}
			file.Close();

			return fileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a temp file that does not have an ID line.
		/// </summary>
		/// <param name="dataLines">Array of lines of SF text to write.
		/// This text should not include line-break characters.</param>
		/// <returns>Name of file that was created</returns>
		/// ------------------------------------------------------------------------------------
		public string CreateFileNoID(string[] dataLines)
		{
			CheckDisposed();

			// Create a temporary file.
			string fileName = Path.GetTempFileName();

			BinaryWriter file = new BinaryWriter(new FileStream(fileName, FileMode.Create),
				Encoding.ASCII);
			m_files.Add(fileName);

			if (dataLines != null)
			{
				foreach (string sLine in dataLines)
				{
					file.Write(EncodeLine(sLine, Encoding.ASCII));
				}
			}
			file.Close();

			return fileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a temp file that does not have an ID line, but does have a specified
		/// extension.
		/// </summary>
		/// <param name="dataLines">Array of lines of SF text to write.
		/// This text should not include line-break characters.</param>
		/// <returns>Name of file that was created</returns>
		/// ------------------------------------------------------------------------------------
		public string CreateFileNoID(string[] dataLines, string extension)
		{
			CheckDisposed();

			// Create a temporary file.
			string fileName = Path.ChangeExtension(Path.GetTempFileName(), extension);

			BinaryWriter file = new BinaryWriter(new FileStream(fileName, FileMode.Create),
				Encoding.ASCII);
			m_files.Add(fileName);

			if (dataLines != null)
			{
				foreach (string sLine in dataLines)
				{
					file.Write(EncodeLine(sLine, Encoding.ASCII));
				}
			}
			file.Close();

			return fileName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Encodes the characters of the given string according to the specified
		/// <see cref="Encoding"/> and returns the results as a byte array, including a final
		/// CR/LF sequence so the line may be written out to a file.
		/// </summary>
		/// <param name="sLine">The Unicode string containing the characters to be encoded
		/// </param>
		/// <param name="encoding">The type of <see cref="Encoding"/> to be done</param>
		/// <returns>The encoded characters as a byte array, including a final CR/LF sequence
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static byte[] EncodeLine(string sLine, Encoding encoding)
		{
			string sIn = sLine + (char)13 + (char)10;
			byte[] bytes = new byte[sIn.Length * 2];
			int i = 0;
			foreach (char ch in sIn)
			{
				bytes[i++] = (byte) (ch & 0xff);
				bytes[i++] = (byte) (ch >> 8);
			}
			return Encoding.Convert(Encoding.Unicode, encoding, bytes);
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
		~TempSFFileMaker()
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
				if (m_files != null)
				{
					foreach (string sFile in m_files)
					{
						try
						{
							if (File.Exists(sFile))
								File.Delete(sFile);
							if (File.Exists(sFile + ".ec"))
								File.Delete(sFile + ".ec");
						}
						catch
						{
							// Eat the exception.
						}
					}
					m_files.Clear();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_files = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
}
