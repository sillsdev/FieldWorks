// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeDivisionLayoutMgr.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Extends DivisionLayoutMgr to handle application related callbacks from views code
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeDivisionLayoutMgr : DivisionLayoutMgr
	{
		private bool m_fIntroDivision;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TeDivisionLayoutMgr"/> class.
		/// </summary>
		/// <param name="layoutConfig">The layout config.</param>
		/// <param name="pubDiv">The pub div.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// <param name="numberOfColumns">The number of columns.</param>
		/// <param name="fIntroDivision">set to <c>true</c> for an intro division, otherwise
		/// <c>false</c>.</param>
		/// <remarks>We explicitly specify the number of columns for this division because we
		/// use the same IPubDivision for multiple division with different number of columns.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public TeDivisionLayoutMgr(IPrintLayoutConfigurer layoutConfig,
			IPubDivision pubDiv, int filterInstance, int numberOfColumns, bool fIntroDivision)
			: base(layoutConfig, pubDiv, filterInstance)
		{
			m_numberMainStreamColumns = numberOfColumns;
			m_fIntroDivision = fIntroDivision;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get editing helper cast as a TeEditingHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeEditingHelper TeEditingHelper
		{
			get
			{
				CheckDisposed();
				return m_publication.EditingHelper as TeEditingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the default WS for all of the view constructors in the division
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int ViewConstructorWS
		{
			get
			{
				CheckDisposed();
				return ((StVc)m_mainVc).DefaultWs;
			}
			set
			{
				CheckDisposed();

				foreach (SubordinateStream stream in m_subStreams)
					((StVc)stream.m_vc).DefaultWs = value;
				((StVc)m_mainVc).DefaultWs = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the back translation WS for all of the view constructors in the division
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BackTranslationWS
		{
			get
			{
				CheckDisposed();
				return ((StVc)m_mainVc).BackTranslationWS;
			}
			set
			{
				CheckDisposed();

				foreach (SubordinateStream stream in m_subStreams)
					((StVc)stream.m_vc).BackTranslationWS = value;
				((StVc)m_mainVc).BackTranslationWS = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hvo of the book this division displays.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HvoBook
		{
			get { return m_configurer.MainObjectId; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the option for where the content of this division begins
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override DivisionStartOption StartAt
		{
			get
			{
				// Non-intro divisions are always continuous
				if (!m_fIntroDivision)
					return DivisionStartOption.Continuous;
				return base.StartAt;
			}
			set { base.StartAt = value; }
		}
	}
}
