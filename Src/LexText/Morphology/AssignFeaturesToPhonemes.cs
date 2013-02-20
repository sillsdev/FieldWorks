// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AssignFeaturesToPhonemes.cs
// Responsibility: AndyBlack
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using XCore;

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
		/// <param name="configurationParameters"></param>
		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			base.Init(mediator, configurationParameters);
			CheckDisposed();
			var bulkEditBar = m_browseViewer.BulkEditBar;
			// We want a custom name for the tab, the operation label, and the target item
			// Now we use good old List Choice.  bulkEditBar.ListChoiceTab.Text = MEStrings.ksAssignFeaturesToPhonemes;
			bulkEditBar.OperationLabel.Text = MEStrings.ksListChoiceDesc;
			bulkEditBar.TargetFieldLabel.Text = MEStrings.ksTargetFeature;
			bulkEditBar.ChangeToLabel.Text = MEStrings.ksChangeTo;
		}

	#endregion
		protected override BrowseViewer CreateBrowseViewer(XmlNode nodeSpec, int hvoRoot, int fakeFlid, FdoCache cache, Mediator mediator,
			ISortItemProvider sortItemProvider, ISilDataAccessManaged sda)
		{
			var viewer = new FdoUi.BrowseViewerPhonologicalFeatures(nodeSpec,
						 hvoRoot, fakeFlid,
						 cache, mediator, sortItemProvider, sda);
			return viewer;
		}
	}
}
