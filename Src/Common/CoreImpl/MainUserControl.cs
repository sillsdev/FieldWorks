// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.CoreImpl.MessageBoxEx;
using SIL.Utils;

namespace SIL.CoreImpl
{
	// This is a test to see if the MultiPane can be subclassed further - and eventually
	// all of the classes like it: RecordView, ... can follow and inherit the
	// accessibility functionality much like the Slice class implements it for the
	// controls that are derived from it.
	/// <summary>
	/// This class MainUserControl is derived from UserControl.  It was created to
	/// serve as a central place for functionality, currently Accessibility related,
	/// for the many classes that exten UserControl.  Using this class takes just a
	/// minute to implement, but it provides some default Accessibility functionality.
	///
	/// This class declares an XmlNode m_configurationParameters as protected to be
	/// shared with derived classes (and set by the derived class).
	///
	/// To set the default Accessibility name, set the AccNameDefault property to the
	/// name of the derived class.
	///
	/// </summary>
	public class MainUserControl : UserControl, IFWDisposable, IMainUserControl
	{
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

		#region Accessibility functionality for MainUserControl derived objects

		/// <summary>
		/// Create the FwAccessibleObject.
		/// </summary>
		/// <returns>Return the new FwAccessibleObject instance.</returns>
		protected override AccessibleObject CreateAccessibilityInstance()
		{
			return new FwAccessibleObject(this);
		}
		/// <summary>
		/// Override this property to change the default name for a control if none of the looked
		/// up fields are in the configuration parameters.
		/// </summary>
		///
		private string m_accNameDefault = "MainUserControl";
		/// <summary>
		/// Set/Set the default accessible name.
		/// </summary>
		protected string AccNameDefault
		{
			get { return m_accNameDefault; }
			set { m_accNameDefault = value; }
		}

		#region IMainUserControl implementation

		/// <summary>
		/// This is the property that return the name to be used by the accessibility object.
		/// </summary>
		public string AccName
		{
			get
			{
				CheckDisposed();
				return AccNameDefault;
			}
			set
			{
				CheckDisposed();
				AccNameDefault = value;
			}
		}

		#endregion IMainUserControl implementation

		#endregion Accessibility functionality for MainUserControl derived objects

		/// <summary>
		/// Dispose the object.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
//				if(components != null)
//				{
//					components.Dispose();
//				}
			}
			m_accNameDefault = null;

			base.Dispose( disposing );
		}

		/// <summary>
		/// Get/set string that will trigger a message box to show.
		/// </summary>
		public string MessageBoxTrigger { get; set; }

		/// <summary>
		/// Show the message box, if needed.
		/// </summary>
		protected void TriggerMessageBoxIfAppropriate()
		{
			if (!string.IsNullOrWhiteSpace(MessageBoxTrigger))
				MessageBoxExManager.Trigger(MessageBoxTrigger);
		}
	}
}
