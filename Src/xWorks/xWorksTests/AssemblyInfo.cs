// --------------------------------------------------------------------------------------------
// <copyright from='2012' to='2012' company='SIL International'>
// 	Copyright (c) 2012, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------

using NUnit.Framework;

// On Windows we need STA because of the COM objects. On Linux the tests hang when we use
// STA. Since we don't have a "real" COM implementation we don't really need it on Linux.
#if !__MonoCS__
	[assembly: RequiresSTA]
#endif
