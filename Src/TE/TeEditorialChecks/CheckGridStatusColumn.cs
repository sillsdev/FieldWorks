// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CheckGridStatusColumn.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A specialized ImageColumn that deals with zooming the image
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CheckGridStatusColumn: DataGridViewImageColumn
	{
		private float m_ZoomFactor = 1F;
		private bool m_autoSize = true;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckGridStatusColumn"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckGridStatusColumn()
		{
			CellTemplate = new CheckGridStatusCell();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CheckGridStatusColumn"/> class.
		/// </summary>
		/// <param name="valuesAreIcons">true to indicate that the
		/// <see cref="P:System.Windows.Forms.DataGridViewCell.Value"/> property of cells in
		/// this column will be set to values of type <see cref="T:System.Drawing.Icon"/>;
		/// false to indicate that they will be set to values of type
		/// <see cref="T:System.Drawing.Image"/>.</param>
		/// ------------------------------------------------------------------------------------
		public CheckGridStatusColumn(bool valuesAreIcons) : base(valuesAreIcons)
		{
			CellTemplate = new CheckGridStatusCell();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new string HeaderText
		{
			get {return base.HeaderText;}
			set
			{
				base.HeaderText = value;
				if (!m_autoSize)
					return;

				Font fnt = (CellTemplate.Style.Font == null ?
					DefaultCellStyle.Font : CellTemplate.Style.Font);

				if (fnt == null)
				{
					if (DataGridView != null)
						fnt = DataGridView.Font;
					else
						fnt = SystemInformation.MenuFont;
				}

				Width = TextRenderer.MeasureText(value, fnt).Width + 30;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override object Clone()
		{
			CheckGridStatusColumn newColumn = base.Clone() as CheckGridStatusColumn;
			newColumn.ZoomFactor = m_ZoomFactor;
			return newColumn;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the column's size is determined
		/// automatically.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(true)]
		public bool AutoSize
		{
			get { return m_autoSize; }
			set { m_autoSize = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the zoom factor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(1F)]
		public float ZoomFactor
		{
			get { return m_ZoomFactor; }
			set { m_ZoomFactor = value; }
		}
	}
}
