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
// File: TeImportUi.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE.ImportTests
{
	#region TeImportNoUi
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Can be used instead of TeImportUi, but doesn't display any UI elements.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeImportNoUi : TeImportUi
	{
		private int m_Maximum;
		private int m_Current;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TeImportNoUi"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public TeImportNoUi() : base(null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does nothing
		/// </summary>
		/// <param name="e">The exception.</param>
		/// ------------------------------------------------------------------------------------
		public override void ErrorMessage(EncodingConverterException e)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does nothing
		/// </summary>
		/// <param name="message">The message.</param>
		/// ------------------------------------------------------------------------------------
		public override void ErrorMessage(string message)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the maximum number of steps or increments corresponding to a progress
		/// bar that's 100% filled.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override int Maximum
		{
			get { return m_Maximum; }
			set { m_Maximum = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override int Position
		{
			set { m_Current = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a message indicating progress status.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public override string StatusMessage
		{
			get { return string.Empty; }
			set { Debug.WriteLine(value); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="cSteps"></param>
		/// ------------------------------------------------------------------------------------
		public override void Step(int cSteps)
		{
			if (cSteps == 0)
				m_Current++;
			else
				m_Current += cSteps;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is displaying a UI.
		/// </summary>
		/// <value>always <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		public override bool IsDisplayingUi
		{
			get { return false; }
		}
	}
	#endregion
}
