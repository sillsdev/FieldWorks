using System;
using SIL.LCModel;

namespace GenerateHCConfig
{
	internal class ProjectIdentifier : IProjectIdentifier
	{
		private readonly BackendProviderType m_backendProviderType;

		public ProjectIdentifier(string projectPath)
		{
			Path = System.IO.Path.GetFullPath(projectPath);
			string ext = System.IO.Path.GetExtension(Path);
			switch (ext.ToLowerInvariant())
			{
				case LcmFileHelper.ksFwDataXmlFileExtension:
					m_backendProviderType = BackendProviderType.kXML;
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

		public BackendProviderType Type
		{
			get { return m_backendProviderType; }
		}

		public string UiName
		{
			get { return Name; }
		}
	}
}
