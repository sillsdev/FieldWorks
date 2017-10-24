// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Impls;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorerTests
{
	public static class TestSetupServices
	{
		public static void SetupTestPubSubSystem(out IPublisher publisher)
		{
			publisher = new Publisher();
		}
		public static void SetupTestPubSubSystem(out IPublisher publisher, out ISubscriber subscriber)
		{
			subscriber = new Subscriber();
			publisher = new Publisher(subscriber);
		}

		public static IPropertyTable SetupTestPropertyTable(IPublisher publisher)
		{
			return new PropertyTable(publisher);
		}
	}
}
