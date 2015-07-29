// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
#if USE_DOTNETBAR
using DevComponents.DotNetBar;
#endif
using SIL.Utils;

namespace XCore
{
	internal class PanelEx : Panel
	{
		int? m_widthOfLeftDockedControls = null;

		public PanelEx()
		{
			this.Text = String.Empty;

			this.ControlAdded += HandleControlAdded;
		}

		void HandleControlAdded (object sender, ControlEventArgs e)
		{
			m_widthOfLeftDockedControls = null;
		}

		/// <summary>
		/// sets m_widthOfLeftDockedControls
		/// </summary>
		void CalculateWidthOfLeftDockedControls()
		{
			m_widthOfLeftDockedControls = 0;

			foreach(Control c in Controls)
			{
				if ((c.Dock & DockStyle.Left) == 0)
					continue;

				if (m_widthOfLeftDockedControls < c.Left + c.Width)
					m_widthOfLeftDockedControls = c.Left + c.Width;
			}
		}

		public override string Text
		{
			get; set;
		}

		protected override void OnPaintBackground (PaintEventArgs e)
		{
			base.OnPaintBackground (e);

			var rectangleToPaint = ClientRectangle;
			var beginColor = Color.FromArgb(0x58, 0x80, 0xd0);
			var endColor = Color.FromArgb(0x08, 0x40, 0x98);

			using (var brush = new LinearGradientBrush(rectangleToPaint, beginColor, endColor,
				LinearGradientMode.Vertical))
			{
				e.Graphics.FillRectangle(brush, rectangleToPaint);
			}

			// Draw a background image if we have one set.
			// This code assumes it is always centered.

			Graphics g = e.Graphics;
			var backgroundImage = this.BackgroundImage;
			if (backgroundImage != null)
			{
				var drawRect = new Rectangle();
				drawRect.Location = new Point(ClientSize.Width / 2 - backgroundImage.Width /2,
											  ClientSize.Height /2 - backgroundImage.Height /2);
				drawRect.Size = backgroundImage.Size;
				g.DrawImage( backgroundImage, drawRect);
			}
		}

		protected override void OnPaint (PaintEventArgs e)
		{
			using(var brush = new SolidBrush(Color.White))
			{
				if (m_widthOfLeftDockedControls == null)
					CalculateWidthOfLeftDockedControls();

				e.Graphics.DrawString(Text, Font, brush, (int)m_widthOfLeftDockedControls + 2, 0);
				base.OnPaint (e);
			}
		}
	}

	/// <summary>
	/// PaneBar, AKA "information bar"
	/// </summary>
	public class PaneBar : System.Windows.Forms.UserControl, IPaneBar, IxCoreColleague
	{
		private ImageCollection m_smallImages;
		private IUIMenuAdapter m_menuBarAdapter;


		#region Data members

		private XCore.Mediator m_mediator;
		private Hashtable m_propertiesToWatch =new Hashtable();

#if USE_DOTNETBAR
		private DevComponents.DotNetBar.PanelEx m_panelMain;
		private DevComponents.DotNetBar.PanelEx testPanel;
		private DevComponents.DotNetBar.PanelEx spacer;
		private DevComponents.DotNetBar.PanelEx panelEx2;
		private DevComponents.DotNetBar.PanelEx panelEmbedded;
#else
		private PanelEx m_panelMain;
		private PanelEx testPanel;
		private PanelEx spacer;
		private PanelEx panelEx2;
		private PanelEx panelEmbedded;
#endif
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#endregion Data members

		#region IPaneBar implementation

		/// <summary>
		///
		/// </summary>
		public override string Text
		{
			set
			{
				m_panelMain.Text = value;
			}
		}

		protected  void  AddHotlink(XCore.ChoiceBase choice)
		{
			PanelButton button = new PanelButton(choice, m_smallImages);

			m_panelMain.Controls.Add(button);
			button.Dock=DockStyle.Right;

			WatchPropertyOfButton(button);
		}

		protected  void  AddMenu(XCore.ChoiceGroup choice)
		{
			PanelMenu button = new PanelMenu(choice,m_smallImages, m_menuBarAdapter);
			Spacer s = new Spacer();
			m_panelMain.Controls.Add(s);
			s.Dock = DockStyle.Left;
			s.Width = 10;
			m_panelMain.Controls.Add(button);
			button.Dock=DockStyle.Left;
		}

		private void WatchPropertyOfButton(PanelButton button)
		{
			//for now, only handles Boolean properties
			BoolPropertyChoice choice = button.Tag as XCore.BoolPropertyChoice;
			if (choice != null)
				m_propertiesToWatch.Add(choice.BoolPropertyName, button);
		}

