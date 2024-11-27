# Vide - ValheimItemDataExtract tool

Tool to extract Valheim inventory item data from asset files. 

Note: asset files has to be extracted from the game beforehand using another tool, e.g., AssetRipper. Short description on how to do that below. One way of doing it.

# Credits
I didn't come up with the concept of this program at all. User [Brandon-T](https://github.com/Brandon-T) did, in a discussion [here](https://github.com/Wufflez/Loki/issues/30). I just got inspired and wrote my own version in C# and used it as an excuse to explore things related to this. It's somewhat modified to actually work as a real program and not the hack I started out to do.


# How to use AssetRipper
Steps required to extract asset files from Valheim game, using AssetRipper:

1. Get a release (or the source code) from: https://github.com/AssetRipper/AssetRipper 
1. Unpack (or build) AssetRipper and run it.
1. Load Valheim game content into AssetRipper:
	1. Select `File > Open Folder`.
	1. Locate and select Valheim installation folder, i.e., `c:\Program Files (x86)\Steam\steamapps\common\Valheim\`.
	1. Wait for AssetRipper to load game content.
1. Export loaded game content from AssetRipper:
	1. Select `Export > Export all Files`.
	1. Select (create if needed) an export folder ***outside the Valheim installation folder***, e.g., `c:\temp\`.
	1. Wait for AssetRipper to export game content.

Nested within the export folder there should be a folder named `Assets` (e.g., `c:\temp\valheim\ExportedProject\Assets`). 
*The path to this folder is what ValheimItemDataExtract expects as source.*

# Implementation notes
ValheimItemDataExtract is just a simple and very specialized text extractor:

- Get localization data from file `localization.txt` within the `Resources\` subfolder.
- Get item data from prefab files (`*.prefab`) within the `PrefabInstance\` subfolder.
- Export item data to a CSV (Comma Separated Values) file, on item per line.
  
Assumptions made (that seems to work good enough):

- The name of a prefab file (without .prefab extension) equals the name of the item it defines.
	- Not true beginning with Valheim 0.218.15 (Ashlands). Item data for some items moved to a new file with suffix '_0'.
	- For now, fix is just to remove the suffix. Probably should read name of item from within file instead.
- If a prefab file has a section `m_itemData` in it, the data within that section defines item data.
- If a prefab file has item data defined, it represents an inventory item only if the item name has a valid translation.

