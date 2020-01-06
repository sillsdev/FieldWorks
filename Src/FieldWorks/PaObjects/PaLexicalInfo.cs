// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.PaToFdoInterfaces;

namespace SIL.FieldWorks.PaObjects
{
	/// <summary />
	public class PaLexicalInfo : IPaLexicalInfo, IDisposable
	{
		private List<PaWritingSystem> m_writingSystems;
		private List<PaLexEntry> m_lexEntries;

		#region Disposable stuff

		/// <summary />
		~PaLexicalInfo()
		{
			Dispose(false);
		}

		/// <summary />
		public bool IsDisposed { get; private set; }

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + " *******");
			if (IsDisposed)
			{
				return;
			}
			if (fDisposing)
			{
				m_lexEntries?.Clear();
				m_writingSystems?.Clear();
			}
			m_lexEntries = null;
			m_writingSystems = null;
			IsDisposed = true;
		}
		#endregion

		#region IPaLexicalInfo Members

		/// <inheritdoc />
		public bool ShowOpenProject(Form owner, ref Rectangle dialogBounds, ref int dialogSplitterPos, out string name, out string server)
		{
			FwRegistryHelper.Initialize();
			FwUtils.InitializeIcu();

			using (var dlg = new ChooseLangProjectDialog(dialogBounds, dialogSplitterPos))
			{
				if (dlg.ShowDialog(owner) == DialogResult.OK)
				{
					name = dlg.Project;
					server = null;
					dialogBounds = dlg.Bounds;
					dialogSplitterPos = dlg.SplitterPosition;
					return true;
				}
			}
			name = null;
			server = null;
			return false;
		}

		/// <inheritdoc />
		public bool LoadOnlyWritingSystems(string name, string server, int timeToWaitForProcessStart, int timeToWaitForLoadingData)
		{
			return InternalInitialize(name, server, true, timeToWaitForProcessStart, timeToWaitForLoadingData);
		}

		/// <inheritdoc />
		public bool Initialize(string name, string server, int timeToWaitForProcessStart, int timeToWaitForLoadingData)
		{
			return InternalInitialize(name, server, false, timeToWaitForProcessStart, timeToWaitForLoadingData);
		}

		/// <summary>
		/// Initializes the LCM repositories from the specified project and server.
		/// </summary>
		/// <returns>
		/// true if the repositories are successfully initialized and FieldWorks started;
		/// otherwise, false.
		/// </returns>
		private bool InternalInitialize(string name, string server, bool loadOnlyWs, int timeToWaitForProcessStart, int timeToWaitForLoadingData)
		{
			var foundFwProcess = false;
			var newProcessStarted = false;
			Process newFwInstance = null;
			var start = DateTime.Now;
			var timeToWaitTotalMillis = timeToWaitForLoadingData + timeToWaitForProcessStart;

			try
			{
				do
				{
					FieldWorks.RunOnRemoteClients(FieldWorks.kPaRemoteRequest, requestor =>
					{
						return LoadFwDataForPa((PaRemoteRequest)requestor, name, server, loadOnlyWs,
							timeToWaitForLoadingData, newProcessStarted, out foundFwProcess);
					});

					if (foundFwProcess)
					{
						return true;
					}
					if (!newProcessStarted)
					{
						newProcessStarted = true;
						newFwInstance = FieldWorks.OpenProjectWithRealNewProcess(name, "-" + FwAppArgs.kNoUserInterface);

						// TODO-Linux: WaitForInputIdle isn't fully implemented on Linux.
						if (!newFwInstance.WaitForInputIdle(timeToWaitForProcessStart))
						{
							return false;
						}
					}
				} while ((DateTime.Now - start).TotalMilliseconds <= timeToWaitTotalMillis);
			}
			finally
			{
				if (newFwInstance != null)
				{
					if (!newFwInstance.HasExited)
					{
						newFwInstance.Kill();
					}
					newFwInstance.Dispose();
				}

				Debug.WriteLine((DateTime.Now - start).TotalMilliseconds);
			}

			// this line is reached only on startup timeout.
			return false;
		}

		private bool LoadFwDataForPa(PaRemoteRequest requestor, string name, string server,
			bool loadOnlyWs, int timeToWaitForLoadingData,
			bool newProcessStarted, out bool foundFwProcess)
		{
			foundFwProcess = false;

			Func<string, string, bool> invoker = requestor.ShouldWait;
			var endTime = DateTime.Now.AddMilliseconds(timeToWaitForLoadingData);

			// Wait until this process knows which project it is loading.
			bool shouldWait;
			do
			{
				var ar = invoker.BeginInvoke(name, server, null, null);
				if (!ar.AsyncWaitHandle.WaitOne(endTime - DateTime.Now, false))
				{
					return false;
				}
				// Get the return value of the ShouldWait method.
				shouldWait = invoker.EndInvoke(ar);
				if (shouldWait)
				{
					if (timeToWaitForLoadingData > 0 && DateTime.Now > endTime)
					{
						return false;
					}
					Thread.Sleep(100);
				}
			} while (shouldWait);

			if (!requestor.IsMyProject(name, server))
			{
				return false;
			}
			var xml = requestor.GetWritingSystems();
			m_writingSystems = XmlSerializationHelper.DeserializeFromString<List<PaWritingSystem>>(xml);

			if (!loadOnlyWs)
			{
				xml = requestor.GetLexEntries();
				m_lexEntries = XmlSerializationHelper.DeserializeFromString<List<PaLexEntry>>(xml);
			}
			if (newProcessStarted)
			{
				requestor.ExitProcess();
			}
			foundFwProcess = true;
			return true;
		}

		/// <inheritdoc />
		public IEnumerable<IPaLexEntry> LexEntries => m_lexEntries;

		/// <inheritdoc />
		public IEnumerable<IPaWritingSystem> WritingSystems => m_writingSystems;

		#endregion
	}
}
