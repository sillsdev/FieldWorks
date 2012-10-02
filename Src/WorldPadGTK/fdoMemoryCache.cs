using System;
using System.Collections;
using System.Collections.Generic;

using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FdoMemoryCache.
	/// This is FdoCache that doesn't use a database
	/// </summary>
	/// ---
	public class FdoMemoryCache : FdoCache
	{

		public FdoMemoryCache(ISilDataAccess sda) : base()
		{

			m_odde = sda;
			m_lef = sda.WritingSystemFactory;
			m_mdc = new FwMetaDataCache();
		}

	}

}
