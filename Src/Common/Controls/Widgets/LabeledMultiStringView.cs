using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;		// controls and etc...
using System.Windows.Forms.VisualStyles;
using System.Xml;
using Palaso.Media;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using System.Text;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// LabeledMultiStringView displays one or more writing system alternatives of a string property.
	/// It simply edits that property.
	/// </summary>
	public class LabeledMultiStringView : RootSiteControl
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
		private List<Palaso.Media.ShortSoundFieldControl> m_soundControls = new List<ShortSoundFieldControl>();
		private string m_textStyle;

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
		public LabeledMultiStringView(int hvo, int flid, int wsMagic, bool forceIncludeEnglish, bool editable)
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
		public LabeledMultiStringView(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			ConstructReuseCore(hvo, flid, wsMagic, wsOptional, forceIncludeEnglish, editable, spellCheck);
		}

		/// <summary>
		/// Arrange our sound controls if any.
		/// </summary>
		/// <param name="levent"></param>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
			if (m_vc == null || m_rootb == null) // We can come in with no rootb from a dispose call.
				return;
			int dpiX;
			using (var graphics = CreateGraphics())
			{
				dpiX = (int)graphics.DpiX;
			}
			int indent = m_vc.m_mDxmpLabelWidth * dpiX / 72000 + 5; // 72000 millipoints/inch
			foreach (var control in m_soundControls)
			{
				int wsIndex;
				var ws = WsForSoundField(control, out wsIndex);
				var sel = GetSelAtStartOfWs(wsIndex, ws);
				Rectangle selRect;
				bool fEndBeforeAnchor; // not used
				using (new HoldGraphics(this))
					SelectionRectangle(sel, out selRect, out fEndBeforeAnchor);
				control.Left = indent;
				control.Width = Width - indent;
				control.Top = selRect.Top;
			}
		}

		private IVwSelection GetSelAtStartOfWs(int wsIndex, IWritingSystem ws)
		{
			try
			{
				return m_rootb.MakeTextSelection(0, 0, null, m_flid, wsIndex, 0, 0, ws.Handle, false, -1, null, false);
			}
			catch (COMException)
			{
				return null; // can fail if we are hiding an empty WS.
			}
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
				// Not yet, may depend on configuration node.
				//m_rootb.SetRootObject(hvo, m_vc, 1, m_styleSheet);
			}
			SuspendLayout();
			try
			{
				DisposeSoundControls();
				SetupSoundControls();
			}
			finally
			{
				ResumeLayout();
			}
		}

		private void DisposeSoundControls()
		{
			foreach (var sc in m_soundControls)
			{
				Controls.Remove(sc);
			}
			m_soundControls.Clear();
		}

		private void SetupSoundControls()
		{
			if (m_rgws == null)
				return; // should get called again when it is set up.
			if (m_rootb == null)
				return; // called again in MakeRoot, when information more complete.
			int index = -1;
			foreach (var ws in m_rgws)
			{
				index++;
				var pws = ws as WritingSystemDefinition;
				if (pws == null || !pws.IsVoice || GetSelAtStartOfWs(index, ws) == null)
					continue;
				var soundFieldControl = new ShortSoundFieldControl();
				m_soundControls.Add(soundFieldControl); // todo: one for each audio one
				soundFieldControl.Visible = true;
				soundFieldControl.PlayOnly = false;
				var filename = m_fdoCache.DomainDataByFlid.get_MultiStringAlt(m_hvoObj, m_flid, ws.Handle).Text ?? "";
				string path;
				if (String.IsNullOrEmpty(filename))
				{
					// Provide a filename for copying an existing file to.
					CreateNewSoundFilename(out path);
				}
				else
				{
					var mediaDir = DirectoryFinder.GetMediaDir(m_fdoCache.LangProject.LinkedFilesRootDir);
					Directory.CreateDirectory(mediaDir); // Palaso media library does not cope if it does not exist.
					path = Path.Combine(mediaDir, filename.Normalize(NormalizationForm.FormC));

					// Windows in total defiance of the Unicode standard does not consider alternate normalizations
					// of file names equal. The name in our string will always be NFD. From 7.2.2 we are saving files
					// in NFC, but files from older versions could be NFD, so we need to check both. This is not
					// foolproof...don't know any way to look for all files that might exist with supposedly equivalent
					// names not normalized at all.
					if (!File.Exists(path))
					{
						var tryPath = path.Normalize(NormalizationForm.FormD);
						if (File.Exists(tryPath))
							path = tryPath;
					}
				}
				soundFieldControl.Path = path;
				soundFieldControl.BeforeStartingToRecord += soundFieldControl_BeforeStartingToRecord;
				soundFieldControl.SoundRecorded += soundFieldControl_SoundRecorded;
				soundFieldControl.SoundDeleted += soundFieldControl_SoundDeleted;
				Controls.Add(soundFieldControl);
			}
		}

		void soundFieldControl_SoundDeleted(object sender, EventArgs e)
		{
			// We don't want the file name hanging aroudn once we deleted the file.
			var sc = (ShortSoundFieldControl)sender;
			int dummy;
			var ws = WsForSoundField(sc, out dummy);
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_fdoCache.ActionHandlerAccessor,
				() =>
					m_fdoCache.DomainDataByFlid.SetMultiStringAlt(m_hvoObj, m_flid, ws.Handle,
						m_fdoCache.TsStrFactory.MakeString("", ws.Handle)));
		}

		IWritingSystem WsForSoundField(ShortSoundFieldControl sc, out int wsIndex)
		{
			int index = m_soundControls.IndexOf(sc);
			wsIndex = -1;
			foreach (var ws in m_rgws)
			{
				wsIndex++;
				var pws = ws as WritingSystemDefinition;
				if (pws == null || !pws.IsVoice)
					continue;
				if (index == 0)
					return ws;
				index--;
			}
			throw new InvalidOperationException("trying to get WS for sound field failed");
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
		/// <param name="configurationNode"></param>
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
			DisposeSoundControls(); // before we do the base refresh, which will layout, and possibly miss a deleted WS.
			m_rgws = WritingSystemOptions;
			bool baseResult = base.RefreshDisplay();
			SetupSoundControls();
			return baseResult;
		}

		void soundFieldControl_BeforeStartingToRecord(object sender, EventArgs e)
		{
			var sc = (ShortSoundFieldControl)sender;
			string path;
			string filename = CreateNewSoundFilename(out path);
			sc.Path = path;
			int dummy;
			var ws = WsForSoundField(sc, out dummy);
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_fdoCache.ActionHandlerAccessor,
				() =>
					m_fdoCache.DomainDataByFlid.SetMultiStringAlt(m_hvoObj, m_flid, ws.Handle,
						m_fdoCache.TsStrFactory.MakeString(filename, ws.Handle)));
		}

		private string CreateNewSoundFilename(out string path)
		{
			var obj = m_fdoCache.ServiceLocator.GetObject(m_hvoObj);
			var mediaDir = DirectoryFinder.GetMediaDir(m_fdoCache.LangProject.LinkedFilesRootDir);
			Directory.CreateDirectory(mediaDir); // Palaso media library does not cope if it does not exist.
			// Make up a unique file name for the new recording. It starts with the shortname of the object
			// so as to somewhat link them together, then adds a unique timestamp, then if by any chance
			// that exists it keeps trying.
			var baseNameForFile = obj.ShortName;
			// LT-12926: Path.ChangeExtension checks for invalid filename chars,
			// so we need to fix the filename before calling it.
			foreach (var c in Path.GetInvalidFileNameChars())
				baseNameForFile = baseNameForFile.Replace(c, '_');
			// WeSay and most other programs use NFC for file names, so we'll standardize on this.
			baseNameForFile = baseNameForFile.Normalize(NormalizationForm.FormC);
			string filename;
			do
			{
				filename = baseNameForFile;
				filename = Path.ChangeExtension(DateTime.UtcNow.Ticks + filename, "wav");
				path = Path.Combine(mediaDir, filename);

			} while (File.Exists(path));
			return filename;
		}

		void soundFieldControl_SoundRecorded(object sender, EventArgs e)
		{
			var sc = (ShortSoundFieldControl)sender;
			int dummy;
			var ws = WsForSoundField(sc, out dummy);
			var filenameNew = Path.GetFileName(sc.Path);
			var filenameOld = m_fdoCache.DomainDataByFlid.get_MultiStringAlt(m_hvoObj, m_flid, ws.Handle).Text ?? "";
			if (filenameNew != filenameOld)
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_fdoCache.ActionHandlerAccessor,
					() =>
						m_fdoCache.DomainDataByFlid.SetMultiStringAlt(m_hvoObj, m_flid, ws.Handle,
							m_fdoCache.TsStrFactory.MakeString(filenameNew, ws.Handle)));
			}
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
			TruncatePasteIfNecessary(e, m_flid);
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Show the writing system choices?
		/// </summary>
		/// -----------------------------------------------------------------------------------
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				DisposeSoundControls();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rgws = null;
			m_vc = null;
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a rootbox. When changing this, give careful consideration to changing Reuse().
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			m_rootb = null;
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rgws = WritingSystemOptions;

			int wsUser = m_fdoCache.WritingSystemFactory.UserWs;
			m_vc = new LabeledMultiStringViewVc(m_flid, m_rgws, wsUser, m_editable, m_fdoCache.TsStrFactory, this);

			// Review JohnT: why doesn't the base class do this??
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			// And maybe this too, at least by default?
			m_rootb.DataAccess = m_fdoCache.DomainDataByFlid;

			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			// arg4 could be used to supply a stylesheet.
			m_rootb.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);
			SetupSoundControls();
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
		/// <returns></returns>
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Triggers the display.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// ------------------------------------------------------------------------------------
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
		/// <param name="ws"></param>
		/// <param name="ich"></param>
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
	/// LabeledMultiStringControl (used in InsertEntryDlg)
	/// has an FdoCache, but it is used only to figure out the writing systems to use; the control
	/// works with a dummy cache, object, and flid, and the resulting text must be read back.
	/// </summary>
	public class LabeledMultiStringControl : UserControl, IVwNotifyChange, IFWDisposable
	{
		InnerLabeledMultiStringControl m_innerControl;
		bool m_isHot = false;
		bool m_hasBorder;
		Padding m_textPadding;

		/// <summary>
		/// Initializes a new instance of the <see cref="LabeledMultiStringControl"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsMagic">The ws magic.</param>
		/// <param name="vss">The VSS.</param>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		public LabeledMultiStringControl(FdoCache cache, int wsMagic, IVwStylesheet vss)
		{
			if (Application.RenderWithVisualStyles)
				DoubleBuffered = true;

			m_innerControl = new InnerLabeledMultiStringControl(cache, wsMagic);
			if (vss != null)
				m_innerControl.StyleSheet = vss;
			m_innerControl.Dock = DockStyle.Fill;
			this.Controls.Add(m_innerControl);
			m_innerControl.MakeRoot();

			m_innerControl.RootBox.DataAccess.AddNotification(this);
			m_innerControl.MouseEnter += new EventHandler(m_innerControl_MouseEnter);
			m_innerControl.MouseLeave += new EventHandler(m_innerControl_MouseLeave);
			m_innerControl.GotFocus += new EventHandler(m_innerControl_GotFocus);
			m_innerControl.LostFocus += new EventHandler(m_innerControl_LostFocus);

			HasBorder = true;
			Height = PreferredHeight;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LabeledMultiStringControl"/> class.
		/// For use with a non-standard list of wss (like available UI languages).
		/// (See CustomListDlg)
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsList">The non-standard list of IWritingSystems.</param>
		/// <param name="vss">The VSS.</param>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		public LabeledMultiStringControl(FdoCache cache, List<IWritingSystem> wsList, IVwStylesheet vss)
		{
			if (Application.RenderWithVisualStyles)
				DoubleBuffered = true;

			m_innerControl = new InnerLabeledMultiStringControl(cache, wsList);
			if (vss != null)
				m_innerControl.StyleSheet = vss;
			m_innerControl.Dock = DockStyle.Fill;
			this.Controls.Add(m_innerControl);
			m_innerControl.MakeRoot();

			m_innerControl.RootBox.DataAccess.AddNotification(this);
			m_innerControl.MouseEnter += new EventHandler(m_innerControl_MouseEnter);
			m_innerControl.MouseLeave += new EventHandler(m_innerControl_MouseLeave);
			m_innerControl.GotFocus += new EventHandler(m_innerControl_GotFocus);
			m_innerControl.LostFocus += new EventHandler(m_innerControl_LostFocus);

			HasBorder = true;
			Height = PreferredHeight;
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_innerControl != null)
				{
					if (m_innerControl.RootBox != null && m_innerControl.RootBox.DataAccess != null)
						m_innerControl.RootBox.DataAccess.RemoveNotification(this);

					m_innerControl.MouseEnter -= new EventHandler(m_innerControl_MouseEnter);
					m_innerControl.MouseLeave -= new EventHandler(m_innerControl_MouseLeave);
					m_innerControl.GotFocus -= new EventHandler(m_innerControl_GotFocus);
					m_innerControl.LostFocus -= new EventHandler(m_innerControl_LostFocus);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_innerControl = null;

			base.Dispose(disposing);
		}

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

		#endregion IDisposable override

		/// <summary>
		/// Gets the preferred height.
		/// </summary>
		/// <value>The preferred height.</value>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int PreferredHeight
		{
			get
			{
				CheckDisposed();
				int borderHeight = 0;
				switch (BorderStyle)
				{
					case BorderStyle.Fixed3D:
						borderHeight = SystemInformation.Border3DSize.Height * 2;
						break;

					case BorderStyle.FixedSingle:
						borderHeight = SystemInformation.BorderSize.Height * 2;
						break;
				}
				int height = 0;
				if (m_innerControl.RootBox != null && m_innerControl.RootBox.Height > 0)
					height = Math.Min(m_innerControl.RootBox.Height + 8, 66);
				else
					height = 46;	// barely enough to make a scroll bar workable
				return height + base.Padding.Vertical + borderHeight;
			}
		}

		Rectangle ContentRectangle
		{
			get
			{
				if (!Application.RenderWithVisualStyles || !m_hasBorder)
					return ClientRectangle;

				using (Graphics g = CreateGraphics())
				{
					VisualStyleRenderer renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal);
					return renderer.GetBackgroundContentRectangle(g, ClientRectangle);
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the text box has a border.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has a border, otherwise <c>false</c>.
		/// </value>
		public bool HasBorder
		{
			get
			{
				CheckDisposed();
				return m_hasBorder;
			}

			set
			{
				CheckDisposed();
				m_hasBorder = value;
				if (Application.RenderWithVisualStyles)
					SetPadding();
				else
					BorderStyle = m_hasBorder ? BorderStyle.Fixed3D : BorderStyle.None;
			}
		}

		/// <summary>
		/// Gets or sets the border style of the tree view control.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of the <see cref="T:System.Windows.Forms.BorderStyle"/> values. The default is <see cref="F:System.Windows.Forms.BorderStyle.Fixed3D"/>.
		/// </returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
		/// The assigned value is not one of the <see cref="T:System.Windows.Forms.BorderStyle"/> values.
		/// </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new BorderStyle BorderStyle
		{
			get
			{
				return base.BorderStyle;
			}

			set
			{
				if (!Application.RenderWithVisualStyles)
				{
					base.BorderStyle = value;
					m_hasBorder = value != BorderStyle.None;
				}
			}
		}

		/// <summary>
		/// Gets or sets padding within the control. This adjusts the padding around the text.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// A <see cref="T:System.Windows.Forms.Padding"/> representing the control's internal spacing characteristics.
		/// </returns>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Padding Padding
		{
			get
		{
			CheckDisposed();
				return m_textPadding;
			}

			set
			{
				CheckDisposed();
				m_textPadding = value;
				SetPadding();
			}
		}

		/// <summary>
		/// Gets a value indicating whether the control has input focus.
		/// </summary>
		/// <value></value>
		/// <returns>true if the control has focus; otherwise, false.
		/// </returns>
		public override bool Focused
		{
			get
			{
				CheckDisposed();
				return m_innerControl.Focused;
			}
		}

		/// <summary>
		/// Gets the root box.
		/// </summary>
		/// <value>The root box.</value>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwRootBox RootBox
		{
			get
			{
				CheckDisposed();
				return m_innerControl.RootBox;
			}
		}

		TextBoxState State
		{
			get
			{
				if (Enabled)
					return m_isHot ? TextBoxState.Hot : TextBoxState.Normal;
				else
					return TextBoxState.Disabled;
			}
		}

		void SetPadding()
		{
			Rectangle rect = ContentRectangle;
			base.Padding = new Padding((rect.Left - ClientRectangle.Left) + m_textPadding.Left,
				(rect.Top - ClientRectangle.Top) + m_textPadding.Top, (ClientRectangle.Right - rect.Right) + m_textPadding.Right,
				(ClientRectangle.Bottom - rect.Bottom) + m_textPadding.Bottom);
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			VisualStyleRenderer renderer = FwTextBox.CreateRenderer(State, ContainsFocus, true);
			if (renderer != null)
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
		}

		void m_innerControl_MouseLeave(object sender, EventArgs e)
		{
			m_isHot = false;
			Invalidate();
		}

		void m_innerControl_MouseEnter(object sender, EventArgs e)
		{
			m_isHot = true;
			Invalidate();
		}

		void m_innerControl_LostFocus(object sender, EventArgs e)
		{
			Invalidate();
		}

		void m_innerControl_GotFocus(object sender, EventArgs e)
		{
			Invalidate();
		}

		/// <summary>
		/// Activates a child control.
		/// </summary>
		/// <param name="directed">true to specify the direction of the control to select; otherwise, false.</param>
		/// <param name="forward">true to move forward in the tab order; false to move backward in the tab order.</param>
		protected override void Select(bool directed, bool forward)
		{
			base.Select(directed, forward);
			if (!directed)
				SelectNextControl(null, forward, true, true, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get one of the resulting strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsString Value(int ws)
		{
			CheckDisposed();

			return m_innerControl.Value(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set one of the strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetValue(int ws, ITsString tss)
		{
			CheckDisposed();

			m_innerControl.SetValue(ws, tss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set one of the strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetValue(int ws, string txt)
		{
			CheckDisposed();

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			SetValue(ws, tsf.MakeString(txt, ws));
		}

		/// <summary>
		/// Get the number of writing systems being displayed.
		/// </summary>
		public int NumberOfWritingSystems
		{
			get
			{
				CheckDisposed();
				return m_innerControl.WritingSystems.Count;
			}
		}

		/// <summary>
		/// Get the nth string and writing system.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString ValueAndWs(int index, out int ws)
		{
			CheckDisposed();

			ws = m_innerControl.WritingSystems[index].Handle;
			return m_innerControl.RootBox.DataAccess.get_MultiStringAlt(InnerLabeledMultiStringControl.khvoRoot,
				InnerLabeledMultiStringControl.kflid, ws);
		}

		/// <summary>
		/// Get the nth writing system.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		public int Ws(int index)
		{
			CheckDisposed();
			return m_innerControl.WritingSystems[index].Handle;
		}

		/// <summary>
		/// Selects a range of text based on the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// <param name="start">The position of the first character in the current text selection within the text box.</param>
		/// <param name="length">The number of characters to select.</param>
		/// <remarks>
		/// If you want to set the start position to the first character in the control's text, set the <i>start</i> parameter to 0.
		/// You can use this method to select a substring of text, such as when searching through the text of the control and replacing information.
		/// <b>Note:</b> You can programmatically move the caret within the text box by setting the <i>start</i> parameter to the position within
		/// the text box where you want the caret to move to and set the <i>length</i> parameter to a value of zero (0).
		/// The text box must have focus in order for the caret to be moved.
		/// </remarks>
		/// <exception cref="ArgumentException">
		/// The value assigned to either the <i>start</i> parameter or the <i>length</i> parameter is less than zero.
		/// </exception>
		public void Select(int ws, int start, int length)
		{
			CheckDisposed();

			if (start < 0)
				throw new ArgumentException("Starting position is less than zero.", "start");
			if (length < 0)
				throw new ArgumentException("Length is less than zero.", "length");

			IVwSelection sel = m_innerControl.RootBox.Selection;
			if (sel != null)
			{
				// See if the desired thing is already selected. If so do nothing. This can prevent stack overflow!
				ITsString tssDummy;
				int ichAnchor, ichEnd, hvo, tag, wsDummy;
				bool fAssocPrev;
				sel.TextSelInfo(true, out tssDummy, out ichEnd, out fAssocPrev, out hvo, out tag, out wsDummy);
				sel.TextSelInfo(false, out tssDummy, out ichAnchor, out fAssocPrev, out hvo, out tag, out wsDummy);
				if (Math.Min(ichAnchor, ichEnd) == start && Math.Max(ichAnchor, ichEnd) == start + length)
					return;
			}
			try
			{
				m_innerControl.RootBox.MakeTextSelection(0, 0, null, InnerLabeledMultiStringControl.kflid, 0, start, start + length,
					ws, false, -1, null, true);
			}
			catch
			{
			}
		}

		#region IVwNotifyChange Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// ------------------------------------------------------------------------------------
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			OnTextChanged(new EventArgs());
		}

		#endregion
	}

	internal class InnerLabeledMultiStringControl : SimpleRootSite
	{
		LabeledMultiStringVc m_vc;
		FdoCache m_realCache; // real one we get writing system info from
		ISilDataAccess m_sda; // one actually used in the view.
		List<IWritingSystem> m_rgws;

		internal const int khvoRoot = -3045; // arbitrary but recognizeable numbers for debugging.
		internal const int kflid = 4554;

		public InnerLabeledMultiStringControl(FdoCache cache, int wsMagic)
		{
			m_realCache = cache;
			m_sda = new TextBoxDataAccess { WritingSystemFactory = cache.WritingSystemFactory };
			m_rgws = WritingSystemServices.GetWritingSystemList(cache, wsMagic, 0, false);

			AutoScroll = true;
			IsTextBox = true;	// range selection not shown when not in focus
		}

		public InnerLabeledMultiStringControl(FdoCache cache, List<IWritingSystem> wsList)
		{
			// Ctor for use with a non-standard list of wss (like available UI languages)
			m_realCache = cache;
			m_sda = new TextBoxDataAccess { WritingSystemFactory = cache.WritingSystemFactory };
			m_rgws = wsList;

			AutoScroll = true;
			IsTextBox = true;	// range selection not shown when not in focus
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
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_realCache = null;
			m_rgws = null;
			m_vc = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Get the number of writing systems being displayed.
		/// </summary>
		public List<IWritingSystem> WritingSystems
		{
			get
			{
				CheckDisposed();
				return m_rgws;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_sda;

			int wsUser = m_realCache.ServiceLocator.WritingSystemManager.UserWs;
			int wsEn = m_realCache.ServiceLocator.WritingSystemManager.GetWsFromStr("en");
			m_vc = new LabeledMultiStringVc(kflid, WritingSystems, wsUser, true, wsEn, m_realCache.TsStrFactory);

			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			m_rootb.SetRootObject(khvoRoot, m_vc, 1, m_styleSheet);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			// The simple root site won't lay out properly until this is done.
			// It needs to be done before base.MakeRoot or it won't lay out at all ever!
			WritingSystemFactory = m_realCache.WritingSystemFactory;
			base.MakeRoot();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User pressed a key.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (!m_editingHelper.HandleOnKeyDown(e))
				base.OnKeyDown(e);
		}

		internal ITsString Value(int ws)
		{
			return m_sda.get_MultiStringAlt(khvoRoot, kflid, ws);
		}

		internal void SetValue(int ws, ITsString tss)
		{
			m_sda.SetMultiStringAlt(khvoRoot, kflid, ws, tss);
		}
	}

	/// <summary>
	/// View constructor for LabeledMultiStringView.
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
		/// <param name="visibleWss"></param>
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
	internal class LabeledMultiStringViewVc : LabeledMultiStringVc
	{
		private LabeledMultiStringView m_view;

		public LabeledMultiStringViewVc(int flid, List<IWritingSystem> rgws, int wsUser, bool editable,
			ITsStrFactory tsf, LabeledMultiStringView view)
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
		/// <param name="env"></param>
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
