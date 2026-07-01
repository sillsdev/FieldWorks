// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ServiceModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// The one net.pipe binding definition shared by both ends of the HermitCrab worker channel:
	/// the worker's ServiceHost (HCWorker.exe) and the in-FieldWorks client (HCWorkerClient). Both
	/// sides must agree on quotas/timeouts, so there is exactly one copy here.
	/// </summary>
	public static class PipeBindingFactory
	{
		// Real compiled grammars can be several MB once serialized to the HC.NET XML input format
		// (a real Sena grammar is ~1.4 MB). NetNamedPipeBinding's 64 KB default is nowhere near
		// enough and fails with a low-level "pipe is being closed" error rather than a clear
		// quota-exceeded one, so size generously - grammars only grow as projects grow.
		private const long MaxMessageSize = 256L * 1024 * 1024;

		public static NetNamedPipeBinding Create()
		{
			var pipeBinding = new NetNamedPipeBinding();
			pipeBinding.Security.Mode = NetNamedPipeSecurityMode.None;
			pipeBinding.MaxBufferSize = ClampToInt(MaxMessageSize);
			pipeBinding.MaxReceivedMessageSize = MaxMessageSize;
			pipeBinding.MaxBufferPoolSize = MaxMessageSize;
			pipeBinding.ReaderQuotas.MaxArrayLength = ClampToInt(MaxMessageSize);
			pipeBinding.ReaderQuotas.MaxStringContentLength = ClampToInt(MaxMessageSize);
			pipeBinding.ReaderQuotas.MaxBytesPerRead = 65536;
			pipeBinding.ReaderQuotas.MaxDepth = 64;
			pipeBinding.ReaderQuotas.MaxNameTableCharCount = 65536;
			pipeBinding.SendTimeout = TimeSpan.FromMinutes(10);
			pipeBinding.ReceiveTimeout = TimeSpan.FromMinutes(10);
			return pipeBinding;
		}

		private static int ClampToInt(long value) => (int)Math.Min(value, int.MaxValue);
	}
}
