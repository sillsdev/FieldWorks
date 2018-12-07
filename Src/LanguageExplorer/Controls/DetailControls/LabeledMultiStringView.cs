// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SIL.Media;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Infrastructure;
using System.Text;
using System.Xml.Linq;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// LabeledMultiStringView displays one or more writing system alternatives of a string property.
	/// It simply edits that property.
	/// </summary>
	/// <remarks>It must implement IFlexComponent so that the inner view will be a message target when
	/// the containing slice is. This allows things like appropriately enabling the writing system combo.</remarks>
	public class LabeledMultiStringView : UserControl, IFlexComponent
	{
		private List<ShortSoundFieldControl> m_soundControls = new List<ShortSoundFieldControl>();

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
			InnerView = new InnerLabeledMultiStringView(hvo, flid, wsMagic, wsOptional, forceIncludeEnglish, editable, spellCheck);
			InnerView.SetupOtherControls += (sender, e) => { SetupSoundControls(); };
			InnerView.Dock = DockStyle.Fill;
			Controls.Add(InnerView);
		}

		/// <summary>
		/// Re-initialize this view as if it had been constructed with the specified arguments.
		/// </summary>
		public void Reuse(int hvo, int flid, int wsMagic, int wsOptional, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			InnerView.Reuse(hvo, flid, wsMagic, wsOptional, forceIncludeEnglish, editable, spellCheck);
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
		internal InnerLabeledMultiStringView InnerView { get; private set; }

		/// <summary>
		/// Call this on initialization when all properties (e.g., ConfigurationNode) are set.
		/// The purpose is when reusing the slice, when we may have to reconstruct the root box.
		/// </summary>
		public void FinishInit()
		{
			InnerView.FinishInit();
		}

		/// <summary>
		/// Get any text styles from configuration node (which is now available; it was not at construction)
		/// </summary>
		public void FinishInit(XElement configurationNode)
		{
			InnerView.FinishInit(configurationNode);
		}

		/// <summary>
		/// Get or set the text style name
		/// </summary>
		public string TextStyle
		{
			get { return InnerView.TextStyle; }
			set { InnerView.TextStyle = value; }
		}

		/// <summary>
		/// On a major refresh, the writing system list may have changed; update accordingly.
		/// </summary>
		public bool RefreshDisplay()
		{
			SuspendLayout();
			DisposeSoundControls(); // before we do the base refresh, which will layout, and possibly miss a deleted WS.
			var ret = InnerView.RefreshDisplay();
			SetupSoundControls();
			ResumeLayout();
			return ret;
		}

		/// <summary></summary>
		public bool IsSelectionFormattable => InnerView.IsSelectionFormattable;

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
			{
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				// In Mono, DisposeSoundControls() causes a layout, which then crashes
				// nicely when called from here.  See FWNX-967.  Since we'll be
				// disposed, there's no point in resuming layout later...
				SuspendLayout();
				// Dispose managed resources here.
				DisposeSoundControls();
				InnerView.Dispose();
			}

			m_soundControls = null;
			InnerView = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;
		}

		#endregion IDisposable override

		/// <summary>
		/// This is the list of writing systems that can be enabled for this control. It should be either the Vernacular list
		/// or Analysis list shown in the WritingSystemPropertiesDialog which are checked and unchecked.
		/// </summary>
		public List<CoreWritingSystemDefinition> WritingSystemOptions => InnerView.WritingSystemOptions;

		/// <summary>
		/// returns a list of the writing systems available to display for this view
		/// </summary>
		/// <param name="fIncludeUncheckedActiveWss">if false, include only current wss,
		/// if true, includes unchecked active wss.</param>
		public List<CoreWritingSystemDefinition> GetWritingSystemOptions(bool fIncludeUncheckedActiveWss)
		{
			return InnerView.GetWritingSystemOptions(fIncludeUncheckedActiveWss);
		}

		/// <summary>
		/// if non-null, we'll use this list to determine which writing systems to display. These
		/// are the writing systems the user has checked in the WritingSystemPropertiesDialog.
		/// if null, we'll display every writing system option.
		/// </summary>
		public List<CoreWritingSystemDefinition> WritingSystemsToDisplay
		{
			get { return InnerView.WritingSystemsToDisplay; }
			set { InnerView.WritingSystemsToDisplay = value; }
		}

		/// <summary />
		internal void TriggerDisplay(IVwEnv vwenv)
		{
			InnerView.TriggerDisplay(vwenv);
		}

		/// <summary>
		/// Make a selection in the specified writing system at the specified character offset.
		/// Note: selecting other than the first writing system is not yet implemented.
		/// </summary>
		public void SelectAt(int ws, int ich)
		{
			InnerView.SelectAt(ws, ich);
		}

		/// <summary>
		/// If we're shutting down, this might return null
		/// </summary>
		CoreWritingSystemDefinition WsForSoundField(ShortSoundFieldControl sc, out int wsIndex)
		{
			var index = m_soundControls.IndexOf(sc);
			wsIndex = -1;
			foreach (var ws in InnerView.WritingSystemsToDisplay)
			{
				wsIndex++;
				if (!ws.IsVoice)
				{
					continue;
				}
				if (index == 0)
				{
					return ws;
				}
				index--;
			}
			return null;
		}

		/// <summary>
		/// Arrange our sound controls if any.
		/// </summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
			if (InnerView.VC == null || InnerView.RootBox == null) // We can come in with no rootb from a dispose call.
			{
				return;
			}
			if (Visible)
			{
				InnerView.RefreshDisplayIfPending(); // Reconstruct the innerView's RootBox only if it is pending.
			}
			int dpiX;
			using (var graphics = CreateGraphics())
			{
				dpiX = (int)graphics.DpiX;
			}
			var indent = InnerView.VC.m_mDxmpLabelWidth * dpiX / 72000 + 5; // 72000 millipoints/inch
			if (m_soundControls.Count == 0)
				SetupSoundControls();
			foreach (var control in m_soundControls)
			{
				int wsIndex;
				var ws = WsForSoundField(control, out wsIndex);
				if (ws != null)
				{
					control.Left = indent;
					control.Width = Width - indent;
					control.Top = Height - indent + 5;
					var sel = MultiStringSelectionUtils.GetSelAtStartOfWs(InnerView.RootBox, InnerView.Flid, wsIndex, ws);
					if (sel != null)
					{
						// not sure how it could be null, but see LT-13984. Most likely we are doing it too
						// soon, perhaps before the root box is even constructed.
						// Leave control.Top zero and hope layout gets called again when we can make
						// the selection successfully.
						Rectangle selRect;
						if (InnerView.GetSoundControlRectangle(sel, out selRect))
						{
							control.Top = selRect.Top;
					}
					}
					// Don't crash trying to bring to front if control is not a child control on Linux (FWNX-1348).
					// If control.Parent is null, don't crash, and bring to front anyway on Windows (LT-15148).
					if (control.Parent == null || control.Parent.Controls.Contains(control))
					{
						control.BringToFront();
				}
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

		private void SetupSoundControls()
		{
			if (InnerView.WritingSystemsToDisplay == null)
			{
				return; // should get called again when it is set up.
			}
			if (InnerView.RootBox == null)
			{
				return; // called again in MakeRoot, when information more complete.
			}
			var index = -1;
			foreach (var ws in InnerView.WritingSystemsToDisplay)
			{
				index++;
				if (!ws.IsVoice || MultiStringSelectionUtils.GetSelAtStartOfWs(InnerView.RootBox, InnerView.Flid, index, ws) == null)
				{
					continue;
				}
				var soundFieldControl = new ShortSoundFieldControl();
				m_soundControls.Add(soundFieldControl); // todo: one for each audio one
				soundFieldControl.Visible = true;
				soundFieldControl.PlayOnly = false;
				var filename = InnerView.Cache.DomainDataByFlid.get_MultiStringAlt(InnerView.HvoObj, InnerView.Flid, ws.Handle).Text ?? "";
				string path;
				if (string.IsNullOrEmpty(filename))
				{
					// Provide a filename for copying an existing file to.
					CreateNewSoundFilename(out path);
				}
				else
				{
					var mediaDir = LcmFileHelper.GetMediaDir(InnerView.Cache.LangProject.LinkedFilesRootDir);
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
						{
							path = tryPath;
					}
				}
				}
				try
				{
					soundFieldControl.Path = path;
					soundFieldControl.BeforeStartingToRecord += soundFieldControl_BeforeStartingToRecord;
					soundFieldControl.SoundRecorded += soundFieldControl_SoundRecorded;
					soundFieldControl.SoundDeleted += soundFieldControl_SoundDeleted;
					Controls.Add(soundFieldControl);
				}
				catch (Exception e)
				{
					Debug.WriteLine(e.Message);
				}
			}
		}

		private void soundFieldControl_SoundDeleted(object sender, EventArgs e)
		{
			// We don't want the file name hanging aroudn once we deleted the file.
			var sc = (ShortSoundFieldControl)sender;
			int dummy;
			var ws = WsForSoundField(sc, out dummy);
			var handle = ws?.Handle ?? 0;
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(InnerView.Cache.ActionHandlerAccessor,
				() => InnerView.Cache.DomainDataByFlid.SetMultiStringAlt(InnerView.HvoObj, InnerView.Flid, handle, TsStringUtils.EmptyString(handle)));
		}

		private void soundFieldControl_BeforeStartingToRecord(object sender, EventArgs e)
		{
			var sc = (ShortSoundFieldControl)sender;
			string path;
			var filename = CreateNewSoundFilename(out path);
			sc.Path = path;
			int dummy;
			var ws = WsForSoundField(sc, out dummy);
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(InnerView.Cache.ActionHandlerAccessor,
				() => InnerView.Cache.DomainDataByFlid.SetMultiStringAlt(InnerView.HvoObj, InnerView.Flid, ws.Handle, TsStringUtils.MakeString(filename, ws.Handle)));
		}

		private string CreateNewSoundFilename(out string path)
		{
			var obj = InnerView.Cache.ServiceLocator.GetObject(InnerView.HvoObj);
			var mediaDir = LcmFileHelper.GetMediaDir(InnerView.Cache.LangProject.LinkedFilesRootDir);
			Directory.CreateDirectory(mediaDir); // Palaso media library does not cope if it does not exist.
			// Make up a unique file name for the new recording. It starts with the shortname of the object
			// so as to somewhat link them together, then adds a unique timestamp, then if by any chance
			// that exists it keeps trying.
			var baseNameForFile = obj.ShortName ?? string.Empty;
			// LT-12926: Path.ChangeExtension checks for invalid filename chars,
			// so we need to fix the filename before calling it.
			baseNameForFile = Path.GetInvalidFileNameChars().Aggregate(baseNameForFile, (current, c) => current.Replace(c, '_'));
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

		private void soundFieldControl_SoundRecorded(object sender, EventArgs e)
		{
			var sc = (ShortSoundFieldControl)sender;
			int dummy;
			var ws = WsForSoundField(sc, out dummy);
			var filenameNew = Path.GetFileName(sc.Path);
			var filenameOld = InnerView.Cache.DomainDataByFlid.get_MultiStringAlt(InnerView.HvoObj, InnerView.Flid, ws.Handle).Text ?? "";
			if (filenameNew != filenameOld)
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(InnerView.Cache.ActionHandlerAccessor,
					() => InnerView.Cache.DomainDataByFlid.SetMultiStringAlt(InnerView.HvoObj, InnerView.Flid, ws.Handle, TsStringUtils.MakeString(filenameNew, ws.Handle)));
			}
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			InnerView.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
		}

		#endregion
	}
}
