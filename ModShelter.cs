﻿using Enums;
using ModManager.Data.Enums;
using ModManager.Data.Interfaces;
using ModShelter.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModShelter
{

    /// <summary>
    /// ModShelter is a mod for Green Hell that allows a player to unlock all shelters and beds.
    /// It also gives the player the possibility to instantly finish any ongoing building by pressing F8.
    /// Press Keypad0 (default) or the key configurable in ModAPI to open the mod screen.
    /// </summary>
    public class ModShelter : MonoBehaviour
    {
        private static readonly string ModName = nameof(ModShelter);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinWidth = 450f;
        private static readonly float ModScreenMaxWidth = 550f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 200f;

        private Color DefaultGuiColor = GUI.color;

        private static ModShelter Instance;
        private static ItemsManager LocalItemsManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;

        private static float ModScreenStartPositionX { get; set; } = Screen.width / 3f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height / 3f;
        private static bool IsMinimized { get; set; } = false;

        private bool ShowUI = false;

        public static Rect ModShelterScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        public static List<ItemID> ShelterItemIds { get; set; } = new List<ItemID> {
            ItemID.Bed_Shelter,
            ItemID.Hut_Shelter,
            ItemID.Medium_Shelter,
            ItemID.Medium_Bamboo_Shelter,
            ItemID.Small_Shelter,
            ItemID.Small_Bamboo_Shelter,
            ItemID.tribe_shelter_small,
            ItemID.tribe_shelter_big
        };

        public static List<ItemID> BedItemIds { get; set; } = new List<ItemID> {
            ItemID.Leaves_Bed,
            ItemID.banana_leaf_bed,
            ItemID.Logs_Bed,
            ItemID.BambooLog_Bed
        };
        public static List<ItemInfo> RestingPlaceItemInfos = new List<ItemInfo>();

        public IConfigurableMod SelectedMod { get; set; }
        public bool HasUnlockedRestingPlaces { get; set; } = false;
        public bool InstantFinishConstructionsOption { get; private set; } = false;
        public bool IsModActiveForMultiplayer { get; private set; } = false;
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();
        private string HUDBigInfoMessage(string message, Enums.MessageType messageType, Color? headcolor = null)
            => $"<color=#{(headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>{messageType}</color>\n{message}";
        private void OnlyForSingleplayerOrWhenHostBox()
        {
            using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUI.color = Color.yellow;
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                GUI.color = DefaultGuiColor;
            }
        }

        private KeyCode GetConfigurableModShortcutKey(string buttonId)
        {
            KeyCode result = KeyCode.None;
            string value = string.Empty;
            try
            {
                if (File.Exists(RuntimeConfiguration))
                {
                    using (XmlReader xmlReader = XmlReader.Create(new StreamReader(RuntimeConfiguration)))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader["ID"] == ModName && xmlReader.ReadToFollowing("Button") && xmlReader["ID"] == buttonId)
                            {
                                value = xmlReader.ReadElementContentAsString();
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(value))
                {
                    result = EnumUtils<KeyCode>.GetValue(value);
                }
                else if (buttonId == nameof(ShortcutKey))
                {
                    result = ShortcutKey;
                }
                return result;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetConfigurableModShortcutKey));
                if (buttonId == nameof(ShortcutKey))
                {
                    result = ShortcutKey;
                }
                return result;
            }
        }

        public KeyCode GetShortcutKey(string buttonID)
        {
            var ConfigurableModList = GetModList();
            if (ConfigurableModList != null && ConfigurableModList.Count > 0)
            {
                SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == ModName);
                return SelectedMod.ConfigurableModButtons.Find(cfgButton => cfgButton.ID == buttonID).ShortcutKey;
            }
            else
            {
                return KeyCode.Keypad2;
            }
        }

        private List<IConfigurableMod> GetModList()
        {
            List<IConfigurableMod> modList = new List<IConfigurableMod>();
            try
            {
                if (File.Exists(RuntimeConfiguration))
                {
                    using (XmlReader configFileReader = XmlReader.Create(new StreamReader(RuntimeConfiguration)))
                    {
                        while (configFileReader.Read())
                        {
                            configFileReader.ReadToFollowing("Mod");
                            do
                            {
                                string gameID = GameID.GreenHell.ToString();
                                string modID = configFileReader.GetAttribute(nameof(IConfigurableMod.ID));
                                string uniqueID = configFileReader.GetAttribute(nameof(IConfigurableMod.UniqueID));
                                string version = configFileReader.GetAttribute(nameof(IConfigurableMod.Version));

                                var configurableMod = new ModManager.Data.Modding.ConfigurableMod(gameID, modID, uniqueID, version);

                                configFileReader.ReadToDescendant("Button");
                                do
                                {
                                    string buttonID = configFileReader.GetAttribute(nameof(IConfigurableModButton.ID));
                                    string buttonKeyBinding = configFileReader.ReadElementContentAsString();

                                    configurableMod.AddConfigurableModButton(buttonID, buttonKeyBinding);

                                } while (configFileReader.ReadToNextSibling("Button"));

                                if (!modList.Contains(configurableMod))
                                {
                                    modList.Add(configurableMod);
                                }

                            } while (configFileReader.ReadToNextSibling("Mod"));
                        }
                    }
                }
                return modList;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetModList));
                modList = new List<IConfigurableMod>();
                return modList;
            }
        }
        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);           
        }

        public static string AlreadyUnlockedBlueprints()
            => $"All blueprints were already unlocked!";
        public static string OnlyForSinglePlayerOrHostMessage() 
            => $"Only available for single player or when host. Host can activate using ModManager.";
        public static string PermissionChangedMessage(string permission, string reason) 
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            string reason = optionValue ? "the game host allowed usage" : "the game host did not allow usage";
            IsModActiveForMultiplayer = optionValue;

            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted", $"{reason}"), Enums.MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked", $"{reason}"), Enums.MessageType.Info, Color.yellow))
                            );
        }

        public void ShowHUDBigInfo(string text, float duration = 3f)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();
            HUDBigInfo obj = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = duration;
            HUDBigInfoData data = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            obj.AddInfo(data);
            obj.Show(show: true);
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            var messages = ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages)));
            messages.AddMessage($"{localization.Get(localizedTextKey)}  {localization.Get(itemID)}");
        }

        private static readonly string RuntimeConfiguration = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        public KeyCode ShortcutKey { get; set; } = KeyCode.Keypad0;
        private KeyCode GetConfigurableKey(string buttonId)
        {
            KeyCode configuredKeyCode = default;
            string configuredKeybinding = string.Empty;

            try
            {
                if (File.Exists(RuntimeConfiguration))
                {
                    using (var xmlReader = XmlReader.Create(new StreamReader(RuntimeConfiguration)))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader["ID"] == ModName)
                            {
                                if (xmlReader.ReadToFollowing(nameof(Button)) && xmlReader["ID"] == buttonId)
                                {
                                    configuredKeybinding = xmlReader.ReadElementContentAsString();
                                }
                            }
                        }
                    }
                }

                configuredKeybinding = configuredKeybinding?.Replace("NumPad", "Keypad").Replace("Oem", "");

                configuredKeyCode = (KeyCode)(!string.IsNullOrEmpty(configuredKeybinding)
                                                            ? Enum.Parse(typeof(KeyCode), configuredKeybinding)
                                                            : GetType().GetProperty(buttonId)?.GetValue(this));
                return configuredKeyCode;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetConfigurableKey));
                configuredKeyCode = (KeyCode)(GetType().GetProperty(buttonId)?.GetValue(this));
                return configuredKeyCode;
            }
        }

        protected virtual void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ShortcutKey = GetShortcutKey(nameof(ShortcutKey));
        }

        public ModShelter()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static ModShelter Get()
        {
            return Instance;
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer, false);

            if (blockPlayer)
            {
                LocalPlayer.BlockMoves();
                LocalPlayer.BlockRotation();
                LocalPlayer.BlockInspection();
            }
            else
            {
                LocalPlayer.UnblockMoves();
                LocalPlayer.UnblockRotation();
                LocalPlayer.UnblockInspection();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ShortcutKey))
            {
                if (!ShowUI)
                {
                    InitData();
                    EnableCursor(true);
                }
                ToggleShowUI();
                if (!ShowUI)
                {
                    EnableCursor(false);
                }
            }
        }

        private void ToggleShowUI()
        {
            ShowUI = !ShowUI;
        }

        private void OnGUI()
        {
            if (ShowUI)
            {
                InitData();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitData()
        {
            LocalItemsManager = ItemsManager.Get();
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitWindow()
        {
            int wid = GetHashCode();
            ModShelterScreen = GUILayout.Window(wid, ModShelterScreen, InitModShelterScreen, ModName,
                                                                                        GUI.skin.window,
                                                                                        GUILayout.ExpandWidth(true),
                                                                                        GUILayout.MinWidth(ModScreenMinWidth),
                                                                                        GUILayout.MaxWidth(ModScreenMaxWidth),
                                                                                        GUILayout.ExpandHeight(true),
                                                                                        GUILayout.MinHeight(ModScreenMinHeight),
                                                                                        GUILayout.MaxHeight(ModScreenMaxHeight)
                                                                                    );
        }

        private void CollapseWindow()
        {
            if (!IsMinimized)
            {
                ModShelterScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                ModShelterScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                IsMinimized = false;
            }
            InitWindow();
        }

        private void ScreenMenuBox()
        {
            if (GUI.Button(new Rect(ModShelterScreen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
            {
                CollapseWindow();
            }

            if (GUI.Button(new Rect(ModShelterScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor(false);
        }

        private void InitModShelterScreen(int windowID)
        {
            ModScreenStartPositionX = ModShelterScreen.x;
            ModScreenStartPositionY = ModShelterScreen.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsMinimized)
                {
                    ModOptionsBox();
                    UnlockRestingPlacesBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ModOptionsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To toggle the mod main UI, press [{ShortcutKey}]", GUI.skin.label);

                    MultiplayerOptionBox();
                    ConstructionsOptionBox();
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void ConstructionsOptionBox()
        {
            try
            {
                using (var constructionsoptionScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = DefaultGuiColor;
                    GUILayout.Label($"Construction options: ", GUI.skin.label);
                    InstantFinishConstructionsOption = GUILayout.Toggle(InstantFinishConstructionsOption, $"Use [F8] to instantly finish any constructions?", GUI.skin.toggle);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ConstructionsOptionBox));
            }
        }

        private void MultiplayerOptionBox()
        {
            try
            {
                using (var multiplayeroptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Multiplayer options: ", GUI.skin.label);
                    string multiplayerOptionMessage = string.Empty;
                    if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                    {
                        GUI.color = Color.green;
                        if (IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are the game host";
                        }
                        if (IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host allowed usage";
                        }
                        _ = GUILayout.Toggle(true, PermissionChangedMessage($"granted", multiplayerOptionMessage), GUI.skin.toggle);
                    }
                    else
                    {
                        GUI.color = Color.yellow;
                        if (!IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are not the game host";
                        }
                        if (!IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host did not allow usage";
                        }
                        _ = GUILayout.Toggle(false, PermissionChangedMessage($"revoked", $"{multiplayerOptionMessage}"), GUI.skin.toggle);
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MultiplayerOptionBox));
            }
        }

        private void UnlockRestingPlacesBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var unlockrestingScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Click to unlock all shelter - and bed info: ", GUI.skin.label);
                    if (GUILayout.Button("Unlock blueprints", GUI.skin.button))
                    {
                        OnClickUnlockRestingPlacesButton();
                    }
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void OnClickUnlockRestingPlacesButton()
        {
            try
            {
                UnlockAllRestingPlaces();
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickUnlockRestingPlacesButton));
            }
        }

        public void UnlockAllRestingPlaces()
        {
            try
            {
                if (!HasUnlockedRestingPlaces)
                {
                    UnlockShelters();
                    UnlockBeds();
                    foreach (ItemInfo restingPlaceItemInfo in RestingPlaceItemInfos)
                    {
                        LocalItemsManager.UnlockItemInNotepad(restingPlaceItemInfo.m_ID);
                        LocalItemsManager.UnlockItemInfo(restingPlaceItemInfo.m_ID.ToString());
                        ShowHUDInfoLog(restingPlaceItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                    }
                    HasUnlockedRestingPlaces = true;
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(AlreadyUnlockedBlueprints(), Enums.MessageType.Warning, Color.yellow));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(UnlockAllRestingPlaces));
            }
        }

        public void UnlockShelters()
        {
            foreach (ItemID shelterItemId in ShelterItemIds)
            {
                ItemInfo shelterInfo = LocalItemsManager.GetInfo(shelterItemId);
                if (!RestingPlaceItemInfos.Contains(shelterInfo))
                {
                    RestingPlaceItemInfos.Add(shelterInfo);
                }
            }
        }

        public void UnlockBeds()
        {
            foreach (ItemID bedItemId in BedItemIds)
            {
                ItemInfo bedInfo = LocalItemsManager.GetInfo(bedItemId);
                if (!RestingPlaceItemInfos.Contains(bedInfo))
                {
                    RestingPlaceItemInfos.Add(bedInfo);
                }
            }
        }
    }
}
