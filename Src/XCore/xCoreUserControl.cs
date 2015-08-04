// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: xCoreUserControl.cs
// Authorship History: Dan Hinton
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Xml;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// This is the base AccessibleObject that is used by controls derived from
	/// XCoreUserControl.
	/// </summary>
	public class XCoreAccessibleObject : Control.ControlAccessibleObject
	{
		IXCoreUserControl m_xcontrol;
		public XCoreAccessibleObject(IXCoreUserControl xcontrol) : base(xcontrol as Control)
		{
			m_xcontrol = xcontrol;
		}

		public override string Name
		{
			get	{ return m_xcontrol.AccName; }
			set	{}	// The name can't be changed here (yet).
		}
	}


	// This is a test to see if the MultiPane can be subclassed further - and eventually
	// all of the classes like it: RecordView, ... can follow and inherit the
	// accessibility functionality much like the Slice class implements it for the
	// controls that are derived from it.
	/// <summary>
	/// This class XCoreUserControl is derived from UserControl.  It was created to
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
	public class XCoreUserControl : UserControl, IFWDisposable, IXCoreUserControl
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

		#region Accessibility functionality for XCoreUserControl derived objects

		protected XmlNode m_configurationParameters;
		protected override AccessibleObject CreateAccessibilityInstance()
		{
			return new XCoreAccessibleObject(this);
		}
		/// <summary>
		/// Override this property to change the default name for a control if none of the looked
		/// up fields are in the configuration parameters.
		/// </summary>
		///
		private string m_AccNameDefault = "XCoreUserControl";
		protected string AccNameDefault
		{
			get { return m_AccNameDefault; }
			set { m_AccNameDefault = value; }
		}

		#region IXCoreUserControl implementation

		/// <summary>
		/// This is the property that return the name to be used by the accessibility object.
		/// </summary>
		public string AccName
		{
			get
			{
				CheckDisposed();

				string name;
				if (this is IxCoreColleague)
				{
					name = XmlUtils.GetOptionalAttributeValue( m_configurationParameters, "persistContext");

					if (name == null || name == "")
						name = XmlUtils.GetOptionalAttributeValue( m_configurationParameters, "id");

					if (name == null || name == "")
						name = XmlUtils.GetOptionalAttributeValue( m_configurationParameters, "label", AccNameDefault);
				}
				else
					name = AccNameDefault;

				return name;
			}
		}

		#endregion IXCoreUserControl implementation

		#endregion Accessibility functionality for XCoreUserControl derived objects

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
			m_configurationParameters = null;
			m_AccNameDefault = null;

			base.Dispose( disposing );
		}

		protected void TriggerMessageBoxIfAppropriate()
		{
			string msgBoxTrigger = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "msgBoxTrigger","");
			if(msgBoxTrigger !="")
				XCore.XMessageBoxExManager.Trigger(msgBoxTrigger);
		}

		#region Component Designer generated code

//		private System.ComponentModel.Container components = null;
//		/// <summary>
//		/// Required method for Designer support - do not modify
//		/// the contents of this method with the code editor.
//		/// </summary>
//		private void InitializeComponent()
//		{
//			components = new System.ComponentModel.Container();
//		}
		#endregion
	}
}
