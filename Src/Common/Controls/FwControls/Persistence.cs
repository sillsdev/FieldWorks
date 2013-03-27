// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: Persistence.cs
// Responsibility: RonM
// Last reviewed:
//
// <remarks>
// Implementation of Persistence
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;
using Microsoft.Win32;

using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Controls
{
	#region ISettings
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The ISettings interface should be implemented by forms, controls, and apps that wish to
	/// participate in the FW standard way of saving settings in the registry. Forms can have
	/// their size, position, and window state information persisted merely by instantiating the
	/// <see cref="Persistence"/> class, and calling m_persistence.LoadWindowPosition from
	/// an override of OnLayout(). However, forms which have controls that must be
	/// persisted (which is probably almost any persisted form) must implement ISettings as
	/// well. ISettings should also be implemented by each application, which should at least
	/// override SettingsKey to return the base key for the appliation. (An app's implementation
	/// of <see cref="SaveSettingsNow"/> will normally be a no-op.)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ISettings
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a key in the registry where <see cref="Persistence"/> should store settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		RegistryKey SettingsKey { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement this method to save the persistent settings for your class. Normally, this
		/// should also result in recursively calling SaveSettingsNow for all children which
		/// also implement ISettings.
		/// </summary>
		/// <remarks>Note: A form's implementation of <see cref="SaveSettingsNow"/> normally
		/// calls <see cref="Persistence.SaveSettingsNow"/> (after optionally saving any
		/// class-specific properties).</remarks>
		/// ------------------------------------------------------------------------------------
		void SaveSettingsNow();
	}
	#endregion

	#region Persistence class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The Persistence class is a control designed to be dropped onto forms to persist certain
	/// standard settings (optional) and to provide methods which enable forms to implement
	/// ISettings in a standard way.
	/// </summary>
	/// <remarks>
	/// Persistence class provides methods to load and save the owner's window position
	/// and the state of the window. When a persistence object is dropped on a form, Event
	/// handlers (OnLoadSettings, etc) are hooked up so that settings are loaded and
	/// saved automatically when the owner's is initialized or its window is created or
	/// destroyed.
	/// In addition, SaveSettingsNow can be called at other times, when needed.
	/// Also, any control or form class can instantiate a persistence object and have
	/// class-specific settings loaded and saved by handling Persistence events LoadSettings
	/// and SaveSettngs.
	/// To get a window's position persisted, you should also call LoadWindowPosition() from
	/// an override of OnLayout(). It will only take effect the first time it is called
	/// (after the parent's InitializeComponent is complete, as detected by having it
	/// call EndInit()).
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	[ToolboxBitmap(typeof(Persistence), "resources.Persistence.bmp")]
	[Designer("SIL.FieldWorks.Common.Controls.Design.PersistenceDesigner")]
	public class Persistence : Component, ISupportInitialize, IFWDisposable
	{
		#region Variables and declarations
		/// <summary></summary>
		public delegate void Settings(RegistryKey key);
		private const string sWindowState = "WindowState";
		private static readonly RegistryKey s_defaultKeyPath = FwRegistryHelper.FieldWorksRegistryKey;
		private Control m_parent;
		private bool m_fInInit;
		private bool m_fLoadSettingsPending;
		private bool m_fSaveWindowSettings = true;
		private int m_normalTop;
		private int m_normalLeft;
		private int m_normalWidth;
		private int m_normalHeight;
		// Set true, so LoadWindowPosition can be called in every OnLayout call,
		// but only take effect once.
		private bool m_fHaveLoadedPosition;

		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <c>Persistence</c> class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Persistence()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <c>Persistence</c> class
		/// </summary>
		/// <param name="container"></param>
		/// ------------------------------------------------------------------------------------
		public Persistence(System.ComponentModel.IContainer container)
		{
			// Required for Windows.Forms Class Composition Designer support
			container.Add(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <c>Persistence</c> class
		/// </summary>
		/// <param name="container"></param>
		/// <param name="parent"></param>
		/// ------------------------------------------------------------------------------------
		public Persistence(System.ComponentModel.IContainer container, Control parent)
		{
			// Required for Windows.Forms Class Composition Designer support
			container.Add(this);

			Parent = parent;
			m_fSaveWindowSettings = parent is Form;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <c>Persistence</c> class
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="loadFunct"></param>
		/// <param name="saveFunct"></param>
		/// ------------------------------------------------------------------------------------
		public Persistence(Control parent, Settings loadFunct, Settings saveFunct)
		{
			Parent = parent;
			m_fSaveWindowSettings = parent is Form;
			LoadSettings = new Settings(loadFunct);
			SaveSettings = new Settings(saveFunct);
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
		///
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		private bool m_isDisposed = false;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases the resources used by <see cref="Persistence"/>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				if (m_parent != null)
				{
					if (m_parent is Form)
						((Form)m_parent).Closed -= new EventHandler(this.OnSaveSettings);
					else
						m_parent.HandleDestroyed -= new System.EventHandler(this.OnSaveSettings);
					m_parent.HandleCreated -= new System.EventHandler(this.OnLoadSettings);
					m_parent.Move -= new System.EventHandler(this.OnMoveResize);
					m_parent.Resize -= new System.EventHandler(this.OnMoveResize);
				}
			}
			m_parent = null;

			base.Dispose(disposing);

			m_isDisposed = true;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Registry key where settings are saved
		/// Normally this is the key provided by our parent form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();

				RegistryKey key = null;
				if (Parent is ISettings)
					key = ((ISettings)Parent).SettingsKey;

				return key ?? s_defaultKeyPath;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the parent control whose settings are being persisted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public Control Parent
		{
			get
			{
				CheckDisposed();

				return m_parent;
			}
			set
			{
				CheckDisposed();

				m_parent = value;

				if (m_parent != null)
				{
					if (m_parent is Form)
						((Form)m_parent).Closed += new EventHandler(this.OnSaveSettings);
					else
						m_parent.HandleDestroyed += new System.EventHandler(this.OnSaveSettings);

					m_parent.HandleCreated += new System.EventHandler(this.OnLoadSettings);
					m_parent.Move += new System.EventHandler(this.OnMoveResize);
					m_parent.Resize += new System.EventHandler(this.OnMoveResize);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the flag indicating if window settings (position, size and state)
		/// should be saved for the parent control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Determines if window settings should be saved for the parent component")]
		public bool EnableSaveWindowSettings
		{
			get
			{
				CheckDisposed();

				return m_fSaveWindowSettings;
			}
			set
			{
				CheckDisposed();

				m_fSaveWindowSettings = value;
			}
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a Rectangle representing the position and size of the window in its
		/// normal (non-minimized, non-maximized) state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Rectangle NormalStateDesktopBounds
		{
			get
			{
				CheckDisposed();

				return new Rectangle(m_normalLeft, m_normalTop, m_normalWidth,
					m_normalHeight);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if the parent control is in design mode
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public bool GlobalDesignMode
		{
			get
			{
				CheckDisposed();

				if (Parent == null)
					return true;

				Type t = typeof(System.ComponentModel.Component);

				return (bool)t.InvokeMember("DesignMode",
					BindingFlags.DeclaredOnly | BindingFlags.NonPublic |
					BindingFlags.GetProperty | BindingFlags.Instance, null, Parent, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event that is raised when the control is loaded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("Occurs when the control gets loaded")]
		public event Settings LoadSettings;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event that is raised when the control is closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Description("Occurs when the control is closed")]
		public event Settings SaveSettings;

		#endregion

		#region Event handling functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tasks needing to be done when Window is being closed:
		///		Save window position.
		///		Save window state.
		///		Save settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnSaveSettings(object obj, System.EventArgs e)
		{
			if (!GlobalDesignMode)
			{
				// Get the SettingsKey
				RegistryKey key = SettingsKey;

				if (EnableSaveWindowSettings)
				{
					SaveWindowPosition(key);

					if (Parent is Form)
						SaveWindowState(key);
				}
				if (SaveSettings != null)
					SaveSettings(key);
			}
		}

		///***********************************************************************************
		/// <summary>
		/// Tasks needing to be done when Window is being created:
		///		Load window position.
		///		Load settings.
		/// </summary>
		/// <remark>Note that the window state must be loaded immediately after window
		/// initialization, not when the window handle is created and this OnLoadSettings
		/// gets run. Therefore the ISupportInitialize EndInit() method loads the window
		/// state. However, the window size must be set here otherwise child controls
		/// won't resize properly.</remark>
		///***********************************************************************************
		private void OnLoadSettings(object obj, System.EventArgs e)
		{
			if (!GlobalDesignMode && obj is Control && !((Control)obj).Disposing)
			{
				if (m_fInInit)
					m_fLoadSettingsPending = true;
				else
				{
					Parent.SuspendLayout();

					if (m_normalWidth > 0 && m_normalHeight > 0)
						Parent.Size = new Size(m_normalWidth, m_normalHeight);

					if (LoadSettings != null)
						LoadSettings(SettingsKey);

					Parent.ResumeLayout();
				}
			}
		}

		///***********************************************************************************
		/// <summary>
		/// Tasks needing to be done when Window is moved:
		///		Save window position if the Window State is "Normal".
		/// </summary>
		///***********************************************************************************
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		private void OnMoveResize(object sender, System.EventArgs e)
		{
			// Save position
			if (!GlobalDesignMode && Parent.FindForm() != null)
			{
				if (Parent.FindForm().WindowState == FormWindowState.Normal)
				{
					m_normalLeft = (Parent is Form ?
						((Form)Parent).DesktopBounds.Left : Parent.Left);

					m_normalTop = (Parent is Form ?
						((Form)Parent).DesktopBounds.Top : Parent.Top);

					m_normalWidth = (Parent is Form ?
						((Form)Parent).DesktopBounds.Width : Parent.Width);

					m_normalHeight = (Parent is Form ?
						((Form)Parent).DesktopBounds.Height : Parent.Height);
				}
			}
		}

		#endregion

		#region Load/Save methods
		///***********************************************************************************
		/// <summary>
		/// Load the top, left, width, and height of the window from the registry, use default
		/// application parameters if not present in registry. This should be called from
		/// OnLayout; it takes effect in the first call AFTER EndInit.
		/// </summary>
		///***********************************************************************************
		public void LoadWindowPosition()
		{
			if (m_fInInit)
				return;
			if (m_fHaveLoadedPosition)
				return;
			m_fHaveLoadedPosition = true;
			CheckDisposed();

			RegistryKey key = SettingsKey;

			int iLeft = (int)key.GetValue(Parent.GetType().Name + "Left", (Parent is Form ?
				((Form)Parent).DesktopBounds.Left : Parent.Left));

			int iTop = (int)key.GetValue(Parent.GetType().Name + "Top", (Parent is Form ?
				((Form)Parent).DesktopBounds.Top : Parent.Top));

			int iWidth = (int)key.GetValue(Parent.GetType().Name + "Width", (Parent is Form ?
				((Form)Parent).DesktopBounds.Width : Parent.Width));

			int iHeight = (int)key.GetValue(Parent.GetType().Name + "Height", (Parent is Form ?
				((Form)Parent).DesktopBounds.Height : Parent.Height));

			Rectangle rect = new Rectangle(iLeft, iTop, iWidth, iHeight);

			if (Parent is Form)
			{
				Form parent = Parent as Form;
				ScreenUtils.EnsureVisibleRect(ref rect);
				if (rect != parent.DesktopBounds)
				{
					// this means we loaded values from the registry - or the form is to big
					parent.StartPosition = FormStartPosition.Manual;
				}
				parent.DesktopLocation = new Point(rect.X, rect.Y);

				// we can't set the width and height on the form yet - if we do it won't
				// resize our child controls
				m_normalLeft = rect.X;
				m_normalTop = rect.Y;
				parent.Width = m_normalWidth = rect.Width;
				parent.Height = m_normalHeight = rect.Height;
				parent.WindowState = (FormWindowState)SettingsKey.GetValue(
					parent.GetType().Name + sWindowState, parent.WindowState);
			}
			else
			{
				// Set parent dimensions based upon possible adjustments in EnsureVisibleRect
				Parent.Top = rect.Top;
				Parent.Left = rect.Left;
				Parent.Width = rect.Width;
				Parent.Height = rect.Height;
			}
		}

		///***********************************************************************************
		/// <summary>
		///	Save the top, left, width, and height of the window to the registry.
		/// </summary>
		///
		/// <param name='key'>The Registry Key.</param>
		///***********************************************************************************
		public void SaveWindowPosition(RegistryKey key)
		{
			CheckDisposed();

			if ((m_normalWidth == 0 || m_normalHeight == 0) && Parent != null)
			{
				// make sure that we have a width and height set
				m_normalLeft = (Parent is Form ?
					((Form)Parent).DesktopBounds.Top : Parent.Left);

				m_normalTop = (Parent is Form ?
					((Form)Parent).DesktopBounds.Top : Parent.Top);

				m_normalWidth = (Parent is Form ?
					((Form)Parent).DesktopBounds.Width : Parent.Width);

				m_normalHeight = (Parent is Form ?
					((Form)Parent).DesktopBounds.Height : Parent.Height);
			}

			key.SetValue(Parent.GetType().Name + "Top", m_normalTop);
			key.SetValue(Parent.GetType().Name + "Left", m_normalLeft);
			key.SetValue(Parent.GetType().Name + "Width", m_normalWidth);
			key.SetValue(Parent.GetType().Name + "Height", m_normalHeight);
		}

		///***********************************************************************************
		/// <summary>
		///	Save the window state to the registry. Do not allow the window state to be saved
		///	as "Minimized", instead save as "Normal".
		/// </summary>
		///
		/// <param name='key'>The Registry Key.</param>
		///***********************************************************************************
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FindForm() returns a reference")]
		public void SaveWindowState(RegistryKey key)
		{
			CheckDisposed();

			if(Parent.FindForm().WindowState == FormWindowState.Minimized)
				key.SetValue(Parent.GetType().Name + sWindowState, (int)FormWindowState.Normal);
			else
				key.SetValue(Parent.GetType().Name + sWindowState, (int)Parent.FindForm().WindowState);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initiate saving all the settings for the specified control, and for all of the
		/// control's owned controls which implement ISettings.
		/// </summary>
		/// <remarks>
		/// Note: A parent's ISettings::SaveSettingsNow should call this method
		/// (Persistence::SaveSettingsNow).
		/// </remarks>
		///
		/// <param name="ctrl">Control or form whose settings should be saved.</param>
		/// -----------------------------------------------------------------------------------
		public void SaveSettingsNow(Control ctrl)
		{
			CheckDisposed();

			OnSaveSettings(ctrl, null);

			foreach (Control control in ctrl.Controls)
			{
				if (control is ISettings)
					((ISettings)control).SaveSettingsNow();
			}
		}
		#endregion

		#region Additional serialization methods
		///***********************************************************************************
		/// <summary>
		/// Serializes to binary an object.
		/// </summary>
		///
		/// <param name='obj'>The object to be serialized.</param>
		/// <returns></returns>
		///***********************************************************************************
		public static MemoryStream SerializeToBinary(object obj)
		{
			MemoryStream stream = new MemoryStream();
			//Construct a serialization formatter
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(stream, obj);
			return stream;
		}

		///***********************************************************************************
		/// <summary>
		/// Deserializes an object from binary.
		/// </summary>
		///
		/// <param name="stream">The stream from which to deserialize</param>
		/// <returns>The deserialized object.</returns>
		///***********************************************************************************
		public static Object DeserializeFromBinary(Stream stream)
		{
			//Construct a serialization formatter
			BinaryFormatter formatter = new BinaryFormatter();
			return (formatter.Deserialize(stream));
		}
		#endregion

		#region ISupportInitialize Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required to implement ISupportInitialize.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void BeginInit()
		{
			CheckDisposed();

			m_fInInit = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When all properties have been initialized, this will be called. This is where we
		/// can load the window state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual void EndInit()
		{
			CheckDisposed();

			m_fInInit = false;
			if (m_fLoadSettingsPending)
				OnLoadSettings(Parent, EventArgs.Empty);
			m_fLoadSettingsPending = false;
		}
		#endregion
	}
	#endregion
}
