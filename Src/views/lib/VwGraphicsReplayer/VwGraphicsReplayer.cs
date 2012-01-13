using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.Utils.FileDialog;

namespace VwGraphicsReplayer
{
	/// <summary>
	/// This simple program is used to run log files produced by VwGraphicsCairo (VwGraphics on Linux).
	/// It is intended to allow testing of VwGraphics with real world data. Particually in hunting down
	/// hard to repoduce crashes involving pango-graphite etc.
	/// When a debug build of FieldWorks is run with LOG_VWGRAPHICS set. For example:
	/// LOG_VWGRAPHICS=1 mono FieldWorks.exe -app TE
	/// </summary>
	class VwGraphicsReplayer : Form
	{
		// true if screen has been painted
		bool m_painted;

		FileInfo m_datafile;

		IVwGraphicsWin32 m_vwGraphics32;
		string m_currentVwGraphicHandle;

		public static void Main(string[] args)
		{
			var form = new VwGraphicsReplayer();

			Application.Run(form);
		}

		protected override void OnLoad(EventArgs e)
		{
			var menu = new MainMenu();
			MenuItem open = new MenuItem("Open", (s, a) =>
			{
				using (var dialog = new OpenFileDialogAdapter())
				{
					dialog.InitialDirectory = "/tmp";
					if (dialog.ShowDialog() == DialogResult.OK)
					{
						m_datafile = new FileInfo(dialog.FileName);
						m_painted = false;
					}
				}
			});

			MenuItem refresh = new MenuItem("Refresh", (s, a) =>
			{
				m_painted = false;
				this.Invalidate();
			});

			base.OnLoad(e);

			menu.MenuItems.Add(open);
			menu.MenuItems.Add(refresh);
			Menu = menu;
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			if (m_datafile != null && m_painted == false) {
				TextReader reader = new StreamReader(m_datafile.FullName);
				string line = reader.ReadLine();
				while (line != null) {
					ProcessLine(e.Graphics, line);
					line = reader.ReadLine();
				}

				m_painted = true;
			}
		}

		/// <summary>
		/// Skip the argument name
		/// Read the ptr value which should always be the first item.
		/// Return the rest of the parameters (if any)
		/// </summary>
		protected IEnumerable<string> SwitchToCorrectVwGraphics(IEnumerable<string> arguments)
		{
			var ptr = arguments.Skip(1);
			m_currentVwGraphicHandle = ptr.First();
			if (VwGraphicsLookup.Keys.Contains(ptr.First()))
				m_vwGraphics32 = VwGraphicsLookup[ptr.First()];

			return ptr.Skip(1);
		}


		protected void ProcessLine(Graphics gr, string line)
		{
			// TODO: this doesn't handle " inside quotes, but nor does our logging currently... :)
			var matches = Regex.Matches(line, "(?<match>\\w+)|\"(?<match>[^\"]*)\"").Cast<Match>().Select(m => m.Groups["match"].Value);

			var args = SwitchToCorrectVwGraphics(matches);

			// TODO: could use reflection to do these method calls...
			switch (matches.First()) {
			case "Initialize":
				ProcessInitialize(matches);
				break;

			case "put_XUnitsPerInch":
				ProcessXUnitsPerInch(args);
				break;

			case "get_XUnitsPerInch":
				ProcessXUnitsPerInch();
				break;

			case "GetDeviceContext":
				ProcessGetDeviceContext();
				break;

			case "ReleaseDC":
				ProcessReleaseDC(gr);
				break;

			case "put_YUnitsPerInch":
				ProcessYUnitsPerInch(args);
				break;

			case "get_YUnitsPerInch":
				ProcessYUnitsPerInch();
				break;

			case "GetClipRect":
				ProcessGetClipRect();
				break;

			case "SetupGraphics":
				ProcessSetupGraphics(args);
				break;

			case "DrawText":
				ProcessDrawText(args);
				break;

			case "get_FontCharProperties":
				ProcessFontCharProperties();
				break;

			case "put_BackColor":
				ProcessBackColor(args);
				break;

			case "InvertRect":
				ProcessInvertRect(args);
				break;

			case "GetTextLeadWidth":
				ProcessGetTextLeadWidth(args);
				break;

			case "GetTextExtent":
				ProcessGetTextExtent(args);
				break;

			case "get_FontAscent":
				ProcessFontAscent();
				break;

			case "DrawRectangle":
				ProcessDrawRectangle(args);
				break;

			case "SetClipRect":
				ProcessSetClipRect(args);
				break;
			default:

				Console.WriteLine("Warning unhandled line {0}", matches.First());
				break;
			}
		}

