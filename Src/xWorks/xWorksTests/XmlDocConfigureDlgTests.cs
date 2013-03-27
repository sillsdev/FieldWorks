using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Tests for routines in XmlDocConfigDlg.
	/// </summary>
	[TestFixture]
	public class XmlDocConfigureDlgTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void FlattenPossibilityList()
		{
			using (XmlDocConfigureDlg dlg = new XmlDocConfigureDlg())
			{
				Guid thirdLevelGuid;
				ICmPossibilityList theList = null;
				UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				  () =>
				{
					theList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
					var topItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
					theList.PossibilitiesOS.Add(topItem);
					var secondLevelItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
					var thirdLevelItemItem = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
					topItem.SubPossibilitiesOS.Add(secondLevelItem);
					secondLevelItem.SubPossibilitiesOS.Add(thirdLevelItemItem);
					thirdLevelGuid = thirdLevelItemItem.Guid;
				});

				Assert.AreEqual(3, XmlDocConfigureDlg.FlattenPossibilityList(theList.PossibilitiesOS).Count);
			}
		}
	}
}