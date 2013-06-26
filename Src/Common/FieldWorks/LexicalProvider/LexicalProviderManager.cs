// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LexicalProviderManager.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.ServiceModel;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a LexicalServiceProvider listener for the specified project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static void StartLexicalServiceProvider(ProjectId projectId, FdoCache cache)
		{
			if (projectId == null)
				throw new InvalidOperationException("Project identity must be known before creating the lexical provider listener");

			StartProvider(new Uri("net.pipe://localhost/" + projectId.PipeHandle),
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="See review comment")]
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		internal static void StartProvider(Uri providerLocation, object provider, Type providerType)
		{
#if __MonoCS__
			Logger.WriteEvent("Cannot start provider " + providerLocation + " for type " + providerType + " in Mono because WCF is not sufficiently implemented!");
#else
			if (s_runningProviders.ContainsKey(providerType))
				return;

			string sNamedPipe = providerLocation.ToString();
			// REVIEW: we don't dispose ServiceHost. It might be better to add it to the
			// SingletonsContainer
			ServiceHost providerHost = null;
			try
			{
				providerHost = new ServiceHost(provider);
				// TODO-Linux: various properties of NetNamedPipeBinding are marked with MonoTODO
				// attributes. Test if this affects us.
				NetNamedPipeBinding binding = new NetNamedPipeBinding();
				binding.Security.Mode = NetNamedPipeSecurityMode.None;
				binding.MaxBufferSize *= 4;
				binding.MaxReceivedMessageSize *= 4;
				binding.MaxBufferPoolSize *= 2;
				binding.ReaderQuotas.MaxBytesPerRead *= 4;
				binding.ReaderQuotas.MaxArrayLength *= 4;
				binding.ReaderQuotas.MaxDepth *= 4;
				binding.ReaderQuotas.MaxNameTableCharCount *= 4;
				binding.ReaderQuotas.MaxStringContentLength *= 4;

				providerHost.AddServiceEndpoint(providerType, binding, sNamedPipe);
				providerHost.Open();
			}
			catch (Exception e)
			{
				Logger.WriteError(e);
				providerHost = null;
				var paratextInstalled = FwRegistryHelper.Paratext7orLaterInstalled();
				if (paratextInstalled)
				{
					MessageBox.Show(PtCommunicationProb, PtCommunicationProbTitle,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				return;
			}
			Logger.WriteEvent("Started provider " + providerLocation + " for type " + providerType + ".");
			s_runningProviders.Add(providerType, providerHost);
#endif
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
