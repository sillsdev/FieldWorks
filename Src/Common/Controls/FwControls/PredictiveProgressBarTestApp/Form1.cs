using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using  SIL.FieldWorks.Common.Controls;

namespace ProgressBarTest
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class Form1 : System.Windows.Forms.Form
	{
		//private StatusBarProgressPanel statusBarProgressPanel1;
		private StatusBarProgressPanel statusBarProgressPanel2;
		private System.Windows.Forms.StatusBar statusBar1;
		private System.Windows.Forms.Timer timer1;
		private System.ComponentModel.IContainer components;
		private System.Windows.Forms.Button button2;

		//private ProgressState m_state;
		private System.Windows.Forms.Button button3;
		private System.Windows.Forms.Button btnMilestoneTest;
		private System.Windows.Forms.Button button1;
		private ProgressState m_state;

		public Form1()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

//			this.statusBar1.Panels.Add(this.statusBarProgressPanel1);
			statusBarProgressPanel2 = new StatusBarProgressPanel(this.statusBar1);
			//((System.ComponentModel.ISupportInitialize)(this.statusBarProgressPanel2)).BeginInit();
			this.statusBar1.Panels.Add(this.statusBarProgressPanel2);
			statusBarProgressPanel2.Width=200;

		//	statusBarProgressPanel2.Parent.Refresh();
			//((System.ComponentModel.ISupportInitialize)(this.statusBarProgressPanel2)).EndInit();
			this.statusBar1.Refresh();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(m_state!=null)
					m_state.Dispose();

				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.statusBar1 = new System.Windows.Forms.StatusBar();
			this.timer1 = new System.Windows.Forms.Timer(this.components);
			this.button2 = new System.Windows.Forms.Button();
			this.button3 = new System.Windows.Forms.Button();
			this.btnMilestoneTest = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// statusBar1
			//
			this.statusBar1.Location = new System.Drawing.Point(0, 320);
			this.statusBar1.Name = "statusBar1";
			this.statusBar1.ShowPanels = true;
			this.statusBar1.Size = new System.Drawing.Size(584, 22);
			this.statusBar1.TabIndex = 0;
			this.statusBar1.Text = "statusBar1";
			//
			// timer1
			//
			this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
			//
			// button2
			//
			this.button2.Location = new System.Drawing.Point(336, 200);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(136, 23);
			this.button2.TabIndex = 2;
			this.button2.Text = "25 75 100";
			this.button2.Click += new System.EventHandler(this.button2_Click);
			//
			// button3
			//
			this.button3.Location = new System.Drawing.Point(336, 144);
			this.button3.Name = "button3";
			this.button3.Size = new System.Drawing.Size(128, 23);
			this.button3.TabIndex = 3;
			this.button3.Text = "20 40 60 80 100";
			this.button3.Click += new System.EventHandler(this.button3_Click);
			//
			// btnMilestoneTest
			//
			this.btnMilestoneTest.Location = new System.Drawing.Point(48, 64);
			this.btnMilestoneTest.Name = "btnMilestoneTest";
			this.btnMilestoneTest.Size = new System.Drawing.Size(112, 23);
			this.btnMilestoneTest.TabIndex = 4;
			this.btnMilestoneTest.Text = "milestoneTest";
			this.btnMilestoneTest.Click += new System.EventHandler(this.btnMilestoneTest_Click);
			//
			// button1
			//
			this.button1.Location = new System.Drawing.Point(200, 64);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(112, 23);
			this.button1.TabIndex = 4;
			this.button1.Text = "milestone (1 too many)";
			this.button1.Click += new System.EventHandler(this.button1_Click);
			//
			// Form1
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(584, 342);
			this.Controls.Add(this.btnMilestoneTest);
			this.Controls.Add(this.button3);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.statusBar1);
			this.Controls.Add(this.button1);
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new Form1());
		}

		private void timer1_Tick(object sender, System.EventArgs e)
		{

		}

		private void Form1_Load(object sender, System.EventArgs e)
		{
//			m_state = new ProgressState(statusBarProgressPanel1);
		}



		private void button2_Click(object sender, System.EventArgs e)
		{
			string label = this.button2.Text;
			string taskLabel="Test"+label;
			m_state = new PredictiveProgressState(statusBarProgressPanel2, taskLabel);

			using(m_state)
			{
				this.button2.Text="Working";
				Do(1,"one");
				Do(2,"two");
				Do(1,"three");
				this.button2.Text=label;
			}
		}

		private void Do(double secs,string label)
		{
			m_state.SetMilestone(label);
			int mils = (int)(secs*1000);
			int step = 100;
			for(int m=0; m<mils; m+= step)
			{
				m_state.Breath();
				System.Threading.Thread.Sleep(step);
			}
		}

		private void button3_Click(object sender, System.EventArgs e)
		{
			string label = this.button3.Text;
			string taskLabel="Test"+label;
			m_state = new PredictiveProgressState(statusBarProgressPanel2, taskLabel);

			using(m_state)
			{
				this.button3.Text="Working";
				Do(.5,"a");
				Do(.5,"b");
				Do(.5,"c");
				Do(.5,"d");
				Do(.5,"e");
				this.button3.Text=label;
			}

		}

		private void btnMilestoneTest_Click(object sender, System.EventArgs e)
		{
			string label = this.btnMilestoneTest.Text;
			string taskLabel="Test"+label;
			m_state = new MilestoneProgressState(statusBarProgressPanel2);
			MilestoneProgressState s = (MilestoneProgressState) m_state;
			s.AddMilestone(1);
			s.AddMilestone(1);
			s.AddMilestone(1);

			using(m_state)
			{
				this.button3.Text="Working";
				Do(.5,"a");
				Do(.5,"b");
				Do(.5,"c");
				this.button3.Text=label;
			}

		}

		private void button1_Click(object sender, System.EventArgs e)
		{
			string label = this.btnMilestoneTest.Text;
			string taskLabel="Test"+label;
			m_state = new MilestoneProgressState(statusBarProgressPanel2);
			MilestoneProgressState s = (MilestoneProgressState) m_state;
			s.AddMilestone(1);
			s.AddMilestone(1);
			s.AddMilestone(1);
			//test what happens when we have more milestones and we signed up for
			using(m_state)
			{
				this.button3.Text="Working";
				Do(.5,"a");
				Do(.5,"b");
				Do(.5,"c");
				Do(.5,"extra");
				this.button3.Text=label;
			}
		}

	}
}
