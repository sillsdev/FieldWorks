// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AssignFeaturesToPhonemes.cs
// Responsibility: AndyBlack
using System.Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using XCore;
using System.Diagnostics.CodeAnalysis;
using SIL.CoreImpl;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class AssignFeaturesToPhonemes : RecordBrowseView
	{
		private MEImages m_images;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AssignFeaturesToPhonemes"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AssignFeaturesToPhonemes()
		{
			InitializeComponent();
		}
		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize this as an IxCoreColleague
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters"></param>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "bulkEditBar is a reference")]
		public override void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			base.Init(mediator, propertyTable, configurationParameters);
			CheckDisposed();
			var bulkEditBar = m_browseViewer.BulkEditBar;
			// We want a custom name for the tab, the operation label, and the target item
			// Now we use good old List Choice.  bulkEditBar.ListChoiceTab.Text = MEStrings.ksAssignFeaturesToPhonemes;
			bulkEditBar.OperationLabel.Text = MEStrings.ksListChoiceDesc;
			bulkEditBar.TargetFieldLabel.Text = MEStrings.ksTargetFeature;
			bulkEditBar.ChangeToLabel.Text = MEStrings.ksChangeTo;
		}

	#endregion
		protected override BrowseViewer CreateBrowseViewer(XmlNode nodeSpec, int hvoRoot, int fakeFlid, FdoCache cache,
			Mediator mediator, PropertyTable propertyTable,
			ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			var viewer = new FdoUi.BrowseViewerPhonologicalFeatures(nodeSpec,
						 hvoRoot, fakeFlid,
						 cache, mediator, propertyTable, sortItemProvider, sda);
			return viewer;
		}
	}
}
