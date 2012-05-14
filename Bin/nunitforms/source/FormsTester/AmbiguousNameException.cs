#region Copyright (c) 2003-2005, Luke T. Maxon

/********************************************************************************************************************
'
' Copyright (c) 2003-2005, Luke T. Maxon
' All rights reserved.
'
' Redistribution and use in source and binary forms, with or without modification, are permitted provided
' that the following conditions are met:
'
' * Redistributions of source code must retain the above copyright notice, this list of conditions and the
' 	following disclaimer.
'
' * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and
' 	the following disclaimer in the documentation and/or other materials provided with the distribution.
'
' * Neither the name of the author nor the names of its contributors may be used to endorse or
' 	promote products derived from this software without specific prior written permission.
'
' THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
' WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
' PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
' ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
' LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
' INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
' OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
' IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
'
'*******************************************************************************************************************/

#endregion

using System;

namespace NUnit.Extensions.Forms
{
	/// <summary>
	/// Exception is thrown when there is more than one control with the specified name.
	/// </summary>
	/// <remarks>
	/// You should qualify the name according to the name property of the parent control in
	/// a dot-delimited string.
	/// <para>
	/// If you have multiple dynamic controls with the same name, consider giving them unique
	/// names or else access them using the indexer property on each ControlTester.
	/// </para>
	///</remarks>
	///<example>
	///grandparent.parent.child is a valid name string.. You can use the shortest name string
	///that uniquely identifies a control.
	///</example>
	public class AmbiguousNameException : Exception
	{
		/// <summary>
		/// Creates an AmbiguousNameException.
		/// </summary>
		/// <remarks>
		/// The message string can be specified.
		/// </remarks>
		/// <param name="message">The messasge for the exception.</param>
		public AmbiguousNameException(string message)
			: base(message)
		{
		}
	}
}