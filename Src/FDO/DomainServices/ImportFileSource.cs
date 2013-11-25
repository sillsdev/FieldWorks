// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportFileSource.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FDO.DomainServices
{
	#region class ImportFileSource
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to enumerate the files for an import
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ImportFileSource : IEnumerable
	{
		#region Data members
		private FdoCache m_cache;
		private ScrSfFileList m_fileList;
		private Hashtable m_sourceTable;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor based on a ScrSfFileList
		/// </summary>
		/// <param name="fileList">The list of files</param>
		/// ------------------------------------------------------------------------------------
		public ImportFileSource(ScrSfFileList fileList)
		{
			Debug.Assert(fileList != null);
			m_cache = null;
			m_sourceTable = null;
			m_fileList = fileList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor based on a hashtable which maps HVOs of ScrImportSource objects to
		/// ScrSfFileList objects
		/// </summary>
		/// <param name="sourceTable">The hashtable</param>
		/// <param name="cache">The FDO cache needed for interpreting the HVOs</param>
		/// ------------------------------------------------------------------------------------
		public ImportFileSource(Hashtable sourceTable, FdoCache cache)
		{
			Debug.Assert(sourceTable != null);
			m_cache = cache;
			m_sourceTable = sourceTable;
			m_fileList = null;
		}

		#region public properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the count of the number of files in the source
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Count
		{
			get
			{
				if (m_fileList != null)
					return m_fileList.Count;

				int count = 0;
				foreach (ScrSfFileList list in m_sourceTable.Values)
					count += list.Count;
				return count;
			}
		}
		#endregion

		#region IEnumerable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the enumerator
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IEnumerator GetEnumerator()
		{
			return new ImportFileEnumerator(m_cache, m_fileList, m_sourceTable);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The enumerator used to implement GetEnumerator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class ImportFileEnumerator : IEnumerator
		{
			private IDictionaryEnumerator m_sourceEnumerator;
			private IEnumerator m_fileEnumerator;

			public ImportFileEnumerator(FdoCache cache, ScrSfFileList fileList, Hashtable sourceTable)
			{
				if (sourceTable == null)
				{
					m_fileEnumerator = fileList.GetEnumerator();
					m_sourceEnumerator = null;
				}
				else
				{
					m_fileEnumerator = null;
					m_sourceEnumerator = sourceTable.GetEnumerator();
				}
				Reset();
			}

			#region IEnumerator Members
			/// ------------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public void Reset()
			{
				if (m_sourceEnumerator == null)
				{
					m_fileEnumerator.Reset();
				}
				else
				{
					m_sourceEnumerator.Reset();
					m_fileEnumerator = null;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Get the current IScrImportFileInfo.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public object Current
			{
				get { return m_fileEnumerator.Current; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Move to the next IScrImportFileInfo
			/// </summary>
			/// <returns><c>true</c> if the enumerator was successfully advanced to the next
			/// element; <c>false</c> if the enumerator has passed the end of the collection.
			/// </returns>
			/// ------------------------------------------------------------------------------------
			public bool MoveNext()
			{
				// If there is another file in the file enumerator then we're fine
				if (m_fileEnumerator != null && m_fileEnumerator.MoveNext())
					return true;

				// If there is no source enumerator, then we're done
				if (m_sourceEnumerator == null)
					return false;
				do
				{
					// Move to the next source. If there are no more then quit.
					if (!m_sourceEnumerator.MoveNext())
						return false;
					// Get a new file enumerator from the source
					DictionaryEntry entry = (DictionaryEntry)m_sourceEnumerator.Current;
					m_fileEnumerator = ((ScrSfFileList)entry.Value).GetEnumerator();
				}
				while (!m_fileEnumerator.MoveNext());
				return true;
			}
			#endregion
		}
	}
	#endregion
}
