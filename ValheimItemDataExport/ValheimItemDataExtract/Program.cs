using CsvHelper;

// Print help if asked for
if (args.Length > 0 && new string[] { "/?", "--help" }.Any(s => args[0].ToLower().Equals(s)))
{
    Console.WriteLine($"Usage: ValheimItemDataExtract [ASSETROOT] [CSVFILE]");
    Console.WriteLine($"Extract Valheim item data from unpacked Unity assets within ASSETROOT directory and write them to CSVFILE.");
    Environment.Exit(0);
}

// Prepare what we need in order to run, including using defaults if cmdline args are missing
var PathRoot = args.Length > 0 ? args[0] : @"c:\_temp\ValheimExports\AssetRipper\globalgamemanagers\ExportedProject\";
var PathFile = args.Length > 1 ? args[1] : @".\SharedItemData.csv";
var Localization = new Dictionary<string, string>();
var ItemData = new List<Object>();

// Process assets and save result
Console.WriteLine($"ASSETROOT is {Path.GetFullPath(PathRoot)}.");
LoadLocalization(PathRoot);
ExtractItemData(PathRoot);
SaveItemDataAsCsvFile(PathFile);

// Function to load localization file into a dictionary with name as key and English translation as value
void LoadLocalization(string pathRoot)
{
    var subPath = @"Assets\Resources\localization.txt";
    Console.WriteLine($"Processing file {subPath}...");

    foreach (var line in File.ReadAllText($@"{pathRoot}\{subPath}").Split("\"\n\""))
    {
        var parts = line.Split(new string[] { "\",\"", ",\"" }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && !Localization.ContainsKey(parts[0]))
        {
            Localization[parts[0]] = parts[1];
        }
    }
    Console.WriteLine($"Loaded {Localization.Count} translations.");    
}

// Function to extract item data from prefab files that have it and store it in a list
void ExtractItemData(string pathRoot)
{
    string[] ids = { "m_name", "m_teleportable", "m_useDurability", "m_maxDurability", "m_durabilityPerLevel", "m_maxStackSize", "m_maxQuality", "m_itemType" };
    var subPath = @"Assets\PrefabInstance";
    var prefabs = Directory.GetFiles($@"{pathRoot}/{subPath}", "*.prefab", SearchOption.TopDirectoryOnly);
    Console.WriteLine($"Processing {prefabs.Length} files in directory {subPath}...");

    foreach (var filename in prefabs)
    {
        var sections = File.ReadAllText(filename).Split("m_itemData:\n");
        if (sections.Length < 2)
        {
            continue;
        }
        var data = new Dictionary<string, string>(ids.Length);
        foreach (var line in sections[1].Split("\n").Where(line => line.Contains(':') && ids.Any(id => line.Contains($"{id}:"))))
        {
            var parts = line.Split(':', 2); // Some variable values are strings with : in them, we don't want to split there
            var id = parts[0].Trim();
            var value = parts[1].Trim(new char[] { ' ', '\t', '$' }); // Note the $ since that's preceeding localized string m_name
            data[id] = value;
        }
        if (Localization.TryGetValue(data[ids[0]], out string? englishTranslation))
        {
            // Anonymous type objects will do as records
            ItemData.Add(new 
            {
                ItemName = Path.GetFileNameWithoutExtension(filename),
                IsTeleportable = Convert.ToInt32(data[ids[1]]) != 0,
                UsesDurability = Convert.ToInt32(data[ids[2]]) != 0,
                MaxDurability = Convert.ToInt32(data[ids[3]]),
                DurabilityPerLevel = Convert.ToInt32(data[ids[4]]),
                MaxStack = Convert.ToInt32(data[ids[5]]),
                DisplayName = englishTranslation,
                MaxQuality = Convert.ToInt32(data[ids[6]]),
                ItemType = Convert.ToInt32(data[ids[7]]),
            });
        }
    }
    Console.WriteLine($"Extracted {ItemData.Count} items.");    
}


// Function to save the extracted item data to CSV file, provided out of the box by CSV helper package
void SaveItemDataAsCsvFile(string pathFile)
{
    using (var writer = new StreamWriter(pathFile))
    using (var csv = new CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture))
    {
        csv.WriteRecords(ItemData);
    }
    Console.WriteLine($"Saved {ItemData.Count} items to {Path.GetFullPath(pathFile)}");
}
