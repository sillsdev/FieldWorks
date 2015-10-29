// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// This class represents a rule insertion control. A rule insertion control
	/// consists of a set of hotlinks that are used to insert various rule items.
	/// Rule formula controls provide information about which type of items it is
	/// interested in inserting and in what context the hotlinks should be displayed.
	/// It provides an <c>Insert</c> event that indicates when a user has attempted
	/// to insert an item.
	/// </summary>
	public class InsertionControl : UserControl, IFWDisposable
	{
		public event EventHandler<InsertEventArgs> Insert;

		private class GrowLabel : Label
		{
			private bool m_growing;

			public GrowLabel()
			{
				AutoSize = false;
			}

			private void ResizeLabel()
			{
				if (m_growing) return;
				try
				{
					m_growing = true;
					var sz = new Size(Width, Int32.MaxValue);
					sz = TextRenderer.MeasureText(Text, Font, sz, TextFormatFlags.WordBreak);
					// The mono implementation chops off the bottom line of the display (FWNX-752).
					if (MiscUtils.IsMono)
						Height = sz.Height + 7;
					else
						Height = sz.Height;
				}
				finally
				{
					m_growing = false;
				}
			}
			protected override void OnTextChanged(EventArgs e)
			{
				base.OnTextChanged(e);
				ResizeLabel();
			}
			protected override void OnFontChanged(EventArgs e)
			{
				base.OnFontChanged(e);
				ResizeLabel();
			}
			protected override void OnSizeChanged(EventArgs e)
			{
				base.OnSizeChanged(e);
				ResizeLabel();
			}
		}

		private Panel m_labelPanel;
		private FlowLayoutPanel m_insertPanel;
		private Label m_insertLabel;

		private List<Tuple<object, Func<object, bool>, Func<IEnumerable<object>>>> m_options;
		private Func<string> m_noOptsMsg;
		private int m_prevWidth;
		private Label m_msgLabel;

		public InsertionControl()
		{
			m_options = new List<Tuple<object, Func<object, bool>, Func<IEnumerable<object>>>>();
			InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the no options message delegate. This is called to retrieve the appropriate no options
		/// message.
		/// </summary>
		/// <value>The no options message delegate.</value>
		public Func<string> NoOptionsMessage
		{
			get
			{
				CheckDisposed();
				return m_noOptsMsg;
			}

			set
			{
				CheckDisposed();
				m_noOptsMsg = value;
			}
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
			}

			m_options = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Adds an insertion option. A predicate can be provided to determine in what contexts
		/// this insertion option can be displayed.
		/// </summary>
		/// <param name="option"></param>
		/// <param name="shouldDisplay">The should display predicate.</param>
		public void AddOption(object option, Func<object, bool> shouldDisplay)
		{
			CheckDisposed();

			m_options.Add(Tuple.Create(option, shouldDisplay, (Func<IEnumerable<object>>) null));
		}

		/// <summary>
		/// Adds an index option. A predicate can be provided to determine what indices to display.
		/// </summary>
		/// <param name="option"></param>
		/// <param name="shouldDisplay">The should display predicate.</param>
		/// <param name="displaySuboptions"></param>
		public void AddMultiOption(object option, Func<object, bool> shouldDisplay, Func<IEnumerable<object>> displaySuboptions)
		{
			CheckDisposed();

			m_options.Add(Tuple.Create(option, shouldDisplay, displaySuboptions));
		}

		/// <summary>
		/// Updates the options display.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "The controls are always added to m_insertPanel. They should be disposed when m_insertPanel is diposed.")]
		public void UpdateOptionsDisplay()
		{
			CheckDisposed();

			m_insertPanel.SuspendLayout();
			SuspendLayout();
			foreach (Control c in m_insertPanel.Controls.Cast<Control>().ToArray())
				c.Dispose();
			bool displayingOpts = false;
			foreach (Tuple<object, Func<object, bool>, Func<IEnumerable<object>>> opt in m_options)
			{
				if (opt.Item2 == null || opt.Item2(opt.Item1))
				{
					var linkLabel = new LinkLabel {AutoSize = true, Font = new Font(MiscUtils.StandardSansSerif, 10), TabStop = true, VisitedLinkColor = Color.Blue};
					linkLabel.LinkClicked += link_LinkClicked;
					if (opt.Item3 != null)
					{
						object[] options = opt.Item3().ToArray();
						var sb = new StringBuilder();
						for (int i = 0; i < options.Length; i++)
						{
							sb.Append(options[i]);
							if (i < options.Length - 1)
								sb.Append(" ");
						}
						linkLabel.Text = sb.ToString();

						linkLabel.Links.Clear();
						int start = 0;
						foreach (int option in options)
						{
							int len = Convert.ToString(option).Length;
							LinkLabel.Link link = linkLabel.Links.Add(start, len, opt.Item1);
							// use the tag property to store the index for this link
							link.Tag = option;
							start += len + 1;
						}
					}
					else
					{
						linkLabel.Text = opt.Item1.ToString();
						linkLabel.Links[0].LinkData = opt.Item1;
					}

					m_insertPanel.Controls.Add(linkLabel);
					displayingOpts = true;
				}
			}

			if (!displayingOpts && m_noOptsMsg != null)
			{
				string text = m_noOptsMsg();
				if (text != null)
				{
					m_msgLabel = new GrowLabel {Font = new Font(MiscUtils.StandardSansSerif, 10), Text = text, Width = m_insertPanel.ClientSize.Width};
					m_insertPanel.Controls.Add(m_msgLabel);
				}
			}
			else
			{
				m_msgLabel = null;
			}

			m_insertPanel.ResumeLayout(false);
			m_insertPanel.PerformLayout();
			ResumeLayout(false);

			Height = m_insertPanel.PreferredSize.Height;
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			if (m_prevWidth != Width)
			{
				if (m_msgLabel != null)
					m_msgLabel.Width = m_insertPanel.ClientSize.Width;
				Height = m_insertPanel.PreferredSize.Height;
				m_prevWidth = Width;
			}
			base.OnSizeChanged(e);
		}

		private void link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Insert(this, new InsertEventArgs(e.Link.LinkData, e.Link.Tag));
		}

		#region Component Designer generated code

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InsertionControl));
			this.m_labelPanel = new System.Windows.Forms.Panel();
			this.m_insertLabel = new System.Windows.Forms.Label();
			this.m_insertPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.m_labelPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_labelPanel
			//
			this.m_labelPanel.Controls.Add(this.m_insertLabel);
			resources.ApplyResources(this.m_labelPanel, "m_labelPanel");
			this.m_labelPanel.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
			this.m_labelPanel.Name = "m_labelPanel";
			//
			// m_insertLabel
			//
			resources.ApplyResources(this.m_insertLabel, "m_insertLabel");
			this.m_insertLabel.Name = "m_insertLabel";
			//
			// m_insertPanel
			//
			resources.ApplyResources(this.m_insertPanel, "m_insertPanel");
			this.m_insertPanel.Name = "m_insertPanel";
			//
			// InsertionControl
			//
			this.Controls.Add(this.m_insertPanel);
			this.Controls.Add(this.m_labelPanel);
			this.Name = "InsertionControl";
			resources.ApplyResources(this, "$this");
			this.m_labelPanel.ResumeLayout(false);
			this.m_labelPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
	}

	/// <summary>
	/// The enumeration of rule insertion types.
	/// </summary>


	public class InsertEventArgs : EventArgs
	{
		public InsertEventArgs(object option, object suboption)
		{
			Option = option;
			Suboption = suboption;
		}

		public object Option { get; private set; }

		public object Suboption { get; private set; }
	}
}
