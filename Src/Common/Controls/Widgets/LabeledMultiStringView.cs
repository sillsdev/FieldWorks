using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
using XCore;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// LabeledMultiStringView displays one or more writing system alternatives of a string property.
	/// It simply edits that property.
	/// </summary>
	/// <remarks>It must implement IxCoreColleague so that the inner view will be a message target when
	/// the containing slice is. This allows things like appropriately enabling the writing system combo.</remarks>
	public class LabeledMultiStringView : UserControl, IxCoreColleague
	{
		private InnerLabeledMultiStringView m_innerView;
		private List<Palaso.Media.ShortSoundFieldControl> m_soundControls = new List<ShortSoundFieldControl>();

		/// <summary>
		/// Constructor.
		/// </summary>
		public LabeledMultiStringView(int hvo, int flid, int wsMagic, bool forceIncludeEnglish, bool editable)
			: this(hvo, flid, wsMagic, 0, forceIncludeEnglish, editable, true)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public LabeledMultiStringView(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			m_innerView = new InnerLabeledMultiStringView(hvo, flid, wsMagic, wsOptional, forceIncludeEnglish, editable, spellCheck);
			m_innerView.SetupOtherControls += (sender, e) => { SetupSoundControls(); };
			m_innerView.Dock = DockStyle.Fill;
			Controls.Add(m_innerView);
		}

		/// <summary>
		/// Re-initialize this view as if it had been constructed with the specified arguments.
		/// </summary>
		public void Reuse(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			m_innerView.Reuse(hvo, flid, wsMagic, wsOptional, forceIncludeEnglish, editable, spellCheck);
			try
			{
				SuspendLayout();
				DisposeSoundControls();
				SetupSoundControls();
			}
			finally
			{
				ResumeLayout();
			}
		}

		/// <summary>
		/// Provide access to the inner view to set delegates.
		/// </summary>
		public InnerLabeledMultiStringView InnerView
		{
			get { return m_innerView; }
		}

		/// <summary>
		/// Call this on initialization when all properties (e.g., ConfigurationNode) are set.
		/// The purpose is when reusing the slice, when we may have to reconstruct the root box.
		/// </summary>
		public void FinishInit()
		{
			m_innerView.FinishInit();
		}

		/// <summary>
		/// Get any text styles from configuration node (which is now available; it was not at construction)
		/// </summary>
		public void FinishInit(XmlNode configurationNode)
		{
			m_innerView.FinishInit(configurationNode);
		}

		/// <summary>
		/// Get or set the text style name
		/// </summary>
		public string TextStyle
		{
			get { return m_innerView.TextStyle; }
			set { m_innerView.TextStyle = value; }
		}

		/// <summary>
		/// On a major refresh, the writing system list may have changed; update accordingly.
		/// </summary>
		public bool RefreshDisplay()
		{
			DisposeSoundControls(); // before we do the base refresh, which will layout, and possibly miss a deleted WS.
			var ret = m_innerView.RefreshDisplay();
			SetupSoundControls();
			return ret;
		}

		/// <summary></summary>
		public bool IsSelectionFormattable
		{
			get { return m_innerView.IsSelectionFormattable; }
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
				// In Mono, DisposeSoundControls() causes a layout, which then crashes
				// nicely when called from here.  See FWNX-967.  Since we'll be
				// disposed, there's no point in resuming layout later...
				SuspendLayout();
				// Dispose managed resources here.
				DisposeSoundControls();
				m_innerView.Dispose();
				m_innerView = null;
			}
		}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("LabeledMultiStringView", "This object is being used after it has been disposed: this is an Error.");
		}
		#endregion IDisposable override

		/// <summary>
		/// This is the list of writing systems that can be enabled for this control. It should be either the Vernacular list
		/// or Analysis list shown in the WritingSystemPropertiesDialog which are checked and unchecked.
		/// </summary>
		public List<IWritingSystem> WritingSystemOptions
		{
			get
			{
				CheckDisposed();
				return m_innerView.WritingSystemOptions;
			}
		}

		/// <summary>
		/// returns a list of the writing systems available to display for this view
		/// </summary>
		/// <param name="fIncludeUncheckedActiveWss">if false, include only current wss,
		/// if true, includes unchecked active wss.</param>
		public List<IWritingSystem> GetWritingSystemOptions(bool fIncludeUncheckedActiveWss)
		{
			CheckDisposed();
			return m_innerView.GetWritingSystemOptions(fIncludeUncheckedActiveWss);
		}

		/// <summary>
		/// if non-null, we'll use this list to determine which writing systems to display. These
		/// are the writing systems the user has checked in the WritingSystemPropertiesDialog.
		/// if null, we'll display every writing system option.
		/// </summary>
		public List<IWritingSystem> WritingSystemsToDisplay
		{
			get { return m_innerView.WritingSystemsToDisplay; }
			set { m_innerView.WritingSystemsToDisplay = value; }
		}

		/// <summary></summary>
		internal void TriggerDisplay(IVwEnv vwenv)
		{
			CheckDisposed();
			m_innerView.TriggerDisplay(vwenv);
		}

		/// <summary>
		/// Make a selection in the specified writing system at the specified character offset.
		/// Note: selecting other than the first writing system is not yet implemented.
		/// </summary>
		public void SelectAt(int ws, int ich)
		{
			CheckDisposed();
			m_innerView.SelectAt(ws, ich);
		}

		/// <summary>
		/// If we're shutting down, this might return null
		/// </summary>
		IWritingSystem WsForSoundField(ShortSoundFieldControl sc, out int wsIndex)
		{
			int index = m_soundControls.IndexOf(sc);
			wsIndex = -1;
			foreach (var ws in m_innerView.WritingSystems)
			{
				wsIndex++;
				var pws = ws as WritingSystemDefinition;
				if (pws == null || !pws.IsVoice)
					continue;
				if (index == 0)
					return ws;
				index--;
			}
			return null;
			//throw new InvalidOperationException("trying to get WS for sound field failed");
		}

		/// <summary>
		/// Arrange our sound controls if any.
		/// </summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
			if (m_innerView.VC == null || m_innerView.RootBox == null) // We can come in with no rootb from a dispose call.
				return;
			int dpiX;
			using (var graphics = CreateGraphics())
			{
				dpiX = (int)graphics.DpiX;
			}
			int indent = m_innerView.VC.m_mDxmpLabelWidth * dpiX / 72000 + 5; // 72000 millipoints/inch
			foreach (var control in m_soundControls)
			{
				int wsIndex;
				var ws = WsForSoundField(control, out wsIndex);
				if (ws != null)
				{
					control.Left = indent;
					control.Width = Width - indent;
					var sel = MultiStringSelectionUtils.GetSelAtStartOfWs(m_innerView.RootBox, m_innerView.Flid, wsIndex, ws);
					if (sel != null)
					{
						// not sure how it could be null, but see LT-13984. Most likely we are doing it too
						// soon, perhaps before the root box is even constructed.
						// Leave control.Top zero and hope layout gets called again when we can make
						// the selection successfully.
						Rectangle selRect;
						m_innerView.GetSoundControlRectangle(sel, out selRect);
						control.Top = selRect.Top;
					}
					control.BringToFront();
				}
			}
		}

		private void DisposeSoundControls()
		{
			foreach (var sc in m_soundControls)
			{
				Controls.Remove(sc);
				sc.Dispose();
			}
			m_soundControls.Clear();
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
						Justification="soundFieldControl gets disposed in Dispose method")]
		private void SetupSoundControls()
		{
			if (m_innerView.WritingSystemsToDisplay == null)
				return; // should get called again when it is set up.
			if (m_innerView.RootBox == null)
				return; // called again in MakeRoot, when information more complete.
			int index = -1;
			foreach (var ws in m_innerView.WritingSystemsToDisplay)
			{
				index++;
				var pws = ws as WritingSystemDefinition;
				if (pws == null || !pws.IsVoice ||
					MultiStringSelectionUtils.GetSelAtStartOfWs(m_innerView.RootBox, m_innerView.Flid, index, ws) == null)
				{
					continue;
				}
				var soundFieldControl = new ShortSoundFieldControl();
				m_soundControls.Add(soundFieldControl); // todo: one for each audio one
				soundFieldControl.Visible = true;
				soundFieldControl.PlayOnly = false;
				var filename = m_innerView.Cache.DomainDataByFlid.get_MultiStringAlt(m_innerView.HvoObj, m_innerView.Flid, ws.Handle).Text ?? "";
				string path;
				if (String.IsNullOrEmpty(filename))
				{
					// Provide a filename for copying an existing file to.
					CreateNewSoundFilename(out path);
				}
				else
				{
					var mediaDir = FdoFileHelper.GetMediaDir(m_innerView.Cache.LangProject.LinkedFilesRootDir);
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
			var handle = ws == null ? 0 : ws.Handle;
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_innerView.Cache.ActionHandlerAccessor,
				() => m_innerView.Cache.DomainDataByFlid.SetMultiStringAlt(m_innerView.HvoObj, m_innerView.Flid, handle,
					m_innerView.Cache.TsStrFactory.MakeString("", handle)));
		}

		void soundFieldControl_BeforeStartingToRecord(object sender, EventArgs e)
		{
			var sc = (ShortSoundFieldControl)sender;
			string path;
			string filename = CreateNewSoundFilename(out path);
			sc.Path = path;
			int dummy;
			var ws = WsForSoundField(sc, out dummy);
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_innerView.Cache.ActionHandlerAccessor,
				() => m_innerView.Cache.DomainDataByFlid.SetMultiStringAlt(m_innerView.HvoObj, m_innerView.Flid,
					ws.Handle, m_innerView.Cache.TsStrFactory.MakeString(filename, ws.Handle)));
		}

		private string CreateNewSoundFilename(out string path)
		{
			var obj = m_innerView.Cache.ServiceLocator.GetObject(m_innerView.HvoObj);
			var mediaDir = FdoFileHelper.GetMediaDir(m_innerView.Cache.LangProject.LinkedFilesRootDir);
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
			var filenameOld = m_innerView.Cache.DomainDataByFlid.get_MultiStringAlt(m_innerView.HvoObj, m_innerView.Flid, ws.Handle).Text ?? "";
			if (filenameNew != filenameOld)
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_innerView.Cache.ActionHandlerAccessor,
					() => m_innerView.Cache.DomainDataByFlid.SetMultiStringAlt(m_innerView.HvoObj, m_innerView.Flid,
						ws.Handle, m_innerView.Cache.TsStrFactory.MakeString(filenameNew, ws.Handle)));
			}
		}

		/// <summary>
		/// Required method for IXCoreColleague. As a colleague, it behaves exactly like its inner view.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
			m_innerView.Init(mediator, configurationParameters);
		}

		/// <summary>
		/// Required method for IXCoreColleague. Return the message targets for the inner view.
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			return m_innerView == null ? new IxCoreColleague[0] : m_innerView.GetMessageTargets();
		}

		/// <summary>
		/// Required method for IXCoreColleague. Behaves exactly like its inner view.
		/// </summary>
		public bool ShouldNotCall { get { return m_innerView.ShouldNotCall; } }

		/// <summary>
		/// Required method for IXCoreColleague. Behaves exactly like its inner view.
		/// </summary>
		public int Priority { get { return m_innerView.Priority; } }
	}
}
