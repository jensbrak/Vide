using CsvHelper;
using System.Globalization;

// Command line arguments and switches
string[] SwitchHelp = ["/?", "/h", "--help"];
string[] SwitchVerbose = ["/v", "--verbose"];
string[] SwitchLocalization = ["/l", "--localization"];
bool SwitchEnabled(string[] sw) => args.Length > 0 && args.Any(a => sw.Any(s => a.ToLower().Equals(s)));

// Check switches that affect program behaviour
var VerboseMode = true;//SwitchEnabled(SwitchVerbose);
if (SwitchEnabled(SwitchHelp))
{
    ShowHelpThenExit();
}

// Setup paths and data containers for data extraction
var PathSource = Path.GetFullPath(args.Length > 0 ? args[0] :  @".\");
var PathDestination = args.Length > 1 ? args[1] : "SharedItemData.csv";
var Localization = new Dictionary<string, string>();
var ItemData = new List<object>();

// Data extration steps
Inform($"Asset root is '{PathSource}'.", isVerbose: true);
Inform("Extracting item data...");
EnsureDir(PathSource);
LoadLocalization();
ExtractItemData();
SaveItemData();
Inform("Item data extracted successfully");

// Print information message as verbose or normal
void Inform(string message, bool isVerbose = false)
{
    if (!VerboseMode && isVerbose)
    {
        return;
    }
    Console.WriteLine(message);
}

// Show help message and then exit program gracefully
void ShowHelpThenExit() => 
    Exit("Extracts Valheim inventory item data from asset files.\n\n" +
        $"{AppDomain.CurrentDomain.FriendlyName} source destination [{SwitchVerbose[0]}] [{SwitchLocalization[0]}]\n\n" +
        $"  {"source",-12} Specifies the path to the asset root directory.\n" +
        $"  {"destination",-12} Specifies the path to the directory to export to.\n" +
        $"  {SwitchVerbose[0],-12} Verbose mode: print additional info\n" +
        $"  {SwitchLocalization[0],-12} Dump extracted localization data to file\n" +
        $"\nAsset files has to be extracted from the game beforehand, using some another tool.\n" +
        $"Tool that has been verified to work for this is AssetRipper.\n\n", 0);

// Exit program prematurely after printing reason (error message if exit code represents an error, ie is non zero)
void Exit(string reason, int exitCode)
{
    Inform(exitCode != 0 ? $"ERROR: {reason}\nExtraction aborted!" : reason);
    Environment.Exit(exitCode);
}

// Verify that a directory exists/is accessible and if not exit program with immediately with error
void EnsureDir(string path)
{
    if (!Directory.Exists(path))
    {
        Exit($"Directory does not exist or is inaccessible: '{path}", 2);
    }
}

// Verify that a file exists/is accessible and if not exit program with immediately with error
void EnsureFile(string path)
{
    if (!File.Exists(path))
    {
        Exit($"File does not exist or is inaccessible: '{path}'", 1);
    }
}

// Load English localization from main localization file into dictionary with names as keys and corresponding translations as values
void LoadLocalization()
{
    var sourceFile = Path.Combine("Resources", "localization.txt");
    var fullPathSourceFile = Path.Combine(PathSource, sourceFile);

    Inform($"Processing localization file '{(VerboseMode ? fullPathSourceFile : sourceFile)}'...");
    EnsureFile(fullPathSourceFile);

    // Seems localization file is a CSV (but with plenty of duplicates and empty values)
    using (var reader = new StreamReader(fullPathSourceFile))
    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    {
        while (csv.Read())
        {
            var key = csv[0];
            var value = csv[1];
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value) || Localization.ContainsKey(key))
            {
                continue;
            }
            Localization.Add(key, value);
        }
    } 
    Inform($"Loaded {Localization.Count} translations.");   
    
    if (SwitchEnabled(SwitchLocalization))
    {
        var destinationFile = "LocalizationData.csv";
        var fullPathDestinationFile = Path.Combine(PathDestination, destinationFile);
        SaveDataAsCsvFile(Localization.Select(kvp => new {Name=kvp.Key, Text=kvp.Value}), fullPathDestinationFile);
        Inform($"Localization data saved to '{destinationFile}'");
    }
}

// Extract items from all prefab files that have item data defined, but add only those that has a translation
void ExtractItemData()
{
    string[] keys = ["m_name", "m_teleportable", "m_useDurability", "m_maxDurability", "m_durabilityPerLevel", "m_maxStackSize", "m_maxQuality", "m_itemType", "m_worldLevel", "m_pickedUp"];
    var filePattern = "*.prefab";
    var subDir = "PrefabInstance";
    var relPath = Path.Combine(subDir, filePattern);
    var fullPath = Path.Combine(PathSource, subDir);

    Inform($"Processing prefab files '{relPath}'...");
    EnsureDir(fullPath);

    var prefabs = Directory.GetFiles(fullPath, filePattern, SearchOption.TopDirectoryOnly);
    if (prefabs.Length == 0)
    {
        Exit($"No prefab files ({filePattern}) found in: '{fullPath}'", 3);
    }

    char[] valueTrimChars = [' ', '\t', '$']; // Note the $ since that's preceding localized strings
    var discardedCount = 0;
    var ignoredCount = 0;
    Inform($"Processing {prefabs.Length} prefab files", isVerbose: true);

    for (int i = 0; i < prefabs.Length; i++)
    {
        var filename = prefabs[i];
        var itemId = Path.GetFileNameWithoutExtension(filename);
        var sections = File.ReadAllText(filename).Split("m_itemData:\n");
        if (sections.Length < 2)
        {
            // No item data in this prefab file means not an inventory item at all. Skip to next
            ignoredCount++;
            Inform($"{"",2}[{i+1,4}] {"Ignored",9}: {itemId}", isVerbose: true);
            continue;
        }

        // Extract data for this item
        var data = new Dictionary<string, string>(keys.Length);
        foreach (var line in sections[1].Split("\n").Where(line => line.Contains(':') && keys.Any(id => line.Contains($"{id}:"))))
        {
            var parts = line.Split(':', 2); // Some values are strings with ':' in them. We don't want to split them, take first split only
            var key = parts[0].Trim();
            var value = parts[1].Trim(valueTrimChars); 
            data[key] = value;
        }

        // "name" is used specifically for localization references, but "id" is what we export as name
        var itemName = data[keys[0]];

        // Only add to extracted items if the item has a valid translation 
        if (Localization.TryGetValue(itemName, out string? itemText))
        {
            // Anonymous type objects will do as records
            var id = new 
            {
                ItemName = itemId.EndsWith("_0") ? itemId[..^2] : itemId, // Fix for prefab file suffix introduced in 0.218.15 (Ashlands)
                IsTeleportable = Convert.ToInt32(data[keys[1]]) != 0,
                UsesDurability = Convert.ToInt32(data[keys[2]]) != 0,
                MaxDurability = Convert.ToInt32(data[keys[3]]),
                DurabilityPerLevel = Convert.ToInt32(data[keys[4]]),
                MaxStack = Convert.ToInt32(data[keys[5]]),
                DisplayName = itemText,
                MaxQuality = Convert.ToInt32(data[keys[6]]),
                ItemType = Convert.ToInt32(data[keys[7]]),
            };
            var itemIdFixed = id.ItemName != itemId ? $"{itemId} -> {id.ItemName}" : itemId;
            ItemData.Add(id);
            Inform($"{"",2}[{i+1,4}] {"Added",9}: {itemIdFixed} ({itemText})", isVerbose:true);            
        }
        else
        {
            // Not an inventory item after all. Discard but count for information purpose.
            discardedCount++;
            Inform($"{"",2}[{i+1,4}] {"Discarded",9}: {itemId} ({itemName})", isVerbose:true);
        }
    }
    Inform($"{prefabs.Length} prefab files processed{(VerboseMode ? $": {ignoredCount} ignored, {discardedCount} discarded, {ItemData.Count} used" : "")}.");
}

void SaveDataAsCsvFile(IEnumerable<object> data, string filepath)
{
    try
    {
        using (var writer = new StreamWriter(filepath))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(data);            
        }
        Inform($"Saved {data.Count()} items to '{filepath}'.");
    }
    catch (Exception ex)
    {
        Exit($"Failed to save file '{filepath}'. {ex.Message}", 3);
    }
}

// Save all extracted items and their data to a CSV file
void SaveItemData()
{
    SaveDataAsCsvFile(ItemData, PathDestination);

}