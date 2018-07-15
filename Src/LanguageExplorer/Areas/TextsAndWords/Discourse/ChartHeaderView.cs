// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// This subclass of ListView is used to make the column headers for a Constituent Chart.
	/// It's main function is to handle double-clicks on column boundaries so the chart (which is neither
	/// a ListView nor a BrowseViewer) can resize its columns.
	/// </summary>
	internal class ChartHeaderView : Control
	{
		private ConstituentChart m_chart;
		private bool m_isDraggingNotes;
		private bool m_isResizingColumn;
		private bool m_notesWasOnRight;
		private int m_origHeaderLeft;
		private int m_origMouseLeft;
		private const int kColMinimumWidth = 5;

		/// <summary>
		/// Create one and set the chart it belongs to.
		/// </summary>
		public ChartHeaderView(ConstituentChart chart)
		{
			m_chart = chart;
		}

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.ListView"/> and optionally releases the managed resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
			}
			m_chart = null;

			base.Dispose(disposing);
		}

		public bool NotesOnRight { get; set; } = true;

		/// <summary>
		/// ControlList[] represents the z index, so in order to keep the draggable notes column at the top of the z order,
		/// This custom [] is designed to be used instead representing the x order of column headers
		/// </summary>
		public Control this[int key]
		{
			get
			{
				if (key < 0 || key >= Controls.Count)
				{
					throw new IndexOutOfRangeException();
				}
				return !NotesOnRight ? Controls[key] : Controls[(key + 1) % Controls.Count];
			}
		}

		/// <summary>
		/// New IndexOf to complement custom []
		/// </summary>
		private int IndexOf(Control c)
		{
			for (var i = 0; i < Controls.Count; i++)
			{
				if (this[i] == c)
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>
		/// Updates the positions of all the column headers to consecutive order without gaps or overlaps
		/// </summary>
		public void UpdatePositions()
		{
			if (m_isDraggingNotes)
			{
				return;
			}

			if (Controls.Count < 2)
			{
				return;
			}
			this[0].Left = 1;
			for (var i = 1; i < Controls.Count; i++)
			{
				this[i].Left = this[i - 1].Right;
			}
		}

		/// <summary>
		/// Moves all the other column headers to where they should be when the notes column is dropped
		/// </summary>
		private void UpdatePositionsExceptNotes()
		{
			Controls[1].Left = NotesOnRight ? 1 : Controls[0].Width;

			for (var i = 2; i < Controls.Count; i++)
			{
				Controls[i].Left = Controls[i - 1].Right;
			}
		}

		public event ColumnWidthChangedEventHandler ColumnWidthChanged;

		/// <summary>
		/// Resizes the identified column to the default width
		/// </summary>
		public void AutoResizeColumn(int iColumnChanged)
		{
			var num = Width / (Controls.Count + 1);
			this[iColumnChanged].Width = num;
			UpdatePositions();
			ColumnWidthChanged(this, new ColumnWidthChangedEventArgs(iColumnChanged));
		}

		/// <summary>
		/// Prepares new Control with the proper mouse events and visual elements
		/// </summary>
		protected override void OnControlAdded(ControlEventArgs e)
		{
			//Get the notes value from the property table once the first column has been added
			if (Controls.Count == 1)
			{
				NotesOnRight = m_chart.NotesDataFromPropertyTable;
			}
			var newColumn = e.Control;
			newColumn.Height = 22;
			newColumn.MouseDown += OnColumnMouseDown;
			newColumn.MouseMove += OnColumnMouseMove;
			newColumn.MouseUp += OnColumnMouseUp;
			newColumn.Paint += OnColumnPaint;
			newColumn.DoubleClick += OnColumnDoubleClick;
			if (newColumn is HeaderLabel)
			{
				((HeaderLabel)newColumn).BorderStyle = BorderStyle.None;
			}
		}

		/// <summary>
		/// Handles a column's double click to automatically resize the column to the left of the border clicked
		/// </summary>
		private void OnColumnDoubleClick(object sender, EventArgs e)
		{
			var header = sender as Control;
			if (header.Cursor != Cursors.VSplit)
			{
				return;
			}

			var leftHeader = IndexOf(header);
			if (m_origMouseLeft < 3)
			{
				leftHeader--;
			}
			AutoResizeColumn(leftHeader);
		}

		/// <summary>
		/// Handles a column's mousedown to possibly enter the resizing state and store the initial coordinates
		/// </summary>
		private void OnColumnMouseDown(object sender, MouseEventArgs e)
		{
			var header = (Control)sender;
			if (header.Cursor == Cursors.VSplit)
			{
				m_isResizingColumn = true;
			}

			m_origHeaderLeft = header.Left;
			m_origMouseLeft = e.X;
			m_notesWasOnRight = NotesOnRight;
			header.SuspendLayout();
			SuspendLayout();
		}

		/// <summary>
		/// Handles a column's mousemove to resize if in the resize state or move the notes column
		/// </summary>
		private void OnColumnMouseMove(object sender, MouseEventArgs e)
		{
			var header = (Control)sender;
			if ((e.X < 3 && header != this[0]) || (e.X > header.Width - 3))
			{
				header.Cursor = Cursors.VSplit;
			}
			else
			{
				header.Cursor = DefaultCursor;
			}

			if (e.Button != MouseButtons.Left)
			{
				return;
			}
			if (m_isResizingColumn)
			{
				ResizeColumn(header, e);
			}
			else
			{
				MoveColumn(header, e);
			}
			Parent.Update();
		}

		/// <summary>
		/// Controls MouseMove event for column header in case we are in the resize state
		/// </summary>
		private void ResizeColumn(Control header, MouseEventArgs e)
		{
			Control prevHeader;
			int X;
			if (m_origMouseLeft < 3)
			{
				prevHeader = this[IndexOf(header) - 1];
				X = e.X + header.Left - prevHeader.Left;
			}
			else
			{
				prevHeader = header;
				X = e.X;
			}

			prevHeader.Width = X;
			if (prevHeader.Width < kColMinimumWidth)
			{
				prevHeader.Width = kColMinimumWidth;
			}
			UpdatePositions();
		}

		/// <summary>
		/// Controls MouseMove event for column header in case we are in the move notes column state
		/// </summary>
		private void MoveColumn(Control header, MouseEventArgs e)
		{
			if (header.Text != LanguageExplorerResources.ksNotesColumnHeader)
			{
				return;
			}
			if (header.Left < m_origHeaderLeft - 20)
			{
				NotesOnRight = false;
			}
			else if (header.Left > m_origHeaderLeft + 20 || m_notesWasOnRight)
			{
				NotesOnRight = true;
			}
			else
			{
				NotesOnRight = false;
			}
			UpdatePositionsExceptNotes();
			m_isDraggingNotes = true;
			header.Left += (e.X - m_origMouseLeft);
		}

		/// <summary>
		/// Handles a column's mouseup to remove any state data and finalize changes made in a mousemove
		/// </summary>
		private void OnColumnMouseUp(object sender, MouseEventArgs e)
		{
			var header = sender as Control;
			if (m_isResizingColumn)
			{
				UpdatePositions();
				ColumnWidthChanged?.Invoke(this, new ColumnWidthChangedEventArgs(IndexOf(header)));
			}
			else if (m_isDraggingNotes)
			{
				header.Left = m_origHeaderLeft;
			}

			m_isDraggingNotes = false;
			m_isResizingColumn = false;


			UpdatePositions();
			if (m_notesWasOnRight != NotesOnRight)
			{
				m_chart.NotesDataFromPropertyTable = NotesOnRight;
				ColumnWidthChanged?.Invoke(this, new ColumnWidthChangedEventArgs(0));
				m_chart.RefreshRoot();
			}

			header.ResumeLayout(false);
			ResumeLayout(false);
		}

		/// <summary>
		/// Draws the border around a column header
		/// </summary>
		private void OnColumnPaint(object sender, PaintEventArgs e)
		{
			var header = sender as Control;
			var topLeft = new Point(0, 0);
			var bottomRight = new Size(header.Width - 1, header.Height - 1);
			e.Graphics.DrawRectangle(new Pen(Color.Black), new Rectangle(topLeft, bottomRight));
		}

		/// <summary>
		/// Handles resizing of the last column if the mouse hangs over its border slightly
		/// </summary>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			Cursor = e.X < this[Controls.Count - 1].Right + 3 ? Cursors.VSplit : Cursors.Default;

			if (!m_isResizingColumn)
			{
				return;
			}

			var header = this[Controls.Count - 1];
			header.Width = e.X - header.Left;
			if (header.Width < kColMinimumWidth)
			{
				header.Width = kColMinimumWidth;
			}
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (Cursor == Cursors.VSplit)
			{
				m_isResizingColumn = true;
				SuspendLayout();
				this[Controls.Count - 1].SuspendLayout();
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			m_isResizingColumn = false;
			ResumeLayout(false);
			this[Controls.Count - 1].ResumeLayout(false);
			ColumnWidthChanged(this, new ColumnWidthChangedEventArgs(Controls.Count - 1));
		}
	}
}