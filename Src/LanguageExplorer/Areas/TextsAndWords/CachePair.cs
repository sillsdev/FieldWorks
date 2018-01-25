// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// CachePair maintains a relationship between two caches, a regular LcmCache storing real data,
	/// and an ISilDataAccess, typically a VwCacheDaClass, that stores temporary data for a
	/// secondary view. As well as storing both cache objects, it stores two maps which maintain a
	/// bidirectional link between HVOs in one and those in the other.
	/// </summary>
	public class CachePair : IDisposable
	{
		private LcmCache m_cache;
		private ISilDataAccess m_sda;
		private Dictionary<int, int> m_lcmToSda;
		private Dictionary<int, int> m_SdaToLcm;
		private ICmObjectRepository m_coRepository;

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
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~CachePair()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary />
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
				m_lcmToSda?.Clear();
				m_SdaToLcm?.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			Marshal.ReleaseComObject(m_sda);
			m_sda = null;
			m_cache = null;
			m_lcmToSda = null;
			m_SdaToLcm = null;
			m_coRepository = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// Forget any previously-established relationships between objects in the main
		/// and secondary caches.
		/// </summary>
		public void ClearMaps()
		{
			CheckDisposed();
			m_SdaToLcm.Clear();
			m_lcmToSda.Clear();
		}
		/// <summary />
		public LcmCache MainCache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
			set
			{
				CheckDisposed();
				if (m_cache == value)
				{
					return;
				}

				m_cache = value;
				// Forget any existing relationships.
				m_lcmToSda = new Dictionary<int, int>();
				m_SdaToLcm = new Dictionary<int, int>();
				m_coRepository = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			}
		}

		/// <summary />
		public ISilDataAccess DataAccess
		{
			get
			{
				CheckDisposed();
				return m_sda;
			}
			set
			{
				CheckDisposed();
				if (m_sda == value)
				{
					return;
				}

				if (m_sda != null)
				{
					Marshal.ReleaseComObject(m_sda);
				}
				m_sda = value;
				// Forget any existing relationships.
				m_lcmToSda = new Dictionary<int, int>();
				m_SdaToLcm = new Dictionary<int, int>();
			}
		}

		/// <summary>
		/// Create a new secondary cache.
		/// </summary>
		public void CreateSecCache()
		{
			CheckDisposed();
			var cda = VwCacheDaClass.Create();
			cda.TsStrFactory = TsStringUtils.TsStrFactory;
			DataAccess = cda;
			DataAccess.WritingSystemFactory = m_cache.WritingSystemFactory;
		}

		/// <summary>
		/// Map from secondary hvo (in the SilDataAccess) to real hvo (in the LcmCache).
		/// </summary>
		/// <param name="secHvo"></param>
		/// <returns></returns>
		public int RealHvo(int secHvo)
		{
			CheckDisposed();
			return m_SdaToLcm.ContainsKey(secHvo) ? m_SdaToLcm[secHvo] : 0;
		}

		/// <summary>
		/// Map from secondary hvo (in the SilDataAccess) to real object (in ICmOjectRepository).
		/// </summary>
		public ICmObject RealObject(int secHvo)
		{
			var hvoReal = RealHvo(secHvo);
			return hvoReal != 0 && m_coRepository != null ? m_coRepository.GetObject(hvoReal) : null;
		}

		/// <summary>
		/// Create a two-way mapping.
		/// </summary>
		public void Map(int secHvo, int realHvo)
		{
			CheckDisposed();
			m_SdaToLcm[secHvo] = realHvo;
			m_lcmToSda[realHvo] = secHvo;
		}

		/// <summary>
		/// Removes a two-way mapping.
		/// </summary>
		public bool RemoveSec(int secHvo)
		{
			CheckDisposed();
			int realHvo;
			if (m_SdaToLcm.TryGetValue(secHvo, out realHvo))
			{
				m_lcmToSda.Remove(realHvo);
			}
			return m_SdaToLcm.Remove(secHvo);
		}

		/// <summary>
		/// Removes a two-way mapping.
		/// </summary>
		public bool RemoveReal(int realHvo)
		{
			CheckDisposed();
			int secHvo;
			if (m_lcmToSda.TryGetValue(realHvo, out secHvo))
			{
				m_SdaToLcm.Remove(secHvo);
			}
			return m_lcmToSda.Remove(realHvo);
		}

		/// <summary>
		/// Map from real hvo (in the LcmCache) to secondary (in the SilDataAccess).
		/// </summary>
		public int SecHvo(int realHvo)
		{
			CheckDisposed();
			return m_lcmToSda.ContainsKey(realHvo) ? m_lcmToSda[realHvo] : 0;
		}

		/// <summary>
		/// Look for a secondary-cache object that corresponds to hvoReal. If one does not already exist,
		/// create it by appending to property flidOwn of object hvoOwner.
		/// </summary>
		public int FindOrCreateSec(int hvoReal, int clid, int hvoOwner, int flidOwn)
		{
			CheckDisposed();
			var hvoSec = 0;
			if (hvoReal != 0)
			{
				hvoSec = SecHvo(hvoReal);
			}
			if (hvoSec == 0)
			{
				hvoSec = m_sda.MakeNewObject(clid, hvoOwner, flidOwn, m_sda.get_VecSize(hvoOwner, flidOwn));
				if (hvoReal != 0)
				{
					Map(hvoSec, hvoReal);
				}
			}
			return hvoSec;
		}
		/// <summary>
		/// Look for a secondary-cache object that corresponds to hvoReal. If one does not already exist,
		/// create it by appending to property flidOwn of object hvoOwner.
		/// Set its flidName property to a string name in writing system ws.
		/// If hvoReal is zero, just create an object, but don't look for or create an association.
		/// </summary>
		public int FindOrCreateSec(int hvoReal, int clid, int hvoOwner, int flidOwn, int flidName, ITsString tss)
		{
			CheckDisposed();
			var hvoSec = FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn);
			m_sda.SetString(hvoSec, flidName, tss);
			return hvoSec;
		}

		/// <summary>
		/// Look for a secondary-cache object that corresponds to hvoReal. If one does not already exist,
		/// create it by appending to property flidOwn of object hvoOwner.
		/// Set its flidName property to a string name in writing system ws.
		/// </summary>
		public int FindOrCreateSec(int hvoReal, int clid, int hvoOwner, int flidOwn, string name, int flidName, int ws)
		{
			CheckDisposed();

			return FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn, flidName, TsStringUtils.MakeString(name, ws));
		}
		/// <summary>
		/// Like FindOrCreateSec, except the ws is taken automaticaly as the default analysis ws of the main cache.
		/// </summary>
		public int FindOrCreateSecAnalysis(int hvoReal, int clid, int hvoOwner, int flidOwn, string name, int flidName)
		{
			CheckDisposed();
			return FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn, name, flidName, m_cache.DefaultAnalWs);
		}

		/// <summary>
		/// Like FindOrCreateSec, except the ws is taken automaticaly as the default vernacular ws of the main cache.
		/// </summary>
		public int FindOrCreateSecVern(int hvoReal, int clid, int hvoOwner, int flidOwn, string name, int flidName)
		{
			CheckDisposed();
			return FindOrCreateSec(hvoReal, clid, hvoOwner, flidOwn, name, flidName, m_cache.DefaultVernWs);
		}
	}
}