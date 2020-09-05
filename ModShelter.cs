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

        private bool showUI = false;

        public Rect ModShelterWindow = new Rect(10f, 500f, 450f, 150f);

        private static ItemsManager itemsManager;

        private static HUDManager hUDManager;

        private static Player player;

        public static List<ItemID> ShelterItemIds { get; set; } = new List<ItemID> { ItemID.Hut_Shelter, ItemID.Medium_Bamboo_Shelter, ItemID.Medium_Shelter, ItemID.Small_Bamboo_Shelter, ItemID.Bed_Shelter };

        public static List<ItemID> BedItemIds { get; set; } = new List<ItemID> { ItemID.Leaves_Bed, ItemID.banana_leaf_bed, ItemID.Logs_Bed, ItemID.BambooLog_Bed };

        public static List<ItemInfo> RestingPlaceItemInfos = new List<ItemInfo>();
        public static bool HasUnlockedRestingPlaces { get; set; }
        public bool UseOptionF8 { get; private set; }

        public bool IsModActiveForMultiplayer => FindObjectOfType(typeof(ModManager.ModManager)) != null && ModManager.ModManager.AllowModsForMultiplayer;
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

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
            player = Player.Get();

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
                if (!showUI)
                {
                    InitData();
                    EnableCursor(true);
                }
                // toggle menu
                showUI = !showUI;
                if (!showUI)
                {
                    EnableCursor(false);
                }
            }
        }

        private void OnGUI()
        {
            if (showUI)
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
            ModShelterWindow = GUI.Window(0, ModShelterWindow, InitModWindow, $"{nameof(ModShelter)}", GUI.skin.window);
        }

        private void InitModWindow(int windowId)
        {
            if (GUI.Button(new Rect(440f, 500f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }

            GUI.Label(new Rect(30f, 520f, 200f, 20f), "Shelter- and bed blueprints.", GUI.skin.label);
            if (GUI.Button(new Rect(280f, 520f, 150f, 20f), "Unlock resting places", GUI.skin.button))
            {
                OnClickUnlockRestingPlacesButton();
                CloseWindow();
            }

            CreateF8Option();

            GUI.DragWindow(new Rect(0, 0, 10000, 10000));
        }

        private void CloseWindow()
        {
            showUI = false;
            EnableCursor(false);
        }

        private void CreateF8Option()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                GUI.Label(new Rect(30f, 560f, 200f, 20f), "Use F8 to instantly finish", GUI.skin.label);
                UseOptionF8 = GUI.Toggle(new Rect(280f, 560f, 20f, 20f), UseOptionF8, "");
            }
            else
            {
                GUI.Label(new Rect(30f, 560f, 330f, 20f), "Use F8 to instantly to finish any constructions", GUI.skin.label);
                GUI.Label(new Rect(30f, 580f, 330f, 20f), "is only for single player or when host", GUI.skin.label);
                GUI.Label(new Rect(30f, 600f, 330f, 20f), "Host can activate using ModManager.", GUI.skin.label);
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
                ModAPI.Log.Write($"[{nameof(ModShelter)}.{nameof(ModShelter)}:{nameof(OnClickUnlockRestingPlacesButton)}] throws exception: {exc.Message}");
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
                    ShowHUDBigInfo("All resting places were already unlocked!", $"{nameof(ModShelter)} Info", HUDInfoLogTextureType.Count.ToString());
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModShelter)}.{nameof(ModShelter)}:{nameof(UnlockAllRestingPlaces)}] throws exception: {exc.Message}");
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
