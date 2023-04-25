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
    /// Press Keypad0 (default) or the key configurable in ModAPI to open the mod screen.
    /// </summary>
    public class ModShelter : MonoBehaviour
    {
        private static ModShelter Instance;
        private static readonly string ModName = nameof(ModShelter);
        private static readonly string RuntimeConfiguration = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), $"{nameof(RuntimeConfiguration)}.xml");

        private static  float ModShelterScreenTotalWidth { get; set; } = 500f;
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

        private Color DefaultColor = GUI.color;
        private Color DefaultContentColor = GUI.contentColor;
        private Color DefaultBackGroundColor = GUI.backgroundColor;
        private GUIStyle HeaderLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 16
        };
        private GUIStyle SubHeaderLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = HeaderLabel.alignment,
            fontStyle = HeaderLabel.fontStyle,
            fontSize = HeaderLabel.fontSize - 2,
        };
        private GUIStyle FormFieldNameLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            stretchWidth = true
        };
        private GUIStyle FormFieldValueLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12,
            stretchWidth = true
        };
        private GUIStyle FormInputTextField => new GUIStyle(GUI.skin.textField)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12,
            stretchWidth = true,
            stretchHeight = true,
            wordWrap = true
        };
        private GUIStyle CommentLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Italic,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true
        };
        private GUIStyle TextLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true
        };
        private GUIStyle ToggleButton => new GUIStyle(GUI.skin.toggle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            stretchWidth = true
        };

        public GUIStyle ColoredToggleValueTextLabel(bool enabled, Color enabledColor, Color disabledColor)
        {
            GUIStyle style = TextLabel;
            style.normal.textColor = enabled ? enabledColor : disabledColor;
            return style;
        }

        public GUIStyle ColoredToggleButton(bool activated, Color enabledColor, Color disabledColor)
        {
            GUIStyle style = ToggleButton;
            style.active.textColor = activated ? enabledColor : disabledColor;
            style.onActive.textColor = activated ? enabledColor : disabledColor;
            style = GUI.skin.button;
            return style;
        }

        public GUIStyle ColoredCommentLabel(Color color)
        {
            GUIStyle style = CommentLabel;
            style.normal.textColor = color;
            return style;
        }

        public GUIStyle ColoredFieldNameLabel(Color color)
        {
            GUIStyle style = FormFieldNameLabel;
            style.normal.textColor = color;
            return style;
        }

        public GUIStyle ColoredFieldValueLabel(Color color)
        {
            GUIStyle style = FormFieldValueLabel;
            style.normal.textColor = color;
            return style;
        }

        public GUIStyle ColoredToggleFieldValueLabel(bool enabled, Color enabledColor, Color disabledColor)
        {
            GUIStyle style = FormFieldValueLabel;
            style.normal.textColor = enabled ? enabledColor : disabledColor;
            return style;
        }

        public GUIStyle ColoredHeaderLabel(Color color)
        {
            GUIStyle style = HeaderLabel;
            style.normal.textColor = color;
            return style;
        }

        public GUIStyle ColoredSubHeaderLabel(Color color)
        {
            GUIStyle style = SubHeaderLabel;
            style.normal.textColor = color;
            return style;
        }

        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;
        private static ConstructionsManager LocalConstructionManager;
              
        public Vector2 ModInfoScrollViewPosition { get; private set; }
        public IConfigurableMod SelectedMod { get; set; }
        public bool HasUnlockedRestingPlaces { get; set; } = false;
        public bool InstantBuild { get; private set; } = false;
        public bool IsModActiveForMultiplayer { get; private set; } = false;
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();
        public KeyCode ShortcutKey { get; set; } = KeyCode.Keypad0;       
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
                return ShortcutKey;
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
            using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUI.color = Color.yellow;
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                GUI.color = DefaultColor;
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

        private void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalConstructionManager = ConstructionsManager.Get();
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

        private void CloseWindow()
        {
            ShowModShelter = false;
            EnableCursor(false);
        }

        private void InitModShelterScreen(int windowID)
        {
            ModShelterScreenStartPositionX = ModShelterScreen.x;
            ModShelterScreenStartPositionY = ModShelterScreen.y;

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsModShelterScreenMinimized)
                {
                    ModShelterManagerBox();                   
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ModShelterManagerBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{ModName} Manager", ColoredHeaderLabel(Color.yellow));
                    GUILayout.Label($"{ModName} Options", ColoredSubHeaderLabel(Color.yellow));

                    if (GUILayout.Button($"Mod Info", GUI.skin.button))
                    {
                        ToggleShowUI(3);
                    }
                    if (ShowModInfo)
                    {
                        ModInfoBox();
                    }

                    MultiplayerOptionBox();

                    ConstructionsManagerBox();
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void ModInfoBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModInfoScrollViewPosition = GUILayout.BeginScrollView(ModInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));

                GUILayout.Label("Mod Info", ColoredSubHeaderLabel(Color.cyan));

                using (var gidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.GameID)}:", FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.GameID}", FormFieldValueLabel);
                }
                using (var midScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.ID)}:", FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.ID}", FormFieldValueLabel);
                }
                using (var uidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.UniqueID)}:", FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.UniqueID}", FormFieldValueLabel);
                }
                using (var versionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.Version)}:", FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.Version}", FormFieldValueLabel);
                }

                GUILayout.Label("Buttons Info", ColoredSubHeaderLabel(Color.cyan));

                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (var btnidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.ID)}:", FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.ID}", FormFieldValueLabel);
                    }
                    using (var btnbindScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.KeyBinding)}:", FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", FormFieldValueLabel);
                    }
                }

                GUILayout.EndScrollView();
            }
        }

        private void ConstructionsManagerBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Constructions Manager", ColoredHeaderLabel(Color.yellow));
                    GUILayout.Label($"Constructions Options", ColoredSubHeaderLabel(Color.yellow));

                    InstantBuildOptionBox();

                    GUILayout.Label("Here you can create several items from the game which cannot be crafted by default.", TextLabel);

                    DestroyTargetOptionBox();

                    UnlockRestingPlacesBox();

                    CreateOtherBedBox();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ConstructionsManagerBox));
            }
        }

        private void DestroyTargetOptionBox()
        {
            GUILayout.Label("To be able to remove any of your created items, you will have to enable this option.", TextLabel);
            GUILayout.Label("Once enabled, focus the mouse pointer on the item, then push [KeypadMinus].", TextLabel);
            GUILayout.Label("Please note that you can delete any object you want when this option is enabled!", ColoredCommentLabel(Color.yellow));

            LocalConstructionManager.DestroyTargetOption = GUILayout.Toggle(LocalConstructionManager.DestroyTargetOption, $"Use [{KeyCode.KeypadMinus}] to destroy target?", GUI.skin.toggle);
        }

        private void InstantBuildOptionBox()
        {
            try
            {
                InstantBuild = GUILayout.Toggle(InstantBuild, $"Use [F8] to instantly finish any constructions?", GUI.skin.toggle);
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
                    string multiplayerOptionMessage = string.Empty;
                    GUILayout.Label("Multiplayer Info", ColoredSubHeaderLabel(Color.yellow));
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
                        GUILayout.Label($"{PermissionChangedMessage($"granted", multiplayerOptionMessage)}", ColoredToggleValueTextLabel(true, Color.green, Color.red));
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
                        GUILayout.Label($"{PermissionChangedMessage($"revoked", multiplayerOptionMessage)}", ColoredToggleValueTextLabel(false, Color.green, Color.yellow));
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
                        GUILayout.Label("To unlock all shelters and beds, click ", TextLabel);
                        if (GUILayout.Button("Unlock blueprints", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickUnlockRestingPlacesButton();
                        }
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
                    LocalConstructionManager.UnlockRestingPlaces();
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

        private void UnlockShelters()
        {
            LocalConstructionManager.UnlockShelters();
        }

        private void UnlockBeds()
        {
            LocalConstructionManager.UnlockBeds();
        }

        private void CreateOtherBedBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a military bed, click", TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.military_bed_toUse);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a mattras, click", TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.mattress_a);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a car sofa, click", TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.car_sofa);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a village hammock type A, click", TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.village_hammock_a);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a village hammock type B, click", TextLabel);
                        if (GUILayout.Button("create", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickCreateOtherBedButton(ItemID.village_hammock_b);
                        }
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("To place a hammock type A, click", TextLabel);
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
