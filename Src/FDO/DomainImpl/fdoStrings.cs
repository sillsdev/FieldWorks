// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: fdoStrings.cs
// Responsibility: Randy Regnier
//
// <remarks>
// Implementation of:
//		MultiAccessorBase : IMultiAccessorBase
//			MultiAccessor (adds IMultiStringAccessor)
//				MultiUnicodeAccessor
//				MultiStringAccessor
//			VirtualStringAccessor
// </remarks>

using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;
using System.Xml.Linq;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	/// <summary>
	/// Base class for MultiAccessor and MultiVirtual.
	/// We inherit from SmallDictionary in order to store the alternatives without using memory for another object.
	/// This is MUCH more space-efficient for small numbers of alternatives, especially for only one.
	/// It does mean that zero cannot be used as a writing system ID.
	/// Unfortunately the instance variables of SmallDictionary are wasted for VirtualStringAccessor, but these objects
	/// have a very short lifetime and there are never many in existence.
	/// The other slightly unfortunate thing is that the methods of SmallDictionary (that is, of IDictionary (of int, ITsString))
	/// are visible wherever there is an instance of MultiAccessorBase; but we almost always access them through one of the
	/// interfaces.
	/// </summary>
	internal abstract class MultiAccessorBase : SmallDictionary<int, ITsString>, IMultiAccessorBase
	{
		/// <summary>The object that 'owns' the alternates.</summary>
		protected ICmObject m_object;
		/// <summary>The field ID of the string property.</summary>
		protected int m_flid;

		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		protected MultiAccessorBase(ICmObject obj, int flid)
		{
			m_object = obj;
			m_flid = flid;
		}
		#endregion

		#region ITsMultiString Members

		/// <summary>
		/// Allows iterating over the available alternatives. Nb: VirtualStringAccessor does not implement.
		/// </summary>
		/// <param name="iws"></param>
		/// <param name="_ws"></param>
		/// <returns></returns>
		public abstract ITsString GetStringFromIndex(int iws, out int _ws);

		/// <summary>
		/// Allows iterating over the available alternatives. Nb: VirtualStringAccessor does not implement.
		/// </summary>
		public abstract int StringCount { get; }

		/// <summary>
		/// Get an alternative by writing system
		/// </summary>
		public abstract ITsString get_String(int ws);

		/// <summary>
		/// Get one, but answer null if no known value.
		/// </summary>
		public abstract ITsString StringOrNull(int ws);

		/// <summary>
		/// Set an alternative by writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="_tss"></param>
		public abstract void set_String(int ws, ITsString _tss);
		public abstract int[] AvailableWritingSystemIds { get;}

		#endregion

		#region IMultiStringAccessor implementation

		/// <summary>
		/// The field for which it is an accessor.
		/// </summary>
		public int Flid
		{
			get { return m_flid; }
		}

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
				return TsStringUtils.MakeTss(Strings.ksStars, m_object.Cache.WritingSystemFactory.UserWs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string value for the default analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString AnalysisDefaultWritingSystem
		{
			get { return get_String(m_object.Cache.DefaultAnalWs); }
			set { set_String(m_object.Cache.DefaultAnalWs, value); }
		}

		/// <summary>
		/// Set the analysis default writing system to a simple string in that writing system.
		/// </summary>
		/// <param name="val"></param>
		public void SetAnalysisDefaultWritingSystem(string val)
		{
			set_String(m_object.Cache.DefaultAnalWs, val);
		}
		/// <summary>
		/// Set the analysis default writing system to a simple string in that writing system.
		/// </summary>
		/// <param name="val"></param>
		public void SetVernacularDefaultWritingSystem(string val)
		{
			set_String(m_object.Cache.DefaultVernWs, val);
		}

		/// <summary>
		/// Set the user writing system to a simple string in that writing system.
		/// </summary>
		/// <param name="val"></param>
		public void SetUserWritingSystem(string val)
		{
			set_String(m_object.Cache.DefaultUserWs, val);
		}

		/// <summary>
		/// Set the specified alternative to a simple string in the appropriate writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="val"></param>
		public void set_String(int ws, string val)
		{
			set_String(ws, m_object.Cache.TsStrFactory.MakeString(val, ws));
		}


		/// <summary>
		/// Check that the given specified logical ws can make a string for our owning object and return the actual ws.
		/// </summary>
		/// <param name="ws">the logical ws (e.g. LangProject.kwsFirstVernOrAnal)</param>
		/// <param name="actualWs">the actual ws we can make a string with</param>
		/// <returns>true, if we can make a string with the given logical ws.</returns>
		public bool TryWs(int ws, out int actualWs)
		{
			ITsString tssResult;
			return TryWs(ws, out actualWs, out tssResult);
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
			return WritingSystemServices.TryWs(m_object.Cache, ws, m_object.Hvo, m_flid, out actualWs, out tssResult);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the string value for the default vernacular writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString VernacularDefaultWritingSystem
		{
			get { return get_String(m_object.Cache.DefaultVernWs); }
			set { set_String(m_object.Cache.DefaultVernWs, value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="System.String"/> suitable for displaying in the user interface.
		/// Will fall back to an alternate WS with a real value, but does not do the funky
		/// fall-back to ***.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string UiString
		{
			get
			{
				int ws = WritingSystemServices.ActualWs(m_object.Cache,
					m_object.Cache.WritingSystemFactory.UserWs, m_object.Hvo, m_flid);
				return (ws == 0) ? null : get_String(ws).Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the string value for the default user interface writing system.
		/// Get should hardly ever fail to produce something; try for any other if unsuccessful.
		/// NEVER return null; may cause crashes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString UserDefaultWritingSystem
		{
			get
			{
				var result = RawUserDefaultWritingSystem;
				if (result != null && result.Length > 0 && result.Text != NotFoundTss.Text)
					return result;

				return BestAnalysisVernacularAlternative;
			}
			set { set_String(m_object.Cache.WritingSystemFactory.UserWs, value); }
		}

		/// <summary>
		/// Get the value in the UI WS, without trying to be smart if there isn't one.
		/// </summary>
		public ITsString RawUserDefaultWritingSystem
		{
			get
			{
				return get_String(m_object.Cache.WritingSystemFactory.UserWs);
			}
		}

		/// <summary>
		/// Get the best analysis/vernacular alternative of this string.
		///	First, we try the best analysis writing systems.
		///	Failing that, we try for the best vernacular writing system.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		public ITsString BestAnalysisVernacularAlternative
		{
			get { return GetBest(WritingSystemServices.kwsFirstAnalOrVern); }
		}

		/// <summary>
		/// Get the best analysis alternative of this string.
		///	First, we try the current analysis writing systems.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		public ITsString BestAnalysisAlternative
		{
			get { return GetBest(WritingSystemServices.kwsFirstAnal); }
		}

		/// <summary>
		/// Get the best vernacular alternative of this string.
		///	First, we try the current vernacular writing systems.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		public ITsString BestVernacularAlternative
		{
			get { return GetBest(WritingSystemServices.kwsFirstVern); }
		}

		/// <summary>
		/// Get the best vernacular/analysis alternative of this string.
		///	First, we try the best vernacular writing systems.
		///	Failing that, we try for the best analysis writing system.
		///	Failing that, we try for the DefaultUserWs.
		///	Failing that, we give up and use "***".
		/// </summary>
		public ITsString BestVernacularAnalysisAlternative
		{
			get { return GetBest(WritingSystemServices.kwsFirstVernOrAnal); }
		}

		#endregion IMultiStringAccessor implementation

		private ITsString GetBest(int ws)
		{
			var bestWs = WritingSystemServices.ActualWs(m_object.Cache, ws, m_object.Hvo, m_flid);
			return (bestWs == 0 ? null : get_String(bestWs)) ??
				   m_object.Cache.TsStrFactory.MakeString(Strings.ksStars, m_object.Cache.WritingSystemFactory.UserWs);
		}
	}

	/// <summary>
	/// Superclass for the two main FDO 'Multi' accessor classes.
	/// This class implements the ITsMultiString interface
	/// </summary>
	internal abstract class MultiAccessor : MultiAccessorBase, IMultiStringAccessor, IMultiAccessorInternal
	{
		#region Data Members

		#endregion Data Members

		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		protected MultiAccessor(ICmObject obj, int flid) : base(obj, flid)
		{
			// JohnT: removed some validation from here; this method is called only by generated code, the risk seems
			// very low, and the validation we were doing took significant time.
		}

		/// <summary>
		/// Set an alternate without fanfare, such as prop changes, undo, etc.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="newValue"></param>
		/// <remarks>
		/// This is used by the Undo/Redo mechanism (ONLY).
		/// </remarks>
		void IMultiAccessorInternal.SetAltQuietly(int ws, ITsString newValue)
		{
			if (newValue == null)
			{
				Remove(ws);
			}
			else
			{
				this[ws] = newValue;
			}
		}

		/// <summary>Get an XML string that represents the entire instance.</summary>
		/// <param name='writer'>The writer in which the XML is placed.</param>
		/// <remarks>Only to be used by backend provider system.</remarks>
		internal void ToXMLString(XmlWriter writer)
		{
			var wsf = m_object.Cache.WritingSystemFactory;
			foreach (var kvp in this)
				ToXml(writer, wsf, kvp.Key, kvp.Value);
		}

		/// <summary>
		/// Write the xml for the alternative.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="wsf"></param>
		/// <param name="ws"></param>
		/// <param name="alternative"></param>
		protected virtual void ToXml(XmlWriter writer, ILgWritingSystemFactory wsf, int ws, ITsString alternative)
		{
			if (string.IsNullOrEmpty(alternative.Text))
				return; // Skip writing TsStrings with no content.

			writer.WriteRaw(TsStringUtils.GetXmlRep(alternative, wsf, ws));
		}

		/// <summary>
		/// 'reader' is the main property node.
		/// </summary>
		internal void LoadFromDataStoreInternal(XElement reader, ILgWritingSystemFactory wsf, ITsStrFactory tsf)
		{
			if (!reader.HasElements)
				return;

			FromXml(reader, wsf, tsf);
		}

		/// <summary>
		/// Reconstitute data.
		/// </summary>
		protected virtual void FromXml(XElement reader, ILgWritingSystemFactory wsf, ITsStrFactory tsf)
		{
			foreach (var aStrNode in reader.Elements("AStr"))
			{
				// Throwing out a string without a ws is probably better than crashing
				// and preventing a db from being opened.
				// This code currently accepts this data, only storing en, fr, and es strings.
				// <Description>
				// <AStr ws="en"><Run ws="en">***</Run></AStr>
				// <AStr><Run ws="fr">***</Run></AStr>
				// <AStr ws="du"/>
				// <AStr ws="zh"></AStr>
				// <AStr ws="ko"><Run/></AStr>
				// <AStr ws="in"><Run>bad</Run></AStr>
				// <AStr/>
				// <AStr ws="es"><Run ws="es">help</Run></AStr>
				// </Description>
				ITsString tss;
				int wsHvo = ReadAstrElementOfMultiString(aStrNode, wsf, out tss);
				if (tss != null && wsHvo > 0 && tss.Length > 0)
					Add(wsHvo, tss);
			}
		}

		internal static int ReadAstrElementOfMultiString(XElement aStrNode, ILgWritingSystemFactory wsf, out ITsString tss)
		{
				var wsHvo = 0;
				var wsAttr = aStrNode.Attribute("ws");
				if (wsAttr != null)
					wsHvo = wsf.GetWsFromStr(wsAttr.Value);
			tss = TsStringSerializer.DeserializeTsStringFromXml(aStrNode, wsf);
				if (wsHvo == 0)
				{
					// THIS SHOULD NEVER HAPPEN but we live in a fallen world!
					var ttp = tss.get_PropertiesAt(0);
					int nVar;
					wsHvo = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
				}
			return wsHvo;
		}

		#endregion Construction

		#region ITsMultiString implementation

		/// <summary>Gets a StringCount </summary>
		/// <returns>A System.Int32 </returns>
		public override int StringCount
		{
			get { return Count; }
		}

		/// <summary>Member GetStringFromIndex </summary>
		/// <param name='iws'> </param>
		/// <param name='ws'> </param>
		/// <returns>A ITsString</returns>
		public override ITsString GetStringFromIndex(int iws, out int ws)
		{
			var idx = 0;
			foreach (var kvp in this)
			{
				if (idx++ != iws) continue;

				ws = kvp.Key;
				return kvp.Value;
			}

			throw new IndexOutOfRangeException("'iws' is not a valid index");
		}

		/// <summary>Member get_String </summary>
		/// <param name='ws'> </param>
		/// <returns>A ITsString</returns>
		public override ITsString get_String(int ws)
		{
			ITsString tss;
			return TryGetValue(ws, out tss) ? tss : m_object.Cache.TsStrFactory.EmptyString(ws);
		}

		/// <summary>
		/// Like get_String, except if no value is known it returns null.
		/// </summary>
		public override ITsString StringOrNull(int ws)
		{
			ITsString tss;
			TryGetValue(ws, out tss);
			return tss;
		}

		/// <summary>Member set_String </summary>
		/// <param name='ws'> </param>
		/// <param name='tss'> </param>
		public override void set_String(int ws, ITsString tss)
		{
			tss = TsStringUtils.NormalizeNfd(tss);
			ITsString originalValue;
			TryGetValue(ws, out originalValue);
			if (tss == originalValue)
				return;
			if (tss != null && originalValue != null && tss.Equals(originalValue))
				return;

			// If tss is null, then just remove ws from the dictionary.
			if (tss == null)
			{
				Remove(ws);
			}
			else
			{
				// This check is too strong!  The first run may easily be in the "wrong" writing system.
				// We don't "embed" foreign words in our representation, they're inline with the other
				// runs.
				//var props = tss.get_Properties(0);
				//int var;
				//if (ws != wprops.GetIntPropValues((int)FwTextPropType.ktptWs, out var))
				//    throw new ArgumentException("The outermost ws is a different writing system than 'ws'.");

				var surrogateWS = ws; // (GetType().Name == "MultiUnicodeAccessor") ? 0 : ws;
				this[surrogateWS] = tss;
			}

			((ICmObjectInternal)m_object).ITsStringAltChangedSideEffects(
				m_flid,
				m_object.Services.WritingSystemManager.Get(ws),
				originalValue,
				tss);

			((IServiceLocatorInternal)m_object.Cache.ServiceLocator).UnitOfWorkService.RegisterObjectAsModified(m_object, m_flid, ws, originalValue, tss);
		}

		public override int[] AvailableWritingSystemIds
		{
			get
			{
				return Keys.ToArray();
			}
		}

		#endregion ITsMultiString implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy all writing system alternatives from the given source into this multi-string
		/// accessor overwriting any that already exist. Note that this can replace a non-empty
		/// alternative with an empty alternative. Will not change/remove pre-existing
		/// alternatives missing in the source (i.e., result will be a superset).
		/// </summary>
		/// <param name="source">The source to copy from</param>
		/// ------------------------------------------------------------------------------------
		public void CopyAlternatives(IMultiStringAccessor source)
		{
			CopyAlternatives(source, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy all writing system alternatives from the given source into this multi-string
		/// accessor overwriting any that already exist. Will not change/remove pre-existing
		/// alternatives missing in the source (i.e., result will be a superset).
		/// </summary>
		/// <param name="source">The source to copy from</param>
		/// <param name="fIgnoreEmptySourceStrings"><c>True</c> to ignore any source alternatives
		/// that are empty, keeping the current alternaltive.</param>
		/// ------------------------------------------------------------------------------------
		public void CopyAlternatives(IMultiStringAccessor source, bool fIgnoreEmptySourceStrings)
		{
			if (source == null)
				return; // Nothing to do.

			int cnt = source.StringCount;
			for (int i = 0; i < cnt; i++)
			{
				int ws;
				ITsString tss = source.GetStringFromIndex(i, out ws);
				if (!fIgnoreEmptySourceStrings || tss.Length > 0)
					set_String(ws, tss);
			}
		}

		/// <summary>
		/// Merge two MultiAccessor objects.
		/// These cases are handled:
		///		1. If an alternative exists in both objects, nothing is merged.
		///		2. If the main object (this) is missing an alternative, and the 'source' has it, then add it to 'this'.
		///		3. If the main object has an alternative, then do nothing.
		/// </summary>
		/// <param name="source"></param>
		public void MergeAlternatives(IMultiStringAccessor source)
		{
			MergeAlternatives(source, false);
		}

		/// <summary>
		/// Default uses space to separate.
		/// </summary>
		public void MergeAlternatives(IMultiStringAccessor source, bool fConcatenateIfBoth)
		{
			MergeAlternatives(source, fConcatenateIfBoth, " ");
		}

		/// <summary>
		/// Merge two MultiUnicodeAccessor objects.
		/// These cases are handled:
		///		1. If an alternative exists in both objects, nothing is merged.
		///		2. If the main object (this) is missing an alternative, and the 'source' has it, then add it to 'this'.
		///		3. If the main object has an alternative, then do nothing.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="fConcatenateIfBoth"></param>
		/// <param name="sep">separator to use if concatenating</param>
		public void MergeAlternatives(IMultiStringAccessor source, bool fConcatenateIfBoth, string sep)
		{
			if (source == null)
				return; // Nothing to do.

			foreach (var lws in m_object.Services.WritingSystemManager.LocalWritingSystems)
			{
				var ws = lws.Handle;
				var myAlt = get_String(ws);
				var srcAlt = source.get_String(ws);
				if ((myAlt == null || myAlt.Length == 0)
					&& (srcAlt != null && srcAlt.Length != 0))
				{
					set_String(ws, srcAlt);
				}
				else if (!fConcatenateIfBoth)
				{
					continue;
				}
				else if (myAlt != null && myAlt.Length != 0
						 && srcAlt != null && srcAlt.Length != 0
						 && !myAlt.Equals(srcAlt))
				{
					var newBldr = m_object.Cache.TsStrFactory.GetIncBldr();
					newBldr.AppendTsString(get_String(ws));
					newBldr.Append(sep);
					newBldr.AppendTsString(source.get_String(ws));
					set_String(ws, newBldr.GetString());
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends all the alternatives from the source multi string to the alternatives of
		/// this here one.
		/// </summary>
		/// <param name="src">The source.</param>
		/// ------------------------------------------------------------------------------------
		public void AppendAlternatives(IMultiStringAccessor src)
		{
			AppendAlternatives(src, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends all the alternatives from the source multi string to the alternatives of
		/// this here one.
		/// </summary>
		/// <param name="src">The source.</param>
		/// <param name="fPreventDuplication"><c>true</c> to avoid appending a string that is
		/// identical that matches the end of the existing string.</param>
		/// ------------------------------------------------------------------------------------
		public void AppendAlternatives(IMultiStringAccessor src, bool fPreventDuplication)
		{
			foreach (int ws in src.AvailableWritingSystemIds)
			{
				ITsString sourceStr = src.get_String(ws);
				if (sourceStr.Length == 0)
					continue;
				ITsString originalDest = get_String(ws);
				if (originalDest.Length == 0)
				{
					// There is no translation in the destination string, so just replace the
					// empty string with the source
					set_String(ws, sourceStr);
				}
				else
				{
					// There is existing translation in the destination string, so we need to merge
					// the translations. This should happen only for the first moved translation
					if (!originalDest.Text.EndsWith(sourceStr.Text, StringComparison.Ordinal))
						set_String(ws, originalDest.ConcatenateWithSpaceIfNeeded(sourceStr));
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the given string occurs (as a substring) in any available
		/// alternative (using StringComparison.InvariantCultureIgnoreCase).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool OccursInAnyAlternative(string s)
		{
			return string.IsNullOrEmpty(s) ? Count > 0 :
				Values.Any(str => str.Length > 0 && str.Text.IndexOf(s, StringComparison.InvariantCultureIgnoreCase) >= 0);
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
			ITsString tss;
			if (wsPreferred < 0)
			{
				// already magic, try for that.
				tss = WritingSystemServices.GetMagicStringAlt(m_object.Cache,
					wsPreferred, m_object.Hvo, m_flid, true, out wsActual);
				if ((tss == null || tss.Length == 0) && wsPreferred != WritingSystemServices.kwsFirstAnalOrVern)
				{
					tss = WritingSystemServices.GetMagicStringAlt(m_object.Cache, WritingSystemServices.kwsFirstAnalOrVern,
						m_object.Hvo, m_flid, true, out wsActual);
				}
				return tss;
			}
			var pref = m_object.Services.WritingSystemManager.Get(wsPreferred);
			wsActual = pref.Handle;

			if (!TryGetValue(pref.Handle, out tss))
			{
				tss = WritingSystemServices.GetMagicStringAlt(m_object.Cache,
					WritingSystemServices.kwsFirstVernOrAnal, m_object.Hvo, m_flid, true, out wsActual);
				if (tss == null || tss.Length == 0)
				{
					tss = WritingSystemServices.GetMagicStringAlt(m_object.Cache, WritingSystemServices.kwsFirstAnalOrVern,
						m_object.Hvo, m_flid, true, out wsActual);
				}
			}

			return tss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the best alternative in the order of preference requested. If none of the
		/// preferred writing systems are available, arbitrarily select one from among the
		/// available alternatives. This ensures that this will not return null unless no
		/// alternatives exist.
		/// </summary>
		/// <param name="wsActual">ws of the best found alternative</param>
		/// <param name="preferences">Writing systems to consider in order of preference (can be
		/// real writing system IDs or magic numbers.</param>
		/// ------------------------------------------------------------------------------------
		public ITsString GetBestAlternative(out int wsActual, params int[] preferences)
		{
			foreach (int ws in preferences)
			{
				ITsString tss;
				if (ws < 0)
				{
					tss = WritingSystemServices.GetMagicStringAlt(m_object.Cache,
						ws, m_object.Hvo, m_flid, true, out wsActual);
				}
				else
				{
					wsActual = ws;
					tss = StringOrNull(ws);
				}

				if (tss != null && tss.Length > 0)
					return tss;
			}
			wsActual = AvailableWritingSystemIds.FirstOrDefault();
			return wsActual > 0 ? StringOrNull(wsActual) : null;
		}
	}

	/// <summary>
	/// This class supports multiple ITsStrings, but each must meet these requirements:
	/// 1. There can be only one run in the ITsString,
	/// 2. That run must be only one writing system, and
	/// 3. There can be no fancy formatting (Text Properties),
	/// such as bold or red quiggly underlining, etc.
	/// </summary>
	internal sealed class MultiUnicodeAccessor : MultiAccessor, IMultiUnicode
	{
		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		public MultiUnicodeAccessor(ICmObject obj, int flid)
			: base(obj, flid)
		{ /* Nothing else to do. */ }

		#endregion Construction

		/// <summary>Member set_String </summary>
		/// <param name='ws'> </param>
		/// <param name='tss'> </param>
		public override void set_String(int ws, ITsString tss)
		{
			if (tss != null)
			{
				// Since there's no way to keep users from changing the ws in the middle of the
				// string via keyboard/OS operations, we'll merely Assert for errors rather than
				// throw.
				// Make sure the tss has only one ws in it, and no fancy properties like bold.
				if (tss.RunCount > 1)
				{
					string msg = String.Format(
						"The given ITsString has more than one run in it.  ObjId={0}, Class={1}, Flid={2}, Text=\"{3}\", Run Count={4}",
						m_object == null ? Guid.Empty : m_object.Guid,
						m_object == null ? "<NULL>" : m_object.ClassName, m_flid, tss.Text, tss.RunCount);
					Debug.Assert(tss.RunCount == 1, msg);
				}
				var props = tss.get_Properties(0);
				if (props.IntPropCount != 1)
				{
					StringBuilder bldr = new StringBuilder();
					bldr.Append("Int Props =");
					for (int i = 0; i < props.IntPropCount; ++i)
					{
						int tpt;
						int var;
						int val = props.GetIntProp(i, out tpt, out var);
						if (i > 0)
							bldr.Append(";");
						bldr.AppendFormat(" [tpt={0} value={1} variant={2}]", tpt, val, var);
					}
					string msg = String.Format(
						"The given ITsString has more than one integer property in it.  ObjId={0}, Class={1}, Flid={2}, Text=\"{3}\", {4}",
						m_object == null ? Guid.Empty : m_object.Guid,
						m_object == null ? "<NULL>" : m_object.ClassName, m_flid, tss.Text, bldr.ToString());
					Debug.Assert(props.IntPropCount == 1, msg);
				}
				if (props.StrPropCount > 0)
				{
					StringBuilder bldr = new StringBuilder();
					bldr.Append("String Props =");
					for (int i = 0; i < props.StrPropCount; ++i)
					{
						int tpt;
						string val = props.GetStrProp(i, out tpt);
						if (i > 0)
							bldr.Append(";");
						bldr.AppendFormat(" [tpt={0} value=\"{1}\"]", tpt, val);
					}
					string msg = String.Format(
						"The given ITsString has string properties in it.  ObjId={0}, Class={1}, Flid={2}, Text=\"{3}\", {4}",
						m_object == null ? Guid.Empty : m_object.Guid,
						m_object == null ? "<NULL>" : m_object.ClassName, m_flid, tss.Text, bldr.ToString());
					Debug.Assert(props.StrPropCount == 0, msg);
				}
			}
			base.set_String(ws, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string GetAlternative(SpecialWritingSystemCodes code)
		{
			return GetAlternativeTSS(code).Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GetAlternativeTSS(SpecialWritingSystemCodes code)
		{
			switch (code)
			{
				case SpecialWritingSystemCodes.DefaultAnalysis:
					return AnalysisDefaultWritingSystem;
				case SpecialWritingSystemCodes.DefaultVernacular:
					return VernacularDefaultWritingSystem;
				case SpecialWritingSystemCodes.BestAnalysis:
					return BestAnalysisAlternative;
				case SpecialWritingSystemCodes.BestVernacular:
					return BestVernacularAlternative;
				case SpecialWritingSystemCodes.BestAnalysisOrVernacular:
					return BestAnalysisVernacularAlternative;
				case SpecialWritingSystemCodes.BestVernacularOrAnalysis:
					return BestVernacularAnalysisAlternative;
				default:
					throw new ArgumentException();
			}
		}

		#region Implementation of IMultiStringAccessor

		#endregion

		/// <summary>
		/// Write the xml for the unicode alternative.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="wsf"></param>
		/// <param name="ws"></param>
		/// <param name="alternative"></param>
		protected override void ToXml(XmlWriter writer, ILgWritingSystemFactory wsf, int ws, ITsString alternative)
		{
			var text = alternative.Text;
			if (string.IsNullOrEmpty(text))
				return; // Skip writing TsStrings with no content.

			writer.WriteStartElement("AUni");
			writer.WriteAttributeString("ws", m_object.Services.WritingSystemManager.Get(ws).Id);
			text = Icu.Normalize(text, Icu.UNormalizationMode.UNORM_NFC);
			writer.WriteString(text);
			writer.WriteEndElement();
		}

		protected override void FromXml(XElement reader, ILgWritingSystemFactory wsf, ITsStrFactory tsf)
		{
			foreach (var aUniNode in reader.Elements("AUni"))
			{
				ITsString tss;
				int wsHvo = ReadMultiUnicodeAlternative(aUniNode, wsf, tsf, out tss);
				// Throwing out a string with a duplicate ws is probably better than crashing
				// and preventing a db from being opened.  This should never happen, but there's
				// at least one converted project that has had this occur.
				// (Perhaps we should warn the user?  but then we should do so for ws 0 as well.)
				if (wsHvo != 0 && !ContainsKey(wsHvo) && tss != null && tss.Length > 0)
					Add(wsHvo, tss);
			}
		}

		static internal int ReadMultiUnicodeAlternative(XElement aUniNode, ILgWritingSystemFactory wsf, ITsStrFactory tsf, out ITsString tss)
		{
			tss = null;
			var sValue = aUniNode.Value;
			if (String.IsNullOrEmpty(sValue))
				return 0;
			var wsVal = aUniNode.Attribute("ws");
			if (wsVal == null || String.IsNullOrEmpty(wsVal.Value))
				return 0;
			var wsHvo = wsf.GetWsFromStr(wsVal.Value);
			// Throwing out a string without a ws is probably better than crashing
			// and preventing a db from being opened.
			// This code currently accepts this data, only storing en__IPA and fr strings.
			// <Form>
			// <AUni ws="en" />
			// <AUni ws="en__IPA">problematic</AUni>
			// <AUni>missing</AUni>
			// <AUni></AUni>
			// <AUni ws="fr">french</AUni>
			// <AUni/>
			// </Form>
			if (wsHvo == 0)
				return 0;
			var text = Icu.Normalize(sValue, Icu.UNormalizationMode.UNORM_NFD);
			tss = tsf.MakeString(text, wsHvo);
			return wsHvo;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal sealed class MultiStringAccessor : MultiAccessor, IMultiString
	{
		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="obj"></param>b
		/// <param name="flid"></param>
		public MultiStringAccessor(ICmObject obj, int flid)
			: base(obj, flid)
		{ /* Nothing else to do. */ }

		#endregion Construction

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString GetAlternative(SpecialWritingSystemCodes code)
		{
			switch (code)
			{
				case SpecialWritingSystemCodes.DefaultAnalysis:
					return AnalysisDefaultWritingSystem;
				case SpecialWritingSystemCodes.DefaultVernacular:
					return VernacularDefaultWritingSystem;
				case SpecialWritingSystemCodes.BestAnalysis:
					return BestAnalysisAlternative;
				case SpecialWritingSystemCodes.BestVernacular:
					return BestVernacularAlternative;
				case SpecialWritingSystemCodes.BestAnalysisOrVernacular:
					return BestAnalysisVernacularAlternative;
				case SpecialWritingSystemCodes.BestVernacularOrAnalysis:
					return BestVernacularAnalysisAlternative;
				default:
					throw new ArgumentException();
			}
		}
	}

	/// <summary>
	/// A delegate passed to VirtualStringAccessor which reads one alternative of the multistring from a method of the CmObject.
	/// </summary>
	/// <param name="ws"></param>
	/// <returns></returns>
	public delegate ITsString AlternativeReader(int ws);

	/// <summary>
	/// A delegate passed to VirtualStringAccessor which writes one alternative of the multistring from a method of the CmObject.
	/// </summary>
	/// <param name="ws"></param>
	/// <param name="tss"></param>
	public delegate void AlternativeWriter(int ws, ITsString tss);

	/// <summary>
	/// Implements virtual multistring properties.
	/// </summary>
	internal class VirtualStringAccessor: MultiAccessorBase
	{
		private readonly AlternativeReader m_reader;
		private readonly AlternativeWriter m_writer;
		/// <summary>
		/// Make one (read-only).
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		/// <param name="reader"></param>
		public VirtualStringAccessor(ICmObject obj, int flid, AlternativeReader reader) : base(obj, flid)
		{
			Debug.Assert(reader != null);
			m_reader = reader;
		}

		/// <summary>
		/// Make one (read-write).
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		public VirtualStringAccessor(ICmObject obj, int flid, AlternativeReader reader, AlternativeWriter writer) : this(obj, flid, reader)
		{
			Debug.Assert(writer != null);
			m_writer = writer;
		}

		/// <summary>
		/// Currently there is no way to know how what alternatives there are.
		/// </summary>
		public override ITsString GetStringFromIndex(int iws, out int _ws)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get it.
		/// </summary>
		public override ITsString get_String(int ws)
		{
			return m_reader(ws);
		}

		/// <summary>
		/// Get one, but answer null if no known value.
		/// </summary>
		public override ITsString StringOrNull(int ws)
		{
			return m_reader(ws);
		}

		/// <summary>
		/// Set it, if we know how.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="tss"></param>
		public override void set_String(int ws, ITsString tss)
		{
			if (m_writer == null)
				throw new Exception("Cannot write a virtual string property for which no writer is defined: property " + m_flid);
			tss = TsStringUtils.NormalizeNfd(tss);
			m_writer(ws, tss);
		}

		/// <summary>
		/// Currently there is no way to know how many alternatives there are.
		/// </summary>
		public override int StringCount
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Currently there is no way to know how what alternatives there are.
		/// </summary>
		public override int[] AvailableWritingSystemIds
		{
			get { throw new NotImplementedException(); }
		}
	}
}
