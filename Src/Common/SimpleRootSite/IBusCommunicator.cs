#if __MonoCS__
using System;
using System.Windows.Forms;
using IBusDotNet;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>Normal implementation of IIBusCommunicator</summary>
	public class IBusCommunicator : IIBusCommunicator
	{
		#region protected fields

		/// <summary>
		/// stores Dbus Connection to ibus
		/// </summary>
		protected IBusConnection m_connection;

		/// <summary>
		/// the input Context created for associated SimpleRootSite
		/// </summary>
		protected InputContext m_inputContext;

		/// <summary>
		/// Ibus helper class
		/// </summary>
		protected IBusDotNet.InputBusWrapper m_ibus;

		#endregion

		/// <summary>
		/// Create a Connection to Ibus. If successfull Connected property is true.
		/// </summary>
		public IBusCommunicator()
		{
			m_connection = IBusConnectionFactory.Create();

			if (m_connection == null)
				return;

			// Prevent hanging on exit issues caused by missing dispose calls, or strange interaction
			// between ComObjects and managed object.
			Application.ThreadExit += (sender, args) =>
								{
									if (m_connection != null)
										m_connection.Dispose();
								};

			m_ibus = new IBusDotNet.InputBusWrapper(m_connection);
		}

		/// <summary>
		/// Wrap an ibus with protection incase DBus connection is dropped.
		/// </summary>
		protected void ProtectedIBusInvoke(Action action)
		{
			try
			{
				action();
			}
			catch (NDesk.DBus.DBusConectionErrorException error)
			{
				m_ibus = null;
				m_inputContext = null;
				SIL.FieldWorks.Views.GlobalCachedInputContext.Clear();
				NotifyUserOfIBusConnectionDropped();
			}
			catch(System.NullReferenceException)
			{
			}
		}

		/// <summary>
		/// Inform users of IBus problem.
		/// </summary>
		protected void NotifyUserOfIBusConnectionDropped()
		{
			MessageBox.Show(Form.ActiveForm, "Please restart IBus and FieldWorks.", "IBus connection has stopped.");
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~IBusCommunicator()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed
		{
			get;
			private set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				if (m_connection != null)
					m_connection.Dispose();
			}
			m_connection = null;
			IsDisposed = true;
		}
		#endregion

		#region IIBusCommunicator Implementation

		/// <summary>
		/// Returns true if we have a connection to Ibus.
		/// </summary>
		public bool Connected
			{
			get { return m_connection != null; }
		}

		/// <summary>
		/// If we have a valid inputContext Focus it. Also set the GlobalCachedInputContext.
		/// </summary>
		public void FocusIn()
		{
			if (m_inputContext == null)
				return;

			ProtectedIBusInvoke(() => m_inputContext.FocusIn());

			// For performance reasons we store the active inputContext
			SIL.FieldWorks.Views.GlobalCachedInputContext.InputContext = m_inputContext;
		}

		/// <summary>
		/// If we have a valid inputContext call FocusOut ibus method.
		/// </summary>
		public void FocusOut()
		{
			if (m_inputContext == null)
				return;

			ProtectedIBusInvoke(() => m_inputContext.FocusOut());
		}

		/// <summary>
		/// Sets the cursor location of IBus input context.
		/// </summary>
		public void SetCursorLocation(int x, int y, int width, int height)
		{
			if (m_inputContext == null)
				return;

			ProtectedIBusInvoke(() => m_inputContext.SetCursorLocation(x, y, width, height));
		}

		/// <summary>
		/// Send a KeyEvent to ibus.
		/// </summary>
		public bool ProcessKeyEvent(uint keyval, uint keycode, uint state)
		{
			if (m_inputContext == null)
				return false;

			try
			{
			return m_inputContext.ProcessKeyEvent(keyval, keycode, state);
		}
			catch(NDesk.DBus.DBusConectionErrorException error)
			{
				m_ibus = null;
				m_inputContext = null;
				NotifyUserOfIBusConnectionDropped();
				return false;
			}
		}

		/// <summary>
		/// Reset the Current ibus inputContext.
		/// </summary>
		public void Reset()
		{
			if (m_inputContext == null)
				return;

			ProtectedIBusInvoke(() =>m_inputContext.Reset());
		}

		/// <summary>
		/// Setup ibus inputContext and setup callback handlers.
		/// </summary>
		public void CreateInputContext(string name)
		{
			m_inputContext = m_ibus.InputBus.CreateInputContext(name);

			ProtectedIBusInvoke(() =>
			{
			m_inputContext.CommitText += CommitTextEventHandler;
			m_inputContext.UpdatePreeditText += UpdatePreeditTextEventHandler;
			m_inputContext.HidePreeditText += HidePreeditTextEventHandler;
			m_inputContext.ForwardKeyEvent += ForwardKeyEventHandler;

			m_inputContext.SetCapabilities(Capabilities.Focus | Capabilities.PreeditText);
			});
		}

		/// <summary></summary>
		public event Action<string> CommitText;

		/// <summary></summary>
		public event Action<string, uint, bool> UpdatePreeditText;

		/// <summary></summary>
		public event Action HidePreeditText;

		/// <summary></summary>
		public event Action<uint, uint, uint> ForwardKeyEvent;
		#endregion

		#region private methods

		private void CommitTextEventHandler(object text)
		{
			if (CommitText != null)
			{
				IBusText t = (IBusText)Convert.ChangeType(text, typeof(IBusText));
				CommitText(t.Text);
			}
		}

		private void UpdatePreeditTextEventHandler(object text, uint cursor_pos, bool visible)
		{
			if (UpdatePreeditText != null)
			{
				IBusText t = (IBusText)Convert.ChangeType(text, typeof(IBusText));

				UpdatePreeditText(t.Text, cursor_pos, visible);
			}
		}

		private void HidePreeditTextEventHandler()
		{
			if (HidePreeditText != null)
				HidePreeditText();
		}

		private void ForwardKeyEventHandler(uint keyval, uint keycode, uint modifiers)
		{
			if (ForwardKeyEvent != null)
				ForwardKeyEvent(keyval, keycode, modifiers);
		}

		#endregion
	}
}
#endif
