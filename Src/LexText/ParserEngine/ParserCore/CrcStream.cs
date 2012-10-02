// taken verbatim from http://www.codeproject.com/csharp/crcstream.asp
using System;
using System.IO;
using System.Text;

using SIL.Utils;

namespace CodeProject.ReiMiyasaka
{
	/// <summary>
	/// Encapsulates a <see cref="System.IO.Stream" /> to calculate the CRC32 checksum on-the-fly as data passes through.
	/// </summary>
	internal class CrcStream : Stream, IFWDisposable
	{
		Stream stream;

		internal static uint GetCrc(string sResult)
		{
			UnicodeEncoding enc = new UnicodeEncoding();
			int iCount = enc.GetByteCount(sResult);
			using (CrcStream crc = new CrcStream(iCount))
			{
				crc.Write(enc.GetBytes(sResult), 0, iCount);
				return crc.WriteCrc;
				// Closed in the Dispose of 'crc'.
			}
		}

		/// <summary>
		/// Encapsulate a <see cref="System.IO.Stream" />.
		/// </summary>
		/// <param name="stream">The stream to calculate the checksum for.</param>
		private CrcStream(int iCount)
		{
			this.stream = new MemoryStream(iCount);
		}

		private bool m_isDisposed = false;

		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (stream != null)
					stream.Close();
			}
			stream = null;

			base.Dispose(disposing);
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
		/// Gets the underlying stream.
		/// </summary>
		internal Stream Stream
		{
			get
			{
				CheckDisposed();
				return stream;
			}
		}

		public override bool CanRead
		{
			get
			{
				CheckDisposed();
				return stream.CanRead;
			}
		}

		public override bool CanSeek
		{
			get
			{
				CheckDisposed();
				return stream.CanSeek;
			}
		}

		public override bool CanWrite
		{
			get
			{
				CheckDisposed();
				return stream.CanWrite;
			}
		}

		public override void Flush()
		{
			CheckDisposed();

			stream.Flush();
		}

		public override long Length
		{
			get
			{
				CheckDisposed();
				return stream.Length;
			}
		}

		public override long Position
		{
			get
			{
				CheckDisposed();

				return stream.Position;
			}
			set
			{
				CheckDisposed();

				stream.Position = value;
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			CheckDisposed();

			return stream.Seek(offset, origin);
		}

		public override void SetLength(long value)
		{
			CheckDisposed();

			stream.SetLength(value);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckDisposed();

			count = stream.Read(buffer, offset, count);
			readCrc = CalculateCrc(readCrc, buffer, offset, count);
			return count;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			CheckDisposed();

			stream.Write(buffer, offset, count);

			writeCrc = CalculateCrc(writeCrc, buffer, offset, count);
		}

		private uint CalculateCrc(uint crc, byte[] buffer, int offset, int count)
		{
			unchecked
			{
				for (int i = offset, end = offset + count; i < end; i++)
					crc = (crc >> 8) ^ table[(crc ^ buffer[i]) & 0xFF];
			}
			return crc;
		}

		static private uint[] table = GenerateTable();

		static private uint[] GenerateTable()
		{
			unchecked
			{
				uint[] table = new uint[256];

				uint crc;
				const uint poly = 0xEDB88320;
				for (uint i = 0; i < table.Length; i++)
				{
					crc = i;
					for (int j = 8; j > 0; j--)
					{
						if ((crc & 1) == 1)
							crc = (crc >> 1) ^ poly;
						else
							crc >>= 1;
					}
					table[i] = crc;
				}

				return table;
			}

		}

		private uint readCrc = unchecked(0xFFFFFFFF);

		/// <summary>
		/// Gets the CRC checksum of the data that was read by the stream thus far.
		/// </summary>
		internal uint ReadCrc
		{
			get
			{
				CheckDisposed();
				return unchecked(readCrc ^ 0xFFFFFFFF);
			}
		}

		private uint writeCrc = unchecked(0xFFFFFFFF);

		/// <summary>
		/// Gets the CRC checksum of the data that was written to the stream thus far.
		/// </summary>
		internal uint WriteCrc
		{
			get
			{
				CheckDisposed();
				return unchecked(writeCrc ^ 0xFFFFFFFF);
			}
		}

		/// <summary>
		/// Resets the read and write checksums.
		/// </summary>
		internal void ResetChecksum()
		{
			CheckDisposed();

			readCrc = unchecked(0xFFFFFFFF);
			writeCrc = unchecked(0xFFFFFFFF);
		}
	}
}
