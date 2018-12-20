// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class PossibilityVectorReferenceViewMetaDataCacheDecorator : LcmMetaDataCacheDecoratorBase
	{
		internal PossibilityVectorReferenceViewMetaDataCacheDecorator(IFwMetaDataCacheManaged mdc)
			: base(mdc)
		{
		}

		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// The virtual field store a TsString, so the fake flid returns a type of String.
		/// </summary>
		public override int GetFieldType(int luFlid)
		{
			return luFlid == PossibilityVectorReferenceView.kflidFake ? (int)CellarPropertyType.String : base.GetFieldType(luFlid);
		}
	}
}