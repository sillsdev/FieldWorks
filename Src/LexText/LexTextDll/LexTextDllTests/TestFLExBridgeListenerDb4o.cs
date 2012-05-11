// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TestFLExBridgeListenerDb4o.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.XWorks.LexText;

namespace LexTextDllTests
{
	class TestFLExBridgeListenerDb4o : FLExBridgeListener
	{
		protected override bool IsDb4oProject
		{
			get { return true; }
		}
	}
}
