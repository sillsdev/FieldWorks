// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	public sealed class CacheInfo
	{
		/// <summary />
		public CacheInfo(ObjType type, int hvo, int flid, object obj)
		{
			Type = type;
			Hvo = hvo;
			Flid = flid;
			Object = obj;
		}

		/// <summary />
		public CacheInfo(ObjType type, int hvo, int flid, int ws, object obj)
			: this(type, hvo, flid, obj)
		{
			Ws = ws;
		}

		/// <summary />
		public ObjType Type { get; }
		/// <summary />
		public int Hvo { get; }
		/// <summary />
		public int Flid { get; }
		/// <summary />
		public int Ws { get; }
		/// <summary />
		public object Object { get; }
	}
}