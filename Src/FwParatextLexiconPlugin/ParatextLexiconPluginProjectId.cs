// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class ParatextLexiconPluginProjectId : IProjectIdentifier
	{
		private readonly BackendProviderType m_backendProviderType;

		public ParatextLexiconPluginProjectId(BackendProviderType backendProviderType, string projectPath)
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
