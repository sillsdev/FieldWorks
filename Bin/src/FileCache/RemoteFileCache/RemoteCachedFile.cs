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
// File: RemoteCachedFile.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SIL.FieldWorks.Tools.FileCache
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extends the regular cached file by a file stream that allows copying the file over the
	/// network.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class RemoteCachedFile: CachedFile
	{
		/// <summary>File stream for remote file cache</summary>
		private FileStream m_stream;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:CachedFile"/> class.
		/// </summary>
		/// <param name="originalName">Name of the original.</param>
		/// <param name="cachedFile">The cached file.</param>
		/// <param name="stream">The stream.</param>
		/// ------------------------------------------------------------------------------------
		internal RemoteCachedFile(string originalName, string cachedFile, FileStream stream)
			: base(originalName, cachedFile)
		{
			m_stream = stream;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies cached file to specified directory and gives it the original name.
		/// </summary>
		/// <param name="dir">The target directory.</param>
		/// ------------------------------------------------------------------------------------
		public override void CopyTo(string dir)
		{
			string target = Path.Combine(dir, m_OriginalName);
			if (m_stream != null)
			{
				// copy from stream since it is stored on remote file cache
				FileStream writer = new FileStream(target, FileMode.Create);
				byte[] buffer = new byte[4096];

				for (int nCount = m_stream.Read(buffer, 0, 4096); nCount > 0;
					nCount = m_stream.Read(buffer, 0, 4096))
				{
					writer.Write(buffer, 0, nCount);
				}
				writer.Close();
				m_stream.Close();
			}
			else
				base.CopyTo(dir);
		}
	}
}
