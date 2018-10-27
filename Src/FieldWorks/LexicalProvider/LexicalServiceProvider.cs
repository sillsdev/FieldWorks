// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ServiceModel;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace SIL.FieldWorks.LexicalProvider
{
	/// <summary>
	/// Provides a service contract for getting a lexical provider from an application.
	/// </summary>
	[ServiceBehavior(IncludeExceptionDetailInFaults = true, InstanceContextMode = InstanceContextMode.Single, MaxItemsInObjectGraph = 2147483647)]
	public sealed class LexicalServiceProvider : ILexicalServiceProvider
	{
		/// <summary>String representing the type of the LexicalProvider</summary>
		public const string kLexicalProviderType = "LexicalProvider";
		private const int kSupportedLexicalProviderVersion = 3;

		private LcmCache m_cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="LexicalServiceProvider"/> class.
		/// </summary>
		public LexicalServiceProvider(LcmCache cache)
		{
			m_cache = cache;
		}

		#region ILexicalServiceProvider Members

		/// <inheritdoc />
		public Uri GetProviderLocation(string projhandle, string providerType)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();

			if (providerType == kLexicalProviderType)
			{
				var url = LexicalProviderManager.UrlPrefix + LexicalProviderManager.FixPipeHandle(FwUtils.GeneratePipeHandle(projhandle + ":LP"));
				var projUri = new Uri(url);
				LexicalProviderManager.StartProvider(projUri, new LexicalProviderImpl(m_cache), typeof(ILexicalProvider));
				return projUri;
			}

			return null;
		}

		/// <inheritdoc />
		public int GetSupportedVersion(string providerType)
		{
			LexicalProviderManager.ResetLexicalProviderTimer();
			return providerType == kLexicalProviderType ? kSupportedLexicalProviderVersion : 0;
		}

		/// <inheritdoc />
		public void Ping()
		{
			// Nothing to do for this method except reset our timer for the life of the LexicalProvider.
			// See comment for this method.
			LexicalProviderManager.ResetLexicalProviderTimer();
		}

		#endregion
	}
}