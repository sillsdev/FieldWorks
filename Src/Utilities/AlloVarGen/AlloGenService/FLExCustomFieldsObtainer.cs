// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.AlloGenService
{
	// Some of this code was taken from Src\xWorks\AddCustomFieldDialog.cs
	public class FLExCustomFieldsObtainer
	{
		public LcmCache Cache { get; set; }
		public List<FDWrapper> CustomFields { get; } = new List<FDWrapper>();

		public FLExCustomFieldsObtainer(LcmCache cache)
		{
			Cache = cache;
			// get the custom fields
			FieldDescription.ClearDataAbout();
			CustomFields = (
				from fd in FieldDescription.FieldDescriptors(Cache)
				where
					fd.IsCustomField
					&& fd.IsInstalled
					&& fd.Class == LexEntryTags.kClassId
					&& fd.Type == LCModel.Core.Cellar.CellarPropertyType.String
				select new FDWrapper(fd, false)
			).ToList();
		}

		/// <summary>
		/// This class is a wrapper class for containing the FieldDescription
		/// and the source of it : mem or DB.  This class is added to the LB
		/// of custom fields.
		/// </summary>
		public class FDWrapper
		{
			public FDWrapper(FieldDescription fd, bool isNew)
			{
				Fd = fd;
				IsNew = isNew;
			}

			public override string ToString()
			{
				return Fd.Userlabel ?? "";
			}

			// read only properties
			public FieldDescription Fd { get; private set; }
			public bool IsNew { get; private set; }
		}
	}
}
