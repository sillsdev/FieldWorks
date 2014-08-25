using System;
using System.Windows.Forms;
using SIL.Utils;

using Reflector.UserInterface;

namespace XCore
{
	public abstract class CommandBarAdaptor: IDisposable
	{
		protected Form m_window;
		protected IImageCollection m_smallImages;
		protected IImageCollection m_largeImages;
		protected CommandBarManager m_commandBarManager = new CommandBarManager();

		public CommandBarAdaptor()
		{
			m_commandBarManager.Name = "CommandBarManager";
		}

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~CommandBarAdaptor()
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
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				m_commandBarManager.Dispose();
			}
			m_commandBarManager = null;
			IsDisposed = true;
		}
		#endregion

		/// <summary>
		/// get the control which is shared by all subclasses
		/// </summary>
		/// <returns></returns>
		public bool GetCommandBarManager()
		{
			//see if a menubar has already created one of these command bar managers
			foreach(Control control in m_window.Controls)
			{
				if (control is CommandBarManager)
				{
					m_commandBarManager = (CommandBarManager)control;
					m_commandBarManager.Name = "CommandBarManager";
					return false;
				}
			}

			if (m_commandBarManager == null)
			{
			m_commandBarManager = new CommandBarManager();
			m_commandBarManager.Name = "CommandBarManager";
			}
			return true;
		}

		public void FinishInit()
		{
			//get us to be drawn last so that we can truly own the top of the window.
			m_commandBarManager.SendToBack();
		}

	}
}
