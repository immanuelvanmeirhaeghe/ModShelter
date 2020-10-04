using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ModShelter
{
    /// <summary>
    /// ModShelter is a mod for Green Hell
    /// that allows a player to unlocks all shelters and beds.
    /// It also gives the player the possibility to instantly finish any ongoing building.
	/// (only in single player mode - Use ModManager for multiplayer).
    /// Enable the mod UI by pressing Home.
    /// </summary>
    public class ModShelter : MonoBehaviour
    {
        private static ModShelter s_Instance;

        private static readonly string ModName = nameof(ModShelter);

        private bool ShowUI = false;

        public static Rect ModShelterScreen = new Rect(Screen.width /10f, Screen.height / 2f, 450f, 150f);

        private static ItemsManager itemsManager;

        private static HUDManager hUDManager;

        private static Player player;

        public static List<ItemID> ShelterItemIds { get; set; } = new List<ItemID> { ItemID.Hut_Shelter, ItemID.Medium_Bamboo_Shelter, ItemID.Medium_Shelter, ItemID.Small_Bamboo_Shelter, ItemID.Bed_Shelter };

        public static List<ItemID> BedItemIds { get; set; } = new List<ItemID> { ItemID.Leaves_Bed, ItemID.banana_leaf_bed, ItemID.Logs_Bed, ItemID.BambooLog_Bed };

        public static List<ItemInfo> RestingPlaceItemInfos = new List<ItemInfo>();
        public static bool HasUnlockedRestingPlaces { get; set; }
        public bool InstantFinishConstructionsOption { get; private set; }

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        private static string HUDBigInfoMessage(string message) => $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.red)}>System</color>\n{message}";

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            IsModActiveForMultiplayer = optionValue;
            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>Permission to use mods for multiplayer was granted!</color>")
                            : HUDBigInfoMessage($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.yellow)}>Permission to use mods for multiplayer was revoked!</color>")),
                           $"{ModName} Info",
                           HUDInfoLogTextureType.Count.ToString());
        }

        public ModShelter()
        {
            useGUILayout = true;
            s_Instance = this;
        }

        public static ModShelter Get()
        {
            return s_Instance;
        }

        public void ShowHUDBigInfo(string text, string header, string textureName)
        {
            HUDBigInfo bigInfo = (HUDBigInfo)hUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData bigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            bigInfo.AddInfo(bigInfoData);
            bigInfo.Show(true);
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            var localization = GreenHellGame.Instance.GetLocalization();
            HUDMessages hUDMessages = (HUDMessages)hUDManager.GetHUD(typeof(HUDMessages));
            hUDMessages.AddMessage(
                $"{localization.Get(localizedTextKey)}  {localization.Get(itemID)}"
                );
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer, false);

            if (blockPlayer)
            {
                player.BlockMoves();
                player.BlockRotation();
                player.BlockInspection();
            }
            else
            {
                player.UnblockMoves();
                player.UnblockRotation();
                player.UnblockInspection();
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
            itemsManager = ItemsManager.Get();
            hUDManager = HUDManager.Get();
            player = Player.Get();
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitWindow()
        {
            int wid = GetHashCode();
            ModShelterScreen = GUILayout.Window(wid, ModShelterScreen, InitModShelterScreen, $"{ModName}", GUI.skin.window);
        }

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor(false);
        }

        private void InitModShelterScreen(int windowID)
        {
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUI.Button(new Rect(430f, 0f, 20f, 20f), "X", GUI.skin.button))
                {
                    CloseWindow();
                }

                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("Shelter- and bed blueprints.", GUI.skin.label);
                    if (GUILayout.Button("Unlock resting places", GUI.skin.button))
                    {
                        OnClickUnlockRestingPlacesButton();
                        CloseWindow();
                    }
                }

                InstantFinishConstructionsOptionButton();
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void InstantFinishConstructionsOptionButton()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    InstantFinishConstructionsOption = GUILayout.Toggle(InstantFinishConstructionsOption, $"Use F8 to instantly finish any constructions?", GUI.skin.toggle);
                }
            }
            else
            {
                using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Use F8 to instantly finish any constructions", GUI.skin.label);
                    GUILayout.Label("is only for single player or when host", GUI.skin.label);
                    GUILayout.Label("Host can activate using ModManager.", GUI.skin.label);
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
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickUnlockRestingPlacesButton)}] throws exception: {exc.Message}");
            }
        }

        public void UnlockAllRestingPlaces()
        {
            try
            {
                if (!HasUnlockedRestingPlaces)
                {
                    RestingPlaceItemInfos = itemsManager.GetAllInfos().Values.Where(info => info.IsShelter()).ToList();

                    UnlockShelters();

                    UnlockBeds();

                    foreach (ItemInfo restingPlaceItemInfo in RestingPlaceItemInfos)
                    {
                        itemsManager.UnlockItemInNotepad(restingPlaceItemInfo.m_ID);
                        itemsManager.UnlockItemInfo(restingPlaceItemInfo.m_ID.ToString());
                        ShowHUDInfoLog(restingPlaceItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
                    }
                    HasUnlockedRestingPlaces = true;
                }
                else
                {
                    ShowHUDBigInfo(
                         HUDBigInfoMessage(
                             $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.yellow)}>All resting places were already unlocked!</color>"),
                        $"{ModName} Info",
                        HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(UnlockAllRestingPlaces)}] throws exception: {exc.Message}");
            }
        }

        public void UnlockShelters()
        {
            foreach (ItemID shelterItemId in ShelterItemIds)
            {
                ItemInfo shelterInfo = itemsManager.GetInfo(shelterItemId);
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
                ItemInfo bedInfo = itemsManager.GetInfo(bedItemId);
                if (!RestingPlaceItemInfos.Contains(bedInfo))
                {
                    RestingPlaceItemInfos.Add(bedInfo);
                }
            }
        }
    }
}
