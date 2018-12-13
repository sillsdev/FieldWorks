// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	public class CacheUndoAction : IUndoAction
	{
		private IRealDataCache m_cache;
		private CacheInfo m_before;
		private CacheInfo m_after;

		/// <summary />
		public CacheUndoAction(IRealDataCache cache, CacheInfo before, CacheInfo after)
		{
			m_cache = cache;
			m_before = before;
			m_after = after;
		}

		private void CacheObject(CacheInfo cacheInfo)
		{
			switch (cacheInfo.Type)
			{
				case ObjType.Object:
					m_cache.CacheUnknown(cacheInfo.Hvo, cacheInfo.Flid, cacheInfo.Object);
					break;
				case ObjType.BasicTsString:
					m_cache.CacheStringProp(cacheInfo.Hvo, cacheInfo.Flid, cacheInfo.Object as ITsString);
					break;
				case ObjType.ExtendedTsString:
					m_cache.CacheStringAlt(cacheInfo.Hvo, cacheInfo.Flid, cacheInfo.Ws, cacheInfo.Object as ITsString);
					break;
				case ObjType.ByteArray:
					byte[] array = cacheInfo.Object as byte[];
					m_cache.CacheBinaryProp(cacheInfo.Hvo, cacheInfo.Flid, array, array.Length);
					break;
				case ObjType.String:
					var str = cacheInfo.Object as string;
					m_cache.CacheUnicodeProp(cacheInfo.Hvo, cacheInfo.Flid, str, str.Length);
					break;
				case ObjType.Guid:
					m_cache.CacheGuidProp(cacheInfo.Hvo, cacheInfo.Flid, (Guid)cacheInfo.Object);
					break;
				case ObjType.Int:
					m_cache.CacheIntProp(cacheInfo.Hvo, cacheInfo.Flid, (int)cacheInfo.Object);
					break;
				case ObjType.Long:
					m_cache.CacheInt64Prop(cacheInfo.Hvo, cacheInfo.Flid, (long)cacheInfo.Object);
					break;
				case ObjType.Bool:
					m_cache.CacheBooleanProp(cacheInfo.Hvo, cacheInfo.Flid, (bool)cacheInfo.Object);
					break;
				case ObjType.Vector:
					int[] vector = cacheInfo.Object as int[];
					m_cache.CacheVecProp(cacheInfo.Hvo, cacheInfo.Flid, vector, vector.Length);
					break;
				case ObjType.Time:
					m_cache.CacheTimeProp(cacheInfo.Hvo, cacheInfo.Flid, (long)cacheInfo.Object);
					break;
			}
		}

		public IVwRootBox RootBox { get; set; }

		#region IUndoAction implementation

		public bool Undo()
		{
			CacheObject(m_before);
			RootBox.PropChanged(m_before.Hvo, m_before.Flid, 0, 1000, 1000);
			return true;
		}

		public bool Redo()
		{
			CacheObject(m_after);
			return true;
		}

		public void Commit()
		{
		}

		public bool IsDataChange => true;

		public bool IsRedoable => m_after != null;

		public bool SuppressNotification
		{
			set { }
		}

		#endregion
	}
}