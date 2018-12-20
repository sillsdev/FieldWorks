// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class PhoneEnvReferenceDataCacheDecorator : LcmMetaDataCacheDecoratorBase
	{
		public PhoneEnvReferenceDataCacheDecorator(IFwMetaDataCacheManaged mdc)
			: base(mdc)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotSupportedException();
		}

		public override int GetFieldType(int luFlid)
		{
			switch (luFlid)
			{
				case PhoneEnvReferenceView.kMainObjEnvironments:
					return (int)CellarPropertyType.ReferenceSequence;
				case PhoneEnvReferenceView.kEnvStringRep:
					return (int)CellarPropertyType.String;
				case PhoneEnvReferenceView.kErrorMessage:
					return (int)CellarPropertyType.Unicode;
				default:
					return base.GetFieldType(luFlid);
			}
		}
	}
}