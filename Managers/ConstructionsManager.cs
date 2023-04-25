using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModShelter.Managers
{
    public class ConstructionsManager : MonoBehaviour, IYesNoDialogOwner
    {
        private static ConstructionsManager Instance;
        private static readonly string ModuleName = nameof(ConstructionsManager);

        private static ItemsManager LocalItemsManager;
        private static HUDManager LocalHUDManager;
        private static Player LocalPlayer;

        public List<ItemID> ShelterItemIds { get; set; } = new List<ItemID> {
            ItemID.Bed_Shelter,
            ItemID.Hut_Shelter,
            ItemID.Medium_Shelter,
            ItemID.Medium_Bamboo_Shelter,
            ItemID.Small_Shelter,
            ItemID.Small_Bamboo_Shelter,
            ItemID.tribe_shelter_small,
            ItemID.tribe_shelter_big
        };
        public List<ItemID> BedItemIds { get; set; } = new List<ItemID> {
            ItemID.Leaves_Bed,
            ItemID.banana_leaf_bed,
            ItemID.Logs_Bed,
            ItemID.BambooLog_Bed,
            ItemID.military_bed_toUse,
            ItemID.mattress_a,
            ItemID.village_hammock_a,
            ItemID.village_hammock_b,
            ItemID.hammock_a,
            ItemID.car_sofa
        };
        public List<ItemInfo> RestingPlaceItemInfos = new List<ItemInfo>();
        public Item SelectedItemToDestroy { get; set; } = null;
        public GameObject SelectedGameObjectToDestroy { get; set; } = null;
        public string SelectedGameObjectToDestroyName { get; set; } = string.Empty;
        public string ItemDestroyedMessage(string item)
           => $"{item} destroyed!";
        public string ItemNotSelectedMessage()
            => $"Not any item selected to destroy!";
        public string ItemNotDestroyedMessage(string item)
            => $"{item} cannot be destroyed!";
        public bool DestroyTargetOption { get; set; } = false;
        
        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }

        protected virtual void Start()
        { }

        private void InitData()
        {
            LocalItemsManager = ItemsManager.Get();
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
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

        public ConstructionsManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static ConstructionsManager Get()
        {
            return Instance;
        }

        public void ShowHUDBigInfo(string text, float duration = 3f)
        {
            string header = $"{ModuleName} Info";
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

        public void DestroyOnHit(RaycastHit hitInfo)
        {
            try
            {
                if (DestroyTargetOption)
                {
                    SelectedGameObjectToDestroy = hitInfo.collider.transform.gameObject;
                    if (SelectedGameObjectToDestroy != null)
                    {
                        var localization = GreenHellGame.Instance.GetLocalization();
                        SelectedItemToDestroy = SelectedGameObjectToDestroy?.GetComponent<Item>();
                        if (SelectedItemToDestroy != null && Item.Find(SelectedItemToDestroy.GetInfoID()) != null)
                        {
                            SelectedGameObjectToDestroyName = localization.Get(SelectedItemToDestroy.GetInfoID().ToString()) ?? SelectedItemToDestroy?.GetName();
                        }
                        else
                        {
                            SelectedGameObjectToDestroyName = localization.Get(SelectedGameObjectToDestroy?.name) ?? SelectedGameObjectToDestroy?.name;
                        }

                        ShowConfirmDestroyDialog(SelectedGameObjectToDestroyName);
                    }
                }            
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DestroyOnHit));
            }
        }

        private void ShowConfirmDestroyDialog(string itemToDestroyName)
        {
            try
            {
                EnableCursor(true);
                string description = $"Are you sure you want to destroy {itemToDestroyName}?";
                YesNoDialog destroyYesNoDialog = GreenHellGame.GetYesNoDialog();
                destroyYesNoDialog.Show(this, DialogWindowType.YesNo, $"{ModuleName} Info", description, true, false);
                destroyYesNoDialog.gameObject.SetActive(true);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ShowConfirmDestroyDialog));
            }
        }

        public void DestroySelectedItem()
        {
            try
            {
                if (SelectedItemToDestroy != null || SelectedGameObjectToDestroy != null)
                {
                    if (SelectedItemToDestroy != null && !SelectedItemToDestroy.IsPlayer() && !SelectedItemToDestroy.IsAI() && !SelectedItemToDestroy.IsHumanAI())
                    {
                        LocalItemsManager.AddItemToDestroy(SelectedItemToDestroy);
                    }
                    else
                    {
                        Destroy(SelectedGameObjectToDestroy);
                    }
                    //ShowHUDBigInfo(HUDBigInfoMessage(ItemDestroyedMessage(SelectedGameObjectToDestroyName), MessageType.Info, Color.green));
                }
                else
                {
                    //ShowHUDBigInfo(HUDBigInfoMessage(ItemNotSelectedMessage(), MessageType.Warning, Color.yellow));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(DestroySelectedItem));
            }
        }

        public void OnYesFromDialog()
        {
            DestroySelectedItem();
            EnableCursor(false);
        }

        public void OnNoFromDialog()
        {
            SelectedGameObjectToDestroy = null;
            SelectedItemToDestroy = null;
            EnableCursor(false);
        }

        public void OnOkFromDialog()
        {
            OnYesFromDialog();
        }

        public void OnCloseDialog()
        {
            EnableCursor(false);
        }

        public void CreateOtherBed(ItemID itemID)
        {
            var otherBed = LocalItemsManager.CreateItem(itemID, true, LocalPlayer.transform.position + LocalPlayer.transform.forward * 1f, LocalPlayer.transform.rotation);
            otherBed.gameObject.SetActive(true);
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

        public void UnlockRestingPlaces()
        {
            foreach (ItemInfo restingPlaceItemInfo in RestingPlaceItemInfos)
            {
                LocalItemsManager.UnlockItemInNotepad(restingPlaceItemInfo.m_ID);
                LocalItemsManager.UnlockItemInfo(restingPlaceItemInfo.m_ID.ToString());
                ShowHUDInfoLog(restingPlaceItemInfo.m_ID.ToString(), "HUD_InfoLog_NewEntry");
            }
        }
    }
}
