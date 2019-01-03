// Copyright (c) 2013-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Text;

namespace LanguageExplorerTests.Controls.DetailControls
{
	/// <summary>
	/// Tests (so far minimal) of PhoneEnvReferenceView
	/// </summary>
	[TestFixture]
	public class PhoneEnvReferenceViewTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// There's a lot more we could verify about the end results of this test, as well as more we could
		/// test about EnvsBeingRequestedForThisEntry. This test was intended to cover a particular bug fix.
		/// </summary>
		[Test]
		public void EnvsBeingRequestedForThisEntry_HandlesItemFollowingEmptyString()
		{
			var sda = new PhoneEnvReferenceSda(Cache.DomainDataByFlid as ISilDataAccessManaged);
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			var root = form.Hvo;
			var hvos = new[] { -5000, -5001, -5002 };
			sda.CacheVecProp(root, PhoneEnvReferenceView.kMainObjEnvironments, hvos, hvos.Length);
			sda.SetString(hvos[1], PhoneEnvReferenceView.kEnvStringRep, TsStringUtils.MakeString("abc", 6));
			var flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			try
			{
				using (var view = new PhoneEnvReferenceView())
				{
					view.InitializeFlexComponent(flexComponentParameters);
					view.SetSda(sda);
					view.SetRoot(form);
					view.SetCache(Cache);
					var results = view.EnvsBeingRequestedForThisEntry();
					Assert.That(results, Has.Count.EqualTo(1));
				}
			}
			finally
			{
				flexComponentParameters.PropertyTable.Dispose();
			}
		}
	}
}