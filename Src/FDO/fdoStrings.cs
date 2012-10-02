// --------------------------------------------------------------------------------------------
// Copyright (C) 2002 SIL International. All rights reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: fdoStrings.cs
// Responsibility: John Hatton
// Last reviewed: never
//
//
// <remarks>
// Implementation of:
//		MultiUnicodeAccessor
//		MultiStringAccessor
//		TsStringAccessor
//		TsStringFromMultiAccessor : TsStringAccessor
// </remarks>
// --------------------------------------------------------------------------------------------


using System;
using System.Text;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Collections.Generic;

using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO
{


	/*	public class Unicode  // not allowed to inherit from 'sealed class' 'string'
		{
			//protected int m_ws;
			protected string m_s;

			public Unicode(string s)//, int writing system)
			{
				m_s = s;
				//m_ws = writing system;
			}
		}
	*/
	/// <summary>
	///
	/// </summary>
	public enum SpecialWritingSystemCodes
	{
		/// <summary>
		///
		/// </summary>
		DefaultAnalysis=-1000,
		/// <summary>
		///
		/// </summary>
		DefaultVernacular=-1001,
		/// <summary>
		///
		/// </summary>
		BestAnalysis=-1002,
		/// <summary>
		///
		/// </summary>
		BestVernacular=-1003,
		/// <summary>
		///
		/// </summary>
		BestAnalysisOrVernacular=-1004,
		/// <summary>
		///
		/// </summary>
		BestVernacularOrAnalysis=-1005
	}

	/// <summary>
	/// class to have things common to MultiUnicode and MultiString.
	/// </summary>
	public abstract class MultiAccessor
	{
		/// <summary>
		///
		/// </summary>
		protected FdoCache m_cache;
		/// <summary>
		///
		/// </summary>
		protected int m_hvoOwner;
		/// <summary>
		///
		/// </summary>
		protected int m_flidOwning;
		/// <summary>
		/// the sql view that will cough up this string
		/// </summary>
		protected string m_sView;

		/// <summary>
		/// Check that the given specified logical ws can make a string for our owning object and return the actual ws.
		/// </summary>
		/// <param name="ws">the logical ws (e.g. LangProject.kwsFirstVernOrAnal)</param>
		/// <param name="actualWs">the actual ws we can make a string with</param>
		/// <returns>true, if we can make a string with the given logical ws.</returns>
		public bool TryWs(int ws, out int actualWs)
		{
			return (m_cache.LangProject as LangProject).TryWs(ws, m_hvoOwner, m_flidOwning, out actualWs);
		}

		/// <summary>
		/// try the given ws and return the resulting tss and actualWs.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="actualWs"></param>
		/// <param name="tssResult"></param>
		/// <returns></returns>
		public bool TryWs(int ws, out int actualWs, out ITsString tssResult)
		{
			return (m_cache.LangProject as LangProject).TryWs(ws, m_hvoOwner, m_flidOwning, out actualWs, out tssResult);
		}

		/// <summary>
		/// Creates the appropriate string accessor to the given flid.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwning"></param>
		/// <param name="sView">Class_Field to load from database (obsolete?)</param>
		/// <returns></returns>
		static public MultiAccessor CreateMultiAccessor(FdoCache cache, int hvoOwner, int flidOwning, string sView)
		{
			FieldType flidType = cache.GetFieldType(flidOwning);
			switch (flidType)
			{
				case FieldType.kcptMultiUnicode:
				case FieldType.kcptMultiBigUnicode:
					return new MultiUnicodeAccessor(cache, hvoOwner, flidOwning, sView);
				case FieldType.kcptMultiString:
				case FieldType.kcptMultiBigString:
					return new MultiStringAccessor(cache, hvoOwner, flidOwning, sView);
				default:
					return null;
			}
		}

		/// <summary>
		/// Creates the appropriate string accessor to the given flid.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwning"></param>
		/// <returns></returns>
		static public MultiAccessor CreateMultiAccessor(FdoCache cache, int hvoOwner, int flidOwning)
		{
			return CreateMultiAccessor(cache, hvoOwner, flidOwning, "");
		}

		/// <summary>
		/// The field for which it is an accessor.
		/// </summary>
		public int Flid
		{
			get { return m_flidOwning; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the alternative TSS.
		/// </summary>
		/// <param name="ws">The writing system</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public abstract ITsString GetAlternativeTss(int ws);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the not found TsString.
		/// </summary>
		/// <value>The not found TSS.</value>
		/// ------------------------------------------------------------------------------------
		public virtual ITsString NotFoundTss
		{
			get
			{
				return StringUtils.MakeTss(Strings.ksStars, m_cache.DefaultUserWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// set the tss alternative matching the ws in the tssAlternative.
		/// </summary>
		/// <param name="tssAlternative"></param>
		/// ------------------------------------------------------------------------------------
		public void SetAlternativeTss(ITsString tssAlternative)
		{
			int ws = StringUtils.GetWsAtOffset(tssAlternative, 0);
			SetAlternative(tssAlternative, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// set the tss alternative given tss and ws.
		/// </summary>
		/// <param name="tssAlternative">The TSS alternative.</param>
		/// <param name="ws">The writing system</param>
		/// ------------------------------------------------------------------------------------
		public void SetAlternative(ITsString tssAlternative, int ws)
		{
			TsStringAccessor tssAccessor = GetAlternativeAccessor(ws);
			tssAccessor.UnderlyingTsString = tssAlternative;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the alternative accessor.
		/// </summary>
		/// <param name="ws">The writing system</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected TsStringAccessor GetAlternativeAccessor(int ws)
		{
			return new TsStringFromMultiAccessor(m_cache, m_hvoOwner,
						 m_flidOwning, ws, m_sView);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try GetAlternativeTss on the given 'wsPreferred', else return the BestAlternativeTss, giving
		/// preference to vernacular or analysis if 'wsPreferred' is vernacular or analysis.
		/// </summary>
		/// <param name="wsPreferred">The ws preferred.</param>
		/// <param name="wsActual">ws of the best found alternative</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GetAlternativeOrBestTss(int wsPreferred, out int wsActual)
		{
			int wsBest = 0;
			if (m_cache.LangProject.VernWssRC.Contains(wsPreferred))
			{
				// try for "best vernoranal"
				wsBest = LangProject.kwsFirstVernOrAnal;
			}
			else
			{
				// try for "best analorvern"
				wsBest = LangProject.kwsFirstAnalOrVern;
			}
			return GetAlternativeOrSecondaryTss(wsPreferred, wsBest, out wsActual);
		}

		/// <summary>
		/// Try an alternative 'wsPreferred' and then wsBest.
		/// </summary>
		/// <param name="wsPreferred">the ws to try first</param>
		/// <param name="wsSecondary">typically a magic ws, but could be a real ws (default) too.</param>
		/// <param name="wsActual"></param>
		/// <returns></returns>
		public ITsString GetAlternativeOrSecondaryTss(int wsPreferred, int wsSecondary, out int wsActual)
		{
			ITsString tssResult;
			if ((m_cache.LangProject as LangProject).TryWs(wsPreferred, wsSecondary, m_hvoOwner, m_flidOwning, out wsActual, out tssResult))
				return tssResult;
			return NotFoundTss;
		}

		/// <summary>
		/// Get best vernacular or analysis alternative
		/// </summary>
		public abstract ITsString BestVernacularAnalysisAlternative
		{
			get;
		}

		/// <summary>
		/// Get best analysis or vernacular alternative
		/// </summary>
		public abstract ITsString BestAnalysisVernacularAlternative
		{
			get;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class MultiUnicodeAccessor : MultiAccessor
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwning"></param>
		/// <param name="sView"></param>
		public MultiUnicodeAccessor(FdoCache cache, int hvoOwner, int flidOwning, string sView)
		{
			Debug.Assert(cache != null);
			m_cache = cache;
			Debug.Assert(hvoOwner != 0);
			m_hvoOwner = hvoOwner;
			Debug.Assert(flidOwning != 0);
			m_flidOwning = flidOwning;

			m_sView = sView;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string value for the default analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AnalysisDefaultWritingSystem
		{
			get {return GetAlternative(m_cache.DefaultAnalWs);}
			set	{SetAlternative(value, m_cache.DefaultAnalWs);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string value for the default user interface writing system.
		/// Get should hardly ever fail to produce something; try for any other if unsuccessful.
		/// NEVER return null; may cause crashes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UserDefaultWritingSystem
		{
			get {
				string result = GetAlternative(m_cache.DefaultUserWs);
				if (String.IsNullOrEmpty(result))
					result = BestAnalysisVernacularAlternative.Text;
				if (result == null || result == NotFoundTss.Text)
					return "";
				return result;
			}
			set	{SetAlternative(value, m_cache.DefaultUserWs);}
		}

		/// <summary>
		/// Get the value in the UI WS, without trying to be smart if there isn't one.
		/// </summary>
		public string RawUserDefaultWritingSystem
		{
			get
			{
				return GetAlternative(m_cache.DefaultUserWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string value for the default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string VernacularDefaultWritingSystem
		{
			get {return GetAlternative(m_cache.DefaultVernWs);}
			set {SetAlternative(value, m_cache.DefaultVernWs);}
		}

		#region 'Best of' series

		/// <summary>
		/// Get the best analysis alternative of this string.
		///	First, we try the current analysis writing systems.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		public ITsString BestAnalysisAlternative
		{
			get
			{
				int bestWs = m_cache.LangProject.ActualWs(LangProject.kwsFirstAnal, m_hvoOwner, m_flidOwning);
				if (bestWs > 0)
					return StringUtils.MakeTss(GetAlternative(bestWs), bestWs);

				return this.NotFoundTss;
			}
		}

		/// <summary>
		/// Get the best analysis/vernacular alternative of this string.
		///	First, we try the best analysis writing systems.
		///	Failing that, we try for the best vernacular writing system.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		public override ITsString BestAnalysisVernacularAlternative
		{
			get
			{
				int bestWs = m_cache.LangProject.ActualWs(LangProject.kwsFirstAnalOrVern, m_hvoOwner, m_flidOwning);
				if (bestWs > 0)
					return StringUtils.MakeTss(GetAlternative(bestWs), bestWs);

				return this.NotFoundTss;
			}
		}

		/// <summary>
		/// Get the best vernacular alternative of this string.
		///	First, we try the current vernacular writing systems.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		public ITsString BestVernacularAlternative
		{
			get
			{
				int bestWs = m_cache.LangProject.ActualWs(LangProject.kwsFirstVern, m_hvoOwner, m_flidOwning);
				if (bestWs > 0)
					return StringUtils.MakeTss(GetAlternative(bestWs), bestWs);

				return this.NotFoundTss;
			}
		}

		/// <summary>
		/// Get the best vernacular/analysis alternative of this string.
		///	First, we try the best vernacular writing systems.
		///	Failing that, we try for the best analysis writing system.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		public override ITsString BestVernacularAnalysisAlternative
		{
			get
			{
				int bestWs = m_cache.LangProject.ActualWs(LangProject.kwsFirstVernOrAnal, m_hvoOwner, m_flidOwning);
				if (bestWs > 0)
					return StringUtils.MakeTss(GetAlternative(bestWs), bestWs);

				return this.NotFoundTss;
			}
		}

		#endregion 'Best' of series

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetAlternative(SpecialWritingSystemCodes code)
		{
			switch (code)
			{
				case SpecialWritingSystemCodes.DefaultAnalysis:
					return this.AnalysisDefaultWritingSystem;
				case SpecialWritingSystemCodes.DefaultVernacular:
					return this.VernacularDefaultWritingSystem;
				case SpecialWritingSystemCodes.BestAnalysis:
					return this.BestAnalysisAlternative.Text;
				case SpecialWritingSystemCodes.BestVernacular:
					return this.BestVernacularAlternative.Text;
				case SpecialWritingSystemCodes.BestAnalysisOrVernacular:
					return this.BestAnalysisVernacularAlternative.Text;
				case SpecialWritingSystemCodes.BestVernacularOrAnalysis:
					return this.BestVernacularAnalysisAlternative.Text;
				default:
					throw new ArgumentException();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetAlternative(int ws)
		{
			 return m_cache.GetMultiUnicodeAlt(m_hvoOwner, m_flidOwning, ws, m_sView);
		}

		/// <summary>
		/// Get the value as a TsString.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public override ITsString GetAlternativeTss(int ws)
		{
			return m_cache.GetMultiStringAlt(m_hvoOwner, m_flidOwning, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sAlternative"></param>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		public void SetAlternative(string sAlternative, int ws)
		{
			m_cache.SetMultiUnicodeAlt(m_hvoOwner, m_flidOwning, ws, sAlternative);
		}

		/// <summary>
		/// Merge two MultiUnicodeAccessor objects.
		/// These cases are handled:
		///		1. If an alternative exists in both objects, nothing is merged.
		///		2. If the main object (this) is missing an alternative, and the 'source' has it, then add it to 'this'.
		///		3. If the main object has an alternative, then do nothing.
		/// </summary>
		/// <param name="source"></param>
		public void MergeAlternatives(MultiUnicodeAccessor source)
		{
			MergeAlternatives(source, false);
		}

		/// <summary>
		/// Default uses space to separate.
		/// </summary>
		public void MergeAlternatives(MultiUnicodeAccessor source, bool fConcatenateIfBoth)
		{
			MergeAlternatives(source, fConcatenateIfBoth, " ");
		}

		/// <summary>
		/// Merge two MultiUnicodeAccessor objects.
		/// These cases are handled:
		///		1. If an alternative exists in both objects, nothing is merged if fConcatenateIfBoth is false.
		///		2. If the main object (this) is missing an alternative, and the 'source' has it, then add it to 'this'.
		///		3. If the main object has an alternative, then do nothing.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="fConcatenateIfBoth"></param>
		/// <param name="sep">separator to use if concatenating</param>
		public void MergeAlternatives(MultiUnicodeAccessor source, bool fConcatenateIfBoth, string sep)
		{
			if (source == null)
				return; // Nothing to do.

			foreach (LgWritingSystem lws in m_cache.LanguageEncodings)
			{
				int ws = lws.Hvo;
				string myAlt = GetAlternative(ws);
				string srcAlt = source.GetAlternative(ws);
				if ((myAlt == null || myAlt == String.Empty)
					&& (srcAlt != null && srcAlt != String.Empty))
				{
					SetAlternative(srcAlt, ws);
				}
				else if (!fConcatenateIfBoth)
				{
					continue;
				}
				else if (myAlt != null && myAlt != String.Empty
					&& srcAlt != null && srcAlt != String.Empty
					&& GetAlternative(ws) != source.GetAlternative(ws))
				{
					SetAlternative(GetAlternative(ws) + sep + source.GetAlternative(ws), ws);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy one mua over another.
		/// </summary>
		/// <param name="source">The source.</param>
		/// ------------------------------------------------------------------------------------
		public void CopyAlternatives(MultiUnicodeAccessor source)
		{
			CopyAlternatives(source, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy one mua over another.
		/// </summary>
		/// <param name="source">The source.</param>
		/// <param name="skipIfExist">True to only copy values that aren't present, false to
		/// overwrite all values that were present</param>
		/// ------------------------------------------------------------------------------------
		public void CopyAlternatives(MultiUnicodeAccessor source, bool skipIfExist)
		{
			if (source == null)
				return; // Nothing to do.

			foreach (LgWritingSystem lws in m_cache.LanguageEncodings)
			{
				int ws = lws.Hvo;
				bool overWrite = true;
				if (skipIfExist)
				{
					ITsString tss = GetAlternativeTss(ws);
					overWrite = (tss == null || tss.Length == 0);
				}

				if (overWrite)
				{
					ITsString srcAlt = source.GetAlternativeTss(ws);
					if (srcAlt != null && srcAlt.Length != 0)
						m_cache.SetMultiStringAlt(m_hvoOwner, m_flidOwning, ws, srcAlt);
				}
			}
		}

		/// <summary>
		/// Returns all the existing alternatives for this multilingual string.
		/// (Unfortunately, this requires at least one hit on the SQL database even if
		/// everything is already in the cache.)
		/// </summary>
		/// <returns></returns>
		public Dictionary<int, string> GetAllAlternatives()
		{
			Dictionary<int, string> dict = new Dictionary<int, string>();
			string qry = String.Format("SELECT Ws FROM {0} WHERE Obj={1}", m_sView, m_hvoOwner);
			List<int> rgws = DbOps.ReadIntsFromCommand(m_cache, qry, null);
			for (int i = 0; i < rgws.Count; ++i)
			{
				string sTxt = m_cache.GetMultiUnicodeAlt(m_hvoOwner, m_flidOwning, rgws[i], m_sView);
				dict.Add(rgws[i], sTxt);
			}
			return dict;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current
		/// <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return BestAnalysisAlternative.Text;
		}
	}

	//HACK: since m_odde.MultiStringProp isn't implemented, we're faking things so we can carry on.
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MultiStringAccessor : MultiAccessor
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwning"></param>
		/// <param name="sView"></param>
		/// ------------------------------------------------------------------------------------
		public MultiStringAccessor(FdoCache cache, int hvoOwner, int flidOwning, string sView)
		{
			Debug.Assert(cache != null);
			// JohnT: this is VERY expensive (an SqlQuery each time) and has not been very
			// productive in catching bugs and distorts optimization efforts.
			//Debug.Assert(cache.IsValidObject(hvoOwner));
			m_cache = cache;
			m_hvoOwner = hvoOwner;
			m_flidOwning = flidOwning;
			m_sView = sView;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string value for the default analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TsStringAccessor AnalysisDefaultWritingSystem
		{
			get	{return GetAlternative(m_cache.DefaultAnalWs);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string value for the default user interface writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TsStringAccessor UserDefaultWritingSystem
		{
			get {return GetAlternative(m_cache.DefaultUserWs);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string value for the default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TsStringAccessor VernacularDefaultWritingSystem
		{
			get	{return GetAlternative(m_cache.DefaultVernWs);}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the string value for the default analysis writing system.
		/// </summary>
		/// <param name="str"></param>
		/// ------------------------------------------------------------------------------------
		public void SetAnalysisDefaultWritingSystem(string str)
		{
			SetAlternative(str, m_cache.DefaultAnalWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the string value for the default user interface writing system.
		/// </summary>
		/// <param name="str"></param>
		/// ------------------------------------------------------------------------------------
		public void SetUserDefaultWritingSystem(string str)
		{
			SetAlternative(str, m_cache.DefaultUserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the string value for the default vernacular writing system.
		/// </summary>
		/// <param name="str"></param>
		/// ------------------------------------------------------------------------------------
		public void SetVernacularDefaultWritingSystem(string str)
		{
			SetAlternative(str, m_cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public TsStringAccessor GetAlternative(int ws)
		{
			return GetAlternativeAccessor(ws);
		}

		/// <summary>
		/// Get the best analysis alternative of this string.
		///	First, we try the current analysis writing systems.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		public ITsString BestAnalysisAlternative
		{
			get
			{
				return BestAlternative(LangProject.kwsFirstAnal);
			}
		}

		/// <summary>
		/// Get the best vernacular alternative of this string.
		/// </summary>
		public ITsString BestVernacularAlternative
		{
			get
			{
				return BestAlternative(LangProject.kwsFirstVern);
			}
		}

		/// <summary>
		/// Get best vernacular else best analysis alternative.
		/// </summary>
		public override ITsString BestVernacularAnalysisAlternative
		{
			get
			{
				return BestAlternative(LangProject.kwsFirstVernOrAnal);
			}
		}

		/// <summary>
		/// Get best analysis else best vernacular.
		/// </summary>
		public override ITsString BestAnalysisVernacularAlternative
		{
			get
			{
				return BestAlternative(LangProject.kwsFirstAnalOrVern);
			}
		}

		private ITsString BestAlternative(int kwsFirst)
		{
			int bestWs;
			ITsString tss;
			if (TryWs(kwsFirst, out bestWs, out tss))
				return tss;
			return NotFoundTss;
		}

		/// <summary>
		/// Get the value as a TsString directly.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public override ITsString GetAlternativeTss(int ws)
		{
			return m_cache.GetMultiStringAlt(m_hvoOwner, m_flidOwning, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sAlternative"></param>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		public void SetAlternative(string sAlternative, int ws)
		{
			TsStringAccessor tssAccessor = GetAlternative(ws);
			tssAccessor.Text = sAlternative;
		}

		/// <summary>
		/// Merge two MultiStringAccessor objects.
		/// These cases are handled:
		///		1. If an alternative exists in both objects, nothing is merged.
		///		2. If the main object (this) is missing an alternative, and the 'source' has it, then add it to 'this'.
		///		3. If the main object has an alternative, then do nothing.
		/// </summary>
		/// <param name="source"></param>
		public void MergeAlternatives(MultiStringAccessor source)
		{
			MergeAlternatives(source, false);
		}

		/// <summary>
		/// Default inserts space.
		/// </summary>
		public void MergeAlternatives(MultiStringAccessor source, bool fConcatenateIfBoth)
		{
			MergeAlternatives(source, fConcatenateIfBoth, " ");
		}

		/// <summary>
		/// Merge two MultiStringAccessor objects.
		/// These cases are handled:
		///		1. If the main object (this) is missing an alternative, and the 'source' has it, then add it to 'this'.
		///		2. If the main object has an alternative, and the source has none, then do nothing.
		///		3. If both alternatives are non-empty, then
		///			3.1 if fConcatenateIfBoth is false, keep the current value (do nothing)
		///			3.2 if fConcatenateIfBoth is true, and the values are equal, keep the current value (do nothing);
		///			3.3 if fConcatenateIfBoth is true, and the values are not equal, append the source to the current value.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="fConcatenateIfBoth"></param>
		/// <param name="sep">insert between alterantives when merging.</param>
		public void MergeAlternatives(MultiStringAccessor source, bool fConcatenateIfBoth, string sep)
		{
			if (source == null)
				return; // Nothing to do.

			foreach (LgWritingSystem lws in m_cache.LanguageEncodings)
			{
				int ws = lws.Hvo;
				string myAlt = GetAlternative(ws).Text;
				string srcAlt = source.GetAlternative(ws).Text;
				if ((myAlt == null || myAlt == String.Empty)
					&& (srcAlt != null && srcAlt != String.Empty))
				{
					SetAlternative(source.GetAlternative(ws).UnderlyingTsString, ws);
				}
				else if (!fConcatenateIfBoth)
				{
					continue;
				}
				else if (myAlt != null && myAlt != String.Empty
					&& srcAlt != null && srcAlt != String.Empty
					&& !GetAlternative(ws).UnderlyingTsString.Equals(source.GetAlternative(ws).UnderlyingTsString))
				{
					// concatenate
					ITsStrBldr tsb = GetAlternative(ws).UnderlyingTsString.GetBldr();
					tsb.Replace(tsb.Length, tsb.Length, sep, null);
					tsb.ReplaceTsString(tsb.Length, tsb.Length, source.GetAlternative(ws).UnderlyingTsString);
					SetAlternative(tsb.GetString(), ws);
				}
			}
		}
		/// <summary>
		/// Overwrite all alternatives.
		/// </summary>
		/// <param name="source"></param>
		public void CopyAlternatives(MultiStringAccessor source)
		{
			if (source == null)
				return; // Nothing to do.

			foreach (LgWritingSystem lws in m_cache.LanguageEncodings)
			{
				int ws = lws.Hvo;
				ITsString srcAlt = source.GetAlternativeTss(ws);
				if ((srcAlt != null && srcAlt.Length != 0))
				{
					SetAlternative(srcAlt, ws);
				}
			}
		}

		/// <summary>
		/// Returns all the existing alternatives for this multilingual string.
		/// (Unfortunately, this requires at least one hit on the SQL database even if
		/// everything is already in the cache.)
		/// </summary>
		/// <returns></returns>
		public Dictionary<int, ITsString> GetAllAlternatives()
		{
			Dictionary<int, ITsString> dict = new Dictionary<int, ITsString>();
			string qry = String.Format("SELECT Ws FROM MultiStr$ WHERE Obj={0} AND Flid={1}",
				m_hvoOwner, m_flidOwning);
			List<int> rgws = DbOps.ReadIntsFromCommand(m_cache, qry, null);
			for (int i = 0; i < rgws.Count; ++i)
			{
				ITsString tss = m_cache.GetMultiStringAlt(m_hvoOwner, m_flidOwning, rgws[i]);
				dict.Add(rgws[i], tss);
			}
			return dict;
		}
	}
#if false	//OutOfCommission because m_odde.MultiStringProp is not implemented.  Use TsMultiStringAccessor instead.
	public class TsMultiString
	{
		private FdoCache m_cache;

		private ITsMultiString m_tms;

		//HACK: m_odde.MultiStringProp is not implemented, so this won't be too useful:
		public TsMultiString(FdoCache cache, ITsMultiString tms)
		{
			Debug.Assert(cache != null);
			m_cache = cache;
			Debug.Assert(tms != null);
			m_tms = tms;
		}


		public ITsMultiString  underlyingTsMultiString
		{
			get { return m_tms; }
		}

		public TsStringAccessor AnalysisDefaultWritingSystem
		{
			get
			{
				return new TsStringAccessor(m_tms.get_String(m_cache.DefaultAnalWs));
			}
			set
			{
				m_tms.set_String(m_cache.DefaultAnalWs, value.UnderlyingTsString);
			}
		}
		public TsStringAccessor VernacularDefaultWritingSystem
		{
			get
			{
				return new TsStringAccessor(m_tms.get_String(m_cache.DefaultVernWs));
			}
			set
			{
				m_tms.set_String(m_cache.DefaultVernWs, value.UnderlyingTsString);
			}
		}

/* commented out cause I couldn't return the writing system at the same time.  If the TsStringAccessor could tell its
 * writing system, than this would be ok.
		public MultiStringEnumerator GetEnumerator()
		{
			return new MultiStringEnumerator(m_tms);
		}

		// Declare the enumerator class: for use in foreach(...) statements
		public class MultiStringEnumerator
		{
			int nIndex;
			private ITsMultiString m_tms;
			public MultiStringEnumerator(ITsMultiString tms)
			{
				m_tms = tms;
				nIndex = -1;
			}

			public bool MoveNext()
			{
				nIndex++;
				return(nIndex < m_tms.StringCount);
			}

			public int Current
			{
				get
				{
					int ws;
					return(m_tms.GetStringFromIndex(nIndex, ws));  <---- the problem is how to give back this ws?
				}
			}
		}
*/
	}
#endif

	/// <summary>
	///
	/// </summary>
	public class TsStringAccessor
	{
		/// <summary>Use UnderlyingTsString instead of directly accessing this variable!</summary>
		private ITsString m_tss;
		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected int m_hvoOwner;
		/// <summary></summary>
		protected int m_flidOwning;

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwning"></param>
		public TsStringAccessor(FdoCache cache, int hvoOwner, int flidOwning)
		{
			Debug.Assert(cache != null);
			Debug.Assert(hvoOwner != 0); // JohnT: was >0, but not true for memory-only test data
			m_cache = cache;
			m_hvoOwner = hvoOwner;
			m_flidOwning = flidOwning;
		}

		/// <summary>
		/// Access to the COM interface of the TsString
		/// </summary>
		public ITsString UnderlyingTsString
		{
			get
			{
				if (m_tss == null)
					m_tss = GetFromCache();
				return m_tss;
			}
			set
			{
				m_tss = value;
				UpdateToCache(m_tss);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets from cache.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual ITsString GetFromCache()
		{
			return m_cache.GetTsStringProperty(m_hvoOwner, m_flidOwning);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the string in the cache.
		/// </summary>
		/// <param name="tss">The string.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void UpdateToCache(ITsString tss)
		{
			m_cache.SetTsStringProperty(m_hvoOwner, m_flidOwning, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/set the text of the underlying TsString.  Note that settling this property will
		/// completely replace the existing TsString, so any formatting on it will be lost.
		/// Returns null if the length of the text is zero.
		/// </summary>
		/// <value>The text.</value>
		/// ------------------------------------------------------------------------------------
		public virtual string Text
		{
			get { return UnderlyingTsString.Text; }
			set
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.Append(value);
				UnderlyingTsString = tisb.GetString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the length of the string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Length
		{
			get { return UnderlyingTsString.Length; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="source"></param>
		public void MergeString(TsStringAccessor source)
		{
			MergeString(source, false);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="source"></param>
		/// <param name="fConcatenateIfBoth">If true, and if source and dest both have values that are not
		/// equal, concatenate source on end of dest. Otherwise ignore source if dest has a value.</param>
		public void MergeString(TsStringAccessor source, bool fConcatenateIfBoth)
		{
			if (source == null)
				return;

			string text = null;
			if (UnderlyingTsString != null)
				text = UnderlyingTsString.Text;
			string sourceText = null;
			if (source.UnderlyingTsString != null)
				sourceText = source.UnderlyingTsString.Text;

			if ((text == null
				|| text == string.Empty)
				&& (sourceText != null
				&& sourceText != string.Empty))
			{
				UnderlyingTsString = source.UnderlyingTsString;
			}
			else if (!fConcatenateIfBoth)
			{
				return;
			}
			else if ((text != null
				&& text != string.Empty)
				&& sourceText != null
				&& sourceText != string.Empty
				&& !UnderlyingTsString.Equals(source.UnderlyingTsString))
			{
				// concatenate
				ITsStrBldr tsb = UnderlyingTsString.GetBldr();
				tsb.Replace(tsb.Length, tsb.Length, " ", null);
				tsb.ReplaceTsString(tsb.Length, tsb.Length, source.UnderlyingTsString);
				UnderlyingTsString = tsb.GetString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Marks the indicated text in this string as a hyperlink.
		/// </summary>
		/// <param name="ichStart">The index of the first character in the string which should
		/// be marked as hyperlink text.</param>
		/// <param name="ichLim">The "limit" index in the string indicating the end of the
		/// hyperlink text.</param>
		/// <param name="url">The URL that is the target of the hyperlink.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <returns><c>true</c> if the hyperlink was successfully inserted; <c>false</c>
		/// otherwise (indicating that the hyperlink style could not be found in the given
		/// stylesheet)</returns>
		/// ------------------------------------------------------------------------------------
		public void MarkTextAsHyperlink(int ichStart, int ichLim, string url,
			FwStyleSheet stylesheet)
		{
			ITsStrBldr tssBldr = UnderlyingTsString.GetBldr();
			MarkTextInBldrAsHyperlink(tssBldr, ichStart, ichLim, url, stylesheet);
			UnderlyingTsString = tssBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Marks the indicated text in the given string builder as a hyperlink.
		/// </summary>
		/// <param name="strBldr">The string builder.</param>
		/// <param name="ichStart">The index of the first character in the string builder which
		/// should be marked as hyperlink text.</param>
		/// <param name="ichLim">The "limit" index in the string builder indicating the end of
		/// the hyperlink text.</param>
		/// <param name="url">The URL that is the target of the hyperlink.</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// <returns><c>true</c> if the hyperlink was successfully inserted; <c>false</c>
		/// otherwise (indicating that the hyperlink style could not be found in the given
		/// stylesheet)</returns>
		/// ------------------------------------------------------------------------------------
		public static bool MarkTextInBldrAsHyperlink(ITsStrBldr strBldr, int ichStart,
			int ichLim, string url, FwStyleSheet stylesheet)
		{
			IStStyle hyperlinkStyle = stylesheet.FindStyle(StStyle.Hyperlink);
			if (hyperlinkStyle == null)
				return false;
			string propVal = Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName).ToString() + url;
			hyperlinkStyle.InUse = true;
			strBldr.SetStrPropValue(ichStart, ichLim, (int)FwTextPropType.ktptNamedStyle, StStyle.Hyperlink);
			strBldr.SetStrPropValue(ichStart, ichLim, (int)FwTextPropType.ktptObjData, propVal);
			return true;
		}
	}

	/// <summary>
	///
	/// </summary>
	public class TsStringFromMultiAccessor : TsStringAccessor
	{
		private string m_sView; // the SQL view that will cough up this string
		private int m_ws; // the writing system alternate we belong to

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flidOwning"></param>
		/// <param name="ws"></param>
		/// <param name="sView"></param>
		public TsStringFromMultiAccessor(FdoCache cache, int hvoOwner, int flidOwning, int ws, string sView)
			:base(cache, hvoOwner, flidOwning)
		{
			Debug.Assert(cache != null);
			Debug.Assert(hvoOwner != 0); // JohnT: was > 0, but not true for memory-only test objects.
			m_cache = cache;
			m_hvoOwner = hvoOwner;
			m_sView = sView;
			m_flidOwning = flidOwning;
			m_ws = ws;
		}

		/// <summary>
		/// Get/set the text of the underlying TsString.  Note that settling this property will completely replace the existing TsString, so any formatting on it will be lost.
		/// </summary>
		public override string Text
		{
			get {return UnderlyingTsString.Text;}
			set
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_ws);
				tisb.Append(value);
				UnderlyingTsString = tisb.GetString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets from cache.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override ITsString GetFromCache()
		{
			return m_cache.GetMultiStringAlt(m_hvoOwner, m_flidOwning, m_ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the string in the cache.
		/// </summary>
		/// <param name="tss">The string.</param>
		/// ------------------------------------------------------------------------------------
		protected override void UpdateToCache(ITsString tss)
		{
			m_cache.SetMultiStringAlt(m_hvoOwner, m_flidOwning, m_ws, tss);
		}
	}
}
