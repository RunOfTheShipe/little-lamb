using System.Runtime.Versioning;
using Microsoft.Win32;

[assembly: SupportedOSPlatform("windows")]

// TODO@BMS: Allow this to be configured from the command line
// Read the file, ignoring any empty lines or lines starting with #
var allowed = File.ReadAllLines("allowed.txt")
    .Select(str => str.Trim())
    .Where(str => !String.IsNullOrEmpty(str))
    .Where(str => !str.StartsWith("#"))
    .ToArray();

// Chrome allows the registry to be used to configure allow/block lists
// of websites. In this particular case, we only want a small subset of
// the web to be accessible, so the block list blocks everything and the
// all list allows the sites we want (NOTE: Chrome documentation indicates
// the allow list takes precedence over the block list, which allows this
// to work). Below is an example of a registry script that mimics what
// we're trying to do here:
/*
    Windows Registry Editor Version 5.00
    ; chrome version: 108.0.5359.125

    [HKEY_LOCAL_MACHINE\Software\Policies\Google\Chrome\URLAllowlist]
    "1"="example.com"
    "2"="https://ssl.server.com"
    "3"="hosting.com/good_path"
    "4"="https://server:8080/path"
    "5"=".exact.hostname.com"

    [HKEY_LOCAL_MACHINE\Software\Policies\Google\Chrome\URLBlocklist]
    "1"="*"
*/

// --- Block List --- //

// Software\Policies\Google\Chrome\URLBlocklist
const string BlockListKey = "Software\\Policies\\Google\\Chrome\\URLBlocklist";
const string AllowListKey = "Software\\Policies\\Google\\Chrome\\URLAllowlist";

// first, wipe everything from the block list to effectively start over
Registry.LocalMachine.CreateSubKey(BlockListKey);
Registry.LocalMachine.DeleteSubKeyTree(BlockListKey);
var regKey = Registry.LocalMachine.CreateSubKey(BlockListKey);
regKey.SetValue("1", "*");

// --- Allow List --- //

// create, delete, then create the allow key, effectively clearing anything that
// had been previously setup
Registry.LocalMachine.CreateSubKey(AllowListKey);
Registry.LocalMachine.DeleteSubKeyTree(AllowListKey);
regKey = Registry.LocalMachine.CreateSubKey(AllowListKey);

// add each of the allowed websites
for (int i = 0; i < allowed.Length; i++)
{
    regKey.SetValue($"{i+1}", allowed[i]);
}

// last, always allow "chrome://*" so settings can be viewed and updated
regKey.SetValue($"{allowed.Length+1}", "chrome://*");
