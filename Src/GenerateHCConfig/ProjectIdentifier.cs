using System;
using SIL.FieldWorks.FDO;

namespace GenerateHCConfig
{
	internal class ProjectIdentifier : IProjectIdentifier
	{
		private readonly FDOBackendProviderType m_backendProviderType;

		public ProjectIdentifier(string projectPath)
		{
			Path = System.IO.Path.GetFullPath(projectPath);
			string ext = System.IO.Path.GetExtension(Path);
			switch (ext.ToLowerInvariant())
			{
				case FdoFileHelper.ksFwDataXmlFileExtension:
					m_backendProviderType = FDOBackendProviderType.kXML;
					break;
				case FdoFileHelper.ksFwDataDb4oFileExtension:
					m_backendProviderType = FDOBackendProviderType.kDb4oClientServer;
					break;
			}
		}

		public bool IsLocal
		{
			get { return true; }
		}

		public string Path { get; set; }

		public string ProjectFolder
		{
			get { return System.IO.Path.GetDirectoryName(Path); }
		}

		public string SharedProjectFolder
		{
			get { return ProjectFolder; }
		}

		public string ServerName
		{
			get { return null; }
		}

		public string Handle
		{
			get { return Name; }
		}

		public string PipeHandle
		{
			get { throw new NotImplementedException(); }
		}

		public string Name
		{
			get { return System.IO.Path.GetFileNameWithoutExtension(Path); }
		}

		public FDOBackendProviderType Type
		{
			get { return m_backendProviderType; }
		}

		public string UiName
		{
			get { return Name; }
		}
	}
}
