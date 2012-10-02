// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CSharpCodeProviderEx.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CSharpCodeProviderEx: CSharpCodeProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an instance of the C# code generator.
		/// </summary>
		/// <returns>
		/// An instance of the C# <see cref="T:System.CodeDom.Compiler.ICodeGenerator"></see> implementation.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		[ObsoleteAttribute("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class.")]
		public override System.CodeDom.Compiler.ICodeGenerator CreateGenerator()
		{
			return new CSharpCodeGenerator();
		}
	}
}