		/// <summary>
		///
		/// </summary>
		public void RefreshPane()
		{
			foreach(Control c in m_panelMain.Controls)
			{
				PanelButton b = c as PanelButton;
				if(b!= null)
					b.UpdateDisplay();
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="group"></param>
		public  void  AddGroup(XCore.ChoiceGroup group)
		{
			ClearMainPanelControls();
			ArrayList l = new ArrayList(group.Count);
			foreach(ChoiceRelatedClass item in group)
			{
				l.Add(item);
			}
			l.Reverse();

			foreach(ChoiceRelatedClass item in l)
			{
				XCore.ChoiceRelatedClass choice = (XCore.ChoiceRelatedClass)this.Tag;
				UIItemDisplayProperties display = item.GetDisplayProperties();
				if (!display.Visible)
					continue;
				if(item is ChoiceBase)
				{
					AddHotlink((ChoiceBase)item);
				}
				else if(item is ChoiceGroup)
				{
					AddMenu((ChoiceGroup)item);
				}
			}

		}


		/// <summary>
		///
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="config"></param>
		/// <returns></returns>
		public void Init (Mediator mediator, System.Xml.XmlNode config)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="smallImages"></param>
		/// <param name="mediator"></param>
		/// <returns></returns>
		public void  Init (ImageCollection smallImages, IUIMenuAdapter menuBarAdapter, Mediator mediator)
		{
			m_mediator = mediator;
			mediator.AddColleague(this);
			m_smallImages = smallImages;
			m_menuBarAdapter = menuBarAdapter;
		}

		#endregion IPaneBar implementation

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[]{this};
		}
		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// XCore will call us whenever a property has changed.
		/// </summary>
		/// <param name="name"></param>
		public void OnPropertyChanged(string name)
		{
			//decide if something we are showing is affected by this property
			//foreach(PanelButton panel in m_propertiesToWatch)
			foreach(DictionaryEntry e in m_propertiesToWatch)
			{
				PanelButton panel = e.Value as PanelButton;
				if (panel.IsRelatedProperty(name))
				{
					panel.UpdateDisplay();
				}
			}
		}

		#region Construction and Initialization

		public PaneBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			//get rid of the items that are just there for testing in the designer
			ClearMainPanelControls();
//			testPanel.Hide();
//			panelEx2.Hide();
//			spacer.Hide();

		}

		private void ClearMainPanelControls()
		{
			this.m_panelMain.Controls.Clear();
			m_propertiesToWatch.Clear();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(m_mediator !=null)
					m_mediator.RemoveColleague(this);
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion Construction and Initialization

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
#if USE_DOTNETBAR
			this.m_panelMain = new DevComponents.DotNetBar.PanelEx();
			this.panelEx2 = new DevComponents.DotNetBar.PanelEx();
			this.panelEmbedded = new DevComponents.DotNetBar.PanelEx();
			this.spacer = new DevComponents.DotNetBar.PanelEx();
			this.testPanel = new DevComponents.DotNetBar.PanelEx();
#else
			this.m_panelMain = new PanelEx();
			this.panelEx2 = new PanelEx();
			this.panelEmbedded = new PanelEx();
			this.spacer = new PanelEx();
			this.testPanel = new PanelEx();
#endif
			this.m_panelMain.SuspendLayout();
			this.panelEx2.SuspendLayout();
			this.SuspendLayout();
			//
			// m_panelMain
			//
			this.m_panelMain.Controls.Add(this.panelEx2);
			this.m_panelMain.Controls.Add(this.spacer);
			this.m_panelMain.Controls.Add(this.testPanel);
			this.m_panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panelMain.DockPadding.Bottom = 0;
			this.m_panelMain.DockPadding.Left = 2;
			this.m_panelMain.DockPadding.Right = 2;
			this.m_panelMain.DockPadding.Top = 0;
			this.m_panelMain.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.m_panelMain.Location = new System.Drawing.Point(0, 0);
			this.m_panelMain.Name = "m_panelMain";
			this.m_panelMain.Size = new System.Drawing.Size(704, 24);
#if USE_DOTNETBAR
			this.m_panelMain.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
			this.m_panelMain.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
			this.m_panelMain.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
			this.m_panelMain.Style.BorderSide = DevComponents.DotNetBar.eBorderSide.None;
			this.m_panelMain.Style.BorderWidth = 0;
			this.m_panelMain.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
			this.m_panelMain.Style.GradientAngle = 90;
#endif
			this.m_panelMain.TabIndex = 0;
			this.m_panelMain.Text = "Title";
			//
			// panelEx2
			//
			this.panelEx2.Controls.Add(this.panelEmbedded);
			this.panelEx2.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelEx2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.panelEx2.Location = new System.Drawing.Point(465, 2);
			this.panelEx2.Name = "panelEx2";
			this.panelEx2.Size = new System.Drawing.Size(56, 20);
#if USE_DOTNETBAR
			this.panelEx2.Style.Alignment = System.Drawing.StringAlignment.Far;
			this.panelEx2.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
			this.panelEx2.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
			this.panelEx2.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
			this.panelEx2.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
			this.panelEx2.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedText;
			this.panelEx2.Style.GradientAngle = 90;
			this.panelEx2.StyleMouseOver.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemHotBackground;
			this.panelEx2.StyleMouseOver.ForeColor.Color = System.Drawing.Color.Yellow;
#endif
			this.panelEx2.TabIndex = 2;
			this.panelEx2.Text = "Test2";
			//
			// panelEmbedded
			//
			this.panelEmbedded.Location = new System.Drawing.Point(0, 0);
			this.panelEmbedded.Name = "panelEmbedded";
			this.panelEmbedded.Size = new System.Drawing.Size(20, 20);
#if USE_DOTNETBAR
			this.panelEmbedded.Style.Alignment = System.Drawing.StringAlignment.Center;
			this.panelEmbedded.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
			this.panelEmbedded.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
			this.panelEmbedded.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
			this.panelEmbedded.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
			this.panelEmbedded.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelText;
			this.panelEmbedded.Style.GradientAngle = 90;
#endif
			this.panelEmbedded.TabIndex = 0;
			this.panelEmbedded.Text = "zzz";
			//
			// spacer
			//
			this.spacer.Dock = System.Windows.Forms.DockStyle.Right;
			this.spacer.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.spacer.Location = new System.Drawing.Point(521, 2);
			this.spacer.Name = "spacer";
			this.spacer.Size = new System.Drawing.Size(48, 20);
#if USE_DOTNETBAR
			this.spacer.Style.Alignment = System.Drawing.StringAlignment.Center;
			this.spacer.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
			this.spacer.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
			this.spacer.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
			this.spacer.Style.BorderSide = DevComponents.DotNetBar.eBorderSide.None;
			this.spacer.Style.BorderWidth = 0;
			this.spacer.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedText;
			this.spacer.Style.GradientAngle = 90;
			this.spacer.StyleMouseOver.ForeColor.Color = System.Drawing.Color.Yellow;
#endif
			this.spacer.TabIndex = 1;
			//
			// testPanel
			//
			this.testPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.testPanel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.testPanel.Location = new System.Drawing.Point(569, 2);
			this.testPanel.Name = "testPanel";
			this.testPanel.Size = new System.Drawing.Size(120, 20);
#if USE_DOTNETBAR
			this.testPanel.Style.Alignment = System.Drawing.StringAlignment.Center;
			this.testPanel.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground;
			this.testPanel.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedBackground2;
			this.testPanel.Style.Border = DevComponents.DotNetBar.eBorderType.SingleLine;
			this.testPanel.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
			this.testPanel.Style.ForeColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.ItemCheckedText;
			this.testPanel.Style.GradientAngle = 90;
			this.testPanel.StyleMouseOver.ForeColor.Color = System.Drawing.Color.Yellow;
#endif
			this.testPanel.TabIndex = 0;
			this.testPanel.Text = "Test";
			//
			// PaneBar
			//
			this.Controls.Add(this.m_panelMain);
			this.Name = "PaneBar";
			this.Size = new System.Drawing.Size(696, 24);
			this.m_panelMain.ResumeLayout(false);
			this.panelEx2.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}

#if USE_DOTNETBAR
	class Spacer : PanelEx
	{
		public Spacer():base()
		{
			this.Dock = System.Windows.Forms.DockStyle.Right;
			//this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.Location = new System.Drawing.Point(545, 2);
			this.Name = "spacer";
			this.Size = new System.Drawing.Size(16, 20);
			this.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.Style.Alignment = System.Drawing.StringAlignment.Center;
			this.Style.BackColor1.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground;
			this.Style.BackColor2.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBackground2;
			this.Style.BorderColor.ColorSchemePart = DevComponents.DotNetBar.eColorSchemePart.PanelBorder;
			this.Style.BorderSide = DevComponents.DotNetBar.eBorderSide.None;
			this.Style.BorderWidth = 0;
			this.Style.GradientAngle = 90;
			this.TabIndex = 0;

		}
	}
#else
	class Spacer : PanelEx
	{
		public Spacer():base()
		{
			this.Dock = System.Windows.Forms.DockStyle.Right;
			//this.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.Location = new System.Drawing.Point(545, 2);
			this.Name = "spacer";
			this.Size = new System.Drawing.Size(16, 20);
			this.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.TabIndex = 0;
		}
	}
#endif
}
