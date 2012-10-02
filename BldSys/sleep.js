if (WScript.Arguments.Length < 1)
{
	Echo("Usage: sleep #ms");
	WScript.Quit();
}


WScript.Sleep(WScript.Arguments.Item(0));
