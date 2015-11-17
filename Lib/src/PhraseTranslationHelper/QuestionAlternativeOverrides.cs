// ---------------------------------------------------------------------------------------------
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
