using AdvancedTerrainGrass;
using Enums;
using ModManager.Data.Enums;
using ModManager.Data.Interfaces;
using ModShelter.Managers;
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
    /// Press Keypad1 (default) or the key configurable in ModAPI to open the mod screen.
    /// </summary>
    public class ModShelter : MonoBehaviour
    {
        private static ModShelter Instance;
        private static readonly string ModName = nameof(ModShelter);
        private static readonly string RuntimeConfiguration = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), $"{nameof(RuntimeConfiguration)}.xml");

        private static  float ModShelterScreenTotalWidth { get; set; } = 650f;
        private static  float ModShelterScreenTotalHeight { get; set; } = 350f;
        private static  float ModShelterScreenMinWidth { get; set; } = 500f;
        private static float ModShelterScreenMaxWidth { get; set; } = 550f;
        private static float ModShelterScreenMinHeight { get; set; } = 50f;
        private static float ModShelterScreenMaxHeight { get; set; } = 350f;
        private static int ModShelterScreenId { get; set; }
        private static float ModShelterScreenStartPositionX { get; set; } = Screen.width / 3f;
        private static float ModShelterScreenStartPositionY { get; set; } = Screen.height / 3f;
        private bool IsModShelterScreenMinimized { get; set; } = false;
        private static Rect ModShelterScreen = new Rect(ModShelterScreenStartPositionX, ModShelterScreenStartPositionY, ModShelterScreenTotalWidth, ModShelterScreenTotalHeight);
        private bool ShowModShelter { get; set; } = false;
        private bool ShowModInfo { get; set; } = false;

        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;
        private static ConstructionsManager LocalConstructionManager;
        private static StylingManager LocalStylingManager;
              
        public Vector2 ModInfoScrollViewPosition { get; private set; }
        public IConfigurableMod SelectedMod { get; set; }
      
        public bool IsModActiveForMultiplayer { get; private set; } = false;
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public KeyCode ShortcutKey { get; set; } = KeyCode.Keypad1;
        public Vector2 ModShelterScreenTotalSize { get; set; }

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
                return KeyCode.Keypad1;
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

        private string AlreadyUnlockedBlueprints()
            => $"All blueprints were already unlocked!";
        private string OnlyForSinglePlayerOrHostMessage()
             => "Only available for single player or when host. Host can activate using ModManager.";
        private string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        private string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{(headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>{messageType}</color>\n{message}";
        private void OnlyForSingleplayerOrWhenHostBox()
        {
            using (new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), LocalStylingManager.ColoredCommentLabel(Color.yellow));
            }
        }
        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            string reason = optionValue ? "the game host allowed usage" : "the game host did not allow usage";
            IsModActiveForMultiplayer = optionValue;

            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted", $"{reason}"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked", $"{reason}"), MessageType.Info, Color.yellow))
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

        protected virtual void Update()
        {
            if (Input.GetKeyDown(ShortcutKey))
            {
                if (!ShowModShelter)
                {
                    InitData();
                    EnableCursor(true);
                }
                ToggleShowUI(0);
                if (!ShowModShelter)
                {
                    EnableCursor(false);
                }
            }

            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                DestroyTarget();
            }
        }

        private void DestroyTarget()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    if (LocalConstructionManager.DestroyTargetOption)
                    {
                        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo))
                        {
                            LocalConstructionManager.DestroyOnHit(hitInfo);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DestroyTarget));
            }
        }

        private void ToggleShowUI(int controlId)
        {
            switch (controlId)
            {
                case 0:
                    ShowModShelter = !ShowModShelter;
                    return;             
                case 3:
                    ShowModInfo = !ShowModInfo;
                    return;            
                default:
                    ShowModShelter = !ShowModShelter;
                    ShowModInfo = !ShowModInfo;
                    return;
            }
        }

        private void OnGUI()
        {
            if (ShowModShelter)
            {
                InitData();
                InitSkinUI();
                ShowModShelterWindow();
            }
        }

        protected virtual void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalConstructionManager = ConstructionsManager.Get();
            LocalStylingManager = StylingManager.Get();
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void ShowModShelterWindow()
        {
            if (ModShelterScreenId < 0 )
            {
                ModShelterScreenId = GetHashCode();
            }
            string modShelterScreenTitle = $"{ModName} created by [Dragon Legion] Immaanuel#4300";
            ModShelterScreen = GUILayout.Window(ModShelterScreenId, ModShelterScreen, InitModShelterScreen, modShelterScreenTitle,
                                                                                        GUI.skin.window,
                                                                                        GUILayout.ExpandWidth(true),
                                                                                        GUILayout.MinWidth(ModShelterScreenMinWidth),
                                                                                        GUILayout.MaxWidth(ModShelterScreenMaxWidth),
                                                                                        GUILayout.ExpandHeight(true),
                                                                                        GUILayout.MinHeight(ModShelterScreenMinHeight),
                                                                                        GUILayout.MaxHeight(ModShelterScreenMaxHeight)
                                                                                    );
        }

        private void CollapseWindow()
        {
            if (!IsModShelterScreenMinimized)
            {
                ModShelterScreen = new Rect(ModShelterScreenStartPositionX, ModShelterScreenStartPositionY, ModShelterScreenTotalWidth, ModShelterScreenMinHeight);
                IsModShelterScreenMinimized = true;
            }
            else
            {
                ModShelterScreen = new Rect(ModShelterScreenStartPositionX, ModShelterScreenStartPositionY, ModShelterScreenTotalWidth, ModShelterScreenTotalHeight);
                IsModShelterScreenMinimized = false;
            }
            ShowModShelterWindow();
        }

        private void ScreenMenuBox()
        {
            if (GUI.Button(new Rect(ModShelterScreen.width - 60f, 0f, 20f, 20f), "=", GUI.skin.button))
            {
                ResizeWindow();
            }

            string CollapseButtonText = IsModShelterScreenMinimized ? "O" : "-";
            if (GUI.Button(new Rect(ModShelterScreen.width - 40f, 0f, 20f, 20f), CollapseButtonText, GUI.skin.button))
            {
                CollapseWindow();
            }

            if (GUI.Button(new Rect(ModShelterScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void ResizeWindow()
        {
            try
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 position = ModShelterScreen.position;
                    Vector2 resized = ModShelterScreen.size;

                    ModShelterScreenStartPositionX = ModShelterScreen.x;
                    ModShelterScreenStartPositionY = ModShelterScreen.y;

                    if (resized.x >= ModShelterScreenMinWidth && resized.y >= ModShelterScreenMinHeight)
                    {
                        resized.x = ModShelterScreen.width;
                        resized.y = ModShelterScreen.height;
                        ModShelterScreen.size.Scale(resized);
                        ModShelterScreenTotalWidth = resized.x;
                        ModShelterScreenTotalHeight = resized.y;
                        GUI.DragWindow(new Rect(ModShelterScreenStartPositionX, ModShelterScreenStartPositionY, resized.x, resized.y));
                    }
                }            
                ModShelterScreen = new Rect(ModShelterScreenStartPositionX, ModShelterScreenStartPositionY, ModShelterScreenTotalWidth, ModShelterScreenTotalHeight);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ResizeWindow));
            }
        }

        private void CloseWindow()
        {
            ShowModShelter = false;
            EnableCursor(false);
        }

        private void InitModShelterScreen(int windowID)
        {
            ModShelterScreenStartPositionX = ModShelterScreen.x;
            ModShelterScreenStartPositionY = ModShelterScreen.y;
            ModShelterScreenTotalWidth = ModShelterScreen.width;
            ModShelterScreenTotalHeight = ModShelterScreen.height;

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsModShelterScreenMinimized)
                {
                    ModShelterManagerBox();

                    ConstructionsManagerBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ModShelterManagerBox()
        {
            try
            {
                if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{ModName} Manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));
                        GUILayout.Label($"{ModName} Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                        if (GUILayout.Button($"Mod Info", GUI.skin.button))
                        {
                            ToggleShowUI(3);
                        }
                        if (ShowModInfo)
                        {
                            ModInfoBox();
                        }

                        MultiplayerOptionBox();                       
                    }
                }
                else
                {
                    OnlyForSingleplayerOrWhenHostBox();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ModShelterManagerBox));
            }
        }

        private void ModInfoBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModInfoScrollViewPosition = GUILayout.BeginScrollView(ModInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));

                GUILayout.Label("Mod Info", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.GameID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.GameID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (var midScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.ID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.ID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (var uidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.UniqueID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.UniqueID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (var versionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.Version)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.Version}", LocalStylingManager.FormFieldValueLabel);
                }

                GUILayout.Label("Buttons Info", LocalStylingManager.ColoredSubHeaderLabel(Color.cyan));

                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (var btnidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.ID)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.ID}", LocalStylingManager.FormFieldValueLabel);
                    }
                    using (var btnbindScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.KeyBinding)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", LocalStylingManager.FormFieldValueLabel);
                    }
                }

                GUILayout.EndScrollView();
            }
        }

        private void ConstructionsManagerBox()
        {
            try
            {
                if (IsModActiveForMultiplayer || IsModActiveForSingleplayer)
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Constructions Manager", LocalStylingManager.ColoredHeaderLabel(Color.yellow));
                        GUILayout.Label($"Constructions Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                        InstantBuildOptionBox();

                        GUILayout.Label("Here you can create several items from the game which cannot be crafted by default.", LocalStylingManager.TextLabel);

                        DestroyTargetOptionBox();

                        UnlockRestingPlacesBox();

                        CreateOtherBedBox();
                    }
                }
                else
                {
                    OnlyForSingleplayerOrWhenHostBox();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ConstructionsManagerBox));
            }
        }

        private void DestroyTargetOptionBox()
        {
            GUILayout.Label("To be able to remove any of your created items, you will have to enable this option.", LocalStylingManager.TextLabel);
            GUILayout.Label("Once enabled, focus the mouse pointer on the item, then push [KeypadMinus].", LocalStylingManager.TextLabel);
            GUILayout.Label("Please note that you can delete any object you want when this option is enabled!", LocalStylingManager.ColoredCommentLabel(Color.yellow));

            LocalConstructionManager.DestroyTargetOption = GUILayout.Toggle(LocalConstructionManager.DestroyTargetOption, $"Use [{KeyCode.KeypadMinus}] to destroy target?", GUI.skin.toggle);
        }

        private void InstantBuildOptionBox()
        {
            try
            {
                LocalConstructionManager.InstantBuildOption = GUILayout.Toggle(LocalConstructionManager.InstantBuildOption, $"Use [F8] to instantly finish any constructions?", GUI.skin.toggle);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(InstantBuildOptionBox));
            }
        }

        private void MultiplayerOptionBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Multiplayer Options", LocalStylingManager.ColoredSubHeaderLabel(Color.yellow));

                    string multiplayerOptionMessage = string.Empty;
                    if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                    {
                        if (IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are the game host";
                        }
                        if (IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host allowed usage";
                        }
                     GUILayout.Label(PermissionChangedMessage($"granted", multiplayerOptionMessage), LocalStylingManager.ColoredFieldValueLabel(Color.green));
                    }
                    else
                    {
                        if (!IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are not the game host";
                        }
                        if (!IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host did not allow usage";
                        }
                        GUILayout.Label(PermissionChangedMessage($"revoked", $"{multiplayerOptionMessage}"), LocalStylingManager.ColoredFieldValueLabel(Color.yellow));
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
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To unlock all shelters and beds, click ", LocalStylingManager.TextLabel);
                        if (GUILayout.Button("Unlock blueprints", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickUnlockBlueprintsButton();
                        }
                    }
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void OnClickUnlockBlueprintsButton()
        {
            try
            {
                UnlockAllRestingPlaces();
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickUnlockBlueprintsButton));
            }
        }

        public void UnlockAllRestingPlaces()
        {
            try
            {
                if (!LocalConstructionManager.HasUnlockedRestingPlaces)
                {
                    LocalConstructionManager.UnlockRestingPlaces();
                    LocalConstructionManager.HasUnlockedRestingPlaces = true;
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
        
        private void CreateOtherBedBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a military bed, click", LocalStylingManager.TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.military_bed_toUse);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a mattras, click", LocalStylingManager.TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.mattress_a);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a car sofa, click", LocalStylingManager.TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.car_sofa);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a village hammock type A, click", LocalStylingManager.TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.village_hammock_a);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a village hammock type B, click", LocalStylingManager.TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.village_hammock_b);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a hammock type A, click", LocalStylingManager.TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.hammock_a);
                        }
                    }
                }                  
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(CreateOtherBedBox));
            }
        }

        private void OnClickCreateOtherBedButton(ItemID itemID)
        {
            try
            {
                LocalConstructionManager.CreateOtherBed(itemID);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickCreateOtherBedButton));
            }
        }
    }
}
