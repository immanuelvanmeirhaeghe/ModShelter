using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModShelter
{
    public enum MessageType
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// ModShelter is a mod for Green Hell
    /// that allows a player to unlocks all shelters and beds.
    /// It also gives the player the possibility to instantly finish any ongoing building.
	/// (only in single player mode - Use ModManager for multiplayer).
    /// Enable the mod UI by pressing Home.
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

        private static ModShelter Instance;
        private static ItemsManager LocalItemsManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;

        private static float ModScreenStartPositionX { get; set; } = (Screen.width - ModScreenMaxWidth) % ModScreenTotalWidth;
        private static float ModScreenStartPositionY { get; set; } = (Screen.height - ModScreenMaxHeight) % ModScreenTotalHeight;
        private static bool IsMinimized { get; set; } = false;

        private bool ShowUI = false;

        public static Rect ModShelterScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        public static List<ItemID> ShelterItemIds { get; set; } = new List<ItemID> {
            ItemID.Bed_Shelter,
            ItemID.Hut_Shelter,
            ItemID.Medium_Shelter,
            ItemID.Medium_Bamboo_Shelter,
            ItemID.Small_Shelter,
            ItemID.Small_Bamboo_Shelter
        };

        public static List<ItemID> BedItemIds { get; set; } = new List<ItemID> {
            ItemID.Leaves_Bed,
            ItemID.banana_leaf_bed,
            ItemID.Logs_Bed,
            ItemID.BambooLog_Bed
        };
        public static List<ItemInfo> RestingPlaceItemInfos = new List<ItemInfo>();

        public bool HasUnlockedRestingPlaces { get; set; } = false;
        public bool InstantFinishConstructionsOption { get; private set; } = false;
        public bool IsModActiveForMultiplayer { get; private set; } = false;
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public static string OnlyForSinglePlayerOrHostMessage() => $"Only available for single player or when host. Host can activate using ModManager.";
        public static string AlreadyUnlockedBlueprints() => $"All blueprints were already unlocked!";
        public static string PermissionChangedMessage(string permission) => $"Permission to use mods and cheats in multiplayer was {permission}!";
        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

        public void ShowHUDBigInfo(string text)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();
            HUDBigInfo hudBigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = 2f;
            HUDBigInfoData hudBigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            hudBigInfo.AddInfo(hudBigInfoData);
            hudBigInfo.Show(true);
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            var localization = GreenHellGame.Instance.GetLocalization();
            HUDMessages hUDMessages = (HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages));
            hUDMessages.AddMessage(
                $"{localization.Get(localizedTextKey)}  {localization.Get(itemID)}"
                );
        }

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            IsModActiveForMultiplayer = optionValue;
            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked"), MessageType.Info, Color.yellow))
                            );
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
            if (Input.GetKeyDown(KeyCode.Home))
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
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    InstantFinishConstructionsOption = GUILayout.Toggle(InstantFinishConstructionsOption, $"Use [F8] to instantly finish any constructions?", GUI.skin.toggle);
                }
            }
            else
            {
                using (var infoScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                    GUI.color = Color.white;
                }
            }
        }

        private void UnlockRestingPlacesBox()
        {
            using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label("Click to unlock all shelter - and bed info: ", GUI.skin.label);
                if (GUILayout.Button("Unlock blueprints", GUI.skin.button))
                {
                    OnClickUnlockRestingPlacesButton();
                    CloseWindow();
                }
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

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
            ModAPI.Log.Write(info);
            ShowHUDBigInfo(HUDBigInfoMessage(info, MessageType.Error, Color.red));
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
                    ShowHUDBigInfo(HUDBigInfoMessage(AlreadyUnlockedBlueprints(), MessageType.Warning, Color.yellow));
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
