// <copyright from='2010' to='2010' company='SIL International'>
// Copyright (c) 2010, SIL International. All Rights Reserved.
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// Various string level domain services.
	/// </summary>
	public static class StringServices
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the empty name of the file.
		/// </summary>
		/// <value>The empty name of the file.</value>
		/// ------------------------------------------------------------------------------------
		public static string EmptyFileName
		{
			get { return ".__NONE__"; }
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
		/// <param name="linkStyle">The style to use to mark the hyperlink.</param>
		/// ------------------------------------------------------------------------------------
		public static void MarkTextInBldrAsHyperlink(ITsStrBldr strBldr, int ichStart,
			int ichLim, string url, IStStyle linkStyle)
		{
			var propVal = Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName) + url;
			if (!linkStyle.InUse && linkStyle is StStyle)
				((StStyle)linkStyle).InUse = true;
			strBldr.SetStrPropValue(ichStart, ichLim, (int)FwTextPropType.ktptNamedStyle, linkStyle.Name);
			strBldr.SetStrPropValue(ichStart, ichLim, (int)FwTextPropType.ktptObjData, propVal);
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
		/// <param name="linkStyle">The style to use to mark the hyperlink.</param>
		/// <param name="linkedFilesRootDir">The project's LinkedFilesRootDir</param>
		/// <returns>The value returned is used to create a CmFile if it represents a file path.</returns>
		/// ------------------------------------------------------------------------------------
		public static string MarkTextInBldrAsHyperlink(ITsStrBldr strBldr, int ichStart,
			int ichLim, string url, IStStyle linkStyle, string linkedFilesRootDir)
		{
			var relativeUrl = LinkedFilesRelativePathHelper.GetRelativeLinkedFilesPath(url, linkedFilesRootDir);
			if (string.IsNullOrEmpty(relativeUrl))
			{
				MarkTextInBldrAsHyperlink(strBldr, ichStart, ichLim, url, linkStyle);
				return url;
			}
			else
			{
				MarkTextInBldrAsHyperlink(strBldr, ichStart, ichLim, relativeUrl, linkStyle);
				return relativeUrl;
			}
		}

		/// <summary>
		/// Result class for GetHyperlinksInFolder. Records the place where one hyperlink occurs.
		/// </summary>
		public class LinkOccurrence
		{
			/// <summary>
			/// The object in which the link occurs.
			/// </summary>
			public ICmObject Object { get; internal set; }
			/// <summary>
			/// The field in which the link occurs.
			/// </summary>
			public int Flid { get; internal set; }
			/// <summary>
			/// The writing system alternative in which the link occurs, or 0 if the property is not multilingual
			/// </summary>
			public int Ws { get; internal set; }

			/// <summary>
			/// The index of the first character of the link.
			/// </summary>
			public int IchMin { get; internal set; }
			/// <summary>
			/// The end of the link (index of the first following character).
			/// </summary>
			public int IchLim { get; internal set; }
			/// <summary>
			/// The actual text of the link, minus the part of the path that was matched. If that starts with
			/// the directory separator, it is removed.
			/// </summary>
			public string RelativePath;
		}

		/// <summary>
		/// Find all the LinkedFiles in the language project which are to files in the specified folder.
		/// (Strictly, any link which begins with the specified string.)
		/// </summary>
		public static List<LinkOccurrence> GetHyperlinksInFolder(FdoCache cache, string folder)
		{
			var result = new List<LinkOccurrence>();
			string prefix = Convert.ToChar((int) FwObjDataTypes.kodtExternalPathName).ToString();
			var match = prefix + folder;
			CrawlStyledStrings(cache,
				(obj, flid, ws, tss) =>
					{
						int crun = tss.RunCount;
						for (int irun = 0; irun < crun; irun++)
						{
							var objData = tss.get_StringProperty(irun, (int) FwTextPropType.ktptObjData);

							// Convert path from database to platform-style path if needed,
							// and don't bother if null for performance.
							if (objData != null)
								objData = FileUtils.ChangePathToPlatformPreservingPrefix(objData, prefix);

							if (objData == null || !objData.StartsWith(match))
								continue; // not an interesting run
							var relPath = objData.Substring(match.Length);
							if (relPath.StartsWith("" + Path.DirectorySeparatorChar))
								relPath = relPath.Substring(1);
							int ichMin, ichLim;
							tss.GetBoundsOfRun(irun, out ichMin, out ichLim);
							result.Add(new LinkOccurrence()
								{Object = obj, Flid = flid, Ws = ws, IchMin = ichMin, IchLim = ichLim, RelativePath = relPath});
						}
					});
			return result;
		}

		/// <summary>
		/// Given a set of link occurrences (as found by GetHyperLinksInFolder), update them to refer to
		/// the new destination folder. If the text that is linked to a destination matches the destination, update it also.
		/// </summary>
		public static void FixHyperlinkFolder(List<LinkOccurrence> hyperlinkInfo, string oldFolder, string newFolder)
		{
			// order by negative ichMin so that in any given string, the last occurrence will be changed first.
			// This means if the length changes we don't get our offsets messed up.
			foreach (var linkInfo in hyperlinkInfo.OrderBy(info => -info.IchMin))
			{
				var obj = linkInfo.Object;
				var sda = obj.Cache.DomainDataByFlid;
				ITsString tss;
				if (linkInfo.Ws == 0)
					tss = sda.get_StringProp(obj.Hvo, linkInfo.Flid);
				else
					tss = sda.get_MultiStringAlt(obj.Hvo, linkInfo.Flid, linkInfo.Ws);
				var objData = tss.get_StringPropertyAt(linkInfo.IchMin, (int) FwTextPropType.ktptObjData);

				// Convert path from database to platform-style path if needed.
				var origObjData = objData;
				objData = FileUtils.ChangePathToPlatformPreservingPrefix(objData, objData.Substring(0, 1));

				var newObjData = objData.Substring(0, 1) + newFolder + objData.Substring(1 + oldFolder.Length);
				var bldr = tss.GetBldr();
				int ichLim = linkInfo.IchLim;
				if (origObjData.Substring(1) == tss.Text.Substring(linkInfo.IchMin, linkInfo.IchLim - linkInfo.IchMin))
				{
					// The link's text is equal to its destination. Update both.
					bldr.Replace(linkInfo.IchMin, linkInfo.IchLim, newObjData.Substring(1), null);
					ichLim = linkInfo.IchMin + newObjData.Length - 1;
				}
				bldr.SetStrPropValue(linkInfo.IchMin, ichLim, (int)FwTextPropType.ktptObjData, newObjData);
				if (linkInfo.Ws == 0)
					sda.SetString(obj.Hvo, linkInfo.Flid, bldr.GetString());
				else
					sda.SetMultiStringAlt(obj.Hvo, linkInfo.Flid, linkInfo.Ws, bldr.GetString());
			}
		}

		/// <summary>
		/// This allows us to get the headword without actually creating an instance...
		/// which can be slow.
		/// </summary>
		internal static ITsString HeadWordStaticForWs(ILexEntry entry, int wsVern)
		{
			return HeadWordForWsAndHn(entry, wsVern, entry.HomographNumber);
		}

		/// <summary>
		/// Get what would be produced for the headword of the specified entry in the specified WS,
		/// if it had the specified homograph number. (This is useful when overriding homograph
		/// numbers because items are omitted in a particular publication.)
		/// </summary>
		public static ITsString HeadWordForWsAndHn(ILexEntry entry, int wsVern, int nHomograph)
		{
			return HeadWordForWsAndHn(entry, wsVern, nHomograph, DefaultHomographString());
		}
		/// <summary>
		/// Get what would be produced for the headword of the specified entry in the specified WS,
		/// if it had the specified homograph number. (This is useful when overriding homograph
		/// numbers because items are omitted in a particular publication.) Use the specified default
		/// Citation Form if no Cf or Lf is present in the entry, and return an empty string for the
		/// whole method if there is no real or default Cf.
		/// </summary>
		public static ITsString HeadWordForWsAndHn(ILexEntry entry, int wsVern, int nHomograph, string defaultCf)
		{
			return HeadWordForWsAndHn(entry, wsVern, nHomograph, defaultCf, HomographConfiguration.HeadwordVariant.Main);
		}

		/// <summary>
		/// Get what would be produced for the headword of the specified entry in the specified WS,
		/// if it had the specified homograph number. (This is useful when overriding homograph
		/// numbers because items are omitted in a particular publication.) Use the specified default
		/// Citation Form if no Cf or Lf is present in the entry, and return an empty string for the
		/// whole method if there is no real or default Cf.
		/// </summary>
		public static ITsString HeadWordForWsAndHn(ILexEntry entry, int wsVern, int nHomograph, string defaultCf,
			HomographConfiguration.HeadwordVariant hv)
		{
			var citationForm = CitationFormWithAffixTypeStaticForWs(entry, wsVern, defaultCf);
			if (String.IsNullOrEmpty(citationForm))
				return entry.Cache.TsStrFactory.EmptyString(wsVern);
			var tisb = TsIncStrBldrClass.Create();
			AddHeadwordForWsAndHn(entry, wsVern, nHomograph, hv, tisb, citationForm);
			return tisb.GetString();
		}

		/// <summary>
		/// This method should have the same logic as the (above) HeadWordForWsAndHn().
		/// </summary>
		/// <param name="tisb"></param>
		/// <param name="entry"></param>
		/// <param name="wsVern"></param>
		/// <param name="nHomograph"></param>
		/// <param name="defaultCf"></param>
		/// <param name="hv"></param>
		internal static void AddHeadWordForWsAndHn(ITsIncStrBldr tisb, ILexEntry entry, int wsVern, int nHomograph, string defaultCf,
	HomographConfiguration.HeadwordVariant hv)
		{
			var citationForm = CitationFormWithAffixTypeStaticForWs(entry, wsVern, defaultCf);
			if (String.IsNullOrEmpty(citationForm))
			{
				tisb.AppendTsString(entry.Cache.TsStrFactory.EmptyString(wsVern)); // avoids COM Exception!
				return;
			}
			AddHeadwordForWsAndHn(entry, wsVern, nHomograph, hv, tisb, citationForm);
		}

		private static void AddHeadwordForWsAndHn(ILexEntry entry, int wsVern, int nHomograph,
			HomographConfiguration.HeadwordVariant hv, ITsIncStrBldr tisb, string citationForm)
		{
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsVern);
			var hc = entry.Services.GetInstance<HomographConfiguration>();
			if (hc.HomographNumberBefore)
				InsertHomographNumber(tisb, nHomograph, hc, hv);
			tisb.Append(citationForm);

			// (EricP) Tried to automatically update the homograph number, but doing that here will
			// steal away manual changes to the HomographNumber column. Also suppressing PropChanged
			// is necessary when HomographNumber column is enabled, otherwise changing the entry index can hang.
			//using (new IgnorePropChanged(cache, PropChangedHandling.SuppressView))
			//{
			//	  ValidateExistingHomographs(CollectHomographs(cache, ShortName1StaticForWs(cache, hvo, wsVern), 0, morphType));
			//}

			if (!hc.HomographNumberBefore)
				InsertHomographNumber(tisb, nHomograph, hc, hv);
		}

		private static void InsertHomographNumber(ITsIncStrBldr tisb, int nHomograph, HomographConfiguration hc,
			HomographConfiguration.HeadwordVariant hv)
		{
			if (nHomograph > 0 && hc.ShowHomographNumber(hv))
			{
				tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, HomographConfiguration.ksHomographNumberStyle);
				tisb.Append(nHomograph.ToString());
				tisb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			}
		}

		/// <summary>
		/// Static version for avoiding creating actual object.
		/// </summary>
		public static string ShortName1Static(ILexEntry entry)
		{
			return ShortName1StaticForWs(entry, entry.Cache.DefaultVernWs);
		}

		/// <summary>
		/// Static version for avoiding creating actual object.
		/// </summary>
		public static string ShortName1StaticForWs(ILexEntry entry, int wsVern)
		{
			return ShortName1StaticForWs(entry, wsVern, DefaultHomographString());
		}

		/// <summary>
		/// Static version for avoiding creating actual object.
		/// </summary>
		public static string ShortName1StaticForWs(ILexEntry entry, int wsVern, string defaultForm)
		{
			// try vernacular citation
			var tss = entry.CitationForm.get_String(wsVern);
			if (tss != null && tss.Length != 0)
				return tss.Text;

			return LexemeFormStaticForWs(entry, wsVern, defaultForm);
		}

		/// <summary>
		/// Static version for avoiding creating actual object.
		/// </summary>
		public static void ShortName1Static(ILexEntry entry, ITsIncStrBldr tsb)
		{
			var ws = entry.Services.WritingSystems.DefaultVernacularWritingSystem;

			// Try vernacular citation
			var tss = entry.CitationForm.get_String(ws.Handle);
			if (tss != null && tss.Length > 0)
			{
				tsb.AppendTsString(tss);
				return;
			}

			// Try lexeme form.
			var form = entry.LexemeFormOA;
			if (form != null)
			{
				tss = form.Form.get_String(ws.Handle);
				if (tss != null && tss.Length > 0)
				{
					tsb.AppendTsString(tss);
					return;
				}
			}

			// Try the first alternate form with the wsVern WS.
			// NB: This may not be the actual first alterantge.
			foreach (var alt in entry.AlternateFormsOS)
			{
				tss = alt.Form.get_String(ws.Handle);
				if (tss != null && tss.Length > 0)
				{
					tsb.AppendTsString(tss);
					return;
				}
			}

			// Give up.
			tsb.AppendTsString(entry.Cache.TsStrFactory.MakeString(
				Strings.ksQuestions,
				entry.Cache.DefaultUserWs));
		}

		/// <summary>
		/// Same as ShortName, but ignores Citation form.
		/// </summary>
		internal static string LexemeFormStatic(ILexEntry entry)
		{
			return LexemeFormStaticForWs(entry, entry.Cache.DefaultVernWs);
		}

		internal static string LexemeFormStaticForWs(ILexEntry entry, int wsVern)
		{
			return LexemeFormStaticForWs(entry, wsVern, DefaultHomographString());
		}

		internal static string LexemeFormStaticForWs(ILexEntry entry, int wsVern, string defaultForm)
		{
			ITsString tss;
			// try lexeme form
			var form = entry.LexemeFormOA;
			if (form != null)
			{
				tss = form.Form.get_String(wsVern);
				if (tss != null && tss.Length > 0)
					return tss.Text;
			}
			// Try the first alternate form with the wsVern WS.
			return FirstAlternateForm(entry, wsVern, defaultForm);
		}

		/// <summary>
		/// The final fall-backin making a homograph form, we try the first alternate form.
		/// (Why not any others??)
		/// </summary>
		internal static string FirstAlternateForm(ILexEntry entry, int wsVern)
		{
			return FirstAlternateForm(entry, wsVern, DefaultHomographString());
		}
		/// <summary>
		/// The final fall-backin making a homograph form, we try the first alternate form.
		/// (Why not any others??)
		/// </summary>
		internal static string FirstAlternateForm(ILexEntry entry, int wsVern, string defaultForm)
		{
			IMoForm form;
			ITsString tss;
			if (entry.AlternateFormsOS.Count > 0)
			{
				form = entry.AlternateFormsOS[0];
				tss = form.Form.get_String(wsVern);
				if (tss != null && tss.Length > 0)
					return tss.Text;
			}

			return defaultForm;
		}

		/// <summary>
		/// The string we use for HomographForm if we can't find any plausible form.
		/// </summary>
		public static string DefaultHomographString()
		{
			return Strings.ksQuestions;
		}

		/// <summary>
		/// Static version for cases where we can't afford to create the object.
		/// Note that the name is now somewhat dubious...This shows citation form if present
		/// otherwise lexeme form, otherwise question marks. Affix markers are added.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="wsVern"></param>
		/// <returns></returns>
		public static string CitationFormWithAffixTypeStaticForWs(ILexEntry entry, int wsVern)
		{
			return CitationFormWithAffixTypeStaticForWs(entry, wsVern, DefaultHomographString());
		}

		/// <summary>
		/// Static version for cases where we can't afford to create the object.
		/// Note that the name is now somewhat dubious...This shows citation form if present
		/// otherwise lexeme form, otherwise question marks. Affix markers are added.
		/// </summary>
		/// <param name="entry"></param>
		/// <param name="wsVern"></param>
		/// <param name="defaultCf">default to show for citation form part of name if none is
		/// recorded for this writing system. If this is empty, the whole method should return
		/// an empty string if there is no valid CF.</param>
		/// <returns></returns>
		public static string CitationFormWithAffixTypeStaticForWs(ILexEntry entry, int wsVern, string defaultCf)
		{
			// This is optimized for speed, in case you were wondering.  PreLoadShortName()
			// loads all of the data this references directly.
			string form = ShortName1StaticForWs(entry, wsVern, defaultCf);
			if (String.IsNullOrEmpty(form))
				return "";
			return DecorateFormWithAffixMarkers(entry, form);
		}

		internal static string DecorateFormWithAffixMarkers(ILexEntry entry, string form)
		{
			var mForm = entry.LexemeFormOA;
			// No type info...return simpler version of name.
			if (mForm == null)
				return form;

			// Add pre- post markers, if any.
			var mmt = mForm.MorphTypeRA;
			var prefix = String.Empty;
			var postfix = String.Empty;
			if (mmt != null) // It may be null.
			{
				prefix = mmt.Prefix;
				postfix = mmt.Postfix;
			}
			return prefix + form + postfix;
		}

		/// <summary>
		/// Strips the 'magic' prefix that identifies a configurable WS parameter.
		/// </summary>
		public static string GetWsSpecWithoutPrefix(XmlNode spec)
		{
			return GetWsSpecWithoutPrefix(XmlUtils.GetOptionalAttributeValue(spec, "ws"));
		}

		/// <summary>
		/// Strips the 'magic' prefix that identifies a configurable WS parameter.
		/// </summary>
		public static string GetWsSpecWithoutPrefix(string wsSpec)
		{
			if (wsSpec != null && wsSpec.StartsWith(WsParamLabel))
				return wsSpec.Substring(WsParamLabel.Length);
			return wsSpec;
		}

		/// <summary>
		/// The 'magic' string that identifies a writing-system parameter.
		/// </summary>
		public static string WsParamLabel
		{
			get { return "$ws="; }
		}

		/// <summary>
		/// Confirm that we can break the specified string into the two parts needed for a circumfix.
		/// Return true (and the two parts, not stripped of affix markers) if successful.
		/// </summary>
		public static bool GetCircumfixLeftAndRightParts(FdoCache cache, ITsString tssLexemeForm,
														 out string sLeftMember, out string sRightMember)
		{
			// split citation form into left and right parts
			sLeftMember = null;
			sRightMember = null;
			var aSpacePeriod = new[] { ' ', '.' };
			var lexemeForm = tssLexemeForm.Text;
			var iLeftEnd = lexemeForm.IndexOfAny(aSpacePeriod);
			if (iLeftEnd < 0)
				return false;

			sLeftMember = lexemeForm.Substring(0, iLeftEnd);
			int iRightBegin = lexemeForm.LastIndexOfAny(aSpacePeriod);
			if (iRightBegin < 0)
				return false;

			sRightMember = lexemeForm.Substring(iRightBegin + 1);
			int clsidForm;
			try
			{
				var temp = sLeftMember;
				MorphServices.FindMorphType(cache, ref temp, out clsidForm);
				temp = sRightMember;
				MorphServices.FindMorphType(cache, ref temp, out clsidForm);
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Convert XML message returned from environ validator to English
		/// </summary>
		/// <param name="strRep1">The environment string itself</param>
		/// <param name="sXmlMessage">XML returned from validator</param>
		/// <param name="pos">position value</param>
		/// <param name="sMessage">The created message</param>
		public static void CreateErrorMessageFromXml(string strRep1, string sXmlMessage, out int pos, out string sMessage)
		{
			var strRep = strRep1;
			if (strRep1 == null)
				strRep = "";
			var xdoc = new XmlDocument();
			var sStatus = "";
			pos = 0;
			try
			{
				// The validator message, unfortunately, may be invalid XML if
				// there were XML reserved characters in the environment.
				// until we get that fixed, at least don't crash, just draw squiggly under the entire word
				xdoc.LoadXml(sXmlMessage);
				var posAttr = xdoc.DocumentElement.Attributes["pos"];
				pos = (posAttr != null) ? Convert.ToInt32(posAttr.Value) : 0;
				var statusAttr = xdoc.DocumentElement.Attributes["status"];
				sStatus = statusAttr.InnerText;
			}
			catch
			{
				// Eat the exception.
			}
			var len = strRep.Length;
			if (pos >= len)
				pos = Math.Max(0, len - 1); // make sure something will show
			//todo: if the string itself will be part of this message, this needs
			// to put the right places in the right writing systems. note that
			//there is a different constructor we can use which takes a sttext.
			var bldrMsg = new StringBuilder();
			bldrMsg.AppendFormat(Strings.ksBadEnv, strRep);
			if (sStatus == "class")
			{
				var iRightBracket = strRep.Substring(pos).IndexOf(']');
				var sClass = strRep.Substring(pos, iRightBracket);
				bldrMsg.AppendFormat(Strings.ksBadClassInEnv, sClass);
			}
			if (sStatus == "segment")
			{
				bldrMsg.AppendFormat(Strings.ksBadPhonemeInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingClosingParen")
			{
				bldrMsg.AppendFormat(Strings.ksMissingCloseParenInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingOpeningParen")
			{
				bldrMsg.AppendFormat(Strings.ksMissingOpenParenInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingClosingSquareBracket")
			{
				bldrMsg.AppendFormat(Strings.ksMissingCloseBracketInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingOpeningSquareBracket")
			{
				bldrMsg.AppendFormat(Strings.ksMissingOpenBracketInEnv, strRep.Substring(pos));
			}
			if (sStatus == "syntax")
			{
				bldrMsg.AppendFormat(Strings.ksBadEnvSyntax, strRep.Substring(pos));
			}
			sMessage = bldrMsg.ToString();
		}

		/// <summary>
		/// Crawls all strings in the specified FDO cache. The string modifier is called for each string property
		/// in FDO. If the string modifier returns <c>null</c>, the string is removed from its original property. The
		/// multi-string modifier is called for each multi-string/multi-unicode property in FDO.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="stringModifier">The string modifier.</param>
		/// <param name="multiStringModifier">The multi string modifier.</param>
		public static void CrawlStrings(FdoCache cache, Func<ITsString, ITsString> stringModifier, Action<ITsMultiString> multiStringModifier)
		{
			CrawlStrings(cache, (obj, str) => stringModifier(str), (obj, ms) => multiStringModifier(ms));
		}

		/// <summary>
		/// Crawls all strings in the specified FDO cache. The string modifier is called for each string property
		/// in FDO. If the string modifier returns <c>null</c>, the string is removed from its original property. The
		/// multi-string modifier is called for each multi-string/multi-unicode property in FDO. This overload uses functions
		/// and actions that are also passed the object being worked on.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="stringModifier">The string modifier.</param>
		/// <param name="multiStringModifier">The multi string modifier.</param>
		public static void CrawlStrings(FdoCache cache, Func<ICmObject, ITsString, ITsString> stringModifier, Action<ICmObject, ITsMultiString> multiStringModifier)
		{
			var sda = (ISilDataAccessManaged)cache.DomainDataByFlid;
			var mdc = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			foreach (ICmObject obj in cache.ServiceLocator.ObjectRepository.AllInstances())
			{
				foreach (int flid in mdc.GetFields(obj.ClassID, true, (int)CellarPropertyTypeFilter.AllMulti
					| (int)CellarPropertyTypeFilter.String))
				{
					if (mdc.get_IsVirtual(flid))
						continue;

					switch ((CellarPropertyType)mdc.GetFieldType(flid))
					{
						case CellarPropertyType.String:
							ITsString oldStr = sda.get_StringProp(obj.Hvo, flid);
							ITsString newStr = stringModifier(obj, oldStr);
							if (oldStr != newStr)
								sda.SetString(obj.Hvo, flid, newStr);
							break;

						case CellarPropertyType.MultiString:
						case CellarPropertyType.MultiUnicode:
							multiStringModifier(obj, sda.get_MultiStringProp(obj.Hvo, flid));
							break;
					}
				}
			}
		}

		/// <summary>
		/// Crawls all strings in the specified FDO cache. doSomething is called for each string property
		/// in FDO. MultiUnicode is currently skipped. The two ints are flid and ws;
		/// ws will be passed as zero for non-multistrings.
		/// </summary>
		public static void CrawlStyledStrings(FdoCache cache, Action<ICmObject, int, int, ITsString> doSomething)
		{
			var sda = (ISilDataAccessManaged)cache.DomainDataByFlid;
			var mdc = (IFwMetaDataCacheManaged)cache.MetaDataCacheAccessor;
			foreach (ICmObject obj in cache.ServiceLocator.ObjectRepository.AllInstances())
			{
				foreach (int flid in mdc.GetFields(obj.ClassID, true, (int)CellarPropertyTypeFilter.AllMulti
					| (int)CellarPropertyTypeFilter.String))
				{
					if (mdc.get_IsVirtual(flid))
						continue;

					switch ((CellarPropertyType)mdc.GetFieldType(flid))
					{
						case CellarPropertyType.String:
							doSomething(obj, flid, 0, sda.get_StringProp(obj.Hvo, flid));
							break;

						case CellarPropertyType.MultiString:
						case CellarPropertyType.MultiUnicode:
							var multistring = sda.get_MultiStringProp(obj.Hvo, flid);
							for (int i = 0; i < multistring.StringCount; i++)
							{
								int ws;
								var tss = multistring.GetStringFromIndex(i, out ws);
								doSomething(obj, flid, ws, tss);
							}
							break;
					}
				}
			}
		}


		/// <summary>
		/// This overload applies the function to all strings, including all alternatives of all multistrings.
		/// </summary>
		public static void CrawlStrings(FdoCache cache, Func<ITsString, ITsString> modifier)
		{
			CrawlStrings(cache, modifier,
				multistring =>
					{
						for (int i = 0; i < multistring.StringCount; i++)
						{
							int ws;
							var tss = multistring.GetStringFromIndex(i, out ws);
							var newTss = modifier(tss);
							if (!tss.Equals(newTss))
								multistring.set_String(ws, newTss);
						}
					});
		}

		/// <summary>
		/// Replace the indicated styles throughout the database.
		/// If a key maps to another string, replace the style with that string.
		/// Otherwise (value is null) remove the style.
		/// Note: the case where the same string occurs as a key and a value is not allowed.
		/// </summary>
		public static void ReplaceStyles(FdoCache cache, Dictionary<string, string> specification)
		{
			foreach (var val in specification.Values)
				if (val != null && specification.ContainsKey(val))
					throw new ArgumentException("ReplaceStyles must not use the same style for input and output");
			CrawlStrings(cache,
				tss =>
					{
						int crun = tss.RunCount;
						ITsStrBldr bldr = null;
						for (int i = 0; i < crun; i++)
						{
							string newStyle;
							string oldStyle = tss.get_Properties(i).GetStrPropValue((int) FwTextPropType.ktptNamedStyle);
							if (oldStyle != null && specification.TryGetValue(oldStyle, out newStyle))
							{
								// We don't want to make a builder unless we need to change.
								if (bldr == null)
									bldr = tss.GetBldr();
								int ichMin, ichLim;
								tss.GetBoundsOfRun(i, out ichMin, out ichLim);
								bldr.SetStrPropValue(ichMin, ichLim, (int) FwTextPropType.ktptNamedStyle, newStyle);
							}
						}
						if (bldr != null)
							return bldr.GetString();
						return tss; // no change.
					});
			foreach (var para in cache.ServiceLocator.GetInstance<IStTxtParaRepository>().AllInstances())
			{
				var rules = para.StyleRules;
				if (rules == null)
					continue;
				string newStyleName;
				var oldStyleName = rules.GetStrPropValue((int) FwTextPropType.ktptNamedStyle);
				if (oldStyleName != null && specification.TryGetValue(oldStyleName, out newStyleName))
				{
					var bldr = rules.GetBldr();
					bldr.SetStrPropValue((int) FwTextPropType.ktptNamedStyle, newStyleName);
					para.StyleRules = bldr.GetTextProps();
				}
			}
		}

		/// <summary>
		/// Return true if the specified string has a run in the specified WS
		/// </summary>
		public static bool HasWs(ITsString tss, int ws)
		{
			var crun = tss.RunCount;
			for (int i = 0; i < crun; i++)
			{
				if (tss.get_WritingSystem(i) == ws)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Crawls all runs in the specified string. The run modifier is called for each run in the
		/// specified string. If the run modifier returns <c>null</c>, the run is removed from
		/// the string. If all runs are removed, this method returns <c>null</c>.
		/// </summary>
		/// <param name="str">The string.</param>
		/// <param name="runModifier">The run modifier.</param>
		/// <returns></returns>
		public static ITsString CrawlRuns(ITsString str, Func<ITsString, ITsString> runModifier)
		{
			ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
			bool modified = false;
			bool empty = true;
			for (int i = 0; i < str.RunCount; i++)
			{
				int ichMin, ichLim;
				str.GetBoundsOfRun(i, out ichMin, out ichLim);
				ITsString oldRun = str.GetSubstring(ichMin, ichLim);
				ITsString newRun = runModifier(oldRun);
				if (newRun != null)
				{
					if (modified || newRun != oldRun)
					{
						tisb.AppendTsString(newRun);
						modified = true;
					}
					empty = false;
				}
				else
				{
					modified = true;
				}
			}

			if (empty)
				return null;

			return modified ? tisb.GetString() : str;
		}
	}
}
