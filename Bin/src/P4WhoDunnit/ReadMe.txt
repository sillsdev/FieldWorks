P4WhoDunnit is a tool which identifies who wrote any particular piece of source code in the Perforce Depot.

To integrate it into Visual Studio (the ideal environment), Follow these steps:
		In Visual Studio, select menu Tools | External tools
		Add new tool: Title: WhoDunnit
					  Command: <your FW root>\bin\P4WhoDunnit.exe
					  Arguments: $(ItemPath) $(CurLine) $(CurText)
					  Check "Use Output Window" box

To run it, open up any file you are interested in, and click on the line whose author you wish to discover. Then select menu item Tools | WhoDunnit. The results will appear in a few seconds in the Visual Studio output window.

If you suspect someone else wrote part of the line in an earlier version, select that part of the line and run WhoDunnit again. When text is preselected, it will deduce who first put that text in, even if someone later edited the line.