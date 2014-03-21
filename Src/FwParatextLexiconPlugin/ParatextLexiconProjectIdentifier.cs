using System;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class ParatextLexiconProjectIdentifier : IProjectIdentifier
	{
		private readonly FDOBackendProviderType m_backendProviderType;

		public ParatextLexiconProjectIdentifier(FDOBackendProviderType backendProviderType, string projectPath)
		{
			m_backendProviderType = backendProviderType;
			Path = projectPath;
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
