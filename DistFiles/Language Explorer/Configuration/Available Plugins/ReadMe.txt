Extensions for Language Explorer go here. Files related to each extension should be in a separate folder in this folder.

A special XML file (named ExtensionManager.xml) needs to be added to each folder. That file will be used by the system to let the user install/uninstall extension plugins. The file will have contents along these lines:

<?xml version="1.0" encoding="UTF-8"?>
<manager name="Concorder" description="Locate 'users' of objects.">
	<configfiles targetdir="Extensions\Concorder">
		<file name="MainConfigurationExtension.xml"/>
		<file name="strings-en.xml"/>
	</configfiles>
	<dlls>
		<file name="RBRExtensions.dll" shared="true"/>
	</dlls>
</manager>