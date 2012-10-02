using System;
using System.Drawing;
using System.Diagnostics;

using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This is a pane which shows the title and Description of the current record.
	/// </summary>
	public class TitleContentsPane : RootSiteControl, IInterlinearTabControl, IStyleSheet
	{
		private int m_hvoRoot; // The Text.
		private TitleContentsVc m_vc;

		public TitleContentsPane()
		{
			this.BackColor = Color.FromKnownColor(KnownColor.ControlLight);
			//this.AutoScroll = false;
		}

		#region IDisposable override

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
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_vc = null;
		}

		#endregion IDisposable override

		#region implemention of IChangeRootObject

		public void SetRoot(int hvo)
		{
			CheckDisposed();

			if (hvo != 0)
			{
				IStText stText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvo);
				if (ScriptureServices.ScriptureIsResponsibleFor(stText))
				{
					m_hvoRoot = hvo;	// StText (i.e. Scripture)
				}
				else
				{
					m_hvoRoot = stText.Owner.Hvo; // Text (i.e. non-scripture). Editable.
				}
				SetupVc();
			}
			else
			{
				m_hvoRoot = 0;
				ReadOnlyView = true;
				if (m_vc != null)
				{
					m_vc.IsScripture = false;
					m_vc.Editable = false;
				}
			}
			ChangeOrMakeRoot(m_hvoRoot, m_vc, TitleContentsVc.kfragRoot, m_styleSheet);
		}

		void SetupVc()
		{
			if (m_vc == null || m_hvoRoot == 0)
				return;
			Debug.Assert(m_hvoRoot != 0, "m_hvoRoot should be set before using SetupVc().");
			ICmObject co = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoRoot);
			m_vc.IsScripture = ScriptureServices.ScriptureIsResponsibleFor(co as IStText);
			// don't allow editing scripture titles.
			m_vc.Editable = !m_vc.IsScripture;
			this.ReadOnlyView = !m_vc.Editable;
		}

		#endregion

		#region Overrides of RootSite
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (m_fdoCache == null || DesignMode /*|| m_hvoRoot == 0*/)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			m_vc = new TitleContentsVc(m_fdoCache);
			SetupVc();

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

			m_rootb.SetRootObject(m_hvoRoot, m_vc, (int)TitleContentsVc.kfragRoot, m_styleSheet);

			base.MakeRoot();

			//TODO:
			//ptmw->RegisterRootBox(qrootb);
		}

		public override void RootBoxSizeChanged(IVwRootBox prootb)
		{
			CheckDisposed();

			base.RootBoxSizeChanged(prootb);
			AdjustHeight();
		}

		#endregion
		/// <summary>
		/// Adjust your height up to some reasonable limit to accommodate the entire title and contents.
		/// </summary>
		public bool AdjustHeight()
		{
			CheckDisposed();

			if (RootBox == null)
				return false; // nothing useful we can do.
			// Ideally we want to be about 5 pixels bigger than the root. This suppresses the scroll bar
			// and makes everything neat. (Anything smaller leaves us with a scroll bar.)
			int desiredHeight = RootBox.Height + 8;
			// But we're not the main event. Let's not use more than half the window.
			if (this.Parent != null)
				desiredHeight = Math.Min(desiredHeight, Parent.Height / 2);
			// On the other hand, we'd better have SOME space.
			desiredHeight = Math.Max(5, desiredHeight);
			// But not MORE than the parent.
			if (this.Parent != null)
				desiredHeight = Math.Min(desiredHeight, Parent.Height);

			if (this.Height != desiredHeight)
			{
				this.Height = desiredHeight;
				return true;
			}
			return false;
		}

		protected override void EnsureDefaultSelection()
		{
			// if we have an editable title, try putting cursor in editable text location.
			if (!ReadOnlyView)
				EnsureDefaultSelection(true);
			else
				base.EnsureDefaultSelection();
		}
	}

	/// <summary>
	/// Vc for title contents pane. Makes a table.
	/// </summary>
	class TitleContentsVc : FwBaseVc
	{
		public const int kfragRoot = 6789;

		ITsString m_tssTitle;
		int m_vtagStTextTitle = 0;
		int m_dxLabWidth = 0;
		int m_dxWsLabWidth = 0; // width of writing system labels.
		ITsTextProps m_ttpBold;
		ITsTextProps m_ttpDataCellProps;
		// int m_wsAnalysis; // CS0414
		IWritingSystem[] m_writingSystems;
		ITsString[] m_WsLabels;
		ITsTextProps m_ttpWsLabel;
		int m_editBackColor = (int)SIL.Utils.ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Window));

		public TitleContentsVc(FdoCache cache)
		{
			int wsUser = cache.DefaultUserWs;
			// m_wsAnalysis = cache.DefaultAnalWs; // CS0414
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			m_tssTitle = tsf.MakeString(ITextStrings.ksTitle, wsUser);
			//m_tssComments = tsf.MakeString("Comments", wsUser);
			//m_tssComments = tsf.MakeString("Source", wsUser);
			ITsPropsBldr tpb = TsPropsBldrClass.Create();
			tpb.SetIntPropValues((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwTextToggleVal.kttvForceOn);
			m_ttpBold = tpb.GetTextProps();
			tpb = TsPropsBldrClass.Create();
			// Set some padding all around.
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadTop,
				(int) FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadBottom,
				(int) FwTextPropVar.ktpvMilliPoint, 1000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadLeading,
				(int) FwTextPropVar.ktpvMilliPoint, 3000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptPadTrailing,
				(int) FwTextPropVar.ktpvMilliPoint, 3000);
			tpb.SetIntPropValues((int)FwTextPropType.ktptMarginTrailing,
				(int) FwTextPropVar.ktpvMilliPoint, 11000); // 10000 clips right border.
			m_ttpDataCellProps = tpb.GetTextProps();

			m_vtagStTextTitle = cache.MetaDataCacheAccessor.GetFieldId("StText", "Title", false);

			// Set up the array of writing systems we will display for title.
			SetupWritingSystemsForTitle(cache);
		}

		private void SetupWritingSystemsForTitle(FdoCache cache)
		{
			m_ttpWsLabel = WritingSystemServices.AbbreviationTextProperties;
			m_writingSystems = new IWritingSystem[2];
			m_writingSystems[0] = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			m_writingSystems[1] = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			m_WsLabels = new ITsString[m_writingSystems.Length];

			int wsEn = cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			if (wsEn == 0)
				wsEn = cache.DefaultUserWs;
			if (wsEn == 0)
				wsEn = WritingSystemServices.FallbackUserWs(cache);

			for (int i = 0; i < m_writingSystems.Length; i++)
			{
				//m_WsLabels[i] = LgWritingSystem.UserAbbr(cache, m_writingSystems[i].Hvo);
				// For now (August 2008), try English abbreviation before UI writing system.
				// (See LT-8185.)
				m_WsLabels[i] = cache.TsStrFactory.MakeString(m_writingSystems[i].Abbreviation, cache.DefaultUserWs);
				if (String.IsNullOrEmpty(m_WsLabels[i].Text))
					m_WsLabels[i] = cache.TsStrFactory.MakeString(m_writingSystems[i].Abbreviation, cache.DefaultUserWs);
			}
		}

		/// <summary>
		/// Indicates whether to treat the RootSite object as a scripture.
		/// </summary>
		internal bool IsScripture { get; set; }

		/// <summary>
		/// Indicates whether we should allow editing in the TitleContents fields.
		/// </summary>
		private bool m_fIsEditable = true;
		internal bool Editable
		{
			get { return m_fIsEditable; }
			set { m_fIsEditable = value; }
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// Ignore 0 hvo's. RootObject may have not been set. FWNX-613.
			if (hvo == 0)
				return;

			switch(frag)
			{
				case kfragRoot:
					if (m_dxLabWidth == 0)
					{
						int dmpx1, dmpy;	//, dmpx2;
						vwenv.get_StringWidth(m_tssTitle, m_ttpBold, out dmpx1, out dmpy);
						//vwenv.get_StringWidth(m_tssComments, m_ttpBold, out dmpx2, out dmpy);
						//m_dxLabWidth = Math.Max(dmpx1, dmpx2) + 13000; // add 3 pt spacing to box, 10 to margin.
						m_dxLabWidth = dmpx1 + 13000; // add 3 pt spacing to box, 10 to margin.

						m_dxWsLabWidth = 0;
						foreach (ITsString tssLabel in m_WsLabels)
						{
							vwenv.get_StringWidth(tssLabel, m_ttpWsLabel, out dmpx1, out dmpy);
							m_dxWsLabWidth = Math.Max(m_dxWsLabWidth, dmpx1);
						}
						m_dxWsLabWidth += 18000; // 3 pts white space each side, 11 margin, 1 border, plus 1 for safety.
					}
					VwLength vlTable;
					vlTable.nVal = 10000;
					vlTable.unit = VwUnit.kunPercent100;

					VwLength vlColLabels; // 5-pt space plus max label width.
					vlColLabels.nVal = m_dxLabWidth;
					vlColLabels.unit = VwUnit.kunPoint1000;

					VwLength vlColWsLabels; // 5-pt space plus max ws label width.
					vlColWsLabels.nVal = m_dxWsLabWidth;
					vlColWsLabels.unit = VwUnit.kunPoint1000;

					// The Main column is relative and uses the rest of the space.
					VwLength vlColMain;
					vlColMain.nVal = 1;
					vlColMain.unit = VwUnit.kunRelative;

					// The Padding column allows for the the trailing margin and border.
					VwLength vlColPadding;
					vlColPadding.nVal = 10000;
					vlColPadding.unit = VwUnit.kunPoint1000;

					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop,
						(int) FwTextPropVar.ktpvMilliPoint, 5000);
					vwenv.OpenTable(4, // Four columns.
						vlTable, // Table uses 100% of available width.
						0, // Border thickness.
						VwAlignment.kvaLeft, // Default alignment.
						VwFramePosition.kvfpVoid, // No border.
						VwRule.kvrlNone, // No rules between cells.
						0, // No forced space between cells.
						0, // no padding inside cells.
						false);
					vwenv.MakeColumns(1, vlColLabels);
					vwenv.MakeColumns(1, vlColWsLabels);
					vwenv.MakeColumns(1, vlColMain);
					vwenv.MakeColumns(1, vlColPadding);

					vwenv.OpenTableBody();

					for (int i = 0; i < m_writingSystems.Length; i++)
					{
						vwenv.OpenTableRow();

						// First cell has 'Title' label in bold.
						vwenv.Props = m_ttpBold;
						vwenv.OpenTableCell(1,1);
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading,
							(int) FwTextPropVar.ktpvMilliPoint, 10000);
						if (i == 0) // only on the first row
						{
							// We want this fixed at 10 point, since it's considered a UI component, not data.
							// See LT-4816
							vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
								(int)FwTextPropVar.ktpvMilliPoint, 10000);
							vwenv.AddString(m_tssTitle);
						}
						vwenv.CloseTableCell();

						// Second cell has ws labels.
						vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
							(int) FwTextPropVar.ktpvDefault, m_editBackColor);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderLeading,
							(int)FwTextPropVar.ktpvMilliPoint, 1000);
						if (i == 0)
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop,
								(int)FwTextPropVar.ktpvMilliPoint, 1000);
						if (i == m_writingSystems.Length - 1)
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom,
								(int)FwTextPropVar.ktpvMilliPoint, 1000);
						vwenv.OpenTableCell(1,1);
						vwenv.Props = m_ttpDataCellProps;

						vwenv.Props = m_ttpWsLabel;
						vwenv.AddString(m_WsLabels[i]);
						vwenv.CloseTableCell();

						// Third cell has the Title property, in a box.

						vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
							(int) FwTextPropVar.ktpvDefault, m_editBackColor);
						// Set the underlying directionality so that arrow keys work properly.
						bool fRTL = m_writingSystems[i].RightToLeftScript;
						vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
							(int)FwTextPropVar.ktpvEnum, fRTL ? -1 : 0);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
							(int)FwTextPropVar.ktpvEnum,
							fRTL ? (int)FwTextAlign.ktalRight : (int)FwTextAlign.ktalLeft);
						if (i == 0)
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop,
								(int)FwTextPropVar.ktpvMilliPoint, 1000);
						if (i == m_writingSystems.Length - 1)
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom,
								(int)FwTextPropVar.ktpvMilliPoint, 1000);
						vwenv.OpenTableCell(1,1);
						vwenv.OpenParagraph();
						vwenv.Props = m_ttpDataCellProps;
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
								this.Editable ? (int)TptEditable.ktptIsEditable : (int)TptEditable.ktptNotEditable);
						if (IsScripture)
						{
							vwenv.AddStringAltMember(m_vtagStTextTitle, m_writingSystems[i].Handle, this);
						}
						else
						{
							vwenv.AddStringAltMember(CmMajorObjectTags.kflidName, m_writingSystems[i].Handle, this);
						}
						vwenv.CloseParagraph();
						vwenv.CloseTableCell();

						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
							(int)FwTextPropVar.ktpvMilliPoint, 10000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
							(int)FwTextPropVar.ktpvMilliPoint, 1000);
						vwenv.OpenTableCell(1, 1);
						vwenv.CloseTableCell();

						vwenv.CloseTableRow();
					}

					//// Second row.
					//vwenv.OpenTableRow();
					//
					//// First cell has 'Comments' label in bold.
					//vwenv.Props = m_ttpBold;
					//vwenv.OpenTableCell(1,1);
					//vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading,
					//	(int)FwTextPropVar.ktpvMilliPoint, 10000);
					//vwenv.AddString(m_tssComments);
					//vwenv.CloseTableCell();
					//
					//// Second cell has the Description property, in a box.
					//vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTrailing,
					//	(int)FwTextPropVar.ktpvMilliPoint, 10000);
					//vwenv.OpenTableCell(1,1);
					//vwenv.Props = m_ttpDataCellProps;
					//vwenv.set_IntProperty((int)FwTextPropType.ktptBorderBottom,
					//	(int)FwTextPropVar.ktpvMilliPoint, 1000);
					//vwenv.AddStringAltMember(
					//	(int)CmMajorObject.CmMajorObjectTags.kflidDescription, m_wsAnalysis, this);
					//vwenv.CloseTableCell();
					//
					//vwenv.CloseTableRow();

					vwenv.CloseTableBody();

					vwenv.CloseTable();

					break;
				default:
					throw new Exception("Bad frag id in TitleContentsVc");
			}
		}
	}
}