		Dictionary<string, IVwGraphicsWin32> VwGraphicsLookup = new Dictionary<string, IVwGraphicsWin32>();
		Dictionary<string, Graphics> GraphicsLookup = new Dictionary<string, Graphics>();
		Dictionary<string, Bitmap> BitmapLookup = new Dictionary<string, Bitmap>();

		protected void ProcessInitialize(IEnumerable<string> arguments)
		{
			string handle = arguments.Skip(1).First();


			if (VwGraphicsLookup.Keys.Contains(handle))
			{
				var bitmap = new Bitmap(1000,1000);
				Graphics gr = Graphics.FromImage(bitmap);
				VwGraphicsLookup[handle].Initialize(gr.GetHdc());
				GraphicsLookup[handle] = gr;
				BitmapLookup[handle] = bitmap;
			}
			else
			{
				IVwGraphicsWin32 vwGraphics = VwGraphicsWin32Class.Create();
				var bitmap = new Bitmap(1000,1000);
				Graphics gr = Graphics.FromImage(bitmap);
				vwGraphics.Initialize(gr.GetHdc());
				VwGraphicsLookup.Add(handle, vwGraphics);
				GraphicsLookup[handle] = gr;
				BitmapLookup[handle] = bitmap;

				m_vwGraphics32 = vwGraphics;
			}
		}

		protected void ProcessReleaseDC(Graphics gr)
		{
			VwGraphicsLookup.Remove(m_currentVwGraphicHandle);
			m_vwGraphics32.ReleaseDC();
			GraphicsLookup[m_currentVwGraphicHandle].Dispose();
		}

		protected void ProcessXUnitsPerInch(IEnumerable<string> arguments)
		{
			m_vwGraphics32.XUnitsPerInch = int.Parse(arguments.First());
		}

		protected void ProcessXUnitsPerInch()
		{
			var unused = m_vwGraphics32.XUnitsPerInch;
		}

		protected void ProcessGetDeviceContext()
		{
			m_vwGraphics32.GetDeviceContext();
		}

		protected void ProcessYUnitsPerInch(IEnumerable<string> arguments)
		{
			m_vwGraphics32.YUnitsPerInch = int.Parse(arguments.First());
		}

		protected void ProcessYUnitsPerInch()
		{
			var unused = m_vwGraphics32.YUnitsPerInch;
		}

		protected void ProcessGetClipRect()
		{
			int unused1, unused2, unused3, unused4;
			m_vwGraphics32.GetClipRect(out unused1, out unused2, out unused3, out unused4);
		}

