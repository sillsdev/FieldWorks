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
using SIL.CoreImpl;
using SIL.FieldWorks.FdoUi;

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

		#region Overrides of RecordBrowseView

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public override void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			base.InitializeFlexComponent(propertyTable, publisher, subscriber);

			var bulkEditBar = m_browseViewer.BulkEditBar;
			// We want a custom name for the tab, the operation label, and the target item
			// Now we use good old List Choice.  bulkEditBar.ListChoiceTab.Text = MEStrings.ksAssignFeaturesToPhonemes;
			bulkEditBar.OperationLabel.Text = MEStrings.ksListChoiceDesc;
			bulkEditBar.TargetFieldLabel.Text = MEStrings.ksTargetFeature;
			bulkEditBar.ChangeToLabel.Text = MEStrings.ksChangeTo;
		}

		#endregion

		protected override BrowseViewer CreateBrowseViewer(XmlNode nodeSpec, int hvoRoot, int fakeFlid, FdoCache cache,
			ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			var viewer = new BrowseViewerPhonologicalFeatures(nodeSpec,
						 hvoRoot, fakeFlid,
						 cache, sortItemProvider, sda);
			viewer.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			return viewer;
		}
	}
}
