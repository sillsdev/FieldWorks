// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PropertyDeleteTask.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Globalization;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Tasks;

namespace SIL.FieldWorks.Build.Tasks
{
	/// <summary>
	/// Delete a property
	/// </summary>
	[TaskName("propertydelete")]
	public class PropertyDeleteTask: Task
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyDeleteTask"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public PropertyDeleteTask()
		{
		}

		string m_name = null;

		/// <summary>the name of the property to delete.</summary>
		[TaskAttribute("name", Required=true)]
		public string PropName
		{
			get { return m_name; }
			set { m_name = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the job
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			if (Properties.IsReadOnlyProperty(m_name))
			{
				throw new BuildException(String.Format(CultureInfo.InvariantCulture,
					"Can't delete read-only property {0}", m_name), Location );
			}

			((IDictionary)Properties).Remove(m_name);
		}
	}
}
