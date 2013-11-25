// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataZip.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace SIL.FieldWorks.Common.Utils
{
	/// <summary>
	/// Summary description for DataZip.
	/// </summary>
	public class DataZip
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The <see cref="DataZip"/> class doesn't need to be constructed, dude!
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private DataZip()
		{
		}

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unzip a byte array and return unpacked data
		/// </summary>
		/// <param name="data">Byte array containing data to be unzipped</param>
		/// <returns>unpacked data</returns>
		/// ------------------------------------------------------------------------------------
		public static byte[] UnpackData(byte[] data)
		{
			if (data == null)
				return null;
			try
			{
				using (MemoryStream memStream = new MemoryStream(data))
				{
					using (var zipStream = new ZipInputStream(memStream))
					{
						zipStream.GetNextEntry();
						using (var streamWriter = new MemoryStream())
						{
							int size = 2048;
							byte[] dat = new byte[2048];
							while (true)
							{
								size = zipStream.Read(dat, 0, dat.Length);
								if (size > 0)
								{
									streamWriter.Write(dat, 0, size);
								}
								else
								{
									break;
								}
							}
							streamWriter.Close();
							zipStream.CloseEntry();
							zipStream.Close();
							memStream.Close();
							return streamWriter.ToArray();
						}
					}
				}
			}
			catch(Exception e)
			{
				System.Console.Error.WriteLine("Got exception: {0} while unpacking data.",
					e.Message);
				throw;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Zip a byte array and return packed data
		/// </summary>
		/// <param name="data">Byte array containing data to be zipped</param>
		/// <returns>packed data</returns>
		/// ------------------------------------------------------------------------------------
		public static byte[] PackData(byte[] data)
		{
			if (data == null)
				return null;
			try
			{
				using (var memStream = new MemoryStream())
				{
					using (var zipStream = new ZipOutputStream(memStream))
					{
						zipStream.PutNextEntry(new ZipEntry("packeddata"));
						using (var streamReader = new MemoryStream(data))
						{
							int size = 2048;
							byte[] dat = new byte[2048];
							while (true)
							{
								size = streamReader.Read(dat, 0, dat.Length);
								if (size > 0)
								{
									zipStream.Write(dat, 0, size);
								}
								else
								{
									break;
								}
							}
							streamReader.Close();
							zipStream.CloseEntry();
							zipStream.Close();
							memStream.Close();
							return memStream.ToArray();
						}
					}
				}
			}
			catch(Exception e)
			{
				System.Console.Error.WriteLine("Got exception: {0} while packing data.",
					e.Message);
				throw;
			}
		}
		#endregion

	}
}
