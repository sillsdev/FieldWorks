// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwTextBoxRow.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A row in a DataGridView that can contain DataGridViewColumns.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwTextBoxRow : DataGridViewRow
	{
		/// <summary>HVO of the writing system for this row.</summary>
		private int m_writingSystemHandle;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwTextBoxRow"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwTextBoxRow()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwTextBoxRow"/> class.
		/// </summary>
		/// <param name="wsHandle">The handle of the writing system.</param>
		/// ------------------------------------------------------------------------------------
		public FwTextBoxRow(int wsHandle)
		{
			WritingSystemHandle = wsHandle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an exact copy of this row.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Object"></see> that represents the cloned <see cref="T:System.Windows.Forms.DataGridViewRow"></see>.
		/// </returns>
		/// <PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence"/><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/></PermissionSet>
		/// ------------------------------------------------------------------------------------
		public override object Clone()
		{
			var newRow = (FwTextBoxRow) base.Clone();
			newRow.WritingSystemHandle = m_writingSystemHandle;
			return newRow;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the writing system hvo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int WritingSystemHandle
		{
			get { return m_writingSystemHandle; }
			set
			{
				m_writingSystemHandle = value;
				foreach (DataGridViewCell cell in Cells)
				{
					cell.Value = cell.DefaultNewRowValue;
				}
			}
		}
	}
}
