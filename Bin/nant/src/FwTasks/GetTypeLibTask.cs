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
// File: GetTypeLibTask.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using System.Text;
using NAnt.Core;
using NAnt.Core.Tasks;
using NAnt.Core.Attributes;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Gets the required information about a type library from the registry
	/// </summary>
	/// <example>
	/// <para>Find the path for the COM object with the specified <c>guid</c> (and version
	/// numbers/lcid) and store the result in the named property <c>DbAccess.path</c></para>
	/// <code><![CDATA[
	/// <gettypelib guid="{AAB4A4A1-3C83-11D4-A1BB-00C04F0C9593}" propertyname="DbAccess.path"
	/// versionmajor="1" versionminor="0" lcid="0"/>
	/// ]]></code>
	/// </example>
	/// ----------------------------------------------------------------------------------------
	[TaskName("gettypelib")]
	public class GetTypeLibTask: Task
	{
		private Guid m_guid;
		private string m_PropertyName;
		private short m_VersionMajor = 1;
		private short m_VersionMinor = 0;
		private int m_Lcid = 0;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="GetTypeLibTask"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public GetTypeLibTask()
		{
		}

		[DllImport( "oleaut32.dll", CharSet = CharSet.Auto, PreserveSig = false,
			 SetLastError=true )]
		private static extern void QueryPathOfRegTypeLib(ref Guid guid, short wVerMajor,
			short wVerMinor, int lcid, [MarshalAs(UnmanagedType.VBByRefStr)]
			ref StringBuilder lpbstrPathName);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Guid of the type library
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("guid", Required=true)]
		public string Guid
		{
			get
			{
				if (m_guid != System.Guid.Empty)
					return m_guid.ToString();
				else
					return string.Empty;
			}
			set
			{
				try
				{
					m_guid = new Guid(value);
				}
				catch(Exception e)
				{
					throw new BuildException("Invalid Guid\n", Location, e);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Name of the property where the path is stored
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("propertyname", Required=true)]
		public string PropertyName
		{
			get { return m_PropertyName; }
			set { m_PropertyName = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Major version number for type library
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("versionmajor")]
		[Int32Validator(0, short.MaxValue)]
		public short VersionMajor
		{
			get { return m_VersionMajor; }
			set { m_VersionMajor = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Minor version number for type library
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("versionminor")]
		[Int32Validator(0, short.MaxValue)]
		public short VersionMinor
		{
			get { return m_VersionMinor; }
			set { m_VersionMinor = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// LCID for type library
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("lcid")]
		[Int32Validator(0, int.MaxValue)]
		public int Lcid
		{
			get { return m_Lcid; }
			set { m_Lcid = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the job
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void ExecuteTask()
		{
			try
			{
				StringBuilder pathResult = new StringBuilder(1024);
				QueryPathOfRegTypeLib(ref m_guid, VersionMajor, VersionMinor, Lcid,
					ref pathResult);

				Properties[PropertyName] = pathResult.ToString();
			}
			catch (Exception e)
			{
				throw new BuildException(
					string.Format("Error {0} getting typelib path for guid {1}", e.Message, m_guid),
					Location, e);
			}
		}
	}
}
