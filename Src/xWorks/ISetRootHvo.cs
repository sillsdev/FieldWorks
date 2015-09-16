// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Similarly an interface that Virtual List Publisher decorators may implement if they need
	/// to be notified of the root object that their virtual property applies to.
	/// </summary>
	interface ISetRootHvo
	{
		void SetRootHvo(int hvo);
	}
}