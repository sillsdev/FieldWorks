// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ValidationProgress.cs
// Responsibility: Steve McConnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wrap an object with an IAdvInd4 interface with an interface usable by the LiftIO
	/// library.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ValidationProgress : LiftIO.Validation.IValidationProgress
	{
		private IAdvInd4 m_progress;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ValidationProgress(IAdvInd4 progress)
		{
			m_progress = progress;
		}

		#region IValidationProgress Members

		/// <summary>
		/// Get/set the maximum value of the progress bar, initializing the current position to
		/// 0 when setting the maximum.
		/// </summary>
		public int MaxRange
		{
			get
			{
				if (m_progress != null)
				{
					int nMin, nMax;
					m_progress.GetRange(out nMin, out nMax);
					return Math.Abs(nMax - nMin);
				}
				else
				{
					return 0;
				}
			}
			set
			{
				if (m_progress != null && value > 0)
				{
					m_progress.SetRange(0, value);
					m_progress.Position = 0;
				}
			}
		}

		/// <summary>
		/// Get/set the status string.
		/// </summary>
		public string Status
		{
			get
			{
				if (m_progress != null)
					return m_progress.Message;
				else
					return String.Empty;
			}
			set
			{
				if (m_progress != null)
					m_progress.Message = value;
			}
		}

		/// <summary>
		/// Step the progress bar by the given amount.
		/// </summary>
		public void Step(int n)
		{
			if (m_progress != null)
				m_progress.Step(n);
		}

		#endregion
	}
}
