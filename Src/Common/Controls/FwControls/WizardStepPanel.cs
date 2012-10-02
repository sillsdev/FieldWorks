// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WizardStepPanel.cs
// Responsibility: DavidO
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Drawing;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for WizardStepPanel.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class WizardStepPanel : UserControl, IFWDisposable, IWizardPaintPanSteps
	{
		private const int kdxpStepListSpacing = 8;
		private const int kdypStepListSpacing = 10;
		private const int kdxpStepSquareWidth = 14;
		private const int kdypStepSquareHeight = 14;
		private Color kclrPendingStep = Color.LightGray;
		private Color kclrCompletedStep = Color.Gray;
		private Color kclrCurrentStep = Color.LightGreen;
		private Color kclrLastStep = Color.Red;

		private Font m_font;
		private Color m_foreColor = Color.White;
		private string[] m_stepText = new string[] {
			FwControls.kstidStep1, FwControls.kstidStep2, FwControls.kstidStep3
		};
		private int m_currentStepNumber = 0;

		/// <summary></summary>
		protected System.Windows.Forms.Panel panSteps;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		#region Contructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="WizardStepPanel"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public WizardStepPanel()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			m_font = (Font)SystemInformation.MenuFont.Clone();
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
				m_font.Dispose();
			}
			m_font = null;
			m_stepText = null;

			base.Dispose( disposing );
		}
		#endregion

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WizardStepPanel));
			this.panSteps = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			//
			// panSteps
			//
			this.panSteps.BackColor = System.Drawing.Color.Black;
			this.panSteps.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.panSteps, "panSteps");
			this.panSteps.Name = "panSteps";
			this.panSteps.Paint += new System.Windows.Forms.PaintEventHandler(this.panSteps_Paint);
			//
			// WizardStepPanel
			//
			this.Controls.Add(this.panSteps);
			this.Name = "WizardStepPanel";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);

		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the font of the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Font Font
		{
			get
			{
				CheckDisposed();

				return m_font;
			}
			set
			{
				CheckDisposed();

				if (m_font != null)
					m_font.Dispose();

				if (value != null)
				{
					m_font = value;
					panSteps.Invalidate();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the foreground color of the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Color ForeColor
		{
			get
			{
				CheckDisposed();

				return m_foreColor;
			}
			set
			{
				CheckDisposed();

				if (value != Color.Empty)
				{
					m_foreColor = value;
					panSteps.Invalidate();
				}
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the current step number. (Steps numbers are zero-based.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(false)]
		public int CurrentStepNumber
		{
			get
			{
				CheckDisposed();

				return m_currentStepNumber;
			}
			set
			{
				CheckDisposed();

				if (value >= m_stepText.Length)
					return;

				m_currentStepNumber = value;
				panSteps.Invalidate();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the array of strings used in the steps panel. The number of
		/// elements in this array determines the number of wizard steps.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Browsable(true)]
		[Category("Misc")]
		[Localizable(true)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public string[] StepText
		{
			get
			{
				CheckDisposed();

				return m_stepText;
			}
			set
			{
				CheckDisposed();

				m_stepText = value;
				panSteps.Invalidate();
			}
		}

		#endregion

		#region IWizardPaintPanSteps implementation
		string[] IWizardPaintPanSteps.StepNames
		{
			get { return m_stepText; }
		}

		int IWizardPaintPanSteps.LastStepNumber
		{
			get { return m_stepText.Length - 1; }
		}

		int IWizardPaintPanSteps.CurrentStepNumber
		{
			get { return m_currentStepNumber; }
		}

		Font IWizardPaintPanSteps.StepsFont
		{
			get { return m_font; }
		}

		Color IWizardPaintPanSteps.TextColor
		{
			get { return m_foreColor; }
		}

		Panel IWizardPaintPanSteps.PanSteps
		{
			get { return panSteps; }
		}
		#endregion

		#region Events
		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void panSteps_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
		{
			WizardDialog.PanStepsPaint(this, e);
		}

		#endregion
	}
}
