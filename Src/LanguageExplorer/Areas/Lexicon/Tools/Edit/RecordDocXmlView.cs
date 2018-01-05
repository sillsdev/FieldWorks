// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// This is a RecordDocView in which the view of each object is specified by a jtview XML element that is the
	/// first child of the parameters node.
	/// </summary>
	/// <remarks>
	/// Used by: FindExampleSentenceDlg
	/// </remarks>
	internal class RecordDocXmlView : RecordDocView
	{
		XElement m_jtSpecs; // node required by XmlView.
		protected string m_configObjectName; // name to display in Configure dialog.

		public RecordDocXmlView(XElement configurationParametersElement, LcmCache cache, IRecordList recordList, StatusBarProgressPanel progressPanel)
			: base(configurationParametersElement, cache, recordList, progressPanel)
		{
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
			//Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			string sLayout = GetLayoutName(m_jtSpecs, PropertyTable);
			return new XmlDocItemView(0, m_jtSpecs, sLayout);
		}

		protected override TreebarAvailability DefaultTreeBarAvailability
		{
			get { return TreebarAvailability.NotMyBusiness; }
		}

		/// <summary>
		/// This routine encapsulates the process for looking at the spec node of a tool (specifically the
		/// parameters node controlling the tool for a RecordDocView) and determining the layout that should
		/// be used as the root of the view.
		/// Three attributes contribute to this. First, if "layoutProperty" is present, it gives the name of
		/// a property to be looked up in the mediator to get the desired layout name.
		/// If nothing is found in the mediator, we fall back to using the "layout" attribute, which is
		/// mandatory, to determine the view.
		/// Then, if layoutSuffix is specified, it will be appended to whatever we got from the process above,
		/// typically appended to the end, but if there is a # suffix already, it goes before that.
		/// For example: The main dictionary view and the dictionary preview both use views like "publishRoot"
		/// and "publishStem", and optionally user-defined views like publishRoot#root-612, and both use the
		/// mediator property DictionaryPublicationLayout to record which view the user has selected. However,
		/// the preview specifies layoutSuffix "Preview" to indicate that the layout it actually uses
		/// is (e.g.) publishStemPreview. (This view wraps publishStem with some conditional logic to ensure
		/// that we display things like "Not published" if the entry is excluded from all publications.)
		/// </summary>
		public static string GetLayoutName(XElement xnSpec, IPropertyTable propertyTable)
		{
			string sLayout = null;
			string sProp = XmlUtils.GetOptionalAttributeValue(xnSpec, "layoutProperty", null);
			if (!String.IsNullOrEmpty(sProp))
				sLayout = propertyTable.GetValue<string>(sProp);
			if (String.IsNullOrEmpty(sLayout))
				sLayout = XmlUtils.GetMandatoryAttributeValue(xnSpec, "layout");
			var parts = sLayout.Split('#');
			parts[0] += XmlUtils.GetOptionalAttributeValue(xnSpec, "layoutSuffix", "");
			return string.Join("#", parts);
		}
		protected override void SetupDataContext()
		{
			// The base class uses these specs, so locate them first!
			m_jtSpecs = m_configurationParametersElement;
			base.SetupDataContext();
		}

		protected override void ReadParameters()
		{
			m_configObjectName = StringTable.Table.LocalizeAttributeValue(XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "configureObjectName", null));
			base.ReadParameters();
		}

		internal void ReallyShowRecordNow()
		{
			ShowRecord();
		}

#if RANDYTODO
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

			if (string.IsNullOrEmpty(m_configObjectName))
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
#endif

		/// <summary>
		/// Launch the configure dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnConfigureXmlDocView(object commandObject)
		{
			CheckDisposed();
			string sProp = XmlUtils.GetOptionalAttributeValue(m_configurationParametersElement, "layoutProperty");
			if(String.IsNullOrEmpty(sProp))
				sProp = "DictionaryPublicationLayout";
			using(var dlg = new XmlDocConfigureDlg())
			{
				var mainWindow = PropertyTable.GetValue<IFwMainWnd>("window");
				dlg.SetConfigDlgInfo(m_configurationParametersElement, Cache, FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable),
					mainWindow, PropertyTable, Publisher, sProp);
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					// LT-8767 When this dialog is launched from the Configure Dictionary View dialog
					// m_mediator != null && m_rootSite == null so we need to handle this to prevent a crash.
					if (PropertyTable != null && m_rootSite != null)
					{
						(m_rootSite as XmlDocItemView).ResetTables(GetLayoutName(m_configurationParametersElement, PropertyTable));
					}
				}
				if (dlg.MasterRefreshRequired)
				{
					mainWindow.RefreshAllViews();
				}
				return true; // we handled it
			}
		}

	}
}