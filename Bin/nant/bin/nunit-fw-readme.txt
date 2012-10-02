NAnt.NUnit2Tasks.dll depends on nunit.core.dll.

If we use a newer version of NUnit (e.g. 2.4.0) while NAnt still uses the older version (e.g. 2.2.8), it doesn't work or we get ugly warnings.

The solution is to rename the new version of nunit.core.dll to nunit.core.<version>.dll and edit the nunit-console.exe.config file:

		<assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
			<dependentAssembly>
				<assemblyIdentity name="nunit.core" publicKeyToken="96d09a1eb7f44a77" />
				<codeBase version="2.4.0.2" href="nunit.core.2.4.dll" />
			</dependentAssembly>
		</assemblyBinding>

Once we upgrade to a version of NAnt that uses the same NUnit version we can get rid of this entry in the config file, and also delete nunit.core.<version>.dll.