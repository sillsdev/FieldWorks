// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Configuration;
using System.Reflection;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that helps getting the ports used to connect to the DB4O database
	/// </summary>
	/// <remarks>We read the ports from a config file so that we can provide different values
	/// when running from tests. This allows to run multiple builds in parallel.</remarks>
	/// ----------------------------------------------------------------------------------------
	public static class Db4OPorts
	{
		private sealed class Db4OPortsImpl: IDisposable
		{
			private int m_ServerPort = -1;
			private int m_ReplyPort = -1;
			private bool? m_AppSettingsExists;
			private AppSettingsSection AppSettings { get; set; }

			#region Disposable stuff
#if DEBUG
			/// <summary/>
			~Db4OPortsImpl()
			{
				Dispose(false);
			}
#endif

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects

				}
				IsDisposed = true;
			}
			#endregion

			private string GetValue(string key)
			{
				if (!m_AppSettingsExists.HasValue)
				{
					var dllName = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
					if (File.Exists(dllName + ".config"))
					{
						var config = ConfigurationManager.OpenExeConfiguration(dllName);
						AppSettings = config.AppSettings;
						m_AppSettingsExists = true;
					}
					else
						m_AppSettingsExists = false;
				}

				if (m_AppSettingsExists.Value)
					return AppSettings.Settings[key].Value;
				return null;
			}

			/// <summary>
			/// Gets the port the database server listens
			/// </summary>
			public int ServerPort
			{
				get
				{
					if (m_ServerPort < 0)
					{
						var value = GetValue("ServerPort");
						m_ServerPort = !string.IsNullOrEmpty(value) ? Int32.Parse(value) : 3333;
					}
					return m_ServerPort;
				}
			}

			/// <summary>
			/// Gets the port the database server sends replies
			/// </summary>
			public int ReplyPort
			{
				get
				{
					if (m_ReplyPort < 0)
					{
						var value = GetValue("ReplyPort");
						m_ReplyPort = !string.IsNullOrEmpty(value) ? Int32.Parse(value) : 3334;
					}
					return m_ReplyPort;
				}
			}
		}

		/// <summary>
		/// Gets the port the database server listens
		/// </summary>
		public static int ServerPort
		{
			get { return SingletonsContainer.Get<Db4OPortsImpl>().ServerPort; }
		}

		/// <summary>
		/// Gets the port the database server sends replies
		/// </summary>
		public static int ReplyPort
		{
			get { return SingletonsContainer.Get<Db4OPortsImpl>().ReplyPort; }
		}
	}
}
