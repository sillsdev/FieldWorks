// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Code;
using SIL.LCModel;

namespace LanguageExplorer
{
	internal sealed class VectorPropertyParameterObject
	{
		internal ICmObject Owner { get; }
		internal string PropertyName { get; }
		internal int Flid { get; }

		internal VectorPropertyParameterObject(ICmObject owner, string propertyName, int flid)
		{
			Guard.AgainstNull(owner, nameof(owner));
			Guard.AgainstNullOrEmptyString(propertyName, nameof(propertyName));

			Owner = owner;
			PropertyName = propertyName;
			Flid = flid;
		}
	}
}