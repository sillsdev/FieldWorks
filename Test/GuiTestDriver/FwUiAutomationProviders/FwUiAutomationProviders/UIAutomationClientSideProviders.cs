// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
//using System.Linq;
using System.Windows.Automation;
using System.Windows.Automation.Provider;

namespace FwUiAutomationProviders
{
	// The assembly must implement a UIAutomationClientSideProviders class,
	// and the namespace must be the same as the name of the DLL, so that
	// UI Automation can find the table of descriptors. In this example,
	// the DLL would be "ClientSideProviderAssembly.dll"

	static class UIAutomationClientSideProviders
	{
		/// <summary>
		/// Implementation of the static ClientSideProviderDescriptionTable field.
		/// List all provider classes in the table.
		/// </summary>
		public static ClientSideProviderDescription[] ClientSideProviderDescriptionTable =
		{
			new ClientSideProviderDescription(
				// Method that creates the provider objects.
				new ClientSideProviderFactoryCallback(RootSiteProvider.Create),
				// Class of window that will be served by the provider.
				"RootSite", "Flex.exe", ClientSideProviderMatchIndicator.None)
		};
	}
}