		protected void ProcessSetupGraphics(IEnumerable<string> arguments)
		{
			Debug.Assert(arguments.Count() == 14);

			LgCharRenderProps props = new LgCharRenderProps();
			props.clrFore = uint.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.clrBack = uint.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.clrUnder = uint.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.dympOffset = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.ws = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.fWsRtl = byte.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.nDirDepth = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.ssv = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.unt = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.ttvBold = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.ttvItalic = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.dympHeight = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			props.szFaceName = new ushort[32];
			for (int ich = 0; ich < arguments.First().Length; ++ich) {
				props.szFaceName[ich] = (ushort)arguments.First()[ich];
			}
			if (arguments.First().Length < 32)
				props.szFaceName[arguments.First().Length] = 0;
			else
				props.szFaceName[31] = 0;

			arguments = arguments.Skip(1);

			props.szFontVar = new ushort[64];
			for (int ich = 0; ich < arguments.First().Length; ++ich) {
				props.szFontVar[ich] = (ushort)arguments.First()[ich];
			}
			if (arguments.First().Length < 64)
				props.szFontVar[arguments.First().Length] = 0;
			else
				props.szFontVar[63] = 0;
			arguments = arguments.Skip(1);

			m_vwGraphics32.SetupGraphics(ref props);

			// all argument should be used up.
			Debug.Assert(arguments.Count() == 0);
		}

		protected void ProcessDrawText(IEnumerable<string> arguments)
		{
			try
			{
				int x = int.Parse(arguments.First());
				arguments = arguments.Skip(1);

				int y = int.Parse(arguments.First());
				arguments = arguments.Skip(1);

				int cch = int.Parse(arguments.First());
				arguments = arguments.Skip(1);

				string rgch = arguments.First();
				arguments = arguments.Skip(1);

				int stretch = int.Parse(arguments.First());
				arguments = arguments.Skip(1);

				m_vwGraphics32.DrawText(x, y, cch, rgch, stretch);

				// all argument should be used up.
				Debug.Assert(arguments.Count() == 0);
			}
			catch(Exception e)
			{
				Console.WriteLine("Warning could not parse line DrawText {0}", arguments.Aggregate((str, next) => (str + " " + next)));
			}
		}

		protected void ProcessFontCharProperties()
		{
			var unused = m_vwGraphics32.FontCharProperties;
		}

		protected void ProcessBackColor(IEnumerable<string> arguments)
		{
			m_vwGraphics32.BackColor = int.Parse(arguments.First());
		}

		protected void ProcessInvertRect(IEnumerable<string> arguments)
		{
			int xLeft = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			int yTop = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			int xRight = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			int yBottom = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			m_vwGraphics32.InvertRect(xLeft, yTop, xRight, yBottom);
		}

		protected void ProcessGetTextLeadWidth(IEnumerable<string> arguments)
		{
			try
			{
				int cch = int.Parse(arguments.First());
				arguments = arguments.Skip(1);

				string rgch = arguments.First();
				arguments = arguments.Skip(1);

				int ich = int.Parse(arguments.First());
				arguments = arguments.Skip(1);

				int stretch = int.Parse(arguments.First());
				arguments = arguments.Skip(1);

				var unused = m_vwGraphics32.GetTextLeadWidth(cch, rgch, ich, stretch);
			}
			catch(Exception e)
			{
				Console.WriteLine("Get TextLead width throw exception {0}", arguments.Aggregate((str, next) => (str + " " + next)));
			}
		}

		protected void ProcessGetTextExtent(IEnumerable<string> arguments)
		{
			int cch = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			string rgch = arguments.First();
			arguments = arguments.Skip(1);

			int unusedX, unusedY;
			m_vwGraphics32.GetTextExtent(cch, rgch, out unusedX, out unusedY);
		}

		protected void ProcessFontAscent()
		{
			var unused = m_vwGraphics32.FontAscent;
		}


		protected void ProcessDrawRectangle(IEnumerable<string> arguments)
		{
			int xLeft = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			int yTop = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			int xRight = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			int yBottom = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			m_vwGraphics32.DrawRectangle(xLeft, yTop, xRight, yBottom);
		}

		protected void ProcessSetClipRect(IEnumerable<string> arguments)
		{
			int left = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			int top = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			int right = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			int bottom = int.Parse(arguments.First());
			arguments = arguments.Skip(1);

			Rect r = new Rect(left, top, right, bottom);

			m_vwGraphics32.SetClipRect(ref r);
		}
	}
}
