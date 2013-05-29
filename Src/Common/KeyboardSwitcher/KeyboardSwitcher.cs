#if __MonoCS__
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using IBusDotNet;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Views
{
	/// <summary>
	/// This class allows COM clients to switch IME keyboards
	/// </summary>
	[Guid("4ED1E8bC-DAdE-11DE-B350-0019DBf4566E")]
	public class KeyboardSwitcher : IDisposable
	{
		private IBusConnection Connection = IBusConnectionFactory.Create();

		#region IDisposable implementation
#if DEBUG
		~KeyboardSwitcher()
		{
			Dispose(false);
		}
#endif
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (fDisposing)
			{
				if (Connection != null)
					Connection.Dispose();
			}
			Connection = null;
		}
		#endregion

		#region IIMEKeyboardSwitcher implementation

		/// <summary>
		/// get/set the keyboard of the current focused inputContext
		/// get returns String.Empty if not connected to ibus
		/// </summary>
		public string IMEKeyboard
		{
			get
			{
				if (Connection == null || GlobalCachedInputContext.InputContext == null)
					return String.Empty;

				InputContext context = GlobalCachedInputContext.InputContext;

				object engine = context.GetEngine();
				IBusEngineDesc engineDesc = IBusEngineDesc.GetEngineDesc(engine);
				return engineDesc.LongName;
			}

			set
			{
				try
				{
					if (Connection == null || GlobalCachedInputContext.InputContext == null)
						return;

					// check our cached value
					if (GlobalCachedInputContext.KeyboardName == value)
						return;

					InputContext context = GlobalCachedInputContext.InputContext;

					if (String.IsNullOrEmpty(value) || value == ResourceHelper.GetResourceString("kstidKeyboardNone"))
					{
						context.Reset();
						GlobalCachedInputContext.KeyboardName = value;
						context.Disable();
						return;
					}

					var ibusWrapper = new IBusDotNet.InputBusWrapper(Connection);
					object[] engines = ibusWrapper.InputBus.ListActiveEngines();

					foreach (object engine in engines)
					{
						IBusEngineDesc engineDesc = IBusEngineDesc.GetEngineDesc(engine);
						if (value == FormatKeyboardIdentifier(engineDesc))
						{
							context.SetEngine(engineDesc.LongName);
							break;
						}
					}

					GlobalCachedInputContext.KeyboardName = value;
				}
				catch (Exception e)
				{
					Debug.WriteLine(String.Format("KeyboardSwitcher changing keyboard failed, is kfml/ibus running? {0}", e));
				}
			}
		}

		/// <summary>Get Ibus keyboard at given index</summary>
		public string GetKeyboardName(int index)
		{
			if (Connection == null)
				return String.Empty;
			var ibusWrapper = new IBusDotNet.InputBusWrapper(Connection);
			object[] engines = ibusWrapper.InputBus.ListActiveEngines();
			IBusEngineDesc engineDesc = IBusEngineDesc.GetEngineDesc(engines[index]);

			return FormatKeyboardIdentifier(engineDesc);
		}

		/// <summary>
		/// Produce IBus keyboard identifier which is simular to the actual ibus switcher menu.
		/// </summary>
		internal string FormatKeyboardIdentifier(IBusEngineDesc engineDesc)
		{
			string id = engineDesc.Language;
			string languageName = string.IsNullOrEmpty(id) ? ResourceHelper.GetResourceString("kstidOtherLanguage") : Icu.GetDisplayName(id);
			return String.Format("{0} - {1}", languageName, engineDesc.Name);
		}

		/// <summary>number of ibus keyboards</summary>
		public int IMEKeyboardsCount
		{
			get
			{
				if (Connection == null)
					return 0;

				var ibusWrapper = new IBusDotNet.InputBusWrapper(Connection);
				object[] engines = ibusWrapper.InputBus.ListActiveEngines();
				return engines.Length;
			}
		}

		/// <summary/>
		public void Close()
		{
			Dispose();
		}
		#endregion
	}
}
#endif
