// Copyright (c) 2007-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public struct AnnotationInfo
	{
		public int annotationId;
		public Guid annotationType;
		public int beginOffset;
		public int endOffset;
		public int paragraphId;
		public int wfId;
	}
}