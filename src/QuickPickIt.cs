using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;
using System;
using System.IO;
using System.Threading.Tasks;
using Input = ExileCore.Input;
using ExileCore.Shared.Enums;

namespace QuickPickIt
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416")]
    public class QuickPickIt : BaseSettingsPlugin<QuickPickItSettings>
    {
        //private const string QuickPickItRuleDirectory = "Rules";
        private TimeCache<List<LabelOnGround>> ChestLabelCacheList { get; set; }
        private readonly Stopwatch _debugTimer = Stopwatch.StartNew();
        public Stopwatch WaitingTime = new Stopwatch();

        private readonly WaitTime _wait2Ms = new WaitTime(2);
        private Vector2 _clickWindowOffset;

        private HashSet<string> _ignoreRules;
        private WaitTime _delayNextClick;
        private uint _coroutineCounter;
        private Coroutine _quickPickItCoroutine;
        private readonly WaitTime _wait10Ms = new WaitTime(10);
        private readonly List<string> _customItems = new List<string>();

        public static QuickPickIt Controller { get; set; }

        private TimeCache<List<CustomItem>> _currentLabels;
        public int GetItemCount { get; set; }  = 0;
        private bool _canPause = true;


        //private bool _enabled;

        public QuickPickIt()
        {
            Name = "QuickPickIt";
        }

        //private List<string> QuickPickItFiles { get; set; }


        public override bool Initialise()
        {
            _currentLabels = new TimeCache<List<CustomItem>>(UpdateCurrentLabels, 300); // alexs idea <3
            
            #region Register keys

            Settings.PickUpKey.OnValueChanged += () => Input.RegisterKey(Settings.PickUpKey);
            Input.RegisterKey(Settings.PickUpKey);
            Input.RegisterKey(Keys.Escape);

            #endregion
            
            Controller = this;
            _quickPickItCoroutine = new Coroutine(MainWorkCoroutine(), this, "Pick It");
            Core.ParallelRunner.Run(_quickPickItCoroutine);
            _quickPickItCoroutine.Pause();
            _debugTimer.Reset();
            //_workCoroutine = new WaitTime(Settings.ExtraDelay);
            //Settings.ExtraDelay.OnValueChanged += (sender, i) => _workCoroutine = new WaitTime(i);
            
            _delayNextClick = new WaitTime(Settings.DelayNextClick);
            Settings.DelayNextClick.OnValueChanged += (_, i) => _delayNextClick = new WaitTime(i);

            ChestLabelCacheList = new TimeCache<List<LabelOnGround>>(UpdateChestList, 200);
            LoadRuleFiles();
            LoadCustomItems();
            return true;
        }

        // bad idea to add hard coded pickups.
        private void LoadCustomItems()
        {
            _customItems.Add("Treasure Key");
            _customItems.Add("Silver Key");
            _customItems.Add("Golden Key");
            _customItems.Add("Flashpowder Keg");
            _customItems.Add("Divine Life Flask");
            _customItems.Add("Quicksilver Flask");
            _customItems.Add("Stone of Passage");
        }

        private IEnumerator MainWorkCoroutine()
        {
            while (true)
            {
                yield return RefreshHighlight();

                yield return FindItemToPick();

                _coroutineCounter++;
                _quickPickItCoroutine.UpdateTicks(_coroutineCounter);
                //yield return _workCoroutine;
            }
        }

        private IEnumerator RefreshHighlight()
        {
            _canPause = false;
            if (GetItemCount >= Settings.MinLoop && Settings.MinLoop >= 1)
            {
                yield return Input.KeyPress(Settings.HighlightToggle.Value);
                Task.Delay(44);
                yield return Input.KeyPress(Settings.HighlightToggle.Value);
                //Task.Delay(44);
                //Input.KeyDown(Settings.HighlightToggle.Value);
                //Task.Delay(44);
                //Input.KeyUp(Settings.HighlightToggle.Value);
                //Task.Delay(44);
                GetItemCount = 0;
                yield return null;
            }
            _canPause = true;

        }

        public override void DrawSettings()
        {
            Settings.PickUpKey = ImGuiExtension.HotkeySelector("Pickup Key: " + Settings.PickUpKey.Value.ToString(), Settings.PickUpKey);
            Settings.GroundChests.Value = ImGuiExtension.Checkbox("Click Chests If No Items Around", Settings.GroundChests);
            Settings.PickupRange.Value = ImGuiExtension.IntSlider("Pickup Radius", Settings.PickupRange);
            Settings.ChestRange.Value = ImGuiExtension.IntSlider("Chest Radius", Settings.ChestRange);
            Settings.DelayNextClick.Value = ImGuiExtension.IntSlider("Delay for next click", Settings.DelayNextClick);
            Settings.WaitPickUp.Value = ImGuiExtension.Checkbox("or wait pickup item for new click", Settings.WaitPickUp);
            Settings.LazyLooting.Value = ImGuiExtension.Checkbox("Use Lazy Looting", Settings.LazyLooting);
            if (Settings.LazyLooting)
                Settings.NoLazyLootingWhileEnemyClose.Value = ImGuiExtension.Checkbox("No lazy looting while enemy is close", Settings.NoLazyLootingWhileEnemyClose);
            Settings.LazyLootingPauseKey.Value = ImGuiExtension.HotkeySelector("Pause lazy looting for 2 sec: " + Settings.LazyLootingPauseKey.Value, Settings.LazyLootingPauseKey);
            
            Settings.MinLoop.Value = ImGuiExtension.IntSlider("After # loot item need toggle highlighting (0 = off)", Settings.MinLoop);
            Settings.HighlightToggle = ImGuiExtension.HotkeySelector("Toggle highlighting key: " + Settings.HighlightToggle.Value.ToString(), Settings.HighlightToggle);

            Settings.ShowDebug.Value = ImGuiExtension.Checkbox("ShowDebug", Settings.ShowDebug);

            

            if (ImGui.CollapsingHeader("Item Logic", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.TreeNode("Influence Types"))
                {
                    Settings.SynthesizedItems.Value = ImGuiExtension.Checkbox("Synthesized Items", Settings.SynthesizedItems);
                    Settings.ShaperItems.Value = ImGuiExtension.Checkbox("Shaper Items", Settings.ShaperItems);
                    Settings.ElderItems.Value = ImGuiExtension.Checkbox("Elder Items", Settings.ElderItems);
                    Settings.HunterItems.Value = ImGuiExtension.Checkbox("Hunter Items", Settings.HunterItems);
                    Settings.CrusaderItems.Value = ImGuiExtension.Checkbox("Crusader Items", Settings.CrusaderItems);
                    Settings.WarlordItems.Value = ImGuiExtension.Checkbox("Warlord Items", Settings.WarlordItems);
                    Settings.RedeemerItems.Value = ImGuiExtension.Checkbox("Redeemer Items", Settings.RedeemerItems);
                    Settings.FracturedItems.Value = ImGuiExtension.Checkbox("Fractured Items", Settings.FracturedItems);
                    Settings.VeiledItems.Value = ImGuiExtension.Checkbox("Veiled Items", Settings.VeiledItems);
                    ImGui.Spacing();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Links/Sockets/RGB"))
                {
                    Settings.RGB.Value = ImGuiExtension.Checkbox("RGB Items", Settings.RGB);
                    Settings.RGBWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RGBWidth", Settings.RGBWidth);
                    Settings.RGBHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RGBHeight", Settings.RGBHeight);
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    Settings.TotalSockets.Value = ImGuiExtension.IntSlider("##Sockets", Settings.TotalSockets);
                    ImGui.SameLine();
                    Settings.Sockets.Value = ImGuiExtension.Checkbox("Sockets", Settings.Sockets);
                    Settings.LargestLink.Value = ImGuiExtension.IntSlider("##Links", Settings.LargestLink);
                    ImGui.SameLine();
                    Settings.Links.Value = ImGuiExtension.Checkbox("Links", Settings.Links);
                    ImGui.Separator();
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Overrides"))
                {
                    Settings.UseWeight.Value = ImGuiExtension.Checkbox("Use Weight", Settings.UseWeight);
                    Settings.IgnoreScrollOfWisdom.Value = ImGuiExtension.Checkbox("Ignore Scroll Of Wisdom", Settings.IgnoreScrollOfWisdom);
                    Settings.IgnorePortalScroll.Value = ImGuiExtension.Checkbox("Ignore Portal Scroll", Settings.IgnorePortalScroll);
                    Settings.PickUpEverything.Value = ImGuiExtension.Checkbox("Pickup Everything", Settings.PickUpEverything);
                    Settings.AllDivs.Value = ImGuiExtension.Checkbox("All Divination Cards", Settings.AllDivs);
                    Settings.AllCurrency.Value = ImGuiExtension.Checkbox("All Currency", Settings.AllCurrency);
                    Settings.AllUniques.Value = ImGuiExtension.Checkbox("All Uniques", Settings.AllUniques);
                    Settings.QuestItems.Value = ImGuiExtension.Checkbox("Quest Items", Settings.QuestItems);
                    Settings.Maps.Value = ImGuiExtension.Checkbox("##Maps", Settings.Maps);
                    ImGui.SameLine();
                    if (ImGui.TreeNode("Maps"))
                    {
                        Settings.MapTier.Value = ImGuiExtension.IntSlider("Lowest Tier", Settings.MapTier);
                        Settings.UniqueMap.Value = ImGuiExtension.Checkbox("All Unique Maps", Settings.UniqueMap);
                        Settings.MapFragments.Value = ImGuiExtension.Checkbox("Fragments", Settings.MapFragments);
                        ImGui.Spacing();
                        ImGui.TreePop();
                    }

                    
                    Settings.Gems.Value = ImGuiExtension.Checkbox("Gems", Settings.Gems);
                    ImGui.SameLine();
                    Settings.GemQuality.Value = ImGuiExtension.IntSlider("##Gems", "Lowest Quality", Settings.GemQuality);
                    
                    //ImGui.SameLine();
                    Settings.Flasks.Value = ImGuiExtension.Checkbox("Flasks", Settings.Flasks);
                    Settings.FlasksQuality.Value = ImGuiExtension.IntSlider( "Flasks Lowest Quality", Settings.FlasksQuality);
                    Settings.FlasksIlvl.Value = ImGuiExtension.IntSlider( "Flasks Lowest Ilvl", Settings.FlasksIlvl);
                    ImGui.Separator();
                    ImGui.TreePop();
                }
                Settings.HeistItems.Value = ImGuiExtension.Checkbox("Heist Items", Settings.HeistItems);
                Settings.ExpeditionChests.Value = ImGuiExtension.Checkbox("Expedition Chests", Settings.ExpeditionChests);

                Settings.Rares.Value = ImGuiExtension.Checkbox("##Rares", Settings.Rares);
                ImGui.SameLine();
                if (ImGui.TreeNode("Rares##asd"))
                {
                    Settings.RareJewels.Value = ImGuiExtension.Checkbox("Jewels", Settings.RareJewels);
                    Settings.RareRingsilvl.Value = ImGuiExtension.IntSlider("##RareRings", "Lowest iLvl", Settings.RareRingsilvl);
                    ImGui.SameLine();
                    Settings.RareRings.Value = ImGuiExtension.Checkbox("Rings", Settings.RareRings);
                    Settings.RareAmuletsilvl.Value = ImGuiExtension.IntSlider("##RareAmulets", "Lowest iLvl", Settings.RareAmuletsilvl);
                    ImGui.SameLine();
                    Settings.RareAmulets.Value = ImGuiExtension.Checkbox("Amulets", Settings.RareAmulets);
                    Settings.RareBeltsilvl.Value = ImGuiExtension.IntSlider("##RareBelts", "Lowest iLvl", Settings.RareBeltsilvl);
                    ImGui.SameLine();
                    Settings.RareBelts.Value = ImGuiExtension.Checkbox("Belts", Settings.RareBelts);
                    Settings.RareGlovesilvl.Value = ImGuiExtension.IntSlider("##RareGloves", "Lowest iLvl", Settings.RareGlovesilvl);
                    ImGui.SameLine();
                    Settings.RareGloves.Value = ImGuiExtension.Checkbox("Gloves", Settings.RareGloves);
                    Settings.RareBootsilvl.Value = ImGuiExtension.IntSlider("##RareBoots", "Lowest iLvl", Settings.RareBootsilvl);
                    ImGui.SameLine();
                    Settings.RareBoots.Value = ImGuiExtension.Checkbox("Boots", Settings.RareBoots);
                    Settings.RareHelmetsilvl.Value = ImGuiExtension.IntSlider("##RareHelmets", "Lowest iLvl", Settings.RareHelmetsilvl);
                    ImGui.SameLine();
                    Settings.RareHelmets.Value = ImGuiExtension.Checkbox("Helmets", Settings.RareHelmets);
                    Settings.RareArmourilvl.Value = ImGuiExtension.IntSlider("##RareArmours", "Lowest iLvl", Settings.RareArmourilvl);
                    ImGui.SameLine();
                    Settings.RareArmour.Value = ImGuiExtension.Checkbox("Armours", Settings.RareArmour);
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    Settings.RareShieldilvl.Value = ImGuiExtension.IntSlider("##Shields", "Lowest iLvl", Settings.RareShieldilvl);
                    ImGui.SameLine();
                    Settings.RareShield.Value = ImGuiExtension.Checkbox("Shields", Settings.RareShield);
                    Settings.RareShieldWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RareShieldWidth", Settings.RareShieldWidth);
                    Settings.RareShieldHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RareShieldHeight", Settings.RareShieldHeight);
                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.Spacing();
                    Settings.RareWeaponilvl.Value = ImGuiExtension.IntSlider("##RareWeapons", "Lowest iLvl", Settings.RareWeaponilvl);
                    ImGui.SameLine();
                    Settings.RareWeapon.Value = ImGuiExtension.Checkbox("Weapons", Settings.RareWeapon);
                    Settings.RareWeaponWidth.Value = ImGuiExtension.IntSlider("Maximum Width##RareWeaponWidth", Settings.RareWeaponWidth);
                    Settings.RareWeaponHeight.Value = ImGuiExtension.IntSlider("Maximum Height##RareWeaponHeight", Settings.RareWeaponHeight);
                    ImGui.TreePop();
                }
            }
            if (ImGui.CollapsingHeader("Custom Ignore Rules", ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.BulletText("ignore rules");
                if (ImGui.Button("Reload ignore.txt file")) LoadRuleFiles();
            }
        }

        private DateTime DisableLazyLootingTill { get; set; }

        public override Job Tick()
        {
            var playerInvCount = GameController?.Game?.IngameState?.ServerData?.PlayerInventories?.Count;
            if (playerInvCount == null || playerInvCount == 0)
                return null;

            if (Input.GetKeyState(Settings.LazyLootingPauseKey)) DisableLazyLootingTill = DateTime.Now.AddSeconds(2);
            if (Input.GetKeyState(Keys.Escape))
            {
                _quickPickItCoroutine.Pause();
            }

            if (Input.GetKeyState(Settings.PickUpKey.Value) ||
                CanLazyLoot())
            {
                _debugTimer.Restart();
                if (_quickPickItCoroutine.IsDone)
                {
                    var firstOrDefault =
                        Core.ParallelRunner.Coroutines.FirstOrDefault(x => x.OwnerName == nameof(QuickPickIt));

                    if (firstOrDefault != null)
                        _quickPickItCoroutine = firstOrDefault;
                }

                _quickPickItCoroutine.Resume();
                //_fullWork = false;
            }
            else
            {
                if (/*_fullWork &&*/ _canPause)
                {
                    GetItemCount = 0;

                    _quickPickItCoroutine.Pause();
                    _debugTimer.Reset();
                }
            }

            //if (_debugTimer.ElapsedMilliseconds > 300)
            //{
            //    _fullWork = true;
            //    //LogMessage("Error pick it stop after time limit 300 ms", 1);
            //    _debugTimer.Reset();
            //}
            //Graphics.DrawText($@"PICKIT :: Debug Tick Timer ({DebugTimer.ElapsedMilliseconds}ms)", new Vector2(100, 100), FontAlign.Left);
            //DebugTimer.Reset();

            return null;
        }




        public bool InCustomList(HashSet<string> checkList, CustomItem itemEntity, ItemRarity rarity)
        {
            if (checkList.Contains(itemEntity.BaseName) && !_ignoreRules.Contains(itemEntity.BaseName) && itemEntity.Rarity == rarity)
                return true;
            if (checkList.Contains(itemEntity.ClassName) && !_ignoreRules.Contains(itemEntity.ClassName) && itemEntity.Rarity == rarity)
                return true;
            return false;
        }

        public bool OverrideChecks(CustomItem item)
        {
            try
            {
                if (_ignoreRules.Contains(item.BaseName) || _ignoreRules.Contains(item.ClassName))
                    return false;

                #region Currency

                if (Settings.AllCurrency && item.ClassName.EndsWith("Currency"))
                {
                    switch (item.Path)
                    {
                        case "Metadata/Items/Currency/CurrencyIdentification":
                            return !Settings.IgnoreScrollOfWisdom;
                        case "Metadata/Items/Currency/CurrencyPortal":
                            return !Settings.IgnorePortalScroll;
                        default:
                            return true;
                    }
                }
                #endregion

                #region Shaper & Elder

                if (Settings.ElderItems)
                {
                    if (item.IsElder)
                        return true;
                }

                if (Settings.ShaperItems)
                {
                    if (item.IsShaper)
                        return true;
                }
                
                if (Settings.SynthesizedItems)
                {
                    if (item.isSynthesized)
                        return true;
                }

                if (Settings.FracturedItems)
                {
                    if (item.IsFractured)
                        return true;
                }

                #endregion


                if (Settings.HeistItems)
                {
                    if (item.IsHeist)
                        return true;
                }

                #region Influenced

                if (Settings.HunterItems)
                {
                    if (item.IsHunter)
                        return true;
                }

                if (Settings.RedeemerItems)
                {
                    if (item.IsRedeemer)
                        return true;
                }

                if (Settings.CrusaderItems)
                {
                    if (item.IsCrusader)
                        return true;
                }

                if (Settings.WarlordItems)
                {
                    if (item.IsWarlord)
                        return true;
                }

                if (Settings.VeiledItems)
                {
                    if (item.IsVeiled)
                        return true;
                }

                #endregion

                #region Rare Overrides

                if (Settings.Rares && item.Rarity == ItemRarity.Rare)
                {
                    if (Settings.RareRings && item.ClassName == "Ring" && item.ItemLevel >= Settings.RareRingsilvl)
                        return true;
                    if (Settings.RareAmulets && item.ClassName == "Amulet" &&
                        item.ItemLevel >= Settings.RareAmuletsilvl) return true;
                    if (Settings.RareBelts && item.ClassName == "Belt" && item.ItemLevel >= Settings.RareBeltsilvl)
                        return true;
                    if (Settings.RareGloves && item.ClassName == "Gloves" && item.ItemLevel >= Settings.RareGlovesilvl)
                        return true;
                    if (Settings.RareBoots && item.ClassName == "Boots" && item.ItemLevel >= Settings.RareBootsilvl)
                        return true;
                    if (Settings.RareHelmets && item.ClassName == "Helmet" &&
                        item.ItemLevel >= Settings.RareHelmetsilvl) return true;
                    if (Settings.RareArmour && item.ClassName == "Body Armour" &&
                        item.ItemLevel >= Settings.RareArmourilvl) return true;

                    if (Settings.RareWeapon && item.IsWeapon && item.ItemLevel >= Settings.RareWeaponilvl)
                        if (item.Width <= Settings.RareWeaponWidth && item.Height <= Settings.RareWeaponHeight)
                            return true;

                    if (Settings.RareShield && item.ClassName == "Shield" && item.ItemLevel >= Settings.RareShieldilvl)
                        if (item.Width <= Settings.RareShieldWidth && item.Height <= Settings.RareShieldHeight)
                            return true;

                    if (Settings.RareJewels && item.ClassName is "Jewel" or "AbyssJewel")
                        return true;
                }

                #endregion

                #region Sockets/Links/RGB

                if (Settings.Sockets && item.Sockets >= Settings.TotalSockets.Value) return true;
                if (Settings.Links && item.LargestLink >= Settings.LargestLink) return true;
                if (Settings.RGB && item.IsRGB && item.Width <= Settings.RGBWidth && item.Height <= Settings.RGBHeight) return true;

                #endregion

                #region Divination Cards

                if (Settings.AllDivs && item.ClassName == "DivinationCard") return true;

                #endregion

                #region Maps

                if (Settings.Maps && item.MapTier >= Settings.MapTier.Value) return true;
                if (Settings.Maps && Settings.UniqueMap && item.MapTier >= 1 && item.Rarity == ItemRarity.Unique) return true;
                if (Settings.Maps && Settings.MapFragments && item.ClassName == "MapFragment") return true;

                #endregion

                #region Quest Items

                if (Settings.QuestItems && item.ClassName == "QuestItem") return true;

                #endregion

                #region Qualiity Rules

                if (Settings.Gems && item.Quality >= Settings.GemQuality.Value && item.ClassName.Contains("Skill Gem")) return true;
                if (Settings.Flasks && item.Quality >= Settings.FlasksQuality.Value
                                    && item.ItemLevel >= Settings.FlasksIlvl.Value
                                    && item.ClassName.Contains("Flask")) return true;

                #endregion

                #region Uniques

                if (Settings.AllUniques && item.Rarity == ItemRarity.Unique) return true;

                #endregion

                #region Custom Rules
                if (_customItems.Contains(item.BaseName)) return true;
                if (item.BaseName.Contains("Watchstone")) return true;
                if (item.BaseName.Contains("Incubator")) return true;
                if (item.BaseName.Contains(" Seed")) return true;
                if (item.BaseName.Contains(" Grain")) return true;
                if (item.BaseName.Contains(" Bulb")) return true;
                if (item.BaseName.Contains(" Cluster ")) return true;
                if (item.BaseName.Contains(" Ultimatum")) return true;
                #endregion
            }
            catch (Exception e)
            {
                LogError($"{nameof(OverrideChecks)} error: {e}");
            }

            return false;
        }

        public bool DoWePickThis(CustomItem itemEntity)
        {
            if (!itemEntity.IsValid)
                return false;

            var pickItemUp = false;


            #region Force Pickup All

            if (Settings.PickUpEverything)
            {
                return true;
            }

            #endregion


            #region Override Rules

            if (OverrideChecks(itemEntity)) pickItemUp = true;

            #endregion

            #region Metamorph

            if (itemEntity.IsMetaItem)
            {
                pickItemUp = true;
            }

            #endregion

            return pickItemUp;
        }

        //public override void ReceiveEvent(string eventId, object args)
        //{
        //    if (eventId == "start_pick_it") _enabled = true;
        //    if (eventId == "end_pick_it") _enabled = false;
            
        //    if (!Settings.Enable.Value) return;

        //    if (eventId == "frsm_display_data")
        //    {
        //        var argSerialised = JsonConvert.SerializeObject(args);
        //        FullRareSetManagerData = JsonConvert.DeserializeObject<FRSetManagerPublishInformation>(argSerialised);
        //    }
        //}

        private List<CustomItem> UpdateCurrentLabels()
        {
            var window = GameController.Window.GetWindowRectangleTimeCache;
            var rect = new RectangleF(window.X, window.X, window.X + window.Width, window.Y + window.Height);
            var labels = GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible;

            return labels.Where(x => x.Address != 0 
                                     && x.ItemOnGround?.Path != null 
                                     && x.IsVisible
                                     && x.Label.GetClientRectCache.Center.PointInRectangle(rect)
                                     /*&& x.CanPickUp*/ 
                                     && x.MaxTimeForPickUp.TotalSeconds <= 0)
                .Select(x => new CustomItem(x, GameController.Files))
                .OrderBy(x => x.Distance).ToList();
        }

        private List<LabelOnGround> UpdateChestList() =>
            GameController?.Game?.IngameState?.IngameUi?.ItemsOnGroundLabelsVisible
                .Where(x => x.Address != 0
                            && x.IsVisible
                            && x.ItemOnGround != null
                            && x.ItemOnGround.HasComponent<Chest>()
                            // && x.CanPickUp
                            && x.ItemOnGround.Path != null
                            && (x.ItemOnGround.Path.StartsWith("Metadata/Chests/Chest")
                                || x.ItemOnGround.Path.Contains("LeaguesExpedition")
                                || x.ItemOnGround.Path.StartsWith("Metadata/Chests/Blight")
                                || x.ItemOnGround.Path.StartsWith("Metadata/Chests/Breach")
                                || x.ItemOnGround.Path.StartsWith("Metadata/Chests/LegionChests"))
                )
                .OrderBy(x => x.ItemOnGround.DistancePlayer)
                .ToList();

        private IEnumerator FindItemToPick()
        {
            if (!GameController.Window.IsForeground()) yield break;
            var portalLabel = GetLabel(@"Metadata/MiscellaneousObjects/MultiplexPortal");
            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;
            rectangleOfGameWindow.Inflate(-36, -36);

            //var pickUpThisItem = _currentLabels.Value.FirstOrDefault(x =>
            //    DoWePickThis(x) && x.Distance < Settings.PickupRange && x.GroundItem != null &&
            //    rectangleOfGameWindow.Intersects(new RectangleF(x.LabelOnGround.Label.GetClientRectCache.Center.X + rectangleOfGameWindow.X,
            //        x.LabelOnGround.Label.GetClientRectCache.Center.Y, 3, 3)) && (Settings.PickUpEvenInventoryFull || Misc.CanFitInventory(x)));

            var pickUpThisItem = _currentLabels.Value.Where(x =>
                DoWePickThis(x) && x.Distance < Settings.PickupRange && x.GroundItem != null &&
                rectangleOfGameWindow.Intersects(new RectangleF(x.LabelOnGround.Label.GetClientRectCache.Center.X + rectangleOfGameWindow.X,
                    x.LabelOnGround.Label.GetClientRectCache.Center.Y, 3, 3)))
                .ToList();

            if (pickUpThisItem.Any() && (Input.GetKeyState(Settings.PickUpKey.Value) 
                || 
                CanLazyLoot() && ShouldLazyLoot(pickUpThisItem.FirstOrDefault())))
            {
                yield return TryToPickV2(pickUpThisItem, portalLabel);
                yield break;
                //_fullWork = true;
            }

            if (Input.GetKeyState(Settings.PickUpKey.Value) && Settings.ExpeditionChests.Value)
            {
                var chestLabel = ChestLabelCacheList?.Value.FirstOrDefault(x =>
                    x.ItemOnGround.Path.Contains("LeaguesExpedition") &&
                    x.ItemOnGround.DistancePlayer < Settings.PickupRange && x.ItemOnGround != null &&
                    rectangleOfGameWindow.Intersects(new RectangleF(x.Label.GetClientRectCache.Center.X,
                        x.Label.GetClientRectCache.Center.Y, 3, 3)));

                if (chestLabel != null)
                {
                    yield return TryToOpenChest(chestLabel);
                    //_fullWork = true;
                    yield break;
                }
                
            }
            if (Input.GetKeyState(Settings.PickUpKey.Value) && Settings.GroundChests.Value)
            {
                var chestLabel = ChestLabelCacheList?.Value.FirstOrDefault(x =>
                    x.ItemOnGround.DistancePlayer < Settings.PickupRange && x.ItemOnGround != null &&
                    rectangleOfGameWindow.Intersects(new RectangleF(x.Label.GetClientRectCache.Center.X,
                        x.Label.GetClientRectCache.Center.Y, 3, 3)));

                if (chestLabel != null)
                {
                    yield return TryToOpenChest(chestLabel);
                    //_fullWork = true;
                    yield break;
                }
                
            }
            yield break;
        }
        
        /// <summary>
        /// LazyLoot item independent checks
        /// </summary>
        /// <returns></returns>
        private bool CanLazyLoot()
        {
            if (!Settings.LazyLooting) return false;
            if (DisableLazyLootingTill > DateTime.Now) return false;
            try
            {
                if (Settings.NoLazyLootingWhileEnemyClose && GameController.EntityListWrapper
                        .ValidEntitiesByType[EntityType.Monster]
                        .Any(x => x != null && x.GetComponent<Monster>() != null && x.IsValid && x.IsHostile &&
                                  x.IsAlive
                                  && !x.IsHidden && !x.Path.Contains("ElementalSummoned")
                                  && Vector3.Distance(GameController.Player.Pos, x.GetComponent<Render>().Pos) <
                                  Settings.PickupRange)) return false;
            }
            catch (Exception e)
            {
                LogError($"ERROR CanLazyLoot {e.Message}");
            };

            return true;
        }
        
        /// <summary>
        /// LazyLoot item dependent checks
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ShouldLazyLoot(CustomItem item)
        {
            if (item == null)
            {
                return false;
            }
            var itemPos = item.LabelOnGround.ItemOnGround.Pos;
            var playerPos = GameController.Player.Pos;
            if (Math.Abs(itemPos.Z - playerPos.Z) > 50) return false;
            var dx = itemPos.X - playerPos.X;
            var dy = itemPos.Y - playerPos.Y;
            if (dx * dx + dy * dy > 275 * 275) return false;

            if (item.IsElder || item.IsFractured || item.IsShaper ||
                item.IsHunter || item.IsCrusader || item.IsRedeemer || item.IsWarlord || item.IsHeist)
                return true;
            
            if (item.Rarity == ItemRarity.Rare && item.Width * item.Height > 1) return false;
            
            return true;
        }

        private IEnumerator TryToPickV2(List<CustomItem> pickItItems, LabelOnGround portalLabel)
        {
            if (pickItItems == null)
            {
                yield break;
            }
            
            var queueItems = CalcQueueDist(pickItItems);
            
            foreach (CustomItem pickItItem in queueItems)
            {
                //_fullWork = false;
                _canPause = false;
                if(Settings.ShowDebug) LogError($"START item: {pickItItem.ToString()}", 5);
                if (!pickItItem.IsValid)
                {
                    //_fullWork = true;
                    ////LogMessage("PickItem is not valid.", 5, Color.Red);
                    //yield break;
                    continue;
                }

                //var centerOfItemLabel = pickItItem.LabelOnGround.Label.GetClientRectCache.Center;
                var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

                _clickWindowOffset = rectangleOfGameWindow.TopLeft;
                rectangleOfGameWindow.Inflate(-36, -36);
                //centerOfItemLabel.X += rectangleOfGameWindow.Left;
                //centerOfItemLabel.Y += rectangleOfGameWindow.Top;

                    var completeItemLabel = pickItItem.LabelOnGround?.Label;
                    if (completeItemLabel == null || pickItItem.LabelOnGround.ItemOnGround.IsTargetable == false)
                    {
                        //_fullWork = true;
                        continue;
                    }

                    Vector2 vector2;
                    if (IsPortalNearby(portalLabel, pickItItem.LabelOnGround))
                        vector2 = completeItemLabel.GetClientRect().ClickRandom() + _clickWindowOffset;
                    else
                        vector2 = completeItemLabel.GetClientRect().Center + _clickWindowOffset;

                    if (!rectangleOfGameWindow.Intersects(new RectangleF(vector2.X, vector2.Y, 3, 3)))
                    {
                        //_fullWork = true;
                        continue;
                    }

                    Input.SetCursorPos(vector2);
                    yield return _delayNextClick;


                    //if (pickItItem.IsTargeted())
                    {
                        // in case of portal nearby do extra checks with delays
                        if (IsPortalNearby(portalLabel, pickItItem.LabelOnGround) && !IsPortalTargeted(portalLabel))
                        {
                            yield return new WaitTime(25);
                            if (IsPortalNearby(portalLabel, pickItItem.LabelOnGround) && !IsPortalTargeted(portalLabel))
                                Input.Click(MouseButtons.Left);
                        }
                        else if (!IsPortalNearby(portalLabel, pickItItem.LabelOnGround))
                        {
                            Input.Click(MouseButtons.Left);
                            if(Settings.ShowDebug) LogError($"click {vector2.ToString()} item: {pickItItem.ToString()}", 5);
                        }

                        GetItemCount++;
                    }
                    //else
                    //{
                    //    LogError($"NOT click {vector2.ToString()} coroutineCounter: {coroutineCounter}", 5);
                    //}

                    if (pickItItem.Distance > 5)
                    {
                        WaitingTime.Start();
                        while (WaitingTime.ElapsedMilliseconds < pickItItem.Distance*2)
                        {
                            yield return _wait10Ms;
                        }
                        if(Settings.ShowDebug) LogError($"wait distance:{WaitingTime.ElapsedMilliseconds} item: {pickItItem.ToString()}", 5);
                        WaitingTime.Reset();
                    }

                    while (GameController.Player.GetComponent<Actor>().isMoving)
                    {
                        yield return _wait10Ms;
                        if(Settings.ShowDebug) LogError($"wait waitPlayerMove:{_wait10Ms} item: {pickItItem.ToString()}", 5);
                    }

                    
                    //yield return _toPick;

                    if (Settings.WaitPickUp)
                    {
                        while (pickItItem.LabelOnGround.ItemOnGround.IsTargetable)
                        {
                            yield return _wait10Ms;
                        }
                    }
                    else
                    {
                        //WaitingTime.Start();
                        //while (WaitingTime.ElapsedMilliseconds < Settings.TimeBeforeNextClick)
                        //{
                        //    yield return _wait10Ms;
                        //}
                        yield return _delayNextClick;

                        if(Settings.ShowDebug) LogError($"wait TimeBeforeNewClick:{_delayNextClick.Milliseconds} item: {pickItItem.ToString()}", 5);
                        //WaitingTime.Reset();
                    }

                    if(Settings.ShowDebug) LogError($"END item: {pickItItem.BaseName}", 5);
                    yield return RefreshHighlight();

                    //_fullWork = true;
                    _canPause = true;

                    if (!Input.GetKeyState(Settings.PickUpKey.Value)
                        && !CanLazyLoot())
                    {
                        GetItemCount = 0;
                        yield break;
                    }
            }
        }

        private static Queue<CustomItem> CalcQueueDist(List<CustomItem> pickItItems)
        {
            var queueItems = new Queue<CustomItem>();
            float dist = 0;
            var newItem = pickItItems.OrderBy(i => i.Distance).ToList().FirstOrDefault();
            if (newItem == null)
            {
                return queueItems;
            }

            queueItems.Enqueue(newItem);
            pickItItems.Remove(newItem);
            for (; pickItItems.Count > 0;)
            {
                var oldItem = newItem;
                newItem = pickItItems.MinBy(it => dist = oldItem.RelativePosition.Distance(it.RelativePosition));
                if (newItem == null)
                {
                    return queueItems;
                }

                newItem.Distance = dist;
                queueItems.Enqueue(newItem);
                pickItItems.Remove(newItem);
            }

            return queueItems;
        }

        private bool IsPortalTargeted(LabelOnGround portalLabel)
        {
            // extra checks in case of HUD/game update. They are easy on CPU
            return
                GameController.IngameState.UIHover.Address == portalLabel.Address ||
                GameController.IngameState.UIHover.Address == portalLabel.ItemOnGround.Address ||
                GameController.IngameState.UIHover.Address == portalLabel.Label.Address ||
                GameController.IngameState.UIHoverElement.Address == portalLabel.Address ||
                GameController.IngameState.UIHoverElement.Address == portalLabel.ItemOnGround.Address ||
                GameController.IngameState.UIHoverElement.Address ==
                portalLabel.Label.Address || // this is the right one
                GameController.IngameState.UIHoverTooltip.Address == portalLabel.Address ||
                GameController.IngameState.UIHoverTooltip.Address == portalLabel.ItemOnGround.Address ||
                GameController.IngameState.UIHoverTooltip.Address == portalLabel.Label.Address ||
                portalLabel?.ItemOnGround?.HasComponent<Targetable>() == true &&
                portalLabel?.ItemOnGround?.GetComponent<Targetable>()?.isTargeted == true;
        }

        private bool IsPortalNearby(LabelOnGround portalLabel, LabelOnGround pickItItem)
        {
            if (portalLabel == null || pickItItem == null) return false;
            var rect1 = portalLabel.Label.GetClientRectCache;
            var rect2 = pickItItem.Label.GetClientRectCache;
            rect1.Inflate(100, 100);
            rect2.Inflate(100, 100);
            return rect1.Intersects(rect2);
        }

        private LabelOnGround GetLabel(string id)
        {
            var labels = GameController?.Game?.IngameState?.IngameUi?.ItemsOnGroundLabels;

            var labelQuery =
                from labelOnGround in labels
                let label = labelOnGround?.Label
                where label?.IsValid == true &&
                      label?.Address > 0 &&
                      label?.IsVisible == true
                let itemOnGround = labelOnGround?.ItemOnGround
                where itemOnGround != null &&
                      itemOnGround?.Metadata?.Contains(id) == true
                let dist = GameController?.Player?.GridPos.DistanceSquared(itemOnGround.GridPos)
                orderby dist
                select labelOnGround;

            return labelQuery.FirstOrDefault();
        }
        
        private IEnumerator TryToOpenChest(LabelOnGround labelOnGround)
        {
            if (labelOnGround == null)
                yield break;

            var centerOfItemLabel = labelOnGround.Label.GetClientRectCache.Center;
            var rectangleOfGameWindow = GameController.Window.GetWindowRectangleTimeCache;

            _clickWindowOffset = rectangleOfGameWindow.TopLeft;
            rectangleOfGameWindow.Inflate(-36, -36);
            centerOfItemLabel.X += rectangleOfGameWindow.Left;
            centerOfItemLabel.Y += rectangleOfGameWindow.Top;
            if (!rectangleOfGameWindow.Intersects(new RectangleF(centerOfItemLabel.X, centerOfItemLabel.Y, 3, 3)))
                yield break;

            var tryCount = 0;

            while (tryCount < 2)
            {
                var completeItemLabel = labelOnGround.Label;

                if (completeItemLabel == null)
                {
                    if (tryCount > 0)
                        yield break;

                    yield break;
                }
                
                var clientRect = completeItemLabel.GetClientRect();

                var clientRectCenter = clientRect.Center;

                var vector2 = clientRectCenter + _clickWindowOffset;

                if (!rectangleOfGameWindow.Intersects(new RectangleF(vector2.X, vector2.Y, 3, 3)))
                    yield break;

                Input.SetCursorPos(vector2);
                yield return new WaitTime(Settings.DelayNextClick*3);
                Input.Click(MouseButtons.Left);

                yield return new WaitTime(Settings.DelayNextClick*3);

                while (GameController.Player.GetComponent<Actor>().isMoving)
                {
                    yield return _wait10Ms;
                    //LogError($"wait waitPlayerMove:{_wait10Ms} item: {pickItItem.ToString()}", 5);
                }

                yield return _wait2Ms;
                tryCount++;
            }

            tryCount = 0;

            while (GameController.Game.IngameState.IngameUi.ItemsOnGroundLabelsVisible.FirstOrDefault(
                       x => x.Address == labelOnGround.Address) != null && tryCount < 6)
            {
                tryCount++;
            }
        }

        #region (Re)Loading Rules

        private void LoadRuleFiles()
        {
            var quickPickItConfigFileDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins", "Compiled", nameof(QuickPickIt));

            if (!Directory.Exists(quickPickItConfigFileDirectory))
            {
                //Directory.CreateDirectory(quickPickItConfigFileDirectory);
                return;
            }

            var dirInfo = new DirectoryInfo(quickPickItConfigFileDirectory);

            //QuickPickItFiles = dirInfo.GetFiles("*.txt").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
            _ignoreRules = LoadRules("ignore.txt");
        }

        public HashSet<string> LoadRules(string fileName)
        {
            var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (fileName == string.Empty)
            {
                return hashSet;
            }

            var pickitFile = $@"{DirectoryFullName}\{fileName}";

            if (!File.Exists(pickitFile))
            {
                File.WriteAllText(pickitFile, "#add ignore rules here");
            }

            var lines = File.ReadAllLines(pickitFile);

            foreach (var x in lines.Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#")))
            {
                hashSet.Add(x.Trim());
            }

            //LogMessage($"QuickPickIt :: (Re)Loaded {fileName}", 5, Color.GreenYellow);
            return hashSet;
        }

        public override void OnPluginDestroyForHotReload()
        {
            _quickPickItCoroutine.Done(true);
        }

        #endregion
    }
}
