using System.Runtime.Versioning;
using Microsoft.Win32;

[assembly: SupportedOSPlatform("windows")]


// app to setup a block list

// Software\Policies\Google\Chrome\URLBlocklist

// first, wipe everything from the block list to effectively start over
var regKey = Registry.LocalMachine.CreateSubKey("Software\\Policies\\Google\\Chrome\\UrlBlocklist");
Registry.LocalMachine.DeleteSubKeyTree("Software\\Policies\\Google\\Chrome\\UrlBlocklist");

// recreate the key (will be empty) and block everything
// regKey = Registry.LocalMachine.CreateSubKey("Software\\Policies\\Google\\Chrome\\UrlBlocklist");
// regKey.SetValue("1", "*");

// Software\Policies\Google\Chrome\URLAllowlist
// allow chrome:// (access to settings)

string[] allowed = new string[]
{
    "example.com",
    "https://ssl.server.com",
    "hosting.com/good_path",
    "https://server:8080/path",
    ".exact.hostname.com"
};

// create, then delete the subkey so it effectively erases everything that may already be present
Registry.LocalMachine.CreateSubKey("Software\\Policies\\Google\\Chrome\\UrlAllowlist");
Registry.LocalMachine.DeleteSubKeyTree("Software\\Policies\\Google\\Chrome\\UrlAllowlist");

// add each of the allowed websites
for (int i = 0; i < allowed.Length; i++)
{
    regKey = Registry.LocalMachine.CreateSubKey($"Software\\Policies\\Google\\Chrome\\UrlAllowlist\\{i+1}");
    regKey.SetValue(allowed[i], "");
}

// last, always allow "chrome//*" so settings can be viewed and updated
regKey = Registry.LocalMachine.CreateSubKey($"Software\\Policies\\Google\\Chrome\\UrlAllowlist\\{allowed.Length+1}");
regKey.SetValue("chrome//*", "");
// regKey = Registry.LocalMachine.CreateSubKey("Software\\Policies\\Google\\Chrome\\UrlAllowlist");
// regKey.SetValue("1", "chrome//*");
