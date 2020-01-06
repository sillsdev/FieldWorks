// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace GenerateHCConfig
{
	internal class ProjectIdentifier : IProjectIdentifier
	{
		public ProjectIdentifier(string projectPath)
		{
			Path = System.IO.Path.GetFullPath(projectPath);
			var ext = System.IO.Path.GetExtension(Path);
			switch (ext.ToLowerInvariant())
			{
				case LcmFileHelper.ksFwDataXmlFileExtension:
					Type = BackendProviderType.kXML;
					break;
			}
		}

		public bool IsLocal => true;

		public string Path { get; set; }

		public string ProjectFolder => System.IO.Path.GetDirectoryName(Path);

		public string SharedProjectFolder => ProjectFolder;

		public string ServerName => null;

		public string Handle => Name;

		public string PipeHandle
		{
			get { throw new NotSupportedException(); }
		}

		public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);

		public BackendProviderType Type { get; }

		public string UiName => Name;
	}
}