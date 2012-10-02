Extensions for Language Explorer Lists Area go here.

Files related to each extension should be in a separate folder in this folder. There must also be a main configuration file named AreaConfigurationExtension.xml located in each such subfolder. Code files can be most anywhere, as long as they can be located from information in the AreaConfigurationExtension.xml file or from the custom Part and Layout file(s).

Speaking of custom Part and Layout file(s)... FLEx automatically loads all official Parts and Layouts that are located in the main Parts folder, as well as in the Language Explorer\Configuration\Parts folders. While one could simply add these custom files to one of those folders, a better way is to put them somewhere else, and have your code load them using the AddCustomFiles method of the Inventory class (probably once for each of the two instances (Parts and Layout). Once can access those via:

	// Add custom parts and layouts.
	string[] files = new string[1];
	files[0] = DirectoryFinder.GetFWInstallSubDirectory(@"Language Explorer\Configuration\Lists\Extensions\MyExtension\");
	Inventory inv = Inventory.GetInventory("parts");
	inv.AddCustomFiles(files);
	inv = Inventory.GetInventory("layouts");
	inv.AddCustomFiles(files);