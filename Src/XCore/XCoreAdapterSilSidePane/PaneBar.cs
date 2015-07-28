// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Data;
using System.Windows.Forms;
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

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "g is a reference")]
		protected override void OnPaintBackground (PaintEventArgs e)
		{
			base.OnPaintBackground (e);

			var rectangleToPaint = ClientRectangle;
			if (rectangleToPaint.Width <= 0 || rectangleToPaint.Height <= 0)
				return; // can't draw anything, and will crash if we try
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

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate(); // need to redraw in the full new width
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
		private IImageCollection m_smallImages;
		private IUIMenuAdapter m_menuBarAdapter;


		#region Data members

		private XCore.Mediator m_mediator;
		private Hashtable m_propertiesToWatch =new Hashtable();

		private PanelEx m_panelMain;
		private PanelEx testPanel;
		private PanelEx spacer;
		private PanelEx panelEx2;
		private PanelEx panelEmbedded;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components;


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
				m_panelMain.Refresh();
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="button added to collection")]
		protected  void  AddHotlink(XCore.ChoiceBase choice)
		{
			PanelButton button = new PanelButton(choice, m_smallImages);

			m_panelMain.Controls.Add(button);
			button.Dock=DockStyle.Right;

			WatchPropertyOfButton(button);
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="button and spacer added to collection")]
		protected void AddMenu(ChoiceGroup choice)
		{
			bool fAlignmentLeft = GetOptionalMenuAlignment(choice);

			var button = new PanelMenu(choice, m_smallImages, m_menuBarAdapter);
			button.AccessibilityObject.Name = choice.Id;
			button.Dock = fAlignmentLeft ? DockStyle.Left : DockStyle.Right;

			var spacer = new Spacer();
			spacer.AccessibilityObject.Name = choice.Id;
			spacer.Dock = DockStyle.Left;
			spacer.Width = 10;

			if (fAlignmentLeft)
			{
				m_panelMain.Controls.Add(spacer);
				m_panelMain.Controls.Add(button);
			}
			else
			{
				spacer.Dock = DockStyle.Right;
				spacer.Width = 10;
				m_panelMain.Controls.Add(button);
				m_panelMain.Controls.Add(spacer);
			}
		}

		/// <summary>
		/// Get the optional 'alignment' attribute from config XML.
		/// Returns true if we want 'normal' left justification.
		/// Returns false if we want right-ish justification.
		/// </summary>
		/// <param name="choice"></param>
		/// <returns></returns>
		private static bool GetOptionalMenuAlignment(ChoiceRelatedClass choice)
		{
			if (choice == null || choice.ConfigurationNode == null)
				return true;
			var alignmentAttr = XmlUtils.GetOptionalAttributeValue(choice.ConfigurationNode, "alignment", "left");
			return alignmentAttr == "left";
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
		public void  Init (IImageCollection smallImages, IUIMenuAdapter menuBarAdapter, Mediator mediator)
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
		/// Mediator message handling Priority
		/// </summary>
		public int Priority
		{
			get
			{
				return (int)ColleaguePriority.Medium;
			}
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
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			this.m_panelMain = new PanelEx();
			this.panelEx2 = new PanelEx();
			this.panelEx2.Text = "Test";
			this.panelEmbedded = new PanelEx();
			this.spacer = new PanelEx();
			this.testPanel = new PanelEx();
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
			this.m_panelMain.Font = new System.Drawing.Font("Tahoma", 13F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.m_panelMain.Location = new System.Drawing.Point(0, 0);
			this.m_panelMain.Name = "m_panelMain";
			this.m_panelMain.Size = new System.Drawing.Size(704, 24);
			this.m_panelMain.TabIndex = 0;

			//
			// panelEx2
			//
			this.panelEx2.Controls.Add(this.panelEmbedded);
			this.panelEx2.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelEx2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.panelEx2.Location = new System.Drawing.Point(465, 2);
			this.panelEx2.Name = "panelEx2";
			this.panelEx2.Size = new System.Drawing.Size(56, 20);
			this.panelEx2.TabIndex = 2;
			this.panelEx2.Text = "Test2";
			//
			// panelEmbedded
			//
			this.panelEmbedded.Location = new System.Drawing.Point(0, 0);
			this.panelEmbedded.Name = "panelEmbedded";
			this.panelEmbedded.Size = new System.Drawing.Size(20, 20);
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
			this.spacer.TabIndex = 1;
			//
			// testPanel
			//
			this.testPanel.Dock = System.Windows.Forms.DockStyle.Right;
			this.testPanel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.testPanel.Location = new System.Drawing.Point(569, 2);
			this.testPanel.Name = "testPanel";
			this.testPanel.Size = new System.Drawing.Size(120, 20);
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
}
