// --------------------------------------------------------------------------------------------
// Copyright (C) 2005 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: MSAInterlinearNameVirtualHandler.cs
// Responsibility: Randy Regnier
//
// <remarks>
// Implementation of:
//		MSAInterlinearNameVirtualHandler.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Collections;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Cellar; // for property types.
using SIL.FieldWorks.FDO.Ling;
using  SIL.Utils;

namespace SIL.FieldWorks.FDO
{
	// TODO: Move into Ling assembly, someday.

	/// <summary>
	/// Summary description for MSAInterlinearNameVirtualHandler.
	/// </summary>
	public class MSAInterlinearNameVirtualHandler : BaseVirtualHandler
	{
		/// <summary>
		///
		/// </summary>
		protected FdoCache m_cache;

		/// <summary>
		///
		/// </summary>
		/// <param name="configuration"></param>
		/// <param name="cache"></param>
		public MSAInterlinearNameVirtualHandler(XmlNode configuration, FdoCache cache)
		{
			m_cache = cache;
			Type = (int)CellarModuleDefns.kcptString;
			this.ClassName = "MoMorphSynAnalysis";
			this.FieldName = "InterlinearName";
			Writeable = false;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override FdoCache Cache
		{
			get { return m_cache; }
			set { m_cache = value; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="cda"></param>
		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			IMoMorphSynAnalysis msa = MoMorphSynAnalysis.CreateFromDBObject(m_cache, hvo);
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			cda.CacheStringProp(hvo, tag,  tsf.MakeString(msa.InterlinearName, m_cache.DefaultAnalWs));
		}
	}
}
