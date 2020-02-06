// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils.MessageBoxEx;

namespace LanguageExplorer.Controls
{
	// This is a test to see if the MultiPane can be subclassed further - and eventually
	// all of the classes like it: RecordView, ... can follow and inherit the
	// accessibility functionality much like the Slice class implements it for the
	// controls that are derived from it.
	/// <summary>
	/// This class MainUserControl is derived from UserControl.  It was created to
	/// serve as a central place for functionality, currently Accessibility related,
	/// for the many classes that extend UserControl.  Using this class takes just a
	/// minute to implement, but it provides some default Accessibility functionality.
	///
	/// This class declares an XmlNode m_configurationParameters as protected to be
	/// shared with derived classes (and set by the derived class).
	///
	/// To set the default Accessibility name, set the AccNameDefault property to the
	/// name of the derived class.
	/// </summary>
	public class MainUserControl : UserControl, IMainUserControl
	{
		#region Accessibility functionality for MainUserControl derived objects

		/// <summary>
		/// Create the FwAccessibleObject.
		/// </summary>
		protected override AccessibleObject CreateAccessibilityInstance()
		{
			return new FwAccessibleObject(this);
		}

		/// <summary>
		/// Set/Set the default accessible name.
		/// </summary>
		protected string AccNameDefault { get; set; } = "MainUserControl";

		#region IMainUserControl implementation

		/// <summary>
		/// This is the property that return the name to be used by the accessibility object.
		/// </summary>
		public string AccName
		{
			get
			{
				return AccNameDefault;
			}
			set
			{
				AccNameDefault = value;
			}
		}

		#endregion IMainUserControl implementation

		#endregion Accessibility functionality for MainUserControl derived objects

		/// <summary>
		/// Sub-classes can override, if they have an interest in (un-)registering any main meus or tool bar buttons.
		/// </summary>
		internal virtual void RegisterUiWidgets(bool shouldRegister)
		{
		}

		/// <summary>
		/// Dispose the object.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}
			AccNameDefault = null;

			base.Dispose(disposing);
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
			{
				MessageBoxExManager.Trigger(MessageBoxTrigger);
			}
		}

		/// <summary>
		/// This is the base AccessibleObject that is used by controls derived from
		/// MainUserControl.
		/// </summary>
		private sealed class FwAccessibleObject : ControlAccessibleObject
		{
			private readonly IMainUserControl m_mainUserControl;

			/// <summary />
			public FwAccessibleObject(IMainUserControl mainUserControl)
				: base((Control)mainUserControl)
			{
				m_mainUserControl = mainUserControl;
			}

			/// <summary>
			/// Get/Set the FwAccessibleObject's name.
			/// </summary>
			public override string Name
			{
				get { return m_mainUserControl.AccName; }
				set { m_mainUserControl.AccName = value; }
			}
		}
	}
}