// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.RootSites
{
	internal sealed class CacheInfo
	{
		/// <summary />
		internal CacheInfo(ObjType type, int hvo, int flid, object obj)
		{
			Type = type;
			Hvo = hvo;
			Flid = flid;
			Object = obj;
		}

		/// <summary />
		internal CacheInfo(ObjType type, int hvo, int flid, int ws, object obj)
			: this(type, hvo, flid, obj)
		{
			Ws = ws;
		}

		/// <summary />
		internal ObjType Type { get; }
		/// <summary />
		internal int Hvo { get; }
		/// <summary />
		internal int Flid { get; }
		/// <summary />
		internal int Ws { get; }
		/// <summary />
		internal object Object { get; }
	}
}