// Copyright (c) 2011-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.LCModel;
using SIL.PlatformUtilities;
using SIL.Reporting;
using Timer = System.Threading.Timer;

namespace SIL.FieldWorks.LexicalProvider
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Manages FieldWorks's lexical service provider for access by external applications.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal static class LexicalProviderManager
	{
		private const int kInactivityTimeout = 1800000; // 30 minutes in msec

		private static Timer s_lexicalProviderTimer;
		private static readonly Dictionary<Type, ServiceHost> s_runningProviders = new Dictionary<Type, ServiceHost>();

		private static string PtCommunicationProbTitle = "Paratext Communication Problem";
		private static string PtCommunicationProb =
			"The project you are opening will not communicate with Paratext because a project with the same name is " +
			"already open. If you want to use Paratext with this project, make a change in this project" +
			" (so that it will start first), close both projects, then restart Flex.";

		// The different URL prefixes that are required for Windows named pipes and Linux basic http binding.
		// On Linux, just in case port 40001 is in use for something else on a particular system,
		// we allow the user to configure both programs to use a different port.
		internal static string UrlPrefix = Platform.IsWindows
			? "net.pipe://localhost/"
			: string.Format("http://127.0.0.1:{0}/", Environment.GetEnvironmentVariable("LEXICAL_PROVIDER_PORT") ?? "40001");

		// Mono requires the pipe handle to use slashes instead of colons.
		// We could put this conditional code somewhere in the routines that generate the pipe handles,
		// but it seemed cleaner to keep all the conditional code for different kinds of pipe more-or-less in one place.
		internal static string FixPipeHandle(string pipeHandle)
		{
			return Platform.IsWindows ? pipeHandle : pipeHandle.Replace (":", "/");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a LexicalServiceProvider listener for the specified project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static void StartLexicalServiceProvider(ProjectId projectId, LcmCache cache)
		{
			if (projectId == null)
				throw new InvalidOperationException("Project identity must be known before creating the lexical provider listener");
			var url = UrlPrefix + FixPipeHandle(projectId.PipeHandle);
			StartProvider(new Uri(url),
				new LexicalServiceProvider(cache), typeof(ILexicalServiceProvider));

			s_lexicalProviderTimer = new Timer(s_timeSinceLexicalProviderUsed_Tick, null,
				kInactivityTimeout, Timeout.Infinite);
			Logger.WriteEvent("Started listening for lexical service provider requests.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starts the provider.
		/// </summary>
		/// <param name="providerLocation">The provider location.</param>
		/// <param name="provider">The provider.</param>
		/// <param name="providerType">Type of the provider.</param>
		/// ------------------------------------------------------------------------------------
		internal static void StartProvider(Uri providerLocation, object provider, Type providerType)
		{
			if (s_runningProviders.ContainsKey(providerType))
				return;

			string sNamedPipe = providerLocation.ToString();
			// REVIEW: we don't dispose ServiceHost. It might be better to add it to the
			// SingletonsContainer
			ServiceHost providerHost = null;
			try
			{
				providerHost = new ServiceHost(provider);
				// Named pipes are better for Windows...don't tie up a dedicated port and perform better.
				// However, Mono does not yet support them, so on Mono we use a different binding.
				// Note that any attempt to unify these will require parallel changes in Paratext
				// and some sort of coordinated release of the new versions.

				System.ServiceModel.Channels.Binding binding;
				if (Platform.IsWindows)
				{
					var pipeBinding = new NetNamedPipeBinding();
					pipeBinding.Security.Mode = NetNamedPipeSecurityMode.None;
					pipeBinding.MaxBufferSize *= 4;
					pipeBinding.MaxReceivedMessageSize *= 4;
					pipeBinding.MaxBufferPoolSize *= 2;
					pipeBinding.ReaderQuotas.MaxBytesPerRead *= 4;
					pipeBinding.ReaderQuotas.MaxArrayLength *= 4;
					pipeBinding.ReaderQuotas.MaxDepth *= 4;
					pipeBinding.ReaderQuotas.MaxNameTableCharCount *= 4;
					pipeBinding.ReaderQuotas.MaxStringContentLength *= 4;
					binding = pipeBinding;
				}
				else
				{
					var httpBinding = new BasicHttpBinding();
					httpBinding.MaxBufferSize *= 4;
					httpBinding.MaxReceivedMessageSize *= 4;
					httpBinding.MaxBufferPoolSize *= 2;
					httpBinding.ReaderQuotas.MaxBytesPerRead *= 4;
					httpBinding.ReaderQuotas.MaxArrayLength *= 4;
					httpBinding.ReaderQuotas.MaxDepth *= 4;
					httpBinding.ReaderQuotas.MaxNameTableCharCount *= 4;
					httpBinding.ReaderQuotas.MaxStringContentLength *= 4;
					binding = httpBinding;
				}

				providerHost.AddServiceEndpoint(providerType, binding, sNamedPipe);
				providerHost.Open();
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				providerHost = null;
				if (ScriptureProvider.IsInstalled)
				{
					MessageBox.Show(PtCommunicationProb, PtCommunicationProbTitle,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				return;
			}
			Logger.WriteEvent("Started provider " + providerLocation + " for type " + providerType + ".");
			s_runningProviders.Add(providerType, providerHost);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the lexical provider timer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static void ResetLexicalProviderTimer()
		{
			s_lexicalProviderTimer.Change(kInactivityTimeout, Timeout.Infinite);
			FieldWorks.InAppServerMode = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static void StaticDispose()
		{
			Logger.WriteEvent("Closing service hosts");

			if (s_lexicalProviderTimer != null)
				s_lexicalProviderTimer.Dispose();
			s_lexicalProviderTimer = null;

			foreach (ServiceHost host in s_runningProviders.Values)
				host.Close();
			s_runningProviders.Clear();
			FieldWorks.InAppServerMode = false; // Make sure FW can shut down
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Tick event of the s_timeSinceLexicalProviderUsed control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// ------------------------------------------------------------------------------------
		private static void s_timeSinceLexicalProviderUsed_Tick(object sender)
		{
			FieldWorks.InAppServerMode = false;
			if (FieldWorks.ProcessCanBeAutoShutDown)
				FieldWorks.GracefullyShutDown();
		}
	}
}
