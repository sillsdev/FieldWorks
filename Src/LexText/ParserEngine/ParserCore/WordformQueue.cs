// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: M3ParserWordformQueue.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// A generic Queue used to hold the hvos of wordformtype as until they are parsed.
	/// </summary>
	internal class M3ParserWordformQueue
	{
		private TraceSwitch lockingSwitch = new TraceSwitch("ParserCore.LockingTrace", "Syncronization tracking", "Off");

		private object m_words_lock = new object();
		private object m_rwords_lock = new object();

		/// <summary>
		/// Method for unlocking an object previously locked by Safe_Lock
		/// </summary>
		/// <param name="lockObject">obrect to pass to Monitor</param>
		private void Safe_Unlock(ref object lockObject)
		{
			Monitor.Exit(lockObject);
		}

		/// <summary>
		/// Method for locking an object used for one of the containers
		/// </summary>
		/// <param name="lockObject">object to use in the Monitor statement</param>
		/// <param name="msg">text to write out if there is a collision</param>
		private void Safe_Lock(ref object lockObject, string msg)
		{
			if (!Monitor.TryEnter(lockObject))
			{
				Trace.WriteLineIf(lockingSwitch.TraceInfo, ">>>> Locking collision - waiting for object to be freed:"+msg, "M3ParserWordformQueue");
				Monitor.Enter(m_words_lock);
			}
		}

		protected Queue<int> m_words;
		// We use this because we cannot remove words out of the middle of the Queue class we are using
		protected Set<int> m_removedWords;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ParserWordformQueue"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ParserWordformQueue()
		{
			// With the use of Generic collections, the locking will now be handled
			// outside of the classes.  Previously it was using the "Synchronized"
			// method on the collections.
			m_words = new Queue<int>();
			m_removedWords = new Set<int>();
		}

		/// <summary>
		/// Add a Wordform to the queue, if it is not already in
		/// </summary>
		/// <param name="hvo">the database ID of the WfiWordform</param>
		internal void EnqueueWordform(int hvo)
		{
			// make sure no one else is working on the removedWords container
			// and then remove the 'hvo' if it is present
			try
			{
				Safe_Lock(ref m_rwords_lock, "EnqueueWordform-remove");
				// Remove the hvo from the remove words list
				m_removedWords.Remove(hvo);
			}
			finally
			{
				Safe_Unlock(ref m_rwords_lock);
			}
			// lastly add the 'hvo' to the queue of words
			Safe_AddWordIfNotPresent(hvo);
		}

		/// <summary>
		/// Private method to add a word if it's not present and handle
		/// all the locking for the container only used vs a general lock
		/// for both containers (which) would be less efficent.
		/// If the word is already present - don't do anything.
		/// No return result required.
		/// </summary>
		/// <param name="hvo"></param>
		private void Safe_AddWordIfNotPresent(int hvo)
		{
			try
			{
				Safe_Lock(ref m_words_lock, "AddWordIfNotPresent");
				if (!m_words.Contains(hvo))
					m_words.Enqueue(hvo);
			}
			finally
			{
				Safe_Unlock(ref m_words_lock);
			}
		}

		/// <summary>
		/// find out whether the given hvo is in the queue
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		internal bool Contains(int hvo)
		{
			return Safe_Contains(hvo);
		}

		/// <summary>
		/// Locking version of the contains member on the words queue.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns>true if present, false otherwise</returns>
		private bool Safe_Contains(int hvo)
		{
			bool rval = false;
			try
			{
				Safe_Lock(ref m_words_lock, "Contains");
				rval = m_words.Contains(hvo);
			}
			finally
			{
				Safe_Unlock(ref m_words_lock);
			}
			return rval;
		}

		internal void RemoveWordformIfPresent(int hvo)
		{
			// have to access both containers so have to get locks to both first
			try
			{
				Safe_Lock(ref m_words_lock, "RemoveWordformIfPresent-words container");
				// logic is:  if the 'hvo' is present in the words queue AND..
				if (m_words.Contains(hvo))
				{
					try
					{
						Safe_Lock(ref m_rwords_lock, "RemoveWordformIfPresent-remove container");
						// // it's not in the removedWords container then add it there
						// if (!m_removedWords.Contains(hvo))
						m_removedWords.Add(hvo); // Sets ignore multiple adds of the same object.
					}
					finally
					{
						Safe_Unlock(ref m_rwords_lock);
					}
				}
			}
			finally
			{
				Safe_Unlock(ref m_words_lock);
			}
		}

		/// <summary>
		/// remove the next Wordform from the queue and return it
		/// </summary>
		/// <returns>the database ID of the WfiWordform, or -1 if the queue is empty</returns
		internal int DequeueWordform()
		{
			try
			{
				int hvo = -1;
				try
				{
					Safe_Lock(ref m_words_lock, "DequeueWordform-A");
					Safe_Lock(ref m_rwords_lock, "DequeueWordform-B");
					while(true)
					{
						hvo = m_words.Dequeue();
						//if this word has been explicitly removed, loop again and get the next one
						if (m_removedWords.Contains(hvo))
							m_removedWords.Remove(hvo);
						else
							break;
					}
				}
				finally
				{
					Safe_Unlock(ref m_rwords_lock);
					Safe_Unlock(ref m_words_lock);
				}
					return hvo;
			}
			catch (InvalidOperationException)
			{
				return -1;	//empty
			}
		}

		/// <summary>
		/// Get the number of words in the queue minus the number that are in the remove queue
		/// </summary>
		internal int Count
		{
			get
			{
				int count = -1;
				// have to access both containers so have to get locks to both first
				try
				{
					Safe_Lock(ref m_words_lock, "Count-A");
					try
					{
						Safe_Lock(ref m_rwords_lock, "Count-B");
						count = m_words.Count - m_removedWords.Count;
					}
					finally
					{
						Safe_Unlock(ref m_rwords_lock);
					}
				}
				finally
				{
					Safe_Unlock(ref m_words_lock);
				}
				return count;
			}
		}
	}
}
