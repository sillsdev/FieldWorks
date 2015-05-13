using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;		// controls and etc...
using System.Xml;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// LabeledMultiStringView displays one or more writing system alternatives of a string property.
	/// It simply edits that property.
	/// </summary>
	public class InnerLabeledMultiStringView : RootSiteControl
	{
		bool m_forceIncludeEnglish;
		bool m_editable;
		int m_hvoObj;
		int m_flid;
		int m_wsMagic;
		// This is additional writing systems that might possibly be relevant in addition to the one(s) indicated
		// by m_wsMagic. Currently the only example is that on a pronunciation field, vernacular as well as
		// the default pronunciation WSS might be relevant.
		int m_wsOptional;
		List<IWritingSystem> m_rgws;
		List<IWritingSystem> m_rgwsToDisplay;
		LabeledMultiStringVc m_vc = null;
		private string m_textStyle;
		/// <summary>
		/// We may need to set up other controls than what this class itself knows about.
		/// </summary>
		public event EventHandler SetupOtherControls;

		/// <summary>
		/// Return the view constructor.
		/// </summary>
		internal LabeledMultiStringVc VC
		{
			get { return m_vc; }
		}

		/// <summary>
		/// Return the relevant writing systems.
		/// </summary>
		internal List<IWritingSystem> WritingSystems
		{
			get { return m_rgws; }
		}

		/// <summary>
		/// Return the flid.
		/// </summary>
		internal int Flid
		{
			get { return m_flid; }
		}

		/// <summary></summary>
		internal int HvoObj
		{
			get { return m_hvoObj; }
		}

		/// <summary>
		/// This event is triggered at the start of the Display() method of the VC.
		/// It provides an opportunity to set overall properties (such as read-only) in the view.
		/// </summary>
		public event VwEnvEventHandler Display;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="hvo">The object to be edited</param>
		/// <param name="flid">The multistring property to be edited</param>
		/// <param name="wsMagic">The magic writing system (like LgWritingSystem.kwsAnals)
		/// indicating which writing systems to display.</param>
		/// <param name="forceIncludeEnglish">True, if English is to be included along with others.</param>
		/// <param name="editable">false if we don't want to allow editing of the strings.</param>
		public InnerLabeledMultiStringView(int hvo, int flid, int wsMagic, bool forceIncludeEnglish, bool editable)
			: this(hvo, flid, wsMagic, 0, forceIncludeEnglish, editable, true)
		{
		}
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="hvo">The object to be edited</param>
		/// <param name="flid">The multistring property to be edited</param>
		/// <param name="wsMagic">The magic writing system (like LgWritingSystem.kwsAnals)
		/// indicating which writing systems to display.</param>
		/// <param name="wsOptional">Additional magic WS for more options (e.g., vernacular as well as pronunciation) allowed
		/// when configuring but not shown by default</param>
		/// <param name="forceIncludeEnglish">True, if English is to be included along with others.</param>
		/// <param name="editable">false if we don't want to allow editing of the strings.</param>
		/// <param name="spellCheck">true if you want the view spell-checked.</param>
		public InnerLabeledMultiStringView(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			ConstructReuseCore(hvo, flid, wsMagic, wsOptional, forceIncludeEnglish, editable, spellCheck);
		}

		/// <summary>
		/// Re-initialize this view as if it had been constructed with the specified arguments.
		/// </summary>
		public void Reuse(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			ConstructReuseCore(hvo, flid, wsMagic, wsOptional, forceIncludeEnglish, editable, spellCheck);
			if (!editable && RootSiteEditingHelper != null)
				RootSiteEditingHelper.PasteFixTssEvent -= new FwPasteFixTssEventHandler(OnPasteFixTssEvent);
			if (m_rootb != null)
			{
				m_rgws = WritingSystemOptions;
				m_vc.Reuse(m_flid, m_rgws, m_editable);
			}
		}

		/// <summary>
		/// Return the sound control rectangle.
		/// </summary>
		internal void GetSoundControlRectangle(IVwSelection sel, out Rectangle selRect)
		{
			bool fEndBeforeAnchor;
			using (new HoldGraphics(this))
				SelectionRectangle(sel, out selRect, out fEndBeforeAnchor);
		}

		/// <summary>
		/// Call this on initialization when all properties (e.g., ConfigurationNode) are set.
		/// The purpose is when reusing the slice, when we may have to reconstruct the root box.
		/// </summary>
		public void FinishInit()
		{
			if (m_rootb != null)
			{
				m_rootb.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);
			}
		}

		/// <summary>
		/// Get any text styles from configuration node (which is now available; it was not at construction)
		/// </summary>
		public void FinishInit(XmlNode configurationNode)
		{
			if (configurationNode.Attributes != null)
			{
				var textStyle = configurationNode.Attributes["textStyle"];
				if (textStyle != null)
				{
					TextStyle = textStyle.Value;
				}
			}
			FinishInit();
		}

		/// <summary>
		/// Get or set the text style name
		/// </summary>
		public string TextStyle
		{
			get
			{
				if (string.IsNullOrEmpty(m_textStyle))
				{
					m_textStyle = "Default Paragraph Characters";
				}
				return m_textStyle;
			}
			set
			{
				m_textStyle = value;
			}
		}

		/// <summary>
		/// On a major refresh, the writing system list may have changed; update accordingly.
		/// </summary>
		public override bool RefreshDisplay()
		{
			m_rgws = WritingSystemOptions;
			return base.RefreshDisplay();
		}

		private void ConstructReuseCore(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			m_hvoObj = hvo;
			m_flid = flid;
			m_wsMagic = wsMagic;
			m_wsOptional = wsOptional;
			m_forceIncludeEnglish = forceIncludeEnglish;
			m_editable = editable;
			if (editable && RootSiteEditingHelper != null)
				RootSiteEditingHelper.PasteFixTssEvent += new FwPasteFixTssEventHandler(OnPasteFixTssEvent);
			DoSpellCheck = spellCheck;
		}

		/// <summary>
		/// If the text for pasting is too long, truncate it and warn the user.
		/// </summary>
		void OnPasteFixTssEvent(EditingHelper sender, FwPasteFixTssEventArgs e)
		{
			EliminateExtraStyleAndWsInfo(e, m_flid);
		}

		/// <summary>
		/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
		/// (See LT-8656 and LT-9119.)
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			try
			{
				m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoObj); // Throws an exception, if not valid.
				base.OnKeyPress(e);
			}
			catch
			{
				e.Handled = true;
			}
		}

		/// <summary>
		/// Override to handle KeyUp/KeyDown within a multi-string field -- LT-13334
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (!e.Handled && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
			{
				MultiStringSelectionUtils.HandleUpDownArrows(e, m_rootb, RootSiteEditingHelper.CurrentSelection,
					WritingSystemsToDisplay, m_flid);
			}
		}

		static bool s_fProcessingSelectionChanged = false;
		/// <summary>
		/// Try to keep the selection from including any of the characters in a writing system label.
		/// See LT-8396.
		/// </summary>
		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			base.HandleSelectionChange(prootb, vwselNew);
			// 1) We don't want to recurse into here.
			// 2) If the selection is invalid we can't use it.
			// 3) If the selection is entirely formattable ("IsSelectionInOneFormattableProp"), we don't need to do
			//    anything.
			if (s_fProcessingSelectionChanged || !vwselNew.IsValid || EditingHelper.IsSelectionInOneFormattableProp())
				return;
			try
			{
				s_fProcessingSelectionChanged = true;

				SelectionHelper hlpr = SelectionHelper.Create(vwselNew, this);
				bool fRange = hlpr.IsRange;
				bool fChangeRange = false;
				if (fRange)
				{
					bool fAnchorEditable = vwselNew.IsEditable;
					hlpr.GetIch(SelectionHelper.SelLimitType.Anchor);
					int tagAnchor = hlpr.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
					hlpr.GetIch(SelectionHelper.SelLimitType.End);
					int tagEnd = hlpr.GetTextPropId(SelectionHelper.SelLimitType.End);
					bool fEndBeforeAnchor = vwselNew.EndBeforeAnchor;
					if (fEndBeforeAnchor)
					{
						if (fAnchorEditable && tagAnchor > 0 && tagEnd < 0)
						{
							hlpr.SetTextPropId(SelectionHelper.SelLimitType.End, tagAnchor);
							hlpr.SetIch(SelectionHelper.SelLimitType.End, 0);
							fChangeRange = true;
						}
					}
					else
					{
						if (!fAnchorEditable && tagAnchor < 0 && tagEnd > 0)
						{
							hlpr.SetTextPropId(SelectionHelper.SelLimitType.Anchor, tagEnd);
							hlpr.SetIch(SelectionHelper.SelLimitType.Anchor, 0);
							fChangeRange = true;
						}
					}
				}
				if (fChangeRange)
					hlpr.SetSelection(true);
			}
			finally
			{
				s_fProcessingSelectionChanged = false;
			}
		}

		private void EliminateExtraStyleAndWsInfo(FwPasteFixTssEventArgs e, int flid)
		{
			var mdc = RootBox.DataAccess.MetaDataCache;
			var type = (CellarPropertyType)mdc.GetFieldType(flid);
			if (type == CellarPropertyType.MultiString ||
				type == CellarPropertyType.MultiUnicode ||
				type == CellarPropertyType.String ||
				type == CellarPropertyType.Unicode)
			{
				e.TsString = e.TsString.ToWsOnlyString();
			}
		}

		/// <summary></summary>
		public override bool IsSelectionFormattable
		{
			get
			{
				var fbaseOpinion = base.IsSelectionFormattable;
				if (!fbaseOpinion)
					return false;

				// We only want to allow applying styles in this type of control if the whole selection is in
				// the same writing system.
				var wsAnchor = EditingHelper.CurrentSelection.GetWritingSystem(SelectionHelper.SelLimitType.Anchor);
				var wsEnd = EditingHelper.CurrentSelection.GetWritingSystem(SelectionHelper.SelLimitType.End);
				return wsAnchor == wsEnd;
			}
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rgws = null;
			m_vc = null;
		}

		#endregion IDisposable override

		/// <summary>
		/// Make a rootbox. When changing this, give careful consideration to changing Reuse().
		/// </summary>
		public override void MakeRoot()
		{
			CheckDisposed();

			m_rootb = null;
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rgws = WritingSystemOptions;

			int wsUser = m_fdoCache.WritingSystemFactory.UserWs;
			m_vc = new InnerLabeledMultiStringViewVc(m_flid, m_rgws, wsUser, m_editable, m_fdoCache.TsStrFactory, this);

			// Review JohnT: why doesn't the base class do this??
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			// And maybe this too, at least by default?
			m_rootb.DataAccess = m_fdoCache.DomainDataByFlid;

			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			// arg4 could be used to supply a stylesheet.
			m_rootb.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);

			if (SetupOtherControls != null)
				SetupOtherControls(this, new EventArgs());
		}

		/// <summary>
		/// This is the list of writing systems that can be enabled for this control. It should be either the Vernacular list
		/// or Analysis list shown in the WritingSystemPropertiesDialog which are checked and unchecked.
		/// </summary>
		public List<IWritingSystem> WritingSystemOptions
		{
			get
			{
				CheckDisposed();
				return GetWritingSystemOptions(true);
			}
		}

		/// <summary>
		/// returns a list of the writing systems available to display for this view
		/// </summary>
		/// <param name="fIncludeUncheckedActiveWss">if false, include only current wss,
		/// if true, includes unchecked active wss.</param>
		public List<IWritingSystem> GetWritingSystemOptions(bool fIncludeUncheckedActiveWss)
		{
			var result = WritingSystemServices.GetWritingSystemList(m_fdoCache, m_wsMagic, m_hvoObj,
				m_forceIncludeEnglish, fIncludeUncheckedActiveWss);
			if (fIncludeUncheckedActiveWss && m_wsOptional != 0)
			{
				result = new List<IWritingSystem>(result); // just in case caller does not want it modified
				var additionalWss = WritingSystemServices.GetWritingSystemList(m_fdoCache, m_wsOptional, m_hvoObj,
					m_forceIncludeEnglish, fIncludeUncheckedActiveWss);
				foreach (var ws in additionalWss)
					if (!result.Contains(ws))
						result.Add(ws);
			}
			return result;
		}

		/// <summary>
		/// if non-null, we'll use this list to determine which writing systems to display. These
		/// are the writing systems the user has checked in the WritingSystemPropertiesDialog.
		/// if null, we'll display every writing system option.
		/// </summary>
		public List<IWritingSystem> WritingSystemsToDisplay
		{
			get { return m_rgwsToDisplay; }
			set
			{
				m_rgwsToDisplay = value;
			}
		}

		/// <summary></summary>
		internal void TriggerDisplay(IVwEnv vwenv)
		{
			CheckDisposed();

			if (Display != null)
				Display(this, new VwEnvEventArgs(vwenv));
		}

		/// <summary>
		/// Make a selection in the specified writing system at the specified character offset.
		/// Note: selecting other than the first writing system is not yet implemented.
		/// </summary>
		public void SelectAt(int ws, int ich)
		{
			CheckDisposed();

			Debug.Assert(ws == m_rgws[0].Handle);
			try
			{
				RootBox.MakeTextSelection(0, 0, null, m_flid, 0, ich, ich, ws, true, -1, null, true);
			}
			catch (Exception)
			{
				Debug.Assert(false, "Unexpected failure to make selection in LabeledMultiStringView");
			}
		}
	}

	/// <summary>
	/// View constructor for InnerLabeledMultiStringView.
	/// </summary>
	internal class LabeledMultiStringVc: FwBaseVc
	{
		internal int m_flid;
		internal List<IWritingSystem> m_rgws; // writing systems to display
		ITsTextProps m_ttpLabel; // Props to use for ws name labels
		bool m_editable = true;
		int m_wsEn;
		internal int m_mDxmpLabelWidth;

		public LabeledMultiStringVc(int flid, List<IWritingSystem> rgws, int wsUser, bool editable, int wsEn, ITsStrFactory tsf)
		{
			Reuse(flid, rgws, editable);
			m_ttpLabel = WritingSystemServices.AbbreviationTextProperties;
			m_wsEn = wsEn == 0 ? wsUser : wsEn;
			m_tsf = tsf;
			// Here's the C++ code which does the same thing using styles.
			//				StrUni stuLangCodeStyle(L"Language Code");
			//				ITsPropsFactoryPtr qtpf;
			//				qtpf.CreateInstance(CLSID_TsPropsFactory);
			//				StrUni stu;
			//				ITsStringPtr qtss;
			//				ITsStrFactoryPtr qtsf;
			//				qtsf.CreateInstance(CLSID_TsStrFactory);
			//				// Get the properties of the "Language Code" style for the writing system
			//				// which corresponds to the user's environment.
			//				qtpf->MakeProps(stuLangCodeStyle.Bstr(), ???->UserWs(), 0, &qttp);
		}

		public virtual string TextStyle
		{
			get
			{

				string sTextStyle = "Default Paragraph Characters";
				/*
				if (m_view != null)
				{
					sTextStyle = m_view.TextStyle;
				}
*/
				return sTextStyle;
			}
			set
			{
				/*m_textStyle = value;*/
			}
		}

		public void Reuse(int flid, List<IWritingSystem> rgws, bool editable)
		{
			m_flid = flid;
			m_rgws = rgws;
			m_editable = editable;
		}

		private ITsString NameOfWs(int i)
		{
			// Display in English if possible for now (August 2008).  See LT-8631 and LT-8574.
			string result = m_rgws[i].Abbreviation;

			if (string.IsNullOrEmpty(result))
				result = "??";

			return m_tsf.MakeString(result, m_wsEn);
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			TriggerDisplay(vwenv);

			// We use a table to display
			// encodings in column one and the strings in column two.
			// The table uses 100% of the available width.
			VwLength vlTable;
			vlTable.nVal = 10000;
			vlTable.unit = VwUnit.kunPercent100;

			// The width of the writing system column is determined from the width of the
			// longest one which will be displayed.
			m_mDxmpLabelWidth = 0;
			for (int i = 0; i < m_rgws.Count; ++i)
			{
				int dxs;	// Width of displayed string.
				int dys;	// Height of displayed string (not used here).

				// Set qtss to a string representing the writing system.
				vwenv.get_StringWidth(NameOfWs(i), m_ttpLabel, out dxs, out dys);
				m_mDxmpLabelWidth = Math.Max(m_mDxmpLabelWidth, dxs);
			}
			VwLength vlColWs; // 5-pt space plus max label width.
			vlColWs.nVal = m_mDxmpLabelWidth + 5000;
			vlColWs.unit = VwUnit.kunPoint1000;

			// Enhance JohnT: possibly allow for right-to-left UI by reversing columns?

			// The Main column is relative and uses the rest of the space.
			VwLength vlColMain;
			vlColMain.nVal = 1;
			vlColMain.unit = VwUnit.kunRelative;

			vwenv.OpenTable(2, // Two columns.
				vlTable, // Table uses 100% of available width.
				0, // Border thickness.
				VwAlignment.kvaLeft, // Default alignment.
				VwFramePosition.kvfpVoid, // No border.
				VwRule.kvrlNone, // No rules between cells.
				0, // No forced space between cells.
				0, // No padding inside cells.
				false);
			// Specify column widths. The first argument is the number of columns,
			// not a column index. The writing system column only occurs at all if its
			// width is non-zero.
			vwenv.MakeColumns(1, vlColWs);
			vwenv.MakeColumns(1, vlColMain);

			vwenv.OpenTableBody();
			var visibleWss = new Set<ILgWritingSystem>();
			// if we passed in a view and have WritingSystemsToDisplay
			// then we'll load that list in order to filter our larger m_rgws list.
			AddViewWritingSystems(visibleWss);
			for (int i = 0; i < m_rgws.Count; ++i)
			{
				if (SkipEmptyWritingSystem(visibleWss, i, hvo))
					continue;
				vwenv.OpenTableRow();

				// First cell has writing system abbreviation displayed using m_ttpLabel.
				vwenv.Props = m_ttpLabel;
				vwenv.OpenTableCell(1,1);
				vwenv.AddString(NameOfWs(i));
				vwenv.CloseTableCell();

				// Second cell has the string contents for the alternative.
				// DN version has some property setting, including trailing margin and
				// RTL.
				if (m_rgws[i].RightToLeftScript)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvForceOn);
					vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextAlign.ktalTrailing);
				}
				if (!m_editable)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptNotEditable);
				}
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, 2000);
				vwenv.OpenTableCell(1,1);
				var wsdef = m_rgws[i] as WritingSystemDefinition;
				if (wsdef != null && wsdef.IsVoice)
				{
					// We embed it in a conc paragraph to ensure it never takes more than a line.
					// It will typically be covered up by a sound control.
					// Also set foreground color to match the window, so nothing shows even if the sound doesn't overlap it perfectly.
					// (transparent does not seem to work as a foreground color)
					vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault,
						(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Window)));
					// Must not spell-check a conc para, leads to layout failures when the paragraph tries to cast the source to
					// a conc text source, if it is overridden by a spelling text source.
					vwenv.set_IntProperty((int)FwTextPropType.ktptSpellCheck, (int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmDoNotCheck);
					vwenv.OpenConcPara(0, 1, VwConcParaOpts.kcpoDefault, 0);
					vwenv.AddStringAltMember(m_flid, m_rgws[i].Handle, this);
					vwenv.CloseParagraph();
				}
				else
				{
					if (!string.IsNullOrEmpty(TextStyle))
					{
						vwenv.set_StringProperty((int) FwTextPropType.ktptNamedStyle, TextStyle);

					}
					vwenv.AddStringAltMember(m_flid, m_rgws[i].Handle, this);
				}
				vwenv.CloseTableCell();

				vwenv.CloseTableRow();
			}
			vwenv.CloseTableBody();

			vwenv.CloseTable();
		}

		/// <summary>
		/// Subclass with LabeledMultiStringView tests for empty alternatives and returns true to skip them.
		/// </summary>
		internal virtual bool SkipEmptyWritingSystem(Set<ILgWritingSystem> visibleWss, int i, int hvo)
		{
			return false;
		}

		/// <summary>
		/// Subclass with LabelledMultiStringView gets extra WSS to display from it.
		/// </summary>
		internal virtual void AddViewWritingSystems(Set<ILgWritingSystem> visibleWss)
		{
		}

		/// <summary>
		/// Subclass with LabelledMultiStringView calls TriggerView
		/// </summary>
		internal virtual void TriggerDisplay(IVwEnv vwenv)
		{

		}
	}

	/// <summary>
	/// Subclass suitable for LabeledMultistringView.
	/// </summary>
	internal class InnerLabeledMultiStringViewVc : LabeledMultiStringVc
	{
		private InnerLabeledMultiStringView m_view;

		public InnerLabeledMultiStringViewVc(int flid, List<IWritingSystem> rgws, int wsUser, bool editable,
			ITsStrFactory tsf, InnerLabeledMultiStringView view)
			: base(flid, rgws, wsUser, editable, view.WritingSystemFactory.GetWsFromStr("en"), tsf)
		{
			m_view = view;
			Debug.Assert(m_view != null);
		}

		internal override void TriggerDisplay(IVwEnv vwenv)
		{
			base.TriggerDisplay(vwenv);
			m_view.TriggerDisplay(vwenv);
		}

		internal override void AddViewWritingSystems(Set<ILgWritingSystem> visibleWss)
		{
			if (m_view.WritingSystemsToDisplay != null)
				visibleWss.AddRange(m_view.WritingSystemsToDisplay);
		}

		internal override bool SkipEmptyWritingSystem(Set<ILgWritingSystem> visibleWss, int i, int hvo)
		{
			// if we have defined writing systems to display, we want to
			// show those, plus other options that have data.
			// otherwise, we'll assume we want to display the given ws fields.
			// (this effectively means that setting WritingSystemsToDisplay to 'null'
			// will display all the ws options in m_rgws. That is also what happens in the base class.)
			if (m_view.WritingSystemsToDisplay != null)
			{
				// if we haven't configured to display this writing system
				// we still want to show it if it has data.
				if (!visibleWss.Contains(m_rgws[i]))
				{
					ITsString result = m_view.Cache.MainCacheAccessor.get_MultiStringAlt(hvo, m_flid, m_rgws[i].Handle);
					if (result == null || result.Length == 0)
						return true;
				}
			}
			return false;
		}
		public override string TextStyle
		{
			get
			{
				string sTextStyle = "Default Paragraph Characters";
				if (m_view != null)
				{
					sTextStyle = m_view.TextStyle;
				}
				return sTextStyle;
			}
			set
			{
				if (m_view != null)
				{
					m_view.TextStyle = value;
				}
			}
		}

	}

	/// <summary>
	/// Delegate defn for an event handler that passes an IVwEnv.
	/// </summary>
	public delegate void VwEnvEventHandler (object sender, VwEnvEventArgs e);

	/// <summary>
	/// Event Args for an event that passes a VwEnv.
	/// </summary>
	public class VwEnvEventArgs : EventArgs
	{
		IVwEnv m_env;
		/// <summary>
		/// Make one.
		/// </summary>
		public VwEnvEventArgs(IVwEnv env)
		{
			m_env = env;
		}

		/// <summary>
		/// Get the environment.
		/// </summary>
		public IVwEnv Environment
		{
			get { return m_env; }
		}
	}
}
