// --------------------------------------------------------------------------------------------
// Copyright (C) 2008 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: RfcWritingSystem.cs
// Responsibility: Steve McConnel
// Last reviewed: never
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Cellar;
using Palaso.WritingSystems;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// This class facilitates converting RFC4646 language codes (from XML) into
	/// FieldWorks writing systems.  It is commonly used in import.
	/// </summary>
	public class RfcWritingSystem
	{
		private Dictionary<string, string> m_mapRFCtoICU = new Dictionary<string, string>();
		private Dictionary<string, int> m_mapRFCtoWs = new Dictionary<string, int>();
		private Dictionary<string, int> m_mapIcuLCToWs = new Dictionary<string, int>();
		private List<ILgWritingSystem> m_rgnewWrtSys = new List<ILgWritingSystem>();
		private FdoCache m_cache;
		private bool m_fUpdateVernWss = false;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache"></param>
		public RfcWritingSystem(FdoCache cache)
		{
			Initialize(cache, true);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="fUpdateVernWss"></param>
		public RfcWritingSystem(FdoCache cache, bool fUpdateVernWss)
		{
			Initialize(cache, fUpdateVernWss);
		}

		private void Initialize(FdoCache cache, bool fUpdateVernWss)
		{
			m_cache = cache;
			m_fUpdateVernWss = fUpdateVernWss;

			// Store a case-insensitive map for looking up existing writing systems.
			foreach (ILgWritingSystem lws in m_cache.LanguageEncodings)
			{
				string key = lws.ICULocale.ToLowerInvariant();
				if (!m_mapIcuLCToWs.ContainsKey(key))
					m_mapIcuLCToWs.Add(key, lws.Hvo);
			}
		}

		/// <summary>
		/// Get the writing system code (int) for the given RFC4646 language code
		/// (string).
		/// </summary>
		public int GetWsFromRfcLang(string code, string sLdmlDir)
		{
			int ws;
			if (m_mapRFCtoWs.TryGetValue(code, out ws))
				return ws;
			string sWs = ConvertFromRFCtoICU(code);
			string sWsLower = sWs.ToLowerInvariant();
			if (!m_mapIcuLCToWs.TryGetValue(sWsLower, out ws))
			{
				// See if a compatible XML file exists defining this writing system.
				LanguageDefinition langDef;
				try
				{
					LanguageDefinitionFactory fact = new LanguageDefinitionFactory();
					langDef = fact.InitializeFromXml(
						m_cache.LanguageWritingSystemFactoryAccessor, sWs) as LanguageDefinition;
				}
				catch
				{
					langDef = null;
				}
				ILgWritingSystem lgws;
				if (langDef != null)
				{
					// ICU locale case may differ - keep existing XML based value
					string sICU = langDef.IcuLocaleOriginal;
					Debug.Assert(sWsLower == sICU.ToLowerInvariant());
					langDef.SaveWritingSystem(sICU, true);
					ws = m_cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr(sICU);
					Debug.Assert(ws >= 1);
					lgws = LgWritingSystem.CreateFromDBObject(m_cache, ws);
				}
				else
				{
					WritingSystemDefinition wsd = null;
					if (!String.IsNullOrEmpty(sLdmlDir))
					{
						LdmlInFolderWritingSystemStore ldmlstore = new LdmlInFolderWritingSystemStore(sLdmlDir);
						foreach (WritingSystemDefinition wsdT in ldmlstore.WritingSystemDefinitions)
						{
							if (wsdT.RFC4646 == code)
							{
								wsd = wsdT;
								break;
							}
						}
					}
					// This creates a new writing system for the given key.
					IWritingSystem wrsy = m_cache.LanguageWritingSystemFactoryAccessor.get_Engine(sWs);
					m_cache.ResetLanguageEncodings();
					ws = wrsy.WritingSystem;
					Debug.Assert(ws >= 1);
					lgws = LgWritingSystem.CreateFromDBObject(m_cache, ws);
					lgws.ICULocale = sWs;
					if (wsd == null)
					{
						lgws.Abbr.UserDefaultWritingSystem = sWs;
						lgws.Name.UserDefaultWritingSystem = sWs;
					}
					else
					{
						lgws.Abbr.UserDefaultWritingSystem = wsd.Abbreviation;
						lgws.Name.UserDefaultWritingSystem = wsd.LanguageName;
						lgws.DefaultSerif = wsd.DefaultFontName;
						lgws.DefaultBodyFont = wsd.DefaultFontName;
						lgws.RightToLeft = wsd.RightToLeftScript;
						// TODO: collation, keyboard.
					}
					// Make sure XML file is written.  See LT-8743.
					wrsy.SaveIfDirty(m_cache.DatabaseAccessor);
				}
				m_rgnewWrtSys.Add(lgws);
				m_cache.LangProject.AnalysisWssRC.Add(ws);
				m_cache.LangProject.CurAnalysisWssRS.Append(ws);
				if (m_fUpdateVernWss)
				{
					m_cache.LangProject.VernWssRC.Add(ws);
					m_cache.LangProject.CurVernWssRS.Append(ws);
				}
				m_mapIcuLCToWs.Add(sWsLower, ws);
			}
			m_mapRFCtoWs.Add(code, ws);
			return ws;
		}

		/// <summary>
		/// Convert the RFC language code (string) to an ICU locale (string).
		/// </summary>
		/// <param name="sRFC"></param>
		/// <returns></returns>
		public string ConvertFromRFCtoICU(string sRFC)
		{
			if (m_mapRFCtoICU.ContainsKey(sRFC))
				return m_mapRFCtoICU[sRFC];
			string sICU = LgWritingSystem.ConvertRFC4646ToICU(sRFC);
			m_mapRFCtoICU[sRFC] = sICU;
			return sICU;
		}

		/// <summary>
		/// Get the list of writing systems added during import.
		/// </summary>
		public List<ILgWritingSystem> AddedWrtSys
		{
			get { return m_rgnewWrtSys; }
		}
	}
}
