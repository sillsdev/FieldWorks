// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.Utils;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// RecordDocView implements a RecordView (view showing one object at a time from a sequence)
	/// in which the single object is displayed using a FieldWorks view. This is an abstract class.
	/// The actual view is the responsibility of the subclass. Subclasses must:
	///
	/// 1. Implement a subclass of RootSite, including a MakeRoot method and suitable
	/// view constructor as needed. It must implement IChangeRootObject.
	/// 2. Override ConstructRoot, which should return a new instance of the SimpleRootSite subclass.
	///		(This class will take care of docking the root site and making it visible and setting
	///		its LcmCache, which will result in its MakeRoot being called.)
	/// </summary>
	internal class RecordDocView : RecordView
	{
		#region Data members

		/// <summary>
		/// The RootSite that displays the current object.
		/// </summary>
		protected RootSite m_rootSite;
		private StatusBarProgressPanel m_statusBarProgressPanel;

		#endregion // Data members

		#region Construction and Removal

		public RecordDocView(XElement configurationParametersElement, LcmCache cache, IRecordList recordList, StatusBarProgressPanel progressPanel)
			: base(configurationParametersElement, cache, recordList)
		{
			m_statusBarProgressPanel = progressPanel;
		}

		#region Overrides of ViewBase

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public override void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			base.InitializeFlexComponent(flexComponentParameters);

			InitBase();

			Subscriber.Subscribe(LanguageExplorerConstants.ConsideringClosing, ConsideringClosing_Handler);
			m_fullyInitialized = true;
		}

		#endregion

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				Subscriber.Unsubscribe(LanguageExplorerConstants.ConsideringClosing, ConsideringClosing_Handler);
				m_rootSite?.Dispose();
			}
			m_rootSite = null;
			m_statusBarProgressPanel = null;

			base.Dispose(disposing);
		}

		#endregion // Construction and Removal

		#region Message Handlers

		private void ConsideringClosing_Handler(object newValue)
		{
			var args = (CancelEventArgs)newValue;
			if (args.Cancel)
			{
				// Someone else wants to cancel.
				return;
			}
			args.Cancel = !PrepareToGoAway();
		}

		#endregion // Message Handlers

		#region Other methods

		protected override void OnHandleCreated(EventArgs e)
		{
			// Must do this BEFORE base.OnHandleCreated, which will otherwise create the root box
			// with no stylesheet.
			if (m_rootSite != null && m_rootSite.StyleSheet == null)
			{
				SetupStylesheet();
			}
			base.OnHandleCreated(e);
		}

		protected override void SetupStylesheet()
		{
			// If possible make it use the style sheet appropriate for its main window.
			m_rootSite.StyleSheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
		}
		protected virtual RootSite ConstructRoot()
		{
			Debug.Assert(false); // subclass must implement.
			return null;
		}

		protected override void ShowRecord()
		{
			Debug.Assert(MyRecordList.CurrentObject != null);
			Debug.Assert(m_rootSite != null);

			//todo: add the document view name to the task label
			//todo: fast machine, this doesn't really seem to do any good. I think maybe the parts that
			//are taking a long time are not getting Breath().
			//todo: test on a machine that is slow enough to see if this is helpful or not!
			using (var progress = ProgressState.CreatePredictiveProgressState(m_statusBarProgressPanel, MyRecordList.PropertyName))
			{
				progress.Breath();
				base.ShowRecord();
				MyRecordList.SaveOnChangeRecord();
				progress.Breath();

				try
				{
					progress.SetMilestone();
					if (!m_rootSite.Visible)
					{
						m_rootSite.Visible = true;
					}
					BringToFront();
					m_rootSite.BringToFront();
					using (new WaitCursor(this))
					{
						var root = m_rootSite as IChangeRootObject;
						if (root != null && !MyRecordList.SuspendLoadingRecordUntilOnJumpToRecord)
						{
							root.SetRoot(MyRecordList.CurrentObject.Hvo);
						}
					}
				}
				catch (Exception error)
				{
					var app = PropertyTable.GetValue<IFlexApp>(LanguageExplorerConstants.App);
					using (var appSettingsKey = app.SettingsKey)
					{
						//don't really need to make the program stop just because we could not show this record.
						ErrorReporter.ReportException(error, appSettingsKey, app.SupportEmailAddress, null, false);
					}
				}
			}
		}

		protected override void SetupDataContext()
		{
			Debug.Assert(m_configurationParametersElement != null);

			base.SetupDataContext();
			m_rootSite = ConstructRoot();
			m_rootSite.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber)); // Init it as Flex component.

			m_rootSite.Dock = DockStyle.Fill;
			m_rootSite.Cache = Cache;

			Controls.Add(m_rootSite);
			m_rootSite.BringToFront(); // Review JohnT: is this needed?
		}

		/// <summary>
		/// if the XML configuration does not specify the availability of the treebar
		/// (e.g. treeBarAvailibility="Required"), then use this.
		/// </summary>
		protected override TreebarAvailability DefaultTreeBarAvailability => TreebarAvailability.NotAllowed;

		#endregion // Other methods
	}
}