using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Collections.Generic;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class manages various information about lists of writing systems.
	/// Review JohnT: would these be more naturally methods of LangProject?
	/// If not, where should this class go? Note that much of the functionality ought to be
	/// used by MultiStringSlice, and probably by other classes that are nothing to do with
	/// IText. I'm thinking it might belong in the same DLL as LangProject.
	/// </summary>
	public class WsListManager : IFWDisposable
	{
		ILangProject m_lp;
		ITsString m_tssColon;
		ITsString[] m_labels;
		int[] m_labelBasis; // Array of HVOs that m_labels was based on.
		ITsTextProps m_ttpLabelStyle;

		/// <summary>
		/// Create one starting from a language project.
		/// </summary>
		/// <param name="lp"></param>
		public WsListManager(ILangProject lp)
		{
			m_lp = lp;
		}

		/// <summary>
		/// Create one starting from an FdoCache.
		/// </summary>
		/// <param name="cache"></param>
		public WsListManager(FdoCache cache): this(cache.LangProject)
		{
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

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

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
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
			// Must not be run more than once.
			if (m_isDisposed)
				return;

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
			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		public int[] AnalysisWsIds
		{
			get
			{
				CheckDisposed();

				Debug.Assert(m_lp != null);
				return m_lp.CurrentAnalysisWritingSystems.Select(ws => ws.Handle).ToArray();
			}
		}

		/// <summary>
		/// Returns the index of ws in AnalysisWsIds
		/// </summary>
		/// <param name="ws"></param>
		/// <returns>-1 if not found</returns>
		public int IndexOf(int ws)
		{
			CheckDisposed();

			for (int index = 0; index < AnalysisWsIds.Length; index++)
			{
				if (AnalysisWsIds[index] == ws)
					return index;
			}
			return -1;
		}

		/// <summary>
		/// Return an array of writing systems given an array of their HVOs.
		/// </summary>
		/// <returns></returns>
		private IWritingSystem[] WssFromHvos(int[] hvos)
		{
			var lgWss = new List<IWritingSystem>();
			foreach (int ws in hvos)
				lgWss.Add(m_lp.Services.WritingSystemManager.Get(ws));
			return lgWss.ToArray();
		}
		/// <summary>
		/// Return an array of the analysis writing systems the user wants.
		/// </summary>
		/// <returns></returns>
		public ILgWritingSystem[] AnalysisWss
		{
			get
			{
				CheckDisposed();

				return WssFromHvos(AnalysisWsIds);
			}
		}

		private bool equalArrays(int[] v1, int[] v2)
		{
			if (v1.Length != v2.Length)
				return false;
			for (int i = 0; i < v1.Length; i++)
				if (v1[i] != v2[i])
					return false;
			return true;
		}

		/// <summary>
		/// Return an array of the strings we typically use as labels for analysis writing systems.
		/// These strings use direct formatting to 8pt and light blue color.
		/// </summary>
		public ITsString[] AnalysisWsLabels
		{
			get
			{
				CheckDisposed();

				if (m_labels == null || ! equalArrays(m_labelBasis, AnalysisWsIds))
				{
					ITsTextProps ttp = LanguageCodeStyle;
					ILgWritingSystem[] wssAnalysis = AnalysisWss;
					List<ITsString> labels = new List<ITsString>();
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					foreach (IWritingSystem ws in AnalysisWss)
					{
						string sAbbr = ws.Abbreviation;
						labels.Add(tsf.MakeStringWithPropsRgch(sAbbr, sAbbr.Length, ttp));
					}
					m_labels = labels.ToArray();
					m_labelBasis = AnalysisWsIds;
				}
				return m_labels;
			}
		}

		public static ITsString WsLabel(FdoCache cache, int ws)
		{
			IWritingSystem wsObj = cache.ServiceLocator.WritingSystemManager.Get(ws);
			ITsString abbr = StringUtils.MakeTss(wsObj.Abbreviation, cache.DefaultUserWs, "Language Code");
			ITsStrBldr tsb = abbr.GetBldr();
			tsb.SetProperties(0, tsb.Length, LanguageCodeTextProps(cache.DefaultUserWs));
			return tsb.GetString();
		}

		/// <summary>
		/// Add to the current display (a paragraph should be open) a label followed by colon, in the standard style,
		/// that identifies a particular writing system from the current list.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="iws"></param>
		public void AddWsLabel(IVwEnv vwenv, int iws)
		{
			CheckDisposed();

			if (m_tssColon == null)
			{
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				m_tssColon = tsf.MakeString(": ", m_lp.Cache.DefaultUserWs);
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
		public ITsTextProps LanguageCodeStyle
		{
			get
			{
				CheckDisposed();
				return LanguageCodeTextProps(m_lp.Cache.DefaultUserWs);
			}
		}

		/// <summary>
		/// Return an ITsTextProps typically used for WS labels in views.
		/// </summary>
		public static ITsTextProps LanguageCodeTextProps(int wsUser)
		{
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
				wsUser);
			tpb.SetIntPropValues((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
				BGR(47, 96, 255));
			tpb.SetIntPropValues((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint,
				8000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
				(int)TptEditable.ktptNotEditable);
			tpb.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, SimpleRootSite.UiElementStylename);
			return tpb.GetTextProps();
			}

		private static int BGR(int red, int green, int blue)
		{
			return red + (blue * 256 + green) * 256;
		}

		// Return a comma-separated list of the current analysis writing systems.
		// Might be marginally faster to use a string builder, but remember that 9/10 times
		// the for loop will execute zero times.
		internal string AnalysisWssIdsString
		{
			get
			{
				CheckDisposed();

				int[] wssAnalysis = AnalysisWsIds;
				if (wssAnalysis.Length == 0)
					return ""; // best we can do, though may not work well.
				string result = "" + wssAnalysis[0];
				for (int i = 1; i < wssAnalysis.Length; ++i)
					result += "," + wssAnalysis[i];
				return result;
			}

		}
		// Enhance JohnT: eventually we need similar stuff for vernacular writing systems, and
		// perhaps for combined lists such as vernacular and analysis.
	}
}
