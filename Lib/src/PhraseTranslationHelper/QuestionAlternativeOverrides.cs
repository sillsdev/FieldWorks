// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2011' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: QuestionAlternativeOverrides.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SILUBS.PhraseTranslationHelper
{
	#region class QuestionAlternativeOverrides
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	[XmlRoot("QuestionAlternativeOverrides", Namespace = "", IsNullable = false)]
	public class QuestionAlternativeOverrides
	{
		[XmlElement("Alternative", typeof(Alternative), Form = XmlSchemaForm.Unqualified)]
		public Alternative[] Items { get; set; }
	}

	#endregion

	#region class Alternative
	[Serializable]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	public class Alternative
	{
		[XmlElement("Original", Form = XmlSchemaForm.Unqualified)]
		public string Text { get; set; }

		[XmlElement("Alt", Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public string[] AlternateForms { get; set; }
	}
	#endregion
}
