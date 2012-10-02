var shellObj = new ActiveXObject("WScript.SHell");
var env = shellObj.Environment("user");
env.Item(WScript.Arguments.Item(0)) = WScript.Arguments.Item(1);