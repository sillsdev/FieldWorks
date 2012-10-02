// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RecordDocView.cs
// Responsibility: John Thomson
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;
using System.IO;
using System.Resources;

using SIL.FieldWorks;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using XCore;

namespace SIL.FieldWorks.XWorks
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
	///		its FdoCache, which will result in its MakeRoot being called.)
	/// </summary>
	public class RecordDocView : RecordView
	{
		#region Data members

		/// <summary>
		/// The RootSite that displays the current object.
		/// </summary>
		protected RootSite m_rootSite;

		#endregion // Data members

		#region Construction and Removal
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RecordDocView"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public RecordDocView()
		{
		}

		/// <summary>
		/// Initialize this as an IxCoreColleague
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			InitBase(mediator, configurationParameters);
			m_fullyInitialized = true;
		}

		protected override IxCoreColleague[] GetMessageAdditionalTargets()
		{
			return new IxCoreColleague[] {m_rootSite};
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_rootSite != null)
					m_rootSite.Dispose();
			}
			m_rootSite = null;
			// m_mediator = null; // Bad idea, since superclass still needs it.

			base.Dispose(disposing);
		}

		#endregion // Construction and Removal

		#region Message Handlers

		public bool OnConsideringClosing(object argument, System.ComponentModel.CancelEventArgs args)
		{
			CheckDisposed();

			args.Cancel = !PrepareToGoAway();
			return args.Cancel; // if we want to cancel, others don't need to be asked.
		}

		#endregion // Message Handlers

		#region Other methods

		protected override void OnHandleCreated(EventArgs e)
		{
			// Must do this BEFORE base.OnHandleCreated, which will otherwise create the root box
			// with no stylesheet.
			if (m_rootSite.StyleSheet == null)
			{
				SetupStylesheet();
			}
			base.OnHandleCreated(e);
		}

		protected override void SetupStylesheet()
		{
			// If possible make it use the style sheet appropriate for its main window.
			IMainWindowDelegatedFunctions continingForm = this.FindForm() as IMainWindowDelegatedFunctions;
			if (continingForm != null)
				m_rootSite.StyleSheet = continingForm.StyleSheet;
		}
		protected virtual RootSite ConstructRoot()
		{
			Debug.Assert(false); // subclass must implement.
			return null;
		}

		protected override void ShowRecord()
		{
			//todo: add the document view name to the task label
			//todo: fast machine, this doesn't really seem to do any good. I think maybe the parts that
			//are taking a longtime are not getting Breath().
			//todo: test on a machine that is slow enough to see if this is helpful or not!
			ProgressState progress = FwXWindow.CreatePredictiveProgressState(m_mediator,this.m_vectorName);
			using (progress)
			{
				progress.Breath();

				Debug.Assert(m_rootSite!=null);

				progress.Breath();

				base.ShowRecord();

				Clerk.SaveOnChangeRecord();

				progress.Breath();

				if(Clerk.CurrentObject == null)
				{
					m_rootSite.Hide();
					return;
				}
				try
				{
					progress.SetMilestone();

					m_rootSite.Show();
					Cursor.Current = Cursors.WaitCursor;
					IChangeRootObject root = m_rootSite as IChangeRootObject;
					if (root != null && !Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
						root.SetRoot(Clerk.CurrentObject.Hvo);
					Cursor.Current = Cursors.Default;
				}
				catch(Exception error)
				{
					if (m_mediator.PropertyTable.GetBoolProperty("DoingAutomatedTest", false))
						throw;
					else	//don't really need to make the program stop just because we could not show this record.
					{
						SIL.Utils.ErrorReporter.ReportException(error, null, false);
					}
				}
			}
		}

		protected override void SetupDataContext()
		{
			Debug.Assert(m_configurationParameters != null);

			base.SetupDataContext();
			m_rootSite = ConstructRoot();
			m_rootSite.Init(m_mediator, m_configurationParameters); // Init it as xCoreColleague.
			//m_rootSite.PersistenceProvder = new XCore.PersistenceProvider(m_mediator.PropertyTable);

			m_rootSite.Dock = System.Windows.Forms.DockStyle.Fill;
			m_rootSite.Cache = Cache;

			Controls.Add(m_rootSite);
			m_rootSite.BringToFront(); // Review JohnT: is this needed?
		}

		/// <summary>
		/// if the XML configuration does not specify the availability of the treebar
		/// (e.g. treeBarAvailibility="Required"), then use this.
		/// </summary>
		protected override RecordView.TreebarAvailability DefaultTreeBarAvailability
		{
			get
			{
				return RecordView.TreebarAvailability.NotAllowed;
			}
		}

		#endregion // Other methods
	}

	/// <summary>
	/// This is a class that can be used as the rootsite of a RecordDocView, to make it a
	/// RecordDocXmlView.
	/// </summary>
	public class XmlDocItemView : XmlView, IChangeRootObject
	{
		#region implemention of IChangeRootObject

		public void SetRoot(int hvo)
		{
			CheckDisposed();
			if (m_hvoRoot == hvo)
				return; // OnRecordNavigation is often called repeatedly wit the same HVO, we don't need to recompute every time.

			m_hvoRoot = hvo;
			if (this.RootBox != null)
				this.RootBox.SetRootObject(m_hvoRoot, m_xmlVc, 1, m_styleSheet);
			// If the root box doesn't exist yet, the right root will be used in MakeRoot.
		}
		#endregion

		public XmlDocItemView(int hvoRoot, XmlNode xnSpec, string sLayout) :
			base(hvoRoot, sLayout, null,
				XmlUtils.GetOptionalBooleanAttributeValue(xnSpec, "editable", true))
		{
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			// Must do this BEFORE base.OnHandleCreated, which will otherwise create the root box
			// with no stylesheet.
			if (StyleSheet == null)
			{
				SetupStylesheet();
			}
			base.OnHandleCreated (e);
		}

		private void SetupStylesheet()
		{
			// If possible make it use the style sheet appropriate for its main window.
			IMainWindowDelegatedFunctions continingForm = this.FindForm() as IMainWindowDelegatedFunctions;
			if (continingForm != null)
				StyleSheet = continingForm.StyleSheet;
		}
	}

	/// <summary>
	/// This is a RecordDocView in which the view of each object is specified by a jtview XML element that is the
	/// first child of the parameters node.
	/// </summary>
	public class RecordDocXmlView : RecordDocView
	{
		XmlNode m_jtSpecs; // node required by XmlView.
		protected string m_configObjectName; // name to display in Configure dialog.

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}
			m_jtSpecs = null;

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		protected override RootSite ConstructRoot()
		{
			string sProp = XmlUtils.GetOptionalAttributeValue(m_jtSpecs, "layoutProperty");
			string sLayout = null;
			if (!String.IsNullOrEmpty(sProp))
				sLayout = m_mediator.PropertyTable.GetStringProperty(sProp, null);
			if (String.IsNullOrEmpty(sLayout))
				sLayout = XmlUtils.GetManditoryAttributeValue(m_jtSpecs, "layout");
			return new XmlDocItemView(0, m_jtSpecs, sLayout);
		}

		protected override void SetupDataContext()
		{
			// The base class uses these specs, so locate them first!
			m_jtSpecs = m_configurationParameters;
			base.SetupDataContext ();
		}

		protected override void ReadParameters()
		{
			m_configObjectName = XmlUtils.GetLocalizedAttributeValue(m_mediator.StringTbl,
				m_configurationParameters, "configureObjectName", null);
			base.ReadParameters();
		}
		/// <summary>
		/// The configure dialog may be launched any time this tool is active.
		/// Its name is derived from the name of the tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureXmlDocView(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_configObjectName == null || m_configObjectName == "")
			{
				display.Enabled = display.Visible = false;
				return true;
			}
			display.Enabled = true;
			display.Visible = true;
			// Enhance JohnT: make this configurable. We'd like to use the 'label' attribute of the 'tool'
			// element, but we don't have it, only the two-level-down 'parameters' element
			// so use "configureObjectName" parameter for now.
			// REVIEW: SHOULD THE "..." BE LOCALIZABLE (BY MAKING IT PART OF THE SOURCE FOR display.Text)?
			display.Text = String.Format(display.Text, m_configObjectName + "...");
			return true; //we've handled this
		}
		/// <summary>
		/// Launch the configure dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnConfigureXmlDocView(object commandObject)
		{
			CheckDisposed();

			XmlDocConfigureDlg dlg = new XmlDocConfigureDlg();
			string sProp = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "layoutProperty");
			if (String.IsNullOrEmpty(sProp))
				sProp = "DictionaryPublicationLayout";
			dlg.SetConfigDlgInfo(m_configurationParameters, Cache, StyleSheet,
				this.FindForm() as IMainWindowDelegateCallbacks, m_mediator, sProp);
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				// LT-8767 When this dialog is launched from the Configure Dictionary View dialog
				// m_mediator != null && m_rootSite == null so we need to handle this to prevent a crash.
				if (m_mediator != null && m_rootSite != null)
				{
					string sNewLayout = m_mediator.PropertyTable.GetStringProperty(sProp, null);
					(m_rootSite as XmlDocItemView).ResetTables(sNewLayout);
				}
			}
			return true; // we handled it
		}
		private FwStyleSheet StyleSheet
		{
			get
			{
				// If possible retrieve the style sheet appropriate for its main window.
				IMainWindowDelegatedFunctions containingForm = this.FindForm() as IMainWindowDelegatedFunctions;
				if (containingForm != null)
					return containingForm.StyleSheet;
				return null;
			}
		}
	}
}
