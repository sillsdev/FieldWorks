using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SIL.FieldWorks.Tools;

namespace FwBuildTasks
{
	/// <summary>
	/// Custom build task to import an IDL file.
	/// Adapted from the custom Nant task bin\nant\src\FwTasks\IdlImpTask.
	/// </summary>
	// A typical invocation is
	//<UsingTask TaskName="IdlImp" AssemblyFile="..\..\..\Build\FwBuildTasks.dll"/>
	//<ItemGroup>
	//    <Namespaces Include="SIL.Utils"/>
	//    <Namespaces Include="SIL.Utils.ComTypes"/>
	//</ItemGroup>

	//<ItemGroup>
	//    <KernelSources Include="$(OutDir)/../Common/FwKernelTlb.idl"/>
	//</ItemGroup>
	//<ItemGroup>
	//    <KernelIdhFiles Include="../../Kernel/FwKernel.idh"/>
	//    <KernelIdhFiles Include="../../Kernel/TextServ.idh"/>
	//    <KernelIdhFiles Include="../../Language/Language.idh"/>
	//    <KernelIdhFiles Include="../../Language/Render.idh"/>
	//</ItemGroup>
	//<ItemGroup>
	//    <KernelInputs Include="@(KernelIdhFiles)" />
	//    <KernelInputs Include="@(KernelSources)" />
	//</ItemGroup>
	//<Target Name="FwKernelCs" Inputs="@(KernelInputs)" Outputs="FwKernel.cs">
	//    <IdlImp Output="FwKernel.cs"
	//            Namespace="SIL.FieldWorks.Common.COMInterfaces"
	//            Sources="@(KernelSources)"
	//            Namespaces="@(Namespaces)"
	//            IdhFiles="@(KernelIdhFiles)">
	//    </IdlImp>
	//</Target>
	// This might well be combined with an AfterClean task like this to get rid of the files IdlImp creates:
	//<Target Name="AfterClean">
	//    <Delete Files="FwKernel.cs;$(OutDir)/../Common/FwKernelTlb.iip;" ContinueOnError="true"/>
	//</Target>
	public class IdlImp : Task
	{
		public IdlImp()
		{
			CreateXmlComments = true;
		}

		/// <summary>Namespace.</summary>
		[Required]
		public string Namespace { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Additional namespaces that are used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] UsingNamespaces { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Source IDH files used to retrieve comments from.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Required]
		public ITaskItem[] IdhFiles { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// List of files for resolving referenced types
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public ITaskItem[] ReferenceFiles { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creation of dummy XML comments. Defaults to <c>true</c>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CreateXmlComments { get; set; }

		/// <summary>
		/// The IDL file (typically only one) we want to compile.
		/// </summary>
		[Required]
		public ITaskItem[] Sources { get; set; }

		StringCollection GetFilesFrom(ITaskItem[] source)
		{
			var result = new StringCollection();
			if (source == null)
				return result;
			foreach (var item in source)
				result.Add(item.ItemSpec);
			return result;
		}

		[Required]
		public string Output { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do the import
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool Execute()
		{
			try
			{
				IDLImporter importer = new IDLImporter();
				var namespaces = new List<string>();
				foreach (var s in GetFilesFrom(UsingNamespaces))
				{
					Log.LogMessage(MessageImportance.Low, "Using namespace " + s);
					namespaces.Add(s);
				}


				foreach (var idlFile in Sources)
				{
					//Log.LogMessage(MessageImportance.Normal, "Creating IDL File " + Path.GetFileName(idlFile.ItemSpec));
					Log.LogMessage(MessageImportance.Normal, "Processing IDL File " + Path.GetFileName(idlFile.ItemSpec) + " to produce " + Output);
					foreach (var s in GetFilesFrom(IdhFiles))
						Log.LogMessage(MessageImportance.Low, "IDH: " + s);
					foreach (var s in GetFilesFrom(ReferenceFiles))
						Log.LogMessage(MessageImportance.Low, "references: " + s);
					bool fOk = importer.Import(namespaces, idlFile.ItemSpec, null,
						Output, Namespace, GetFilesFrom(IdhFiles),
						GetFilesFrom(ReferenceFiles), CreateXmlComments);
					if (!fOk)
					{
						Log.LogMessage(MessageImportance.High, "IDL Import failed: data has errors " + Path.GetFileName(idlFile.ItemSpec));
						return false;
					}
				}
			}

			catch (Exception e)
			{
				Log.LogMessage(MessageImportance.High, "IDL Import threw an exception: " + e.Message);
				return false;
			}
			return true;
		}
	}
}
