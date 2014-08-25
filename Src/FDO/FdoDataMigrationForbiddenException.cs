// Copyright (c) 2014-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Exception thrown when we try to open a project which requires data migration but data migration has been forbidden by the application
	/// </summary>
	public class FdoDataMigrationForbiddenException : Exception
	{
	}
}