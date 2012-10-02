using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

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
		public WsListManager(FdoCache cache)
		{
			m_lp = cache.LangProject;
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_lp = null;
			if (m_labels != null)
			{
				foreach (ITsString label in m_labels)
					Marshal.ReleaseComObject(label);
				m_labels = null;
			}
			m_labelBasis = null;
			if (m_tssColon != null)
			{
				Marshal.ReleaseComObject(m_tssColon);
				m_tssColon = null;
			}
			if (m_ttpLabelStyle != null)
			{
				Marshal.ReleaseComObject(m_ttpLabelStyle);
				m_ttpLabelStyle = null;
			}

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		public int[] AnalysisWsIds
		{
			get
			{
				CheckDisposed();

				Debug.Assert(m_lp != null);
				return m_lp.CurAnalysisWssRS.HvoArray;
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
		private ILgWritingSystem[] WssFromHvos(int[] hvos)
		{
			ILgWritingSystem[] result = new LgWritingSystem[hvos.Length];
			for (int i = 0; i < hvos.Length; i++)
			{
				result[i] = new LgWritingSystem(m_lp.Cache, hvos[i]);
			}
			return result;
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
		/// These strings have the "Language Code" style set; something in the style sheet being
		/// used should define this appropriately.
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
					m_labels = new ITsString[wssAnalysis.Length];
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					int wsUi = m_lp.Cache.DefaultUserWs;
					for (int i = 0; i < wssAnalysis.Length; ++i)
					{
						string sAbbr = wssAnalysis[i].Abbr.UserDefaultWritingSystem;
						m_labels[i] = tsf.MakeStringWithPropsRgch(sAbbr, sAbbr.Length, ttp);
					}
					m_labelBasis = AnalysisWsIds;
				}
				return m_labels;
			}
		}

		public static ITsString WsLabel(FdoCache cache, int ws)
		{
			ITsPropsFactory tpf = TsPropsFactoryClass.Create();
			ITsTextProps ttp = tpf.MakeProps("Language Code", cache.DefaultUserWs, 0);
			ILgWritingSystem wsObj = LgWritingSystem.CreateFromDBObject(cache, ws);
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			string sAbbr = wsObj.Abbr.UserDefaultWritingSystem;
			return tsf.MakeStringWithPropsRgch(sAbbr, sAbbr.Length, ttp);
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
				ITsPropsFactory tpf = TsPropsFactoryClass.Create();
				// Get a ttp invoking the style "Language Code" style for the writing system
				// which corresponds to the user's environment.
				m_ttpLabelStyle = tpf.MakeProps("Language Code", m_lp.Cache.DefaultUserWs, 0);
			}
			vwenv.Props = m_ttpLabelStyle;
			vwenv.OpenSpan();
			vwenv.AddString(AnalysisWsLabels[iws]);
			vwenv.AddString(m_tssColon);
			vwenv.CloseSpan();
		}

		/// <summary>
		/// Return an ITsTextProps indicating the UI language and the "Language Code" style.
		/// This is typically used for WS labels in views.
		/// </summary>
		public ITsTextProps LanguageCodeStyle
		{
			get
			{
				CheckDisposed();

				ITsPropsFactory tpf = TsPropsFactoryClass.Create();
				return tpf.MakeProps("Language Code", m_lp.Cache.DefaultUserWs, 0);
			}
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
