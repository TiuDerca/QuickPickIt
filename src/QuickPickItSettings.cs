using System.Windows.Forms;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace QuickPickIt
{
    public class QuickPickItSettings : ISettings
    {
        public QuickPickItSettings()
        {
            Enable = new ToggleNode(false);
            PickUpKey = Keys.F;
            PickupRange = new RangeNode<int>(600, 1, 1000);
            ChestRange = new RangeNode<int>(500, 1, 1000);
            DelayNextClick = new RangeNode<int>(50, 20, 500);
            Sockets = new ToggleNode(true);
            TotalSockets = new RangeNode<int>(6, 1, 6);
            Links = new ToggleNode(true);
            LargestLink = new RangeNode<int>(6, 1, 6);
            RGB = new ToggleNode(true);
            RGBWidth = new RangeNode<int>(2, 1, 2);
            RGBHeight = new RangeNode<int>(4, 1, 4);
            AllDivs = new ToggleNode(true);
            AllCurrency = new ToggleNode(true);
            IgnoreScrollOfWisdom = new ToggleNode(true);
            IgnorePortalScroll = new ToggleNode(true);
            AllUniques = new ToggleNode(true);
            Maps = new ToggleNode(true);
            UniqueMap = new ToggleNode(true);
            MapFragments = new ToggleNode(true);
            MapTier = new RangeNode<int>(1, 1, 16);
            QuestItems = new ToggleNode(true);
            Gems = new ToggleNode(true);
            GemQuality = new RangeNode<int>(1, 0, 20);
            Flasks = new ToggleNode(true);
            FlasksQuality = new RangeNode<int>(1, 0, 20);
            FlasksIlvl = new RangeNode<int>(85, 0, 100);
            GroundChests = new ToggleNode(false);
            WaitPickUp = new ToggleNode(false);
            ShaperItems = new ToggleNode(true);
            SynthesizedItems = new ToggleNode(true);
            ElderItems = new ToggleNode(true);
            HunterItems = new ToggleNode(true);
            VeiledItems = new ToggleNode(true);
            RedeemerItems = new ToggleNode(true);
            CrusaderItems = new ToggleNode(true);
            WarlordItems = new ToggleNode(true);
            FracturedItems = new ToggleNode(true);
            HeistItems = new ToggleNode(true);
            ExpeditionChests = new ToggleNode(true);
            Rares = new ToggleNode(true);
            RareJewels = new ToggleNode(true);
            RareRings = new ToggleNode(true);
            RareRingsilvl = new RangeNode<int>(1, 0, 100);
            RareAmulets = new ToggleNode(true);
            RareAmuletsilvl = new RangeNode<int>(1, 0, 100);
            RareBelts = new ToggleNode(true);
            RareBeltsilvl = new RangeNode<int>(1, 0, 100);
            RareGloves = new ToggleNode(false);
            RareGlovesilvl = new RangeNode<int>(1, 0, 100);
            RareBoots = new ToggleNode(false);
            RareBootsilvl = new RangeNode<int>(1, 0, 100);
            RareHelmets = new ToggleNode(false);
            RareHelmetsilvl = new RangeNode<int>(1, 0, 100);
            RareWeapon = new ToggleNode(false);
            RareWeaponWidth = new RangeNode<int>(2, 1, 2);
            RareWeaponHeight = new RangeNode<int>(4, 1, 4);
            RareWeaponilvl = new RangeNode<int>(1, 0, 100);
            RareArmour = new ToggleNode(false);
            RareArmourilvl = new RangeNode<int>(1, 0, 100);
            RareShield = new ToggleNode(false);
            RareShieldilvl = new RangeNode<int>(1, 0, 100);
            RareShieldWidth = new RangeNode<int>(2, 1, 2);
            RareShieldHeight = new RangeNode<int>(4, 1, 4);
            PickUpEverything = new ToggleNode(false);
            OverrideItemPickup = new ToggleNode(false);
            ShowDebug = new ToggleNode(false);
        }

        public ToggleNode Enable { get; set; }   
        public HotkeyNode PickUpKey { get; set; }
        public RangeNode<int> PickupRange { get; set; }
        public RangeNode<int> ChestRange { get; set; }
        public RangeNode<int> DelayNextClick { get; set; } = new RangeNode<int>(50, 20, 500);

        [Menu("Toggle highlighting : ")]
        public HotkeyNode HighlightToggle { get; set; } = new HotkeyNode(Keys.Z);
        [Menu("After # loot item need toggle highlighting (0 = off)")]
        public RangeNode<int> MinLoop { get; set; } = new RangeNode<int>(5, 0, 10);

        public ToggleNode ShaperItems { get; set; }
        public ToggleNode SynthesizedItems { get; set; }
        public ToggleNode ElderItems { get; set; }
        public ToggleNode HunterItems { get; set; }
        public ToggleNode VeiledItems { get; set; }
        public ToggleNode CrusaderItems { get; set; }
        public ToggleNode WarlordItems { get; set; }
        public ToggleNode RedeemerItems { get; set; }
        public ToggleNode FracturedItems { get; set; }
        public ToggleNode HeistItems { get; set; }
        public ToggleNode ExpeditionChests { get; set; }
        public ToggleNode Rares { get; set; }
        public ToggleNode RareJewels { get; set; }
        public ToggleNode RareRings { get; set; }
        public RangeNode<int> RareRingsilvl { get; set; }
        public ToggleNode RareAmulets { get; set; }
        public RangeNode<int> RareAmuletsilvl { get; set; }
        public ToggleNode RareBelts { get; set; }
        public RangeNode<int> RareBeltsilvl { get; set; }
        public ToggleNode RareGloves { get; set; }
        public RangeNode<int> RareGlovesilvl { get; set; }
        public ToggleNode RareBoots { get; set; }
        public RangeNode<int> RareBootsilvl { get; set; }
        public ToggleNode RareHelmets { get; set; }
        public RangeNode<int> RareHelmetsilvl { get; set; }
        public ToggleNode RareArmour { get; set; }
        public RangeNode<int> RareArmourilvl { get; set; }
        public ToggleNode RareShield { get; set; }
        public RangeNode<int> RareShieldilvl { get; set; }
        public RangeNode<int> RareShieldWidth { get; set; }
        public RangeNode<int> RareShieldHeight { get; set; }
        public ToggleNode RareWeapon { get; set; }
        public RangeNode<int> RareWeaponWidth { get; set; }
        public RangeNode<int> RareWeaponHeight { get; set; }
        public RangeNode<int> RareWeaponilvl { get; set; }
        public ToggleNode Sockets { get; set; }
        public RangeNode<int> TotalSockets { get; set; }
        public ToggleNode Links { get; set; }
        public RangeNode<int> LargestLink { get; set; }
        public ToggleNode RGB { get; set; }
        public RangeNode<int> RGBWidth { get; set; }
        public RangeNode<int> RGBHeight { get; set; }
        public ToggleNode PickUpEverything { get; set; }
        public ToggleNode AllDivs { get; set; }
        public ToggleNode AllCurrency { get; set; }
        public ToggleNode IgnoreScrollOfWisdom { get; set; }
        public ToggleNode IgnorePortalScroll { get; set; }
        public ToggleNode AllUniques { get; set; }
        public ToggleNode Maps { get; set; }
        public RangeNode<int> MapTier { get; set; }
        public ToggleNode UniqueMap { get; set; }
        public ToggleNode MapFragments { get; set; }
        public ToggleNode Gems { get; set; }
        public RangeNode<int> GemQuality { get; set; }
        public ToggleNode Flasks { get; set; }
        public RangeNode<int> FlasksQuality { get; set; }
        public RangeNode<int> FlasksIlvl { get; set; }
        public ToggleNode QuestItems { get; set; }
        public ToggleNode GroundChests { get; set; }
        public ToggleNode WaitPickUp { get; set; }
        public ToggleNode OverrideItemPickup { get; set; }
        public ToggleNode ShowDebug { get; set; }
        public ToggleNode UseWeight { get; set; } = new ToggleNode(false);
        public ToggleNode LazyLooting { get; set; } = new ToggleNode(false);
        public ToggleNode NoLazyLootingWhileEnemyClose { get; set; } = new ToggleNode(false);
        public HotkeyNode LazyLootingPauseKey { get; set; } = new HotkeyNode(Keys.Space);
    }
}
