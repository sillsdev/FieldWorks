// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// This class manages various information about lists of writing systems.
	/// Review JohnT: would these be more naturally methods of LangProject?
	/// If not, where should this class go? Note that much of the functionality ought to be
	/// used by MultiStringSlice, and probably by other classes that are nothing to do with
	/// IText. I'm thinking it might belong in the same DLL as LangProject.
	/// </summary>
	public class WsListManager : IDisposable
	{
		private ILangProject m_lp;
		private ITsString m_tssColon;
		private ITsString[] m_labels;
		private int[] m_labelBasis; // Array of HVOs that m_labels was based on.
		private ITsTextProps m_ttpLabelStyle;

		/// <summary>
		/// Create one starting from a language project.
		/// </summary>
		public WsListManager(ILangProject lp)
		{
			m_lp = lp;
		}

		/// <summary>
		/// Create one starting from an LcmCache.
		/// </summary>
		public WsListManager(LcmCache cache)
			: this(cache.LangProject)
		{
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~WsListManager()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + ". ******************");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_lp = null;
			m_labels = null;
			m_labelBasis = null;
			m_tssColon = null;
			m_ttpLabelStyle = null;
			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		public int[] AnalysisWsIds
		{
			get
			{
				Debug.Assert(m_lp != null);
				return m_lp.CurrentAnalysisWritingSystems.Select(ws => ws.Handle).ToArray();
			}
		}

		/// <summary>
		/// Returns the index of ws in AnalysisWsIds, or -1 if not found.
		/// </summary>
		public int IndexOf(int ws)
		{
			for (var index = 0; index < AnalysisWsIds.Length; index++)
			{
				if (AnalysisWsIds[index] == ws)
				{
					return index;
				}
			}
			return -1;
		}

		/// <summary>
		/// Return an array of writing systems given an array of their HVOs.
		/// </summary>
		private CoreWritingSystemDefinition[] WssFromHvos(int[] hvos)
		{
			return hvos.Select(ws => m_lp.Services.WritingSystemManager.Get(ws)).ToArray();
		}

		/// <summary>
		/// Return an array of the analysis writing systems the user wants.
		/// </summary>
		public CoreWritingSystemDefinition[] AnalysisWss => WssFromHvos(AnalysisWsIds);

		private bool equalArrays(int[] v1, int[] v2)
		{
			return v1.Length == v2.Length && !v1.Where((t, i) => t != v2[i]).Any();
		}

		/// <summary>
		/// Return an array of the strings we typically use as labels for analysis writing systems.
		/// These strings use direct formatting to 8pt and light blue color.
		/// </summary>
		public ITsString[] AnalysisWsLabels
		{
			get
			{
				if (m_labels != null && equalArrays(m_labelBasis, AnalysisWsIds))
				{
					return m_labels;
				}
				var ttp = LanguageCodeStyle;
				m_labels = AnalysisWss.Select(ws => ws.Abbreviation).Select(sAbbr => TsStringUtils.MakeString(sAbbr, ttp)).ToArray();
				m_labelBasis = AnalysisWsIds;
				return m_labels;
			}
		}

		public static ITsString WsLabel(LcmCache cache, int ws)
		{
			var wsObj = cache.ServiceLocator.WritingSystemManager.Get(ws);
			var abbr = TsStringUtils.MakeString(wsObj.Abbreviation, cache.DefaultUserWs, "Language Code");
			var tsb = abbr.GetBldr();
			tsb.SetProperties(0, tsb.Length, LanguageCodeTextProps(cache.DefaultUserWs));
			return tsb.GetString();
		}

		/// <summary>
		/// Add to the current display (a paragraph should be open) a label followed by colon, in the standard style,
		/// that identifies a particular writing system from the current list.
		/// </summary>
		public void AddWsLabel(IVwEnv vwenv, int iws)
		{
			if (m_tssColon == null)
			{
				m_tssColon = TsStringUtils.MakeString(": ", m_lp.Cache.DefaultUserWs);
			}
			if (m_ttpLabelStyle == null)
			{
				m_ttpLabelStyle = LanguageCodeStyle;
			}
			vwenv.Props = m_ttpLabelStyle;
			vwenv.OpenSpan();
			vwenv.AddString(AnalysisWsLabels[iws]);
			vwenv.AddString(m_tssColon);
			vwenv.CloseSpan();
		}

		/// <summary>
		/// Return an ITsTextProps typically used for WS labels in views.
		/// </summary>
		public ITsTextProps LanguageCodeStyle => LanguageCodeTextProps(m_lp.Cache.DefaultUserWs);

		/// <summary>
		/// Return an ITsTextProps typically used for WS labels in views.
		/// </summary>
		public static ITsTextProps LanguageCodeTextProps(int wsUser)
		{
			var tpb = TsStringUtils.MakePropsBldr();
			tpb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, wsUser);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, BGR(47, 96, 255));
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, 8000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, StyleServices.UiElementStylename);
			return tpb.GetTextProps();
		}

		private static int BGR(int red, int green, int blue)
		{
			return red + (blue * 256 + green) * 256;
		}

		/// <summary>
		/// Return a comma-separated list of the current analysis writing systems.
		/// Might be marginally faster to use a string builder, but remember that 9/10 times
		/// the for loop will execute zero times.
		/// </summary>
		internal string AnalysisWssIdsString
		{
			get
			{
				var wssAnalysis = AnalysisWsIds;
				if (wssAnalysis.Length == 0)
				{
					return string.Empty; // best we can do, though may not work well.
				}
				var result = string.Empty + wssAnalysis[0];
				for (var i = 1; i < wssAnalysis.Length; ++i)
				{
					result += "," + wssAnalysis[i];
				}
				return result;
			}

		}
	}
}