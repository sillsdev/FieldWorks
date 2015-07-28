// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Resources;
using System.Reflection; // to get Assembly for opening resource manager.
using System.Windows.Forms;
using System.Diagnostics;

using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>
	/// The UpDownMeasureControl is a spinner control that handles measurements (in, cm, pt, etc)
	/// </summary>
	public class UpDownMeasureControl : UpDownBase, IFWDisposable
	{
		#region Member Data
		/// <summary>
		/// Event that indicates when the value has changed.
		/// </summary>
		public event EventHandler Changed;

		private double m_mptValue = 0;
		private MsrSysType m_measureType = MsrSysType.Cm;
		private int m_mptMax = 360000;
		private int m_mptMin = -360000;
		private bool m_preventValidation = false;		// prevent unnecessary recursive validation.
		private bool m_fDisplayAbsoluteValues = false;
		private uint m_measureIncrementFactor = 1;
		private bool m_useVariablePrecision = false;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:UpDownMeasureControl"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UpDownMeasureControl()
		{
			this.Name = "UpDownMeasureControl";
		}
		#endregion

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

		#region Public Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the measurement maximum value in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MeasureMax
		{
			get
			{
				CheckDisposed();
				return m_mptMax;
			}
			set
			{
				CheckDisposed();
				m_mptMax = value;
				if (m_mptMin > m_mptMax)
					m_mptMin = m_mptMax;
				if (m_mptValue > m_mptMax)
					SetMsrValue(m_mptMax);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the measurement minimum value in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MeasureMin
		{
			get
			{
				CheckDisposed();
				return m_mptMin;
			}
			set
			{
				CheckDisposed();
				m_mptMin = value;
				if (m_mptMax < m_mptMin)
					m_mptMax = m_mptMin;
				if (m_mptValue < m_mptMin)
					SetMsrValue(m_mptMin);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/Sets the value in millipoints.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int MeasureValue
		{
			get
			{
				CheckDisposed();
				return (int)Math.Round(m_mptValue);
			}
			set
			{
				CheckDisposed();
				SetMsrValue(value);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the display measurement unit of the value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MsrSysType MeasureType
		{
			get
			{
				CheckDisposed();
				return m_measureType;
			}
			set
			{
				CheckDisposed();

				m_measureType = value;
				UpdateEditText();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to display the value as an absolute value.
		/// This is useful for implementations where this control represents a position and
		/// works in conjunction with a combo box whose value changes based on whether the
		/// measurement is positive or negative.
		/// </summary>
		/// <remarks>See the Position control on the Font tab of the styles dialog for an
		/// example of this.</remarks>
		/// ------------------------------------------------------------------------------------
		public bool DisplayAbsoluteValues
		{
			get { CheckDisposed(); return m_fDisplayAbsoluteValues; }
			set
			{
				CheckDisposed();
				m_fDisplayAbsoluteValues = value;
				UpdateEditText();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the measure increment factor, which is multiplied by the
		/// MeasureIncrement amount (the standard number of units by which the arrows
		/// increment/decrement the value) to determine the actual step size taken in response
		/// to the Up and Down buttons
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public uint MeasureIncrementFactor
		{
			get { CheckDisposed(); return m_measureIncrementFactor; }
			set
			{
				CheckDisposed();
				if (value == 0)
					throw new ArgumentOutOfRangeException("MeasureIncrementFactor", "Value can not be 0.");
				m_measureIncrementFactor = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to use variable precision for formatting the
		/// values. Inches use two decimal places, centimeters one, and all other units display
		/// to the nearest integer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool UseVariablePrecision
		{
			get { CheckDisposed(); return m_useVariablePrecision; }
			set
			{
				CheckDisposed();
				m_useVariablePrecision = value;
				UpdateEditText();
			}
		}
		#endregion

		#region UpDownBase Implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the clicking of the down button on the spin box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void DownButton()
		{
			CheckDisposed();
			NudgeValue(-1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the clicking of the up button on the spin box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void UpButton()
		{
			CheckDisposed();
			NudgeValue(+1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the text displayed in the spin box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void UpdateEditText()
		{
			// Prevent validation while updating the text. Since we know what will be
			// set, there is no need to validate it.
			m_preventValidation = true;

			double valueToDisplay = m_fDisplayAbsoluteValues ? Math.Abs(m_mptValue) : m_mptValue;
			Text = MeasurementUtils.FormatMeasurement(valueToDisplay, m_measureType, UseVariablePrecision);
			m_preventValidation = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Validates the text displayed in the spin box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ValidateEditText()
		{
			if (m_preventValidation)
				return;
			m_preventValidation = true;

			string str = Text;
			if (str == string.Empty && Changed != null)
			{
				// Give our "owner" (or whoever cares) a chance to define what should happen if
				// the user deletes our value (such as resetting our value to some default).
				Changed(this, EventArgs.Empty);
				return;
			}

			double nVal = MeasurementUtils.ExtractMeasurementInMillipoints(str, m_measureType, m_mptValue);

			if (m_fDisplayAbsoluteValues && m_mptValue < 0)
				nVal *= -1;

			if (nVal != m_mptValue)
			{
				if (!m_fDisplayAbsoluteValues || Math.Sign(nVal) == Math.Sign(m_mptValue))
					SetMsrValue(nVal);
			}
			UpdateEditText();
			m_preventValidation = false;
		}
		#endregion

		#region Private Methods & Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Nudges the value up or down. If the current value is at a multiple of the increment
		/// amount, just increment/decrement it; otherwise bump it up/down to the next multiple.
		/// </summary>
		/// <param name="sign">+1 to increment; -1 to decrement</param>
		/// ------------------------------------------------------------------------------------
		private void NudgeValue(int sign)
		{
			// The text may have been edited before pressing the up button so validate it first
			ValidateEditText();

			double newValue;
			// If the current value is at a multiple of the increment amount, just increment it;
			// otherwise bump it up to the next multiple.
			double incrUnits = Math.Round(m_mptValue / MeasureIncrement, 5); // Round to guard against precision loss.
			if (Math.Floor(incrUnits) == Math.Ceiling(incrUnits))
			{
				newValue = m_mptValue + MeasureIncrement * sign;
			}
			else
			{
				int evenMultiple = (sign < 1) ? (int)Math.Ceiling(m_mptValue / (int)MeasureIncrement) :
					(int)Math.Floor(m_mptValue / (int)MeasureIncrement);
				newValue = (evenMultiple + sign) * MeasureIncrement;
			}

			SetMsrValue(newValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value of the measurement.
		/// </summary>
		/// <param name="mptValue">Value in millipoints</param>
		/// ------------------------------------------------------------------------------------
		private void SetMsrValue(double mptValue)
		{
			// Adjust the value into range
			if (mptValue < m_mptMin)
				mptValue = m_mptMin;
			if (mptValue > m_mptMax)
				mptValue = m_mptMax;

			// If the value has not changed, then do not do anything.
			if (mptValue == m_mptValue && Text != string.Empty)
				return;

			// Set the value, update the text, and send out a notification of the change
			m_mptValue = mptValue;
			UpdateEditText();
			if (Changed != null)
				Changed(this, EventArgs.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the appropriate increment amount for the current measurement type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private double MeasureIncrement
		{
			get
			{
				double incr;
				switch (m_measureType)
				{
					case MsrSysType.Inch:
						incr = MeasurementUtils.GetMpPerUnitFactor(MsrSysType.Inch) / 10.0; break;
					case MsrSysType.Mm:
					case MsrSysType.Cm:
						incr = MeasurementUtils.GetMpPerUnitFactor(MsrSysType.Mm); break;
					default:
					case MsrSysType.Point:
						incr = MeasurementUtils.GetMpPerUnitFactor(MsrSysType.Point); break;
				}
				return m_measureIncrementFactor * incr;
			}
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Gets the default millipoint per unit factor, depending on the default measure type.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		private double DefaultMpPerUnitFactor
//		{
//			get { return MeasurementUtils.GetMpPerUnitFactor(m_measureType); }
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Gets the get measurement units abbreviation for the default measurement type.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		private string MeasureTypeAsString
//		{
//			get { return MeasurementUtils.GetMeasurementUnitsAbbrev(m_measureType); }
//		}
		#endregion

		#region Event Handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When focus is lost, validate the text since it may have been edited
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			ValidateEditText();
		}
		#endregion
	}
}
