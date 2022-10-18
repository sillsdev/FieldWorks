<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/RequiresSTAOnWindowsAttribute.cs
// Copyright (c) 2018-2020 SIL International
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/RequiresSTAOnWindowsAttribute.cs
// Copyright (c) 2018 SIL International
=======
// Copyright (c) 2018-2021 SIL International
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/RequiresSTAOnWindowsAttribute.cs
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Internal;
using SIL.PlatformUtilities;

namespace FieldWorks.TestUtilities.Attributes
{
	/// <summary>
	/// Marks a test that must run in the STA if running on Windows, causing it to run in a
	/// separate thread if necessary. This is needed on Windows because of the COM objects.
	/// However, on Linux the tests hang when we use STA. Since we don't have a "real" COM on
	/// Linux implementation we don't really need it on there.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
	public class RequiresSTAOnWindowsAttribute : PropertyAttribute
	{
		/// <inheritdoc />
		public RequiresSTAOnWindowsAttribute()
		{
			if (Platform.IsWindows)
<<<<<<< HEAD:Src/FieldWorks.TestUtilities/Attributes/RequiresSTAOnWindowsAttribute.cs
			{
				Properties.Add(PropertyNames.ApartmentState, ApartmentState.STA);
			}
||||||| f013144d5:Src/Common/FwUtils/FwUtilsTests/Attributes/RequiresSTAOnWindowsAttribute.cs
				Properties.Add("APARTMENT_STATE", ApartmentState.STA);
=======
				Properties.Add("ApartmentState", ApartmentState.STA);
>>>>>>> develop:Src/Common/FwUtils/FwUtilsTests/Attributes/RequiresSTAOnWindowsAttribute.cs
		}
	}
}
