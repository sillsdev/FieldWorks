#!/bin/sh
gmcs -out:custom.exe -pkg:glade-sharp-2.0 -pkg:gtk-sharp-2.0 gladeapp.cs gladewin.cs gladedlg.cs
