#if __MonoCS__
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;
using IBusDotNet;
using System.Windows.Forms;

namespace SIL.FieldWorks.Views
{
	/// <summary>
	/// This class allows COM clients to switch IME keyboards
	/// </summary>
	[Guid("4ED1E8bC-DAdE-11DE-B350-0019DBf4566E")]
	public class KeyboardSwitcher : IIMEKeyboardSwitcher, IDisposable
	{
		// enums of supported ibus versions
		// these numbers are NOT the same as the ibus version numbers.
		public enum IBusVersions
		{
			Unknown,
			Version1,
			// commit e2793f52bf3da7a22321 changed IBusEngineDesc to contain rank field.
			Version2,
			Version_1_3_7,
		}

		private IBusVersions m_ibusVersion = IBusVersions.Unknown;

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
				IBusEngineDesc engineDesc = GetEngineDesc(engine);
				return engineDesc.longname;
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

					if (String.IsNullOrEmpty(value) || value == "None")
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
						IBusEngineDesc engineDesc = GetEngineDesc(engine);
						if (value == FormatKeyboardIdentifier(engineDesc))
							context.SetEngine(engineDesc.longname);
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
			IBusEngineDesc engineDesc = GetEngineDesc(engines[index]);

			return FormatKeyboardIdentifier(engineDesc);
		}

		/// <summary>
		/// Produce IBus keyboard identifier which is simular to the actual ibus switcher menu.
		/// </summary>
		internal string FormatKeyboardIdentifier(IBusEngineDesc engineDesc)
		{
			return String.Format("{0} - {1}", engineDesc.language, engineDesc.name);
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

		#region helper methods
		/// <summary>Get IBusEngineDesc names in a way tolerant to IBusEngineDesc versions.</summary>
		internal IBusEngineDesc GetEngineDesc(object engine)
		{
			switch (m_ibusVersion)
			{
			case IBusVersions.Unknown:
			case IBusVersions.Version1:
				try
				{
					IBusEngineDesc_v1 engineDesc = (IBusEngineDesc_v1)Convert.ChangeType(
						engine, typeof(IBusEngineDesc_v1));
					// we must be version1 api.
					m_ibusVersion = IBusVersions.Version1;

					return new IBusEngineDesc { longname = engineDesc.longname,
						name = engineDesc.name, description = engineDesc.description,
						language = engineDesc.language, license = engineDesc.license,
						author = engineDesc.author, icon = engineDesc.icon,
						layout = engineDesc.layout };
				}
				catch (InvalidCastException)
				{
					// try next version.
					m_ibusVersion = IBusVersions.Version2;
					return GetEngineDesc(engine);
				}

			case IBusVersions.Version2:
				try
				{
					IBusEngineDesc_v2 engineDesc = (IBusEngineDesc_v2)Convert.ChangeType(
						engine, typeof(IBusEngineDesc_v2));
					m_ibusVersion = IBusVersions.Version2;

					return new IBusEngineDesc { longname = engineDesc.longname,
						name = engineDesc.name, description = engineDesc.description,
						language = engineDesc.language, license = engineDesc.license,
						author = engineDesc.author, icon = engineDesc.icon,
						layout = engineDesc.layout, rank = engineDesc.rank };
				}
				catch (InvalidCastException)
				{
					// try next version.
					m_ibusVersion = IBusVersions.Version_1_3_7;
					return GetEngineDesc(engine);
				}

			case IBusVersions.Version_1_3_7:
				IBusEngineDesc engineDesc = (IBusEngineDesc)Convert.ChangeType(engine,
					typeof(IBusEngineDesc));
				return engineDesc;

			default:
				throw new NotSupportedException("Unknown ibus version");
			}
		}
		#endregion
	}
}
#endif
