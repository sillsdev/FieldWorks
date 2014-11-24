// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics.CodeAnalysis;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Logs stuff to a file created in
	/// c:\Documents and Settings\Username\Local Settings\Temp\Companyname\Productname\Log.txt
	/// </summary>
	/// <remarks>This class also has a rudimentary implementation of IStream. The only really
	/// implemented method is IStream.Write, the other methods of this interface are no-ops
	/// or throw a NotImplementedException.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class Logger : IFWDisposable, IStream
	{
		#region Data members
		/// <summary></summary>
		private static Logger s_theOne;
		/// <summary></summary>
		protected IndentedTextWriter m_out;
		private readonly string m_logPrefix;
		#endregion

		#region (private) constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Logger"/> class.
		/// </summary>
		/// <param name="logPrefix">Prefix of the log file ('_log.txt' will be appended
		/// to this value).</param>
		/// ------------------------------------------------------------------------------------
		private Logger(string logPrefix)
		{
			m_logPrefix = logPrefix;
			InitializeTextWriter();
			WriteEventInternal("Logger started");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes (or reinitializes) the text writer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeTextWriter()
		{
			try
			{
				StreamWriter writer = (m_out == null) ? File.CreateText(LogPath) :
					File.AppendText(LogPath);
				m_out = new IndentedTextWriter(writer, "        ");
				m_out.Indent = 0;
			}
			catch
			{
				// If the output file can not be created then just disable logging.
				s_theOne = null;
			}
		}
		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~Logger()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
		/// <c>false</c> to release only unmanaged resources.</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "InnerWriter is a reference")]
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				WriteEvent("Logger shutting down");
				// Dispose managed resources here.
				if (m_out != null)
				{
					m_out.InnerWriter.Dispose();
					m_out.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_out = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Static methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a logger suitable to pass as a (log) stream to COM methods.
		/// </summary>
		/// <value>The stream.</value>
		/// ------------------------------------------------------------------------------------
		public static IStream Stream
		{
			get { return s_theOne; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the logger. The logging functions can't be used until this method is
		/// called.
		/// </summary>
		/// <param name="projectName">Name of the project that can be used (and certainly will
		/// be) to generate a unique log file name.</param>
		/// ------------------------------------------------------------------------------------
		public static void Init(string projectName)
		{
			if (s_theOne != null)
				ShutDown();
			s_theOne = new Logger(projectName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shut down the logger. The logging functions can't be used after this method is
		/// called.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ShutDown()
		{
			if (s_theOne != null)
			{
				s_theOne.Dispose();
				s_theOne = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the entire text of the log file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string LogText
		{
			get
			{
				if (s_theOne == null)
					return "No log available.";

				string logText = s_theOne.GetLogText();
				s_theOne.InitializeTextWriter();
				return logText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the log text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string GetLogText()
		{
			CheckDisposed();
			if (m_out != null)
				m_out.Close();

			// get the old data from the file
			try
			{
				using (StreamReader reader = File.OpenText(LogPath))
					return reader.ReadToEnd();
			}
			catch
			{
				// Possibly the file is locked
			}
			return string.Empty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the log path.
		/// </summary>
		/// <value>The log path.</value>
		/// ------------------------------------------------------------------------------------
		private string LogPath
		{
			get
			{
				string path = Path.Combine(Path.GetTempPath(),
					Path.Combine(Application.CompanyName, Application.ProductName));
				Directory.CreateDirectory(path);
				path = Path.Combine(path, m_logPrefix + "_Log.txt");
				return path;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes an event to the logger. This method will do nothing if Init() is not called
		/// first.
		/// </summary>
		/// <param name="message"></param>
		/// ------------------------------------------------------------------------------------
		public static void WriteEvent(string message)
		{
			Debug.WriteLine(message);
			if (s_theOne != null)
				s_theOne.WriteEventInternal(message);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes an exception and its stack trace to the log. This method will do nothing if
		/// Init() is not called first.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void WriteError(Exception e)
		{
			WriteError(null, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes <paramref name="msg"/> and an exception and its stack trace to the log.
		/// This method will do nothing if Init() is not called first.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void WriteError(string msg, Exception e)
		{
			Exception dummy;
			var bldr = new StringBuilder(msg);
			if (bldr.Length > 0)
				bldr.AppendLine();
			bldr.Append(ExceptionHelper.GetHiearchicalExceptionInfo(e, out dummy));
			Debug.WriteLine(bldr.ToString());

			if (s_theOne != null)
				s_theOne.WriteEventInternal(bldr.ToString());
		}
		#endregion

		#region IStream Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new stream object with its own seek pointer that references the same
		/// bytes as the original stream.
		/// </summary>
		/// <param name="ppstm">When this method returns, contains the new stream object.
		/// This parameter is passed uninitialized.</param>
		/// ------------------------------------------------------------------------------------
		public void Clone(out IStream ppstm)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that any changes made to a stream object that is open in transacted mode are
		/// reflected in the parent storage.
		/// </summary>
		/// <param name="grfCommitFlags">A value that controls how the changes for the stream
		/// object are committed.</param>
		/// ------------------------------------------------------------------------------------
		public void Commit(int grfCommitFlags)
		{
			// no-op
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies a specified number of bytes from the current seek pointer in the stream to
		/// the current seek pointer in another stream.
		/// </summary>
		/// <param name="pstm">A reference to the destination stream.</param>
		/// <param name="cb">The number of bytes to copy from the source stream.</param>
		/// <param name="pcbRead">On successful return, contains the actual number of bytes
		/// read from the source.</param>
		/// <param name="pcbWritten">On successful return, contains the actual number of bytes
		/// written to the destination.</param>
		/// ------------------------------------------------------------------------------------
		public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restricts access to a specified range of bytes in the stream.
		/// </summary>
		/// <param name="libOffset">The byte offset for the beginning of the range.</param>
		/// <param name="cb">The length of the range, in bytes, to restrict.</param>
		/// <param name="dwLockType">The requested restrictions on accessing the range.</param>
		/// ------------------------------------------------------------------------------------
		public void LockRegion(long libOffset, long cb, int dwLockType)
		{
			// no-op
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads a specified number of bytes from the stream object into memory starting at
		/// the current seek pointer.
		/// </summary>
		/// <param name="pv">When this method returns, contains the data read from the stream.
		/// This parameter is passed uninitialized.</param>
		/// <param name="cb">The number of bytes to read from the stream object.</param>
		/// <param name="pcbRead">A pointer to a ULONG variable that receives the actual number
		/// of bytes read from the stream object.</param>
		/// ------------------------------------------------------------------------------------
		public void Read(byte[] pv, int cb, IntPtr pcbRead)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Discards all changes that have been made to a transacted stream since the last
		/// <see cref="M:System.Runtime.InteropServices.ComTypes.IStream.Commit(System.Int32)"/>
		/// call.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Revert()
		{
			// no-op
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the seek pointer to a new location relative to the beginning of the stream,
		/// to the end of the stream, or to the current seek pointer.
		/// </summary>
		/// <param name="dlibMove">The displacement to add to dwOrigin.</param>
		/// <param name="dwOrigin">The origin of the seek. The origin can be the beginning of
		/// the file, the current seek pointer, or the end of the file.</param>
		/// <param name="plibNewPosition">On successful return, contains the offset of the seek
		/// pointer from the beginning of the stream.</param>
		/// ------------------------------------------------------------------------------------
		public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition)
		{
			// no-op
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes the size of the stream object.
		/// </summary>
		/// <param name="libNewSize">The new size of the stream as a number of bytes.</param>
		/// ------------------------------------------------------------------------------------
		public void SetSize(long libNewSize)
		{
			// no-op
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the <see cref="T:System.Runtime.InteropServices.STATSTG"></see> structure
		/// for this stream.
		/// </summary>
		/// <param name="pstatstg">When this method returns, contains a STATSTG structure that
		/// describes this stream object. This parameter is passed uninitialized.</param>
		/// <param name="grfStatFlag">Members in the STATSTG structure that this method does not
		/// return, thus saving some memory allocation operations.</param>
		/// ------------------------------------------------------------------------------------
		public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg,
			int grfStatFlag)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the access restriction on a range of bytes previously restricted with the
		/// <see cref="M:System.Runtime.InteropServices.ComTypes.IStream.LockRegion(
		/// System.Int64,System.Int64,System.Int32)"></see> method.
		/// </summary>
		/// <param name="libOffset">The byte offset for the beginning of the range.</param>
		/// <param name="cb">The length, in bytes, of the range to restrict.</param>
		/// <param name="dwLockType">The access restrictions previously placed on the range.</param>
		/// ------------------------------------------------------------------------------------
		public void UnlockRegion(long libOffset, long cb, int dwLockType)
		{
			// no-op
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes a specified number of bytes into the stream object starting at the current
		/// seek pointer.
		/// </summary>
		/// <param name="pv">The buffer to write this stream to.</param>
		/// <param name="cb">The number of bytes to write to the stream.</param>
		/// <param name="pcbWritten">On successful return, contains the actual number of bytes
		/// written to the stream object. If the caller sets this pointer to null, this method
		/// does not provide the actual number of bytes written.</param>
		/// ------------------------------------------------------------------------------------
		public void Write(byte[] pv, int cb, IntPtr pcbWritten)
		{
			try
			{
				string message = new string(Array.ConvertAll(pv,
					new Converter<byte, char>(ConvertByteToChar)));

				// Strip leading and trailing new lines since they don't fit with our logging
				// format
				WriteEventInternal(message.Trim('\r', '\n'));

				if (pcbWritten != IntPtr.Zero)
					Marshal.WriteInt32(pcbWritten, pv.Length);
			}
			catch (Exception e)
			{
				// just ignore any errors - we shouldn't present the user with a green screen
				// if the logging fails
				WriteEventInternal("IStream.Write failed: " + e.Message);
			}
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the event to the logger.
		/// </summary>
		/// <param name="message">The message.</param>
		/// ------------------------------------------------------------------------------------
		private void WriteEventInternal(string message)
		{
			CheckDisposed();
			if (m_out != null)
			{
				using (var process = Process.GetCurrentProcess())
				{
					IntPtr mainWndHandle = process.MainWindowHandle;
					int handleInfo = mainWndHandle != IntPtr.Zero ? mainWndHandle.ToInt32() : 0;
					// We don't want to indent the date...
					m_out.Write(string.Format("{0} [0x{1:x}]\t", Now, handleInfo));

					// ...but we want to indent all following lines (note: the first (current) line
					// won't be indented)
					m_out.Indent++;
					if (message != null)
					{
						string[] lines = message.Split(new string[] { Environment.NewLine, "\n" },
						StringSplitOptions.None);
						foreach (string line in lines)
							m_out.WriteLine(line);
					}
					else
					{
						m_out.WriteLine("Unknown event");
					}

					m_out.Indent--;
					m_out.Flush(); //in case we crash
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current time as string in a standardized format (13:47:00.08).
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string Now
		{
			get
			{
				return DateTime.Now.ToString(@"HH\:mm\:ss.ff");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts the byte to char.
		/// </summary>
		/// <param name="b">The b.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static char ConvertByteToChar(byte b)
		{
			return (char)b;
		}
		#endregion
	}
}
