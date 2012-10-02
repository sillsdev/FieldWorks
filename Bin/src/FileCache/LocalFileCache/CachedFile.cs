// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CachedFile.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores information about the original file name and the location it is stored in the
	/// file cache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class CachedFile
	{
		/// <summary>The name of the original file</summary>
		protected string m_OriginalName;
		/// <summary>The name of the cached file</summary>
		protected string m_CachedFile;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CachedFile"/> class.
		/// </summary>
		/// <param name="originalName">Name of the original file.</param>
		/// <param name="cachedFile">Name of the cached file.</param>
		/// ------------------------------------------------------------------------------------
		internal protected CachedFile(string originalName, string cachedFile)
		{
			m_OriginalName = Path.GetFileName(originalName);
			m_CachedFile = Path.GetFileName(cachedFile);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies cached file to specified directory and gives it the original name. The
		/// creation date is set to the current time.
		/// </summary>
		/// <param name="dir">The target directory.</param>
		/// ------------------------------------------------------------------------------------
		public virtual void CopyTo(string dir)
		{
			string newFile = Path.Combine(dir, m_OriginalName);
			using (BinaryReader reader = new BinaryReader(new FileStream(CachedFileName, FileMode.Open)))
			{
				using (FileStream writer = new FileStream(newFile, FileMode.Create))
				{
					const int kBufSize = 4096; // internal stream buffer size is 4k
					byte[] buffer = new byte[kBufSize];
					for (int nBytes = reader.Read(buffer, 0, kBufSize);
						nBytes > 0;
						nBytes = reader.Read(buffer, 0, kBufSize))
					{
						writer.Write(buffer, 0, nBytes);
					}
					writer.Close();
				}
				reader.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name (without path) of the original file.
		/// </summary>
		/// <value>The name of the original.</value>
		/// ------------------------------------------------------------------------------------
		public string OriginalName
		{
			get { return m_OriginalName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name (and path) of the cached file.
		/// </summary>
		/// <value>The name of the cached file.</value>
		/// ------------------------------------------------------------------------------------
		public string CachedFileName
		{
			get { return FileManager.GetPathFromFileName(m_CachedFile); }
		}
	}
}
