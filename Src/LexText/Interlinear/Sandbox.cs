using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// The 'Sandbox' is a small view used to edit the data associated with a single Wordform...
	/// specifically, the data associated with a standard interlinear view. It uses a different,
	/// simpler model than the real interlinear view, documented below under the constants we
	/// use to describe it. This view makes editing easier. A further advantage is that we don't
	/// have to worry, until the view closes, about whether the user is editing one of the
	/// existing analyses or creating a new one.
	/// </summary>
	public class Sandbox : SandboxBase
	{
		#region Data members

		InterlinDocChild m_interlinDoc = null;

		#endregion Data members

		#region Construction and initialization

		/// <summary>
		/// Default Constructor.
		/// </summary>
		public Sandbox()
		{
		}

		/// <summary>
		/// Create a new one.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoAnalysis"></param>
		/// <param name="ss"></param>
		/// <param name="choices"></param>
		/// <param name="rawWordform"></param>
		/// <param name="fTreatAsSentenceInitial"></param>
		/// <param name="mediator"></param>
		/// <param name="parent"></param>
		public Sandbox(FdoCache cache, Mediator mediator, IVwStylesheet ss,
			InterlinLineChoices choices, int hvoAnnotation, InterlinDocChild interlinDoc)
			: base(cache, mediator, ss, choices)
		{
			m_interlinDoc = interlinDoc;
			m_hvoAnnotation = hvoAnnotation;
			// Finish initialization with twfic context.
			int hvoInstanceOf = Cache.GetObjProperty(m_hvoAnnotation,
				(int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf);
			LoadForWordBundleAnalysis(hvoInstanceOf);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="mediator"></param>
		/// <param name="ss"></param>
		/// <param name="choices"></param>
		public Sandbox(FdoCache cache, Mediator mediator, IVwStylesheet ss, InterlinLineChoices choices)
			: base(cache, mediator, ss, choices)
		{
		}

		/// <summary>
		/// We don't want to do a load on our hvoAnalysis until our HvoAnnotation is setup.
		/// </summary>
		/// <param name="hvoAnalysis"></param>
		protected override void LoadForWordBundleAnalysis(int hvoAnalysis)
		{
			if (HvoAnnotation != 0)
				base.LoadForWordBundleAnalysis(hvoAnalysis);
		}

		/// <summary>
		/// Set up the sandbox to display the specified analysis, hvoAnalysis, which might be a WfiWordform,
		/// WfiAnalysis, or WfiGloss.
		/// </summary>
		/// <param name="hvoAnnotation"></param>
		/// <param name="fValidateRealObject">validate whether annotation exists in the database</param>
		public void SwitchWord(int hvoAnnotation, bool fValidateRealObject)
		{
			CheckDisposed();
			if (fValidateRealObject && !Cache.IsRealObject(hvoAnnotation, CmBaseAnnotation.kClassId))
				throw new ArgumentException(String.Format("invalid hvoAnnotation {0}", hvoAnnotation));
			HvoAnnotation = hvoAnnotation;
			RawWordformWs = 0;
			StTxtPara.TwficInfo twficInfo = new StTxtPara.TwficInfo(Cache, hvoAnnotation);
			this.TreatAsSentenceInitial = twficInfo.IsFirstTwficInSegment;
			ReconstructForWordBundleAnalysis(twficInfo.Object.InstanceOfRAHvo);
		}

		/// <summary>
		/// This Sandbox is based upon an hvoAnnotation, not simply the hvoAnalysis.
		/// </summary>
		protected internal override int RootObjHvo
		{
			get
			{
				CheckDisposed();
				return m_hvoAnnotation;
			}
		}

		/// <summary>
		/// Use the cba offsets in the text to get the RawTextform
		/// </summary>
		public override ITsString RawWordform
		{
			get
			{
				if ((m_rawWordform == null || m_rawWordform.Length == 0) && HvoAnnotation != 0)
				{
					// force reload of this string, just in case
					int vtagStringValue = CmBaseAnnotation.StringValuePropId(Cache);
					IVwVirtualHandler vh;
					if (Cache.TryGetVirtualHandler(vtagStringValue, out vh))
					{
						vh.Load(HvoAnnotation, vtagStringValue, 0, Cache.VwCacheDaAccessor);
					}
					m_rawWordform = Cache.GetTsStringProperty(HvoAnnotation, CmBaseAnnotation.StringValuePropId(Cache));
				}
				return m_rawWordform;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose( disposing );

			if (disposing)
			{
			}

			m_interlinDoc = null;
		}

		#endregion Construction and initialization

		/// <summary>
		/// This is useful for determining the number of instances where a WordGloss is used in IText.
		/// The total number of wordglosses equals
		///		the WfiGlosses referenced by CmBaseAnnotation.InstanceOf plus
		///		the present sandbox WordGloss if it doesn't match the original state of the Sandbox.
		/// cf. (LT-1428).
		/// </summary>
		protected override int WordGlossReferenceCount
		{
			get
			{
				int glossReferenceCount = 0;

				// Find the number of references to this WfiGloss in CmBaseAnnotations.
				string sql = string.Format("select count(ba.id) " +
					"from CmBaseAnnotation_ ba " +
					"where ba.InstanceOf={0}", WordGlossHvo);
				DbOps.ReadOneIntFromCommand(Cache, sql, null, out glossReferenceCount);
				// if FocusBox.InterlinWordControl.WordGlossHvo != m_hvoAnalysis
				//		then we are editing a different WordGloss, whose count is not reflected in
				//		the present state of the database for WfiGlosses.
				//		So, add it to the WfiGloss count before we return.
				if (WordGlossHvo != m_hvoInitialAnalysis)
					++glossReferenceCount;

				return glossReferenceCount;
			}
		}

		internal override InterlinDocChild InterlinDoc
		{
			get
			{
				return m_interlinDoc;
			}
		}
	}
}
