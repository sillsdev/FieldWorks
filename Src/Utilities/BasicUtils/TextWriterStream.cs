using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;

namespace SIL.Utils
{
	/// <summary>
	/// Implementation of the COM IStream interface that wraps a C# TextWriter object.
	/// This is useful for calling the ITsString.WriteAsXml method from C# code.
	/// </summary>
	public class TextWriterStream : IStream
	{
		private TextWriter m_writer;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="writer"></param>
		public TextWriterStream(TextWriter writer)
		{
			m_writer = writer;
		}
		#region IStream methods
		/// <summary>
		/// Creates a new stream object with its own seek pointer that references the same bytes
		/// as the original stream.
		/// </summary>
		/// <param name="pstm"></param>
		public void Clone(out IStream pstm)
		{
			pstm = null;
		}

		/// <summary>
		/// Ensures that any changes made to a stream object opened in transacted mode are
		/// reflected in the parent storage object. If the stream object is opened in direct
		/// mode, this method has no effect other than flushing all memory buffers to the next
		/// level storage object. The OLE compound file implementation of streams does not
		/// support opening streams in transacted mode.
		/// </summary>
		public void Commit(int grfCommitFlags)
		{
		}

		/// <summary>
		/// Copies a specified number of bytes from the current seek pointer in the stream to the current seek pointer in another stream.
		/// </summary>
		public void CopyTo(IStream pstm, long cb, System.IntPtr pcbRead, System.IntPtr pcbWritten)
		{
		}

		/// <summary>
		/// Restricts access to a specified range of bytes in the stream. Supporting this functionality is optional because some file systems do not provide it.
		/// </summary>
		public void LockRegion(Int64 libOffset, Int64 cb, int dwLockType)
		{
		}

		/// <summary>
		/// Reads a specified number of bytes from the stream object into memory, starting at the current seek pointer.
		/// </summary>
		public void Read(byte[] pv, int cb, System.IntPtr pcbRead)
		{
		}
		/// <summary>
		/// Discards all changes that have been made to a transacted stream since the last IStream::Commit call. This method has no effect on streams open in direct mode and streams using the OLE compound file implementation of IStream::Revert.
		/// </summary>
		public void Revert()
		{
		}

		/// <summary>
		/// Changes the seek pointer to a new location relative to the current seek pointer or the beginning or end of the stream.
		/// </summary>
		public void Seek(long dlibMove, int dwOrigin, System.IntPtr plibNewPosition)
		{
		}

		/// <summary>
		/// Resizes of the stream object.
		/// </summary>
		public void SetSize(long libNewSize)
		{
		}

		/// <summary>
		/// Retrieves the STATSTG structure for this stream object.
		/// </summary>
		public void Stat(out STATSTG pstatstg, int grfStatFlag)
		{
			pstatstg = new STATSTG();
		}

		/// <summary>
		/// Removes the access restriction on a range of bytes previously restricted with IStream::LockRegion.
		/// </summary>
		public void UnlockRegion(long libOffset, long cb, int dwLockType)
		{
		}

		/// <summary>
		/// Writes a specified number of bytes into the stream object starting at the current seek pointer.
		/// </summary>
		public void Write(byte[] pv, int cb, System.IntPtr pcbWritten)
		{
			// Create a UTF-8 encoding, and use that to write out the string.
			// This isn't very efficient, but...
			System.Text.UTF8Encoding utf8 = new System.Text.UTF8Encoding();
			string s = utf8.GetString(pv, 0, cb);
			m_writer.Write(s);
			System.Runtime.InteropServices.Marshal.WriteInt32(pcbWritten, cb);
		}

		#endregion
	}
}
