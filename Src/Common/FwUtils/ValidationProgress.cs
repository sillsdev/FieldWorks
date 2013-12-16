// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ValidationProgress.cs
// Responsibility: Steve McConnel
//
// <remarks>
// </remarks>

using System;
using Palaso.Lift.Validation;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Wrap an object with an IAdvInd4 interface with an interface usable by the Palaso.Lift
	/// library.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ValidationProgress : IValidationProgress
	{
		private readonly IProgress m_progress;

		/// <summary>
		/// Constructor.
		/// </summary>
		public ValidationProgress(IProgress progress)
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
					return Math.Abs(m_progress.Maximum - m_progress.Minimum);
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
					m_progress.Minimum = 0;
					m_progress.Maximum = value;
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
