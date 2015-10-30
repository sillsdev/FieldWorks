// Original author or copyright holder unknown.

#if USE_THIS
using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Drawing.Text;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Imaging;
using SidebarLibrary.Win32;
using SidebarLibrary.Menus;
using SidebarLibrary.General;
using SidebarLibrary.Collections;


namespace SidebarLibrary.Menus
{
	[ToolboxBitmap(typeof(MenuControl))]
	[DefaultProperty("MenuCommands")]
	[DefaultEvent("PopupStart")]
	[Designer(typeof(SidebarLibrary.Menus.MenuControlDesigner))]
	public class MenuControl : ContainerControl, IMessageFilter
	{
		// Class constants
		protected const int _breadthGap = 5;
		protected const int _lengthGap = 3;
		protected const int _boxExpandUpper = 1;
		protected const int _boxExpandSides = 2;
		protected const int _shadowGap = 3;
		protected const int _shadowYOffset = 3;
		protected const int _separatorWidth = 15;
		protected const int _subMenuBorderAdjust = 2;
		protected const int _chevronIndex = 0;
		protected const int _chevronLength = 10;
		protected const int _chevronBreadth = 10;

		// Class constant is marked as 'readonly' to allow non constant initialization
		protected readonly int WM_OPERATEMENU = (int)Win32.Msg.WM_USER + 1;

		// Class fields
		protected static ImageList _menuImages = null;
		protected static bool _supportsLayered = false;

		// Declare the popup change event signature
		public delegate void PopupHandler(MenuCommand item);

		// Instance fields
		protected int _rowWidth;
		protected int _rowHeight;
		protected bool _selected;
		protected int _trackItem;
		protected bool _multiLine;
		protected bool _mouseOver;
		protected IntPtr _oldFocus;
		protected bool _manualFocus;
		protected bool _drawUpwards;
		protected VisualStyle _style;
		protected bool _plainAsBlock;
		protected Direction _direction;
		protected bool _ignoreEscapeUp;
		protected PopupMenu _popupMenu;
		protected bool _dismissTransfer;
		protected bool _ignoreMouseMove;
		protected ArrayList _drawCommands;
		protected MenuCommand _chevronStartCommand;
		protected MenuCommandCollection _menuCommands;

		// Instance fields - events
		public event PopupHandler PopupStart;
		public event PopupHandler PopupEnd;

		static MenuControl()
		{
			// Create a strip of images by loading an embedded bitmap resource
			_menuImages = ResourceUtil.LoadImageListResource(Type.GetType("SidebarLibrary.Menus.MenuControl"),
														 "SidebarLibrary.Resources.ImagesMenu",
														 "MenuControlImages",
														 new Size(_chevronLength, _chevronBreadth),
														 true,
														 new Point(0,0));

			// We need to know if the OS supports layered windows
			_supportsLayered = (OSFeature.Feature.GetVersionPresent(OSFeature.LayeredWindows) != null);
		}

		public MenuControl()
		{
			// Set default values
			this.Dock = DockStyle.Top;
			_trackItem = -1;
			_selected = false;
			_multiLine = false;
			_popupMenu = null;
			_mouseOver = false;
			_manualFocus = false;
			_drawUpwards = false;
			_plainAsBlock = false;
			_oldFocus = IntPtr.Zero;
			_ignoreEscapeUp = false;
			_ignoreMouseMove = false;
			_dismissTransfer = false;
			_style = VisualStyle.IDE;
			_chevronStartCommand = null;
			_direction = Direction.Horizontal;
			_menuCommands = new MenuCommandCollection();

			// Prevent flicker with double buffering and all painting inside WM_PAINT
			SetStyle(ControlStyles.DoubleBuffer, true);
			SetStyle(ControlStyles.AllPaintingInWmPaint, true);

			// Should not be allowed to select this control
			SetStyle(ControlStyles.Selectable, false);

			// Hookup to collection events
			_menuCommands.Cleared += new CollectionWithEvents.CollectionClear(OnCollectionCleared);
			_menuCommands.Inserted += new CollectionWithEvents.CollectionChange(OnCollectionInserted);
			_menuCommands.Removed += new CollectionWithEvents.CollectionChange(OnCollectionRemoved);

			// Set the default menu color as background
			this.BackColor = SystemColors.Control;

			// Do not allow tab key to select this control
			this.TabStop = false;

			// Default the Font we use
			this.Font = SystemInformation.MenuFont;

			// Calculate the initial height/width of the control
			_rowWidth = _rowHeight = this.Font.Height + _breadthGap * 2 + 1;

			// Default to one line of items
			this.Height = _rowHeight;

			// Add ourself to the application filtering list
			Application.AddMessageFilter(this);
		}

