// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using XCore;
using SIL.FieldWorks.XWorks;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This is so that we can use InterlinMaster in VS Designer.
	/// Designer won't work with classes that have abstract base classes.
	/// </summary>
	public class InterlinMasterBase : RecordView
	{
		internal InterlinMasterBase()
		{
		}

		public override void Init(Mediator mediator, XmlNode configurationParameters)
		{
			throw new NotImplementedException();
		}
	}
}
