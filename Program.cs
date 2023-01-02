using System.CommandLine;
using System.Runtime.Versioning;
using Microsoft.Win32;

[assembly: SupportedOSPlatform("windows")]

public class Program
{
    public static int Main(string[] args)
    {
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

        // NOTE: Edge works in a very similar fashion, just using a slightly different
        // set of registry keys

        // ---- CLI Options ---- //
        var fileOption = new Option<FileInfo>(
            name: "--allowed",
            description: "The file containing the set of allowed websites.",
            parseArgument: (result) =>
            {
                string? filePath = result.Tokens.Single().Value;
                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = $"File could not be found: {filePath}";
                }
                return new FileInfo(filePath);
            });

        var productOption = new Option<string>(
            name: "--product",
            description: "The product being configured",
            getDefaultValue: () => "Chrome");

        // ---- CLI Commands ---- //
        var applyCommand = new Command("apply", "Applies the policy")
        {
            fileOption,
            productOption
        };
        applyCommand.SetHandler(ApplyPolicies, fileOption, productOption);

        var removeCommand = new Command("remove", "Removes the policy")
        {
            productOption
        };
        removeCommand.SetHandler(RemovePolicies, productOption);

        // root command just aggregates the other commands
        var rootCommand = new RootCommand("A simple application to allow locking down a Chromium based web browser to a limited set of sites that it can visit.");
        rootCommand.AddCommand(applyCommand);
        rootCommand.AddCommand(removeCommand);

        return rootCommand.Invoke(args);
    }

    public static void ApplyPolicies(FileInfo file, string product)
    {
        // Read the file, ignoring any empty lines or lines starting with #
        var allowed = File.ReadAllLines(file.FullName)
            .Select(str => str.Trim())
            .Where(str => !String.IsNullOrEmpty(str))
            .Where(str => !str.StartsWith("#"))
            .ToArray();

        // --- Block List --- //

        // create/open the key, then clear anything that had been previously configured
        var regKey = Registry.LocalMachine.CreateSubKey(GetBlockListKey(product));
        foreach (var name in regKey.GetValueNames())
        {
            regKey.DeleteValue(name);
        }
        regKey.SetValue("1", "*");

        // --- Allow List --- //

        // create/open the key, then clear anything that had been previously configured
        regKey = Registry.LocalMachine.CreateSubKey(GetAllowListKey(product));
        foreach (var name in regKey.GetValueNames())
        {
            regKey.DeleteValue(name);
        }

        // add each of the allowed websites
        for (int i = 0; i < allowed.Length; i++)
        {
            regKey.SetValue($"{i+1}", allowed[i]);
        }

        // last, always allow "chrome://*" so settings can be viewed and updated (or edge)
        regKey.SetValue($"{allowed.Length+1}", $"{product.ToLowerInvariant()}://*");
    }

    public static void RemovePolicies(string product)
    {
        // create the key and then delete it, for both the block and allow
        // lists; this is kind of hacky, but it works
        string blockListKey = GetBlockListKey(product);
        Registry.LocalMachine.CreateSubKey(blockListKey);
        Registry.LocalMachine.DeleteSubKey(blockListKey);

        string allowListKey = GetAllowListKey(product);
        Registry.LocalMachine.CreateSubKey(allowListKey);
        Registry.LocalMachine.DeleteSubKey(allowListKey);
    }

    private static string GetBlockListKey(string product)
    {
        return GetKey(product, "Block");
    }

    private static string GetAllowListKey(string product)
    {
        return GetKey(product, "Allow");
    }

    private static string GetKey(string product, string list)
    {
        string company = "Google";
        if (String.Equals(product, "Edge", StringComparison.InvariantCultureIgnoreCase))
        {
            company = "Microsoft";
        }

        return $"Software\\Policies\\{company}\\{product}\\URL{list}list";
    }
}
