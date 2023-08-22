namespace ValheimItemDataExtract
{
    public class SharedItemData
    {
        public string ItemName { get; set; }
        public bool IsTeleportable { get; set; }
        public bool UsesDurability { get; set; }
        public double MaxDurability { get; set; }
        public double DurabilityPerLevel { get; set; }
        public int MaxStack { get; set; }
        public string DisplayName { get; set; }
        public int MaxQuality { get; set; }
        public ItemType ItemType { get; set; }
    }
}