		[Category("Behaviour")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public MenuCommandCollection MenuCommands
		{
			get { return _menuCommands; }

			set
			{
				_menuCommands.Clear();
				_menuCommands = value;

				Recalculate();
				Invalidate();
			}
		}

		[Category("Appearance")]
		public VisualStyle Style
		{
			get { return _style; }

			set
			{
				if (_style != value)
				{
					_style = value;

					Recalculate();
					Invalidate();
				}
			}
		}

		[Category("Appearance")]
		public override Font Font
		{
			get { return base.Font; }

			set
			{
				base.Font = value;

				// Resize to take into accout the new height
				_rowHeight = this.Font.Height + _lengthGap * 2;

				Recalculate();
				Invalidate();
			}
		}

		[Category("Appearance")]
		[DefaultValue(false)]
		public bool PlainAsBlock
		{
			get { return _plainAsBlock; }

			set
			{
				if (_plainAsBlock != value)
				{
					_plainAsBlock = value;

					Recalculate();
					Invalidate();
				}
			}
		}

		[Category("Appearance")]
		[DefaultValue(false)]
		public bool MultiLine
		{
			get { return _multiLine; }

			set
			{
				if (_multiLine != value)
				{
					_multiLine = value;

					Recalculate();
					Invalidate();
				}
			}
		}

		[Category("Appearance")]
		public Direction Direction
		{
			get { return _direction; }

			set
			{
				if (_direction != value)
				{
					_direction = value;

					Recalculate();
					Invalidate();
				}
			}
		}

		public override DockStyle Dock
		{
			get { return base.Dock; }

			set
			{
				base.Dock = value;

				switch(value)
				{
				case DockStyle.None:
					_direction = Direction.Horizontal;
					break;
				case DockStyle.Top:
				case DockStyle.Bottom:
					this.Height = 0;
					_direction = Direction.Horizontal;
					break;
				case DockStyle.Left:
				case DockStyle.Right:
					this.Width = 0;
					_direction = Direction.Vertical;
					break;
				}

				Recalculate();
				Invalidate();
			}
		}

		protected void OnPopupStart(MenuCommand mc)
		{
			if (PopupStart != null)
				PopupStart(mc);
		}

		protected void OnPopupEnd(MenuCommand mc)
		{
			if (PopupEnd != null)
				PopupEnd(mc);
		}

		protected void OnCollectionCleared()
		{
			// Reset state ready for a recalculation
			_selected = false;
			_trackItem = -1;

			Recalculate();
			Invalidate();
		}

		protected void OnCollectionInserted(int index, object value)
		{
			MenuCommand mc = value as MenuCommand;

			// We need notification whenever the properties of this command change
			mc.PropertyChanged += new MenuCommand.PropChangeHandler(OnCommandChanged);

			// Reset state ready for a recalculation
			_selected = false;
			_trackItem = -1;

			Recalculate();
			Invalidate();
		}

		protected void OnCollectionRemoved(int index, object value)
		{
			// Reset state ready for a recalculation
			_selected = false;
			_trackItem = -1;

			Recalculate();
			Invalidate();
		}

		protected void OnCommandChanged(MenuCommand item, MenuCommand.Property prop)
		{
			Recalculate();
			Invalidate();
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			Point pos = new Point(e.X, e.Y);

			for(int i=0; i<_drawCommands.Count; i++)
			{
				DrawCommand dc = _drawCommands[i] as DrawCommand;

				// Find the DrawCommand this is over
				if (dc.DrawRect.Contains(pos))
				{
					// Is an item already selected?
					if (_selected)
					{
						// Is it this item that is already selected?
						if (_trackItem == i)
						{
							// Is a popupMenu showing
							if (_popupMenu != null)
							{
								// Dismiss the submenu
								_popupMenu.Dismiss();

								// No reference needed
								_popupMenu = null;
							}
						}
					}
					else
					{
						// Select the tracked item
						_selected = true;
						_drawUpwards = false;

						GrabTheFocus();

						// Is there a change in tracking?
						if (_trackItem != i)
						{
							// Modify the display of the two items
							_trackItem = SwitchTrackingItem(_trackItem, i);
						}
						else
						{
							// Update display to show as selected
							DrawCommand(_trackItem, true);
						}

						// Is there a submenu to show?
						if (dc.Chevron || (dc.MenuCommand.MenuCommands.Count > 0))
							WindowsAPI.PostMessage(this.Handle, WM_OPERATEMENU, 1, 0);
					}

					break;
				}
			}

			base.OnMouseDown(e);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			// Is an item currently being tracked?
			if (_trackItem != -1)
			{
				// Is it also selected?
				if (_selected == true)
				{
					// Is it also showing a submenu
					if (_popupMenu == null)
					{
						// Deselect the item
						_selected = false;
						_drawUpwards = false;

						DrawCommand(_trackItem, true);

						ReturnTheFocus();
					}
				}
			}

			base.OnMouseUp(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			// Sometimes we need to ignore this message
			if (_ignoreMouseMove)
				_ignoreMouseMove = false;
			else
			{
				// Is the first time we have noticed a mouse movement over our window
				if (!_mouseOver)
				{
					// Crea the structure needed for WindowsAPI call
					Win32.TRACKMOUSEEVENTS tme = new Win32.TRACKMOUSEEVENTS();

					// Fill in the structure
					tme.cbSize = 16;
					tme.dwFlags = (uint)Win32.TrackerEventFlags.TME_LEAVE;
					tme.hWnd = this.Handle;
					tme.dwHoverTime = 0;

					// Request that a message gets sent when mouse leaves this window
					WindowsAPI.TrackMouseEvent(ref tme);

					// Yes, we know the mouse is over window
					_mouseOver = true;
				}

				Form parentForm = this.FindForm();

				// Only hot track if this Form is active
				if ((parentForm != null) && parentForm.ContainsFocus)
				{
					Point pos = new Point(e.X, e.Y);

					int i = 0;

					for(i=0; i<_drawCommands.Count; i++)
					{
						DrawCommand dc = _drawCommands[i] as DrawCommand;

						// Find the DrawCommand this is over
						if (dc.DrawRect.Contains(pos))
						{
							// Is there a change in selected item?
							if (_trackItem != i)
							{
								// We we currently selecting an item
								if (_selected)
								{
									if (_popupMenu != null)
									{
										// Note that we are dismissing the submenu but not removing
										// the selection of items, this flag is tested by the routine
										// that will return because the submenu has been finished
										_dismissTransfer = true;

										// Dismiss the submenu
										_popupMenu.Dismiss();

										// Default to downward drawing
										_drawUpwards = false;
									}

									// Modify the display of the two items
									_trackItem = SwitchTrackingItem(_trackItem, i);

									// Does the newly selected item have a submenu?
									if (dc.Chevron || (dc.MenuCommand.MenuCommands.Count > 0))
										WindowsAPI.PostMessage(this.Handle, WM_OPERATEMENU, 1, 0);
								}
								else
								{
									// Modify the display of the two items
									_trackItem = SwitchTrackingItem(_trackItem, i);
								}
							}

							break;
						}
					}

					// If not in selected mode
					if (!_selected)
					{
						// None of the commands match?
						if (i == _drawCommands.Count)
						{
							// Modify the display of the two items
							_trackItem = SwitchTrackingItem(_trackItem, -1);
						}
					}
				}
			}

			base.OnMouseMove(e);
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			_mouseOver = false;

			// If we manually grabbed focus then do not switch
			// selection when the mouse leaves the control area
			if (!_manualFocus)
			{
				if (_trackItem != -1)
				{
					// If an item is selected then do not change tracking item when the
					// mouse leaves the control area, as a popup menu might be showing and
					// so keep the tracking and selection indication visible
					if (_selected == false)
						_trackItem = SwitchTrackingItem(_trackItem, -1);
				}
			}

			base.OnMouseLeave(e);
		}

		protected override void OnResize(EventArgs e)
		{
			Recalculate();

			// Any resize of control should redraw all of it otherwise when you
			// stretch to the right it will not paint correctly as we have a one
			// pixel gao between text and min button which is not honoured otherwise
			this.Invalidate();

			base.OnResize(e);
		}

		internal void DrawSelectionUpwards()
		{
			// Double check the state is correct for this method to be called
			if ((_trackItem != -1) && (_selected))
			{
				// This flag is tested in the DrawCommand method
				_drawUpwards = true;

				// Force immediate redraw of the item
				DrawCommand(_trackItem, true);
			}
		}

		protected void Recalculate()
		{
			int length;

			if (_direction == Direction.Horizontal)
				length = this.Width;
			else
				length = this.Height;

			// Is there space for any commands?
			if (length > 0)
			{
				// Count the number of rows needed
				int rows = 0;

				// Number of items on this row
				int columns = 0;

				// Create a collection of drawing objects
				_drawCommands = new ArrayList();

				// Minimum length is a gap on either side of the text
				int cellMinLength = _lengthGap * 2;

				// Each cell is as broad as the whole control
				// int cellBreadth = this.Height;

				// Accumulate starting position of each cell
				int lengthStart = 0;

				// If the chevron is already displayed then reduce length by its length
				if (_chevronStartCommand != null)
					length -= cellMinLength + _chevronLength;

				// Assume chevron is not needed by default
				_chevronStartCommand = null;

				using(Graphics g = this.CreateGraphics())
				{
					// Count the item we are processing
					int index = 0;

					foreach(MenuCommand command in _menuCommands)
					{
						// Give the command a chance to update its state
						command.OnUpdate(EventArgs.Empty);

						// Ignore items that are marked as hidden
						if (!command.Visible)
							continue;

						int cellLength = 0;

						// Is this a separator?
						if (command.Text == "-")
							cellLength = _separatorWidth;
						else
						{
							// Calculate the text width of the cell
							SizeF dimension = g.MeasureString(command.Text, this.Font);

							// Always add 1 to ensure that rounding is up and not down
							cellLength = cellMinLength + (int)dimension.Width + 1;
						}

						Rectangle cellRect;

						// Create a new position rectangle
						if (_direction == Direction.Horizontal)
							cellRect = new Rectangle(lengthStart, _rowHeight * rows, cellLength, _rowHeight);
						else
							cellRect = new Rectangle(_rowWidth * rows, lengthStart, _rowWidth, cellLength);

						lengthStart += cellLength;
						columns++;

						// If this item is overlapping the control edge and it is not the first
						// item on the line then we should wrap around to the next row.
						if ((lengthStart > length) && (columns > 1))
						{
							if (_multiLine)
							{
								// Move to next row
								rows++;

								// Reset number of items on this column
								columns = 1;

								// Reset starting position of next item
								lengthStart = cellLength;

								// Reset position of this item
								if (_direction == Direction.Horizontal)
								{
									cellRect.X = 0;
									cellRect.Y += _rowHeight;
								}
								else
								{
									cellRect.X += _rowWidth;
									cellRect.Y = 0;
								}
							}
							else
							{
								// Is a tracked item being make invisible
								if (index <= _trackItem)
								{
									// Need to remove tracking of this item
									_trackItem = -1;
								}

								// Remember which item is first for the chevron submenu
								_chevronStartCommand = command;

								if (_direction == Direction.Horizontal)
								{
									cellRect.Y = 0;
									cellRect.Width = cellMinLength + _chevronLength;
									cellRect.X = this.Width - cellRect.Width;
									cellRect.Height = _rowHeight;
								}
								else
								{
									cellRect.X = 0;
									cellRect.Height = cellMinLength + _chevronLength;
									cellRect.Y = this.Height - (cellMinLength + _chevronLength);
									cellRect.Width = _rowWidth;
								}

								// Create a draw command for this chevron
								_drawCommands.Add(new DrawCommand(cellRect));

								// Exit, do not add the current item or any afterwards
								break;
							}
						}

						// Create a drawing object
						_drawCommands.Add(new DrawCommand(command, cellRect));

						index++;
					}
				}

				if (_direction == Direction.Horizontal)
				{
					int controlHeight = (rows + 1) * _rowHeight;

					// Ensure the control is the correct height
					if (this.Height != controlHeight)
						this.Height = controlHeight;
				}
				else
				{
					int controlWidth = (rows + 1) * _rowWidth;

					// Ensure the control is the correct width
					if (this.Width != controlWidth)
						this.Width = controlWidth;
				}
			}
		}

		protected void DrawCommand(int drawItem, bool tracked)
		{
			// Create a graphics object for drawing
			using(Graphics g = this.CreateGraphics())
				DrawSingleCommand(g, _drawCommands[drawItem] as DrawCommand, tracked);
		}

		internal void DrawSingleCommand(Graphics g, DrawCommand dc, bool tracked)
		{
			Rectangle drawRect = dc.DrawRect;
			MenuCommand mc = dc.MenuCommand;

			// Copy the rectangle used for drawing cell
			Rectangle shadowRect = drawRect;

			// Expand to right and bottom to cover the area used to draw shadows
			shadowRect.Width += _shadowGap;
			shadowRect.Height += _shadowGap;

			// Draw background color over cell and shadow area to the right
			using (SolidBrush back = new SolidBrush(SystemColors.Control))
				g.FillRectangle(back, shadowRect);

			if (!dc.Separator)
			{
				Rectangle textRect;

				// Text rectangle size depends on type of draw command we are drawing
				if (dc.Chevron)
				{
					// Create chevron drawing rectangle
					textRect = new Rectangle(drawRect.Left + _lengthGap, drawRect.Top + _boxExpandUpper,
											 drawRect.Width - _lengthGap * 2, drawRect.Height - (_boxExpandUpper * 2));
				}
				else
				{
					// Create text drawing rectangle
					textRect = new Rectangle(drawRect.Left + _lengthGap, drawRect.Top + _lengthGap,
											 drawRect.Width - _lengthGap * 2, drawRect.Height - _lengthGap * 2);
				}

				if (dc.Enabled)
				{
					// Draw selection
					if (tracked)
					{
						Rectangle boxRect;

						// Create the rectangle for box around the text
						if (_direction == Direction.Horizontal)
						{
							boxRect = new Rectangle(textRect.Left - _boxExpandSides,
													textRect.Top - _boxExpandUpper,
													textRect.Width + _boxExpandSides * 2,
													textRect.Height + _boxExpandUpper);
						}
						else
						{
							if (!dc.Chevron)
							{
								boxRect = new Rectangle(textRect.Left,
														textRect.Top - _boxExpandSides,
														textRect.Width - _boxExpandSides,
														textRect.Height + _boxExpandSides * 2);
							}
							else
								boxRect = textRect;
						}

						switch (_style)
						{
							case VisualStyle.IDE:
								if (_selected)
								{
									// Fill the entire inside
									g.FillRectangle(SystemBrushes.ControlLight, boxRect);

								int rightLeft = boxRect.Right + 1;
									int rightBottom = boxRect.Bottom;

								if (_drawUpwards && (_direction == Direction.Horizontal))
									{
										// Draw the box around the selection area
										using (Pen dark = new Pen(SystemColors.ControlDark))
											g.DrawRectangle(dark, boxRect.Left, boxRect.Top - _shadowGap,
															  boxRect.Width, boxRect.Height + _shadowGap);

										// Remove the top line of the selection area
										using (Pen dark = new Pen(SystemColors.ControlLight))
											g.DrawLine(dark, boxRect.Left + 1, boxRect.Top, boxRect.Right - 2, boxRect.Top);

										int rightTop = boxRect.Top;
										//int leftLeft = boxRect.Left + _shadowGap;

										SolidBrush shadowBrush = null;
										try
										{
											// Decide if we need to use an alpha brush
											if (_supportsLayered && (_style == VisualStyle.IDE))
												shadowBrush = new SolidBrush(Color.FromArgb(64, 0, 0, 0));
											else
												shadowBrush = new SolidBrush(SystemColors.ControlDark);

											g.FillRectangle(shadowBrush, new Rectangle(rightLeft, rightTop + _shadowGap - 1, _shadowGap, rightBottom - rightTop - _shadowGap * 2));
										}
										finally
										{
											shadowBrush.Dispose();
										}
								}
								else
								{
									// Draw the box around the selection area
									using(Pen dark = new Pen(SystemColors.ControlDark))
										g.DrawRectangle(dark, boxRect);

									if (_direction == Direction.Horizontal)
									{
										// Remove the bottom line of the selection area
										using(Pen dark = new Pen(SystemColors.ControlLight))
											g.DrawLine(dark, boxRect.Left, boxRect.Bottom, boxRect.Right, boxRect.Bottom);

										int rightTop = boxRect.Top + _shadowYOffset;

										SolidBrush shadowBrush = null;
										try
										{
											// Decide if we need to use an alpha brush
											if (_supportsLayered && (_style == VisualStyle.IDE))
												shadowBrush = new SolidBrush(Color.FromArgb(64, 0, 0, 0));
											else
												shadowBrush = new SolidBrush(SystemColors.ControlDark);

											g.FillRectangle(shadowBrush, new Rectangle(rightLeft, rightTop, _shadowGap, rightBottom - rightTop));
										}
										finally
										{
											shadowBrush.Dispose();
										}
									}
									else
									{
										// Remove the right line of the selection area
										using(Pen dark = new Pen(SystemColors.ControlLight))
											g.DrawLine(dark, boxRect.Right, boxRect.Top, boxRect.Right, boxRect.Bottom);

										int leftLeft = boxRect.Left + _shadowYOffset;

										SolidBrush shadowBrush = null;
										try
										{
											// Decide if we need to use an alpha brush
											if (_supportsLayered && (_style == VisualStyle.IDE))
												shadowBrush = new SolidBrush(Color.FromArgb(64, 0, 0, 0));
											else
												shadowBrush = new SolidBrush(SystemColors.ControlDark);

											g.FillRectangle(shadowBrush, new Rectangle(leftLeft, rightBottom + 1, rightBottom - leftLeft - _shadowGap, _shadowGap));
										}
										finally
										{
											shadowBrush.Dispose();
										}
									}
								}
							}
							else
							{
								using (Pen selectPen = new Pen(ColorUtil.VSNetBorderColor))
								{
									// Draw the selection area in white so can alpha draw over the top
									using (SolidBrush whiteBrush = new SolidBrush(Color.White))
										g.FillRectangle(whiteBrush, boxRect);

									using (SolidBrush selectBrush = new SolidBrush(Color.FromArgb(70, ColorUtil.VSNetBorderColor)))
									{
										// Draw the selection area
										g.FillRectangle(selectBrush, boxRect);

										// Draw a border around the selection area
										g.DrawRectangle(selectPen, boxRect);
									}
								}
							}
							break;
						case VisualStyle.Plain:
							if (_plainAsBlock)
							{
								using (SolidBrush selectBrush = new SolidBrush(ColorUtil.VSNetBorderColor))
									g.FillRectangle(selectBrush, drawRect);
							}
							else
							{
								using(Pen lighlight = new Pen(SystemColors.ControlLightLight),
										  dark = new Pen(SystemColors.ControlDark))
								{
									if (_selected)
									{
										g.DrawLine(dark, boxRect.Left, boxRect.Bottom, boxRect.Left, boxRect.Top);
										g.DrawLine(dark, boxRect.Left, boxRect.Top, boxRect.Right, boxRect.Top);
										g.DrawLine(lighlight, boxRect.Right, boxRect.Top, boxRect.Right, boxRect.Bottom);
										g.DrawLine(lighlight, boxRect.Right, boxRect.Bottom, boxRect.Left, boxRect.Bottom);
									}
									else
									{
										g.DrawLine(lighlight, boxRect.Left, boxRect.Bottom, boxRect.Left, boxRect.Top);
										g.DrawLine(lighlight, boxRect.Left, boxRect.Top, boxRect.Right, boxRect.Top);
										g.DrawLine(dark, boxRect.Right, boxRect.Top, boxRect.Right, boxRect.Bottom);
										g.DrawLine(dark, boxRect.Right, boxRect.Bottom, boxRect.Left, boxRect.Bottom);
									}
								}
							}
							break;
						}
					}
				}

				if (dc.Chevron)
				{
					// Draw the chevron image in the centre of the text area
					int yPos = drawRect.Top;
					int xPos = drawRect.X + ((drawRect.Width - _chevronLength) / 2);

					// When selected...
					if (_selected)
					{
						// ...offset down and to the right
						xPos += 1;
						yPos += 1;
					}

					g.DrawImage(_menuImages.Images[_chevronIndex], xPos, yPos);
				}
				else
				{
					// Left align the text drawing on a single line centered vertically
					// and process the & character to be shown as an underscore on next character
					using (StringFormat format = new StringFormat())
					{
						format.FormatFlags = StringFormatFlags.NoClip | StringFormatFlags.NoWrap;
						format.Alignment = StringAlignment.Center;
						format.LineAlignment = StringAlignment.Center;
						format.HotkeyPrefix = HotkeyPrefix.Show;

						if (_direction == Direction.Vertical)
							format.FormatFlags |= StringFormatFlags.DirectionVertical;


						if (dc.Enabled)
						{
							if (tracked && (_style == VisualStyle.Plain) && _plainAsBlock)
							{
								// Is the item selected as well as tracked?
								if (_selected)
								{
									// Offset to show it is selected
									textRect.X += 2;
									textRect.Y += 2;
								}

								using (SolidBrush textBrush = new SolidBrush(SystemColors.HighlightText))
									g.DrawString(mc.Text, this.Font, textBrush, textRect, format);
							}
							else
								using (SolidBrush textBrush = new SolidBrush(SystemColors.MenuText))
									g.DrawString(mc.Text, this.Font, textBrush, textRect, format);
						}
						else
						{
							// Helper values used when drawing grayed text in plain style
							Rectangle rectDownRight = textRect;
							rectDownRight.Offset(1,1);

							// Draw then text offset down and right
							using (SolidBrush whiteBrush = new SolidBrush(SystemColors.HighlightText))
								g.DrawString(mc.Text, this.Font, whiteBrush, rectDownRight, format);

							// Draw then text offset up and left
							using (SolidBrush grayBrush = new SolidBrush(SystemColors.GrayText))
								g.DrawString(mc.Text, this.Font, grayBrush, textRect, format);
						}
					}
				}
			}
		}

		protected void DrawAllCommands(Graphics g)
		{
			for(int i=0; i<_drawCommands.Count; i++)
			{
				// Grab some commonly used values
				DrawCommand dc = _drawCommands[i] as DrawCommand;

				// Draw this command only
				DrawSingleCommand(g, dc, (i == _trackItem));
			}
		}

		protected int SwitchTrackingItem(int oldItem, int newItem)
		{
			// Create a graphics object for drawinh
			using(Graphics g = this.CreateGraphics())
			{
				// Deselect the old draw command
				if (oldItem != -1)
				{
					//DrawCommand dc = _drawCommands[oldItem] as DrawCommand;

					// Draw old item not selected
					DrawSingleCommand(g, _drawCommands[oldItem] as DrawCommand, false);
				}

				_trackItem = newItem;

				// Select the new draw command
				if (_trackItem != -1)
				{
					//DrawCommand dc = _drawCommands[_trackItem] as DrawCommand;

					// Draw new item selected
					DrawSingleCommand(g, _drawCommands[_trackItem] as DrawCommand, true);
				}
			}

			return _trackItem;
		}

		internal void OperateSubMenu(DrawCommand dc, bool selectFirst, bool trackRemove)
		{
			Rectangle drawRect = dc.DrawRect;

			// Find screen positions for popup menu
			Point screenPos;

			if (_style == VisualStyle.IDE)
			{
				if (_direction == Direction.Horizontal)
					screenPos = PointToScreen(new Point(dc.DrawRect.Left + 1, drawRect.Bottom - _lengthGap - 1));
				else
					screenPos = PointToScreen(new Point(dc.DrawRect.Right - _breadthGap, drawRect.Top + _boxExpandSides - 1));
			}
			else
			{
				if (_direction == Direction.Horizontal)
					screenPos = PointToScreen(new Point(dc.DrawRect.Left + 1, drawRect.Bottom));
				else
					screenPos = PointToScreen(new Point(dc.DrawRect.Right, drawRect.Top));
			}

			Point aboveScreenPos;

			if (_style == VisualStyle.IDE)
			{
				if (_direction == Direction.Horizontal)
					aboveScreenPos = PointToScreen(new Point(dc.DrawRect.Left + 1, drawRect.Top + _lengthGap + 1));
				else
					aboveScreenPos = PointToScreen(new Point(dc.DrawRect.Right - _breadthGap, drawRect.Bottom + _lengthGap));
			}
			else
			{
				if (_direction == Direction.Horizontal)
					aboveScreenPos = PointToScreen(new Point(dc.DrawRect.Left + 1, drawRect.Top));
				else
					aboveScreenPos = PointToScreen(new Point(dc.DrawRect.Right, drawRect.Bottom));
			}

			int borderGap;

			// Calculate the missing gap in the PopupMenu border
			if (_direction == Direction.Horizontal)
				borderGap = dc.DrawRect.Width - _subMenuBorderAdjust;
			else
				borderGap = dc.DrawRect.Height - _subMenuBorderAdjust;

			_popupMenu = new PopupMenu();

			// Define the correct visual style based on ours
			_popupMenu.Style = this.Style;

			// Key direction when keys cause dismissal
			int returnDir = 0;

			if (dc.Chevron)
			{
				MenuCommandCollection mcc = new MenuCommandCollection();

				bool addCommands = false;

				// Generate a collection of menu commands for those not visible
				foreach(MenuCommand command in _menuCommands)
				{
					if (!addCommands && (command == _chevronStartCommand))
						addCommands = true;

					if (addCommands)
						mcc.Add(command);
				}

				// Track the popup using provided menu item collection
				_popupMenu.TrackPopup(screenPos,
									  aboveScreenPos,
									  _direction,
									  mcc,
									  borderGap,
									  selectFirst,
									  this,
									  ref returnDir);
			}
			else
			{
				// Generate event so that caller has chance to modify MenuCommand contents
				OnPopupStart(dc.MenuCommand);

				// Track the popup using provided menu item collection
				_popupMenu.TrackPopup(screenPos,
									  aboveScreenPos,
									  _direction,
									  dc.MenuCommand.MenuCommands,
									  borderGap,
									  selectFirst,
									  this,
									  ref returnDir);

				// Generate event so that caller has chance to modify MenuCommand contents
				OnPopupEnd(dc.MenuCommand);
			}

			// Remove unwanted object
			_popupMenu = null;

			// Was arrow key used to dismiss the submenu?
			if (returnDir != 0)
			{
				// Using keyboard movements means we should have the focus
				if (!_manualFocus)
				{
					_manualFocus = true;

					GrabTheFocus();
				}

				if (returnDir < 0)
				{
					// Shift selection left one
					ProcessMoveLeft(true);
				}
				else
				{
					// Shift selection right one
					ProcessMoveRight(true);
				}

				// A WM_MOUSEMOVE is generated when we open up the new submenu for
				// display, ignore this as it causes the selection to move
				_ignoreMouseMove = true;
			}
			else
			{
				// Only if the submenu was dismissed at the request of the submenu
				// should the selection mode be cancelled, otherwise keep selection mode
				if (!_dismissTransfer)
				{
					// This item is no longer selected
					_selected = false;
					_drawUpwards = false;

					// Should we stop tracking this item
					if (trackRemove)
					{
						ReturnTheFocus();

						// Unselect the current item
						_trackItem = SwitchTrackingItem(_trackItem, -1);
					}
					else
					{
						// Repaint the item
						DrawCommand(_trackItem, true);
					}
				}
				else
				{
					// Do not change _selected status
					_dismissTransfer = false;
				}
			}
		}

		protected void GrabTheFocus()
		{
			// Remember the current focus location
			_oldFocus = WindowsAPI.GetFocus();

			// Grab the focus
			this.Focus();
		}

		protected void ReturnTheFocus()
		{
			// Do we remember where it came from?
			if (_oldFocus != IntPtr.Zero)
			{
				// Send the focus back
				WindowsAPI.SetFocus(_oldFocus);

				// No need to remember old focus anymore
				_oldFocus = IntPtr.Zero;
			}

			// We lost the focus
			_manualFocus = false;
		}

		protected override void OnLostFocus(EventArgs e)
		{
			// We lost the focus
			_manualFocus = false;

			// Remove tracking of any item
			_trackItem = SwitchTrackingItem(_trackItem, -1);

			// No need to remember old focus anymore
			_oldFocus = IntPtr.Zero;

			// When we lose focus we sometimes miss the WM_MOUSELEAVE message
			_mouseOver = false;

			base.OnLostFocus(e);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			DrawAllCommands(e.Graphics);
			base.OnPaint(e);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			switch(e.KeyCode)
			{
			case Keys.Escape:
				// Is an item being tracked
				if (_trackItem != -1)
				{
					// Is that item also selected
					if (_selected)
					{
						// Is it also showing a submenu
						if (_popupMenu == null)
						{
							// Deselect the item
							_selected = false;
							_drawUpwards = false;

							// Repaint the item
							DrawCommand(_trackItem, true);

							ReturnTheFocus();

							// Key has been processed
							e.Handled = true;
						}
						else
						{
							// The popup menu will swallow the escape
							_ignoreEscapeUp = true;
						}
					}
				}
				break;
			case Keys.Left:
				if (_direction == Direction.Horizontal)
					ProcessMoveLeft(false);

				if (_selected)
					_ignoreMouseMove = true;
				break;
			case Keys.Right:
				if (_direction == Direction.Horizontal)
					ProcessMoveRight(false);
				else
					ProcessMoveDown();

				if (_selected)
					_ignoreMouseMove = true;
				break;
			case Keys.Down:
				if (_direction == Direction.Horizontal)
					ProcessMoveDown();
				else
					ProcessMoveRight(false);
				break;
			case Keys.Up:
				if (_direction == Direction.Vertical)
					ProcessMoveLeft(false);

				break;
			}

			base.OnKeyDown(e);
		}


		protected override void OnKeyUp(KeyEventArgs e)
		{
			switch(e.KeyCode)
			{
				case Keys.Escape:
					if (_manualFocus)
					{
						if (!_selected)
						{
							// Did we see the key down for escape? If not then the
							// escape must have been used to dismiss a popup menu
							if (!_ignoreEscapeUp)
							{
								// No longer in manual focus mode
								_manualFocus = false;

								// Remove tracking of any item
								_trackItem = SwitchTrackingItem(_trackItem, -1);

								// Send the focus back to origin
								ReturnTheFocus();
							}
						}
					}

					_ignoreEscapeUp = false;

					break;
				default:
					ProcessMnemonicKey((char)e.KeyValue);

					if (_selected)
						_ignoreMouseMove = true;
					break;
			}

			base.OnKeyUp(e);
		}

		protected void ProcessMoveLeft(bool select)
		{
			if (_popupMenu == null)
			{
				int newItem = _trackItem;
				int startItem = newItem;

				for(int i=0; i<_drawCommands.Count; i++)
				{
					// Move to previous item
					newItem--;

					// Have we looped all the way around all choices
					if (newItem == startItem)
						return;

					// Check limits
					if (newItem < 0)
						newItem = _drawCommands.Count - 1;

					DrawCommand dc = _drawCommands[newItem] as DrawCommand;

					// Can we select this item?
					if (!dc.Separator && (dc.Chevron || dc.MenuCommand.Enabled))
					{
						// If a change has occured
						if (newItem != _trackItem)
						{
							// Modify the display of the two items
							_trackItem = SwitchTrackingItem(_trackItem, newItem);

							if (_selected)
							{
								if (dc.Chevron || (dc.MenuCommand.MenuCommands.Count > 0))
									WindowsAPI.PostMessage(this.Handle, WM_OPERATEMENU, 0, 1);
							}

							break;
						}
					}
				}
			}
		}

		protected void ProcessMoveRight(bool select)
		{
			if (_popupMenu == null)
			{
				int newItem = _trackItem;
				//int startItem = newItem;

				for(int i=0; i<_drawCommands.Count; i++)
				{
					// Move to previous item
					newItem++;

					// Check limits
					if (newItem >= _drawCommands.Count)
						newItem = 0;

					DrawCommand dc = _drawCommands[newItem] as DrawCommand;

					// Can we select this item?
					if (!dc.Separator && (dc.Chevron || dc.MenuCommand.Enabled))
					{
						// If a change has occured
						if (newItem != _trackItem)
						{
							// Modify the display of the two items
							_trackItem = SwitchTrackingItem(_trackItem, newItem);

							if (_selected)
							{
								if (dc.Chevron || (dc.MenuCommand.MenuCommands.Count > 0))
									WindowsAPI.PostMessage(this.Handle, WM_OPERATEMENU, 0, 1);
							}

							break;
						}
					}
				}
			}
		}

		protected void ProcessMoveDown()
		{
			if (_popupMenu == null)
			{
				// Are we tracking an item?
				if (_trackItem != -1)
				{
					// The item must not already be selected
					if (!_selected)
					{
						DrawCommand dc = _drawCommands[_trackItem] as DrawCommand;

						// Is there a submenu to show?
						if (dc.Chevron || (dc.MenuCommand.MenuCommands.Count > 0))
						{
							// Select the tracked item
							_selected = true;
							_drawUpwards = false;

							// Update display to show as selected
							DrawCommand(_trackItem, true);

							// Show the submenu
							WindowsAPI.PostMessage(this.Handle, WM_OPERATEMENU, 0, 1);
						}
					}
				}
			}
		}

		protected bool ProcessMnemonicKey(char key)
		{
			// We should always gain focus
			if (!_manualFocus)
			{
				// We keep the focus until they move it
				_manualFocus = true;

				GrabTheFocus();
			}

			// No current selection
			if (!_selected)
			{
				// Search for an item that matches
				for(int i=0; i<_drawCommands.Count; i++)
				{
					DrawCommand dc = _drawCommands[i] as DrawCommand;

					// Only interested in enabled items
					if ((dc.MenuCommand != null) && dc.MenuCommand.Enabled)
					{
						// Does the character match?
						if (key == dc.Mnemonic)
						{
							// Select the tracked item
							_selected = true;
							_drawUpwards = false;

							// Is there a change in tracking?
							if (_trackItem != i)
							{
								// Modify the display of the two items
								_trackItem = SwitchTrackingItem(_trackItem, i);
							}
							else
							{
								// Update display to show as selected
								DrawCommand(_trackItem, true);
							}

							// Is there a submenu to show?
							if (dc.Chevron || (dc.MenuCommand.MenuCommands.Count > 0))
								WindowsAPI.PostMessage(this.Handle, WM_OPERATEMENU, 0, 1);
							else
							{
								// No, pulse the Click event for the command
								dc.MenuCommand.OnClick(EventArgs.Empty);
							}

							return true;
						}
					}
				}
			}

			return false;
		}

		public bool PreFilterMessage(ref Message msg)
		{
			Form parentForm = this.FindForm();

			// Only interested if the Form we are on is activate (i.e. contains focus)
			if ((parentForm != null) && parentForm.ContainsFocus)
			{
				switch(msg.Msg)
				{
					case (int)Win32.Msg.WM_MDISETMENU:
					case (int)Win32.Msg.WM_MDIREFRESHMENU:
						return true;
					case (int)Win32.Msg.WM_KEYUP:
						{
							// Find up/down state of shift and control keys
							ushort shiftKey = WindowsAPI.GetKeyState((int)Win32.VirtualKeys.VK_SHIFT);
							ushort controlKey = WindowsAPI.GetKeyState((int)Win32.VirtualKeys.VK_CONTROL);

							// Basic code we are looking for is the key pressed...
							int code = (int)msg.WParam;

							// ...plus the modifier for SHIFT...
							if (((int)shiftKey & 0x00008000) != 0)
								code += 0x00010000;

							// ...plus the modifier for CONTROL
							if (((int)controlKey & 0x00008000) != 0)
								code += 0x00020000;

							// Construct shortcut from keystate and keychar
							Shortcut sc = (Shortcut)(code);

							// Search for a matching command
							return GenerateShortcut(sc, _menuCommands);
						}
					case (int)Win32.Msg.WM_SYSKEYUP:
						if ((int)msg.WParam == (int)Win32.VirtualKeys.VK_MENU)
						{
							// If not already in manual focus mode
							if (!_manualFocus)
							{
								// Are there any menu commands?
								if (_drawCommands.Count > 0)
								{
									// If no item is currently tracked then...
									if (_trackItem == -1)
									{
										// ...start tracking the first valid command
										for(int i=0; i<_drawCommands.Count; i++)
										{
											DrawCommand dc = _drawCommands[i] as DrawCommand;

											if (!dc.Separator && (dc.Chevron || dc.MenuCommand.Enabled))
											{
												_trackItem = SwitchTrackingItem(-1, i);
												break;
											}
										}
									}

									// We keep the focus until they move it
									_manualFocus = true;

									// Grab the focus for key events
									GrabTheFocus();
								}

								return true;
							}
						}
						else
						{
							// Construct shortcut from ALT + keychar
							Shortcut sc = (Shortcut)(0x00040000 + (int)msg.WParam);

							if (GenerateShortcut(sc, _menuCommands))
								return true;

							// Last resort is treat as a potential mnemonic
							return ProcessMnemonicKey((char)msg.WParam);
						}
						break;
					default:
						break;
				}
			}

			return false;
		}

		protected bool GenerateShortcut(Shortcut sc, MenuCommandCollection mcc)
		{
			foreach(MenuCommand mc in mcc)
			{
				// Does the command match?
				if (mc.Enabled && (mc.Shortcut == sc))
				{
					// Generate event for command
					mc.OnClick(EventArgs.Empty);

					return true;
				}
				else
				{
					// Any child items to test?
					if (mc.MenuCommands.Count > 0)
					{
						// Recursive descent of all collections
						if (GenerateShortcut(sc, mc.MenuCommands))
							return true;
					}
				}
			}

			return false;
		}

		protected void OnWM_OPERATEMENU(ref Message m)
		{
			// Is there a valid item being tracted?
			if (_trackItem != -1)
			{
				DrawCommand dc = _drawCommands[_trackItem] as DrawCommand;

				OperateSubMenu(dc, (m.LParam != IntPtr.Zero), (m.WParam != IntPtr.Zero));
			}
		}

		protected void OnWM_GETDLGCODE(ref Message m)
		{
			// We want to the Form to provide all keyboard input to us
			m.Result = (IntPtr)Win32.DialogCodes.DLGC_WANTALLKEYS;
		}

		protected override void WndProc(ref Message m)
		{
			// WM_OPERATEMENU is not a constant and so cannot be in a switch
			if (m.Msg == WM_OPERATEMENU)
				OnWM_OPERATEMENU(ref m);
			else
			{
				switch(m.Msg)
				{
				case (int)Win32.Msg.WM_GETDLGCODE:
					OnWM_GETDLGCODE(ref m);
					return;
				}
			}

			base.WndProc(ref m);
		}
	}
}
#endif
