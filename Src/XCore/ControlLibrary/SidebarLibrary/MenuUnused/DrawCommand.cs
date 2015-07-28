// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
#if USE_THIS
namespace SidebarLibrary.Menus
{
	internal class DrawCommand
	{
		// Instance fields
		protected int _row;
		protected int _col;
		protected char _mnemonic;
		protected bool _enabled;
		protected bool _subMenu;
		protected bool _expansion;
		protected bool _separator;
		protected bool _vertSeparator;
		protected bool _chevron;
		protected Rectangle _drawRect;
		protected MenuCommand _command;

		public DrawCommand(Rectangle drawRect)
		{
			_row = -1;
			_col = -1;
			_mnemonic = '0';
			_enabled = true;
			_subMenu = false;
			_expansion = false;
			_separator = false;
			_vertSeparator = false;
			_chevron = true;
			_drawRect = drawRect;
			_command = null;
		}

		public DrawCommand(Rectangle drawRect, bool expansion)
		{
			_row = -1;
			_col = -1;
			_mnemonic = '0';
			_enabled = true;
			_subMenu = false;
			_expansion = expansion;
			_separator = !expansion;
			_vertSeparator = !expansion;
			_chevron = false;
			_drawRect = drawRect;
			_command = null;
		}

		public DrawCommand(MenuCommand command, Rectangle drawRect)
		{
			InternalConstruct(command, drawRect, -1, -1);
		}

		public DrawCommand(MenuCommand command, Rectangle drawRect, int row, int col)
		{
			InternalConstruct(command, drawRect, row, col);
		}

		public void InternalConstruct(MenuCommand command, Rectangle drawRect, int row, int col)
		{
			_row = row;
			_col = col;
			_enabled = command.Enabled;
			_expansion = false;
			_vertSeparator = false;
			_drawRect = drawRect;
			_command = command;

			_chevron = false;

			// Is this MenuCommand a separator?
			_separator = (_command.Text == "-");

			// Does this MenuCommand contain a submenu?
			_subMenu = (_command.MenuCommands.Count > 0);

			// Find position of first mnemonic character
			int position = command.Text.IndexOf('&');

			// Did we find a mnemonic indicator?
			if (position != -1)
			{
				// Must be a character after the indicator
				if (position < (command.Text.Length - 1))
				{
					// Remember the character
					_mnemonic = char.ToUpper(command.Text[position + 1]);
				}
			}
		}

		public Rectangle DrawRect
		{
			get { return _drawRect; }
			set { _drawRect = value; }
		}

		public MenuCommand MenuCommand
		{
			get { return _command; }
		}

		public bool Separator
		{
			get { return _separator; }
		}

		public bool VerticalSeparator
		{
			get { return _vertSeparator; }
		}

		public bool Expansion
		{
			get { return _expansion; }
		}

		public bool SubMenu
		{
			get { return _subMenu; }
		}

		public char Mnemonic
		{
			get { return _mnemonic; }
		}

		public bool Enabled
		{
			get { return _enabled; }
		}

		public int Row
		{
			get { return _row; }
		}

		public int Col
		{
			get { return _col; }
		}

		public bool Chevron
		{
			get { return _chevron; }
		}
	}
}
#endif
