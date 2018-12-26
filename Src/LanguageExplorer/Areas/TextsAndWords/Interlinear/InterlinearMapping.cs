// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Simple class to record the bits of information we want about how one marker maps onto FieldWorks.
	/// This is serialized to form the .map file, so change with care.
	/// It is public only because XmlSerializer requires everything to be.
	/// </summary>
	[Serializable]
	public class InterlinearMapping : Sfm2FlexTextMappingBase
	{
		public InterlinearMapping()
		{
		}
		public InterlinearMapping(InterlinearMapping copyFrom)
			: base(copyFrom)
		{
		}
	}
}