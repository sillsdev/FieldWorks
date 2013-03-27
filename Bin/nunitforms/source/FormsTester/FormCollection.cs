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

using System.Collections;
using System.Windows.Forms;

namespace NUnit.Extensions.Forms
{
	/// <summary>
	/// Internal use only.  Represents a collection of controls.
	/// </summary>
	/// <remarks>
	/// NUnitForms users should not have a need for this class.  When C# supports
	/// generics, this should be replaced.
	/// </remarks>
	public class FormCollection
	{
		private readonly ArrayList list = new ArrayList();

		/// <summary>
		/// Add a Form to the collection.
		/// </summary>
		/// <remarks>
		/// Will not add a duplicate form.  In this way, the collection acts like a Set.
		/// </remarks>
		/// <param name="form">The form to add.</param>
		public void Add(Form form)
		{
			if (!Contains(form))
			{
				list.Add(form);
			}
		}

		/// <summary>
		/// Returns a boolean to indicate whether the supplied form exists in this collection.
		/// </summary>
		/// <param name="form">The form to check for existence.</param>
		/// <returns>true if the form is in the collection, false otherwise.</returns>
		public bool Contains(Form form)
		{
			return list.Contains(form);
		}

		/// <summary>
		/// Add one FormCollection to another.  Combines them into one collection.
		/// </summary>
		/// <param name="collection">The collection to merge with this one.</param>
		public void Add(FormCollection collection)
		{
			foreach (Form form in collection)
			{
				Add(form);
			}
		}

		/// <summary>
		/// Returns the number of forms in this FormCollection.
		/// </summary>
		public int Count
		{
			get
			{
				return list.Count;
			}
		}

		/// <summary>
		/// Returns an IEnumerator of the Forms in this collection.
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			return list.GetEnumerator();
		}

		/// <summary>
		/// Returns a Form from this collection according to its index.
		/// </summary>
		public Form this[int i]
		{
			get
			{
				return (Form)list[i];
			}
		}
	}
}