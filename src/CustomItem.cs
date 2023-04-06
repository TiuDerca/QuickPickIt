using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using Map = ExileCore.PoEMemory.Components.Map;

namespace QuickPickIt
{
    public class CustomItem
    {
        public Func<bool> IsTargeted;
        public bool IsValid;

        public CustomItem(LabelOnGround item, FilesContainer fs)
        {
            LabelOnGround = item;
            var itemItemOnGround = item.ItemOnGround;
            var worldItem = itemItemOnGround?.GetComponent<WorldItem>();
            if (worldItem == null) return;
            var groundItem = worldItem.ItemEntity;
            GroundItem = groundItem;
            Distance = itemItemOnGround.DistancePlayer;
            Path = groundItem?.Path;
            RelativePosition = itemItemOnGround.GridPos;
            if (GroundItem == null) return;

            if (Path is { Length: < 1 })
            {
                DebugWindow.LogMsg($"World: {worldItem.Address:X} P: {Path}", 2);
                DebugWindow.LogMsg($"Ground: {GroundItem.Address:X} P {Path}", 2);
                return;
            }

            IsTargeted = () => itemItemOnGround?.GetComponent<Targetable>()?.isTargeted == true;

            var baseItemType = fs.BaseItemTypes.Translate(Path);

            if (baseItemType != null)
            {
                ClassName = baseItemType.ClassName;
                BaseName = baseItemType.BaseName;
                Width = baseItemType.Width;
                Height = baseItemType.Height;
                if (ClassName.StartsWith("Heist")) IsHeist = true;
            }

            var WeaponClass = new List<string>
            {
                "One Hand Mace",
                "Two Hand Mace",
                "One Hand Axe",
                "Two Hand Axe",
                "One Hand Sword",
                "Two Hand Sword",
                "Thrusting One Hand Sword",
                "Bow",
                "Claw",
                "Dagger",
                "Rune Dagger",
                "Sceptre",
                "Staff",
                "Wand"
            };

            if (GroundItem.HasComponent<Quality>())
            {
                var quality = GroundItem.GetComponent<Quality>();
                Quality = quality.ItemQuality;
            }

            if (GroundItem.HasComponent<Base>())
            {
                var @base = GroundItem.GetComponent<Base>();
                IsElder = @base.isElder;
                IsShaper = @base.isShaper;
                IsHunter = @base.isHunter;
                IsRedeemer = @base.isRedeemer;
                IsCrusader = @base.isCrusader;
                IsWarlord = @base.isWarlord;
                isSynthesized = @base.isSynthesized;
            }

            if (GroundItem.HasComponent<Mods>())
            {
                var mods = GroundItem.GetComponent<Mods>();
                Rarity = mods.ItemRarity;
                IsIdentified = mods.Identified;
                ItemLevel = mods.ItemLevel;
                IsFractured = mods.HaveFractured;
                IsVeiled = mods.ItemMods.Any(m => m.DisplayName.Contains("Veil"));
                isSynthesized = mods.ItemMods.Any(m => m.Name.Contains("SynthesisImplicit"));
            }

            if (GroundItem.HasComponent<Sockets>())
            {
                var sockets = GroundItem.GetComponent<Sockets>();
                IsRGB = sockets.IsRGB;
                Sockets = sockets.NumberOfSockets;
                LargestLink = sockets.LargestLinkSize;
            }

            if (GroundItem.HasComponent<Weapon>()) IsWeapon = true;

            MapTier = GroundItem.HasComponent<Map>() ? GroundItem.GetComponent<Map>().Tier : 0;
            IsValid = true;
        }


        public string BaseName { get; } = "";
        public string ClassName { get; } = "";
        public LabelOnGround LabelOnGround { get; }
        public float Distance { get; set; }
        public Entity GroundItem { get; }
        public Vector2 RelativePosition { get; }

        public MinimapIcon WorldIcon { get;}
        public int Height { get; }
        public bool IsElder { get; }
        public bool IsIdentified { get; }
        public bool IsRGB { get; }
        public bool IsShaper { get; }
        public bool IsHunter { get; }
        public bool IsRedeemer { get; }
        public bool IsCrusader { get; }
        public bool IsWarlord { get; }
        public bool isSynthesized { get; }
        public bool IsHeist { get; }
        public bool IsVeiled { get; }
        public bool IsWeapon { get; }
        public int ItemLevel { get; }
        public int LargestLink { get; }
        public int MapTier { get; }
        public string Path { get; }
        public int Quality { get; }
        public ItemRarity Rarity { get; }
        public int Sockets { get; }
        public int Width { get; }
        public bool IsFractured { get; }
        public bool IsMetaItem { get; set; }

        public override string ToString()
        {
            //return $"{BaseName} ({ClassName}) Dist: {Distance}";
            return $"{BaseName} Dist: {Distance}";
        }
    }
}
