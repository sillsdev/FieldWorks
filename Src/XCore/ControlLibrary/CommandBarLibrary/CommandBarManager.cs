// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;
	using System.Drawing;
	using System.Collections;
	using System.Runtime.InteropServices;
	using System.Security.Permissions;
	using System.Windows.Forms;

	public class CommandBarManager : Control
	{
		private CommandBarCollection commandBars;

		public CommandBarManager()
		{
			this.SetStyle(ControlStyles.UserPaint, false);
			this.TabStop = false;
			this.Dock = DockStyle.Top;
			this.commandBars = new CommandBarCollection(this);
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
			if (disposing)
			{
				if (commandBars != null)
					commandBars.Dispose();
			}

			commandBars = null;
			base.Dispose(disposing);
		}

		public CommandBarCollection CommandBars
		{
			get { return this.commandBars; }
		}

		protected override Size DefaultSize
		{
			get { return new Size(100, 22 * 2); }
		}

		protected override void CreateHandle()
		{
			if (!this.RecreatingHandle)
			{
				NativeMethods.INITCOMMONCONTROLSEX icex = new NativeMethods.INITCOMMONCONTROLSEX();
				icex.Size = Marshal.SizeOf(typeof(NativeMethods.INITCOMMONCONTROLSEX));
				icex.Flags = NativeMethods.ICC_BAR_CLASSES | NativeMethods.ICC_COOL_CLASSES;
				NativeMethods.InitCommonControlsEx(icex);
			}

			base.CreateHandle();
		}

		protected override CreateParams CreateParams
		{
			[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.ClassName = NativeMethods.REBARCLASSNAME;
				createParams.Style = NativeMethods.WS_CHILD | NativeMethods.WS_VISIBLE | NativeMethods.WS_CLIPCHILDREN | NativeMethods.WS_CLIPSIBLINGS;
				createParams.Style |= NativeMethods.CCS_NODIVIDER | NativeMethods.CCS_NOPARENTALIGN | NativeMethods.CCS_NORESIZE;
				createParams.Style |= NativeMethods.RBS_VARHEIGHT | NativeMethods.RBS_BANDBORDERS | NativeMethods.RBS_AUTOSIZE;
				return createParams;
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			this.ReleaseBands();
			this.BeginUpdate();

			for (int i = 0; i < this.commandBars.Count; i++)
			{
				NativeMethods.REBARBANDINFO bandInfo = this.GetBandInfo(i);
				NativeMethods.SendMessage(this.Handle, NativeMethods.RB_INSERTBAND, i, ref bandInfo);
			}

			this.UpdateSize();
			this.EndUpdate();
			this.CaptureBands();
		}

		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
		protected override void WndProc(ref Message message)
		{
			base.WndProc(ref message);

			switch (message.Msg)
			{
				case NativeMethods.WM_NOTIFY:
				case NativeMethods.WM_NOTIFY + NativeMethods.WM_REFLECT:
				{
					NativeMethods.NMHDR note = (NativeMethods.NMHDR)message.GetLParam(typeof(NativeMethods.NMHDR));
					switch (note.code)
					{
						case NativeMethods.RBN_HEIGHTCHANGE:
							this.UpdateSize();
							break;

						case NativeMethods.RBN_CHEVRONPUSHED:
							this.NotifyChevronPushed(ref message);
							break;
					}
				}
					break;
			}
		}

		private void NotifyChevronPushed(ref Message message)
		{
			NativeMethods.NMREBARCHEVRON nrch = (NativeMethods.NMREBARCHEVRON)message.GetLParam(typeof(NativeMethods.NMREBARCHEVRON));

			CommandBar commandBar = this.commandBars[nrch.uBand];
			if (commandBar != null)
			{
				Point point = new Point(nrch.rc.left, nrch.rc.bottom);
				commandBar.Show(this, point);
			}
		}

		private void BeginUpdate()
		{
			NativeMethods.SendMessage(Handle, NativeMethods.WM_SETREDRAW, 0, 0);
		}

		private void EndUpdate()
		{
			NativeMethods.SendMessage(Handle, NativeMethods.WM_SETREDRAW, 1, 0);
		}

		//HACK, because without this the CommandBar was not getting keyboard events
		//for when I search for what I added to this class: jdh john hatton
		public bool HandleAltKey(System.Windows.Forms.KeyEventArgs e, bool wasDown)
		{
			Message m = new Message();
			m.WParam= (IntPtr) e.KeyCode;
			m.Msg = wasDown ? NativeMethods.WM_KEYDOWN : NativeMethods.WM_SYSKEYUP;
			return PreProcessMessage(ref m);
		}


		[SecurityPermission(SecurityAction.LinkDemand, Flags=SecurityPermissionFlag.UnmanagedCode)]
		public override bool PreProcessMessage(ref Message msg)
		{
			foreach (CommandBar commandBar in this.commandBars)
			{
				if (commandBar.PreProcessMessage(ref msg))
				{
					return true;
				}
			}

			return false;
		}

		private void UpdateBand(int index)
		{
			if (this.IsHandleCreated)
			{
				this.BeginUpdate();

				NativeMethods.REBARBANDINFO rbbi = this.GetBandInfo(index);
				NativeMethods.SendMessage(this.Handle, NativeMethods.RB_SETBANDINFO, index, ref rbbi);

				this.UpdateSize();
				this.EndUpdate();
			}
		}

		private void UpdateSize()
		{
			this.Height = NativeMethods.SendMessage(this.Handle, NativeMethods.RB_GETBARHEIGHT, 0, 0) + 1;
		}

		private NativeMethods.REBARBANDINFO GetBandInfo(int index)
		{
			CommandBar commandBar = this.commandBars[index];

			NativeMethods.REBARBANDINFO bandInfo = new NativeMethods.REBARBANDINFO();
			bandInfo.cbSize = Marshal.SizeOf(typeof(NativeMethods.REBARBANDINFO));
			bandInfo.fMask = 0;
			bandInfo.clrFore = 0;
			bandInfo.clrBack = 0;
			bandInfo.iImage = 0;
			bandInfo.hbmBack = IntPtr.Zero;
			bandInfo.lParam = 0;
			bandInfo.cxHeader = 0;

			bandInfo.fMask |= NativeMethods.RBBIM_ID;
			bandInfo.wID = 0xEB00 + index;

			if ((commandBar.Text != null) && (commandBar.Text.Length != 0))
			{
				bandInfo.fMask |= NativeMethods.RBBIM_TEXT;
				bandInfo.lpText = Marshal.StringToHGlobalUni(commandBar.Text);
				bandInfo.cch = (commandBar.Text == null) ? 0 : commandBar.Text.Length;
			}

			bandInfo.fMask |= NativeMethods.RBBIM_STYLE;
			bandInfo.fStyle = NativeMethods.RBBS_CHILDEDGE | NativeMethods.RBBS_FIXEDBMP | NativeMethods.RBBS_GRIPPERALWAYS;
			bandInfo.fStyle |= NativeMethods.RBBS_BREAK;
			bandInfo.fStyle |= NativeMethods.RBBS_USECHEVRON;

			bandInfo.fMask |= NativeMethods.RBBIM_CHILD;
			bandInfo.hwndChild = commandBar.Handle;

			bandInfo.fMask |= NativeMethods.RBBIM_CHILDSIZE;
			bandInfo.cyMinChild = commandBar.Height;
			bandInfo.cxMinChild = 0;
			bandInfo.cyChild = 0;
			bandInfo.cyMaxChild = 0;
			bandInfo.cyIntegral = 0;

			bandInfo.fMask |= NativeMethods.RBBIM_SIZE;
			bandInfo.cx = commandBar.Width;

			bandInfo.fMask |= NativeMethods.RBBIM_IDEALSIZE;
			bandInfo.cxIdeal = commandBar.Width;

			return bandInfo;
		}

		private void UpdateBands()
		{
			if (this.IsHandleCreated)
			{
				this.RecreateHandle();
			}
		}

		internal void AddCommandBar()
		{
			this.UpdateBands();
		}

		internal void RemoveCommandBar()
		{
			this.UpdateBands();
		}

		private void CommandBar_HandleCreated(object sender, EventArgs e)
		{
			this.ReleaseBands();

			CommandBar commandBar = (CommandBar) sender;
			this.UpdateBand(this.commandBars.IndexOf(commandBar));

			this.CaptureBands();
		}

		private void CommandBar_TextChanged(object sender, EventArgs e)
		{
			CommandBar commandBar = (CommandBar) sender;
			this.UpdateBand(this.commandBars.IndexOf(commandBar));
		}

		private void CaptureBands()
		{
			foreach (CommandBar commandBar in this.commandBars)
			{
				commandBar.HandleCreated += new EventHandler(this.CommandBar_HandleCreated);
				commandBar.TextChanged += new EventHandler(this.CommandBar_TextChanged);
			}
		}

		private void ReleaseBands()
		{
			foreach (CommandBar commandBar in this.commandBars)
			{
				commandBar.HandleCreated -= new EventHandler(this.CommandBar_HandleCreated);
				commandBar.TextChanged -= new EventHandler(this.CommandBar_TextChanged);
			}
		}
	}
}
