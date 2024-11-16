using BepInEx.Unity.IL2CPP.UnityEngine;
using BokuMono;
using BokuMono.Data;
using UnityEngine;
using ExtensionMethods;

namespace SoSPioneersQoLImprovements
{
    public class PatchManager
    {
        private static UIPageCommonInventoryBox? uiInventoryBox;
        private static PlayerCharacter? playerCharacter;
        private static bool isSprinting = false;
        private static bool canSprint = false;
        private static readonly SteamInputManager steamInputManager = SteamInputManager.GetInstance();
        private static readonly Dictionary<string, bool> isPressingMap = new();
        private static readonly uint ITEM_EMPTY = 0;

        public struct BagItem
        {
            public BagItem(ItemDataInInventory itemData, int index)
            {
                ItemData = itemData;
                Index = index;
            }

            public ItemDataInInventory ItemData;
            public int Index;
        }

        public static void CityDevelopLimit(CityDevelopExpMasterData.CityDevelopType cityDevelop, ref CityDevelopExpMasterData __result)
        {
            var disableLimit = BepInExLoader.configNoGiftExpLimit?.Value ?? BepInExLoader.DEFAULT_NO_GIFT_EXP_LIMIT;
            if (!disableLimit) return;



            if (cityDevelop != CityDevelopExpMasterData.CityDevelopType.Present) return;
            __result.Limit = CityDevelopExpMasterData.LimitType.NoLimit;
        }

        public static void MineFloor(ref MineFloorData __instance)
        {
            if (BepInExLoader.configRevealStairs?.Value ?? BepInExLoader.DEFAULT_REVEAL_STAIRS)
            {
                __instance.StairsHideRate = 0;
            }

            if (BepInExLoader.configRevealPitfalls?.Value ?? BepInExLoader.DEFAULT_REVEAL_PITFALLS)
            {
                __instance.PitFallHideRate = 0;
            }
        }

        public static bool PitFallDamage()
        {
            if (BepInExLoader.configPitfallsNoDamage?.Value ?? BepInExLoader.DEFAULT_PITFALLS_NO_DAMAGE)
            {
                return false;
            } else
            {
                return true;
            }
        }

        public static void PlayerStart(ref PlayerCharacter __instance)
        {
            playerCharacter = __instance;
        }

        public static void PlayerMove(ref PlayerCharacter __instance, float horizontal, float vertical)
        {
            if (!isSprinting) return;
            if (horizontal != 0 || vertical != 0) return;

            isSprinting = false;
            __instance.runSpeed = 4.5f;
            __instance.inWeedSpeed = 3;
        }

        public static void BikeStart(RideableBikeManager.SaveData saveData)
        {
            canSprint = saveData.ParkingPosition != Vector3.zero;
        }

        public static void UIShowInventoryBox(ref UIPageCommonInventoryBox __instance)
        {
            uiInventoryBox = __instance;
        }

        public static void UIHide()
        {
            uiInventoryBox = null;
        }

        public static void Update()
        {
            var shouldSort = CheckKey(
                tag: "SORT",
                keyController: BepInExLoader.configSortButtonController?.Value ?? BepInExLoader.DEFAULT_SORT_CONTROLLER_KEY,
                keyKeyboard: BepInExLoader.configSortButtonKeyboard?.Value ?? BepInExLoader.DEFAULT_SORT_KEYBOARD_KEY
            );
            if (shouldSort) DoSort();

            var shouldDeposit = CheckKey(
                tag: "DEPOSIT",
                keyController: BepInExLoader.configDepositButtonController?.Value ?? BepInExLoader.DEFAULT_DEPOSIT_CONTROLLER_KEY,
                keyKeyboard: BepInExLoader.configDepositButtonKeyboard?.Value ?? BepInExLoader.DEFAULT_DEPOSIT_KEYBOARD_KEY
            );
            if (shouldDeposit) DoDeposit();

            var shouldSprint = CheckKey(
                tag: "SPRINT",
                keyController: BepInExLoader.configSprintButtonController?.Value ?? BepInExLoader.DEFAULT_SPRINT_CONTROLLER_KEY,
                keyKeyboard: BepInExLoader.configSprintButtonKeyboard?.Value ?? BepInExLoader.DEFAULT_SPRINT_KEYBOARD_KEY
            );
            if (shouldSprint) DoSprint();

            var shouldShip = CheckKey(
                tag: "SHIP",
                keyController: BepInExLoader.configShipButtonController?.Value ?? BepInExLoader.DEFAULT_SHIP_CONTROLLER_KEY,
                keyKeyboard: BepInExLoader.configShipButtonKeyboard?.Value ?? BepInExLoader.DEFAULT_SHIP_KEYBOARD_KEY
            );
            if (shouldShip) DoShip();
        }

        private static bool CheckKey(string tag, BokuMono.Steam.InputController.Key keyController, UnityEngine.KeyCode keyKeyboard)
        {
            if (isPressingMap.GetValueOrDefault(tag))
            {
                var isControllerUp = keyController != BokuMono.Steam.InputController.Key.None && steamInputManager.IsInput(BokuMono.Steam.InputFlags.Up, keyController, isMultiple: true);
                var isKeyboardUp = keyKeyboard != UnityEngine.KeyCode.None && (Event.current.keyCode == keyKeyboard && Event.current.type == EventType.KeyUp);

                var isKeyUp = isControllerUp || isKeyboardUp;
                if (!isKeyUp) return false;

                isPressingMap[tag] = false;
            }

            var isControllerDown = keyController != BokuMono.Steam.InputController.Key.None && steamInputManager.IsInput(BokuMono.Steam.InputFlags.Down, keyController, isMultiple: true);
            var isKeyboardDown = keyKeyboard != UnityEngine.KeyCode.None && Input.GetKeyInt((BepInEx.Unity.IL2CPP.UnityEngine.KeyCode)keyKeyboard) && Event.current.type == EventType.KeyDown;

            var isKeyDown = isControllerDown || isKeyboardDown;
            if (isKeyDown)
            {
                isPressingMap[tag] = true;
                return true;
            }

            return false;
        }

        private static void DoSort()
        {
            InventoryDataBase inventoryData;

            if (uiInventoryBox is UIPageCommonRefrigerator)
            {
                var refrigeratorManager = RefrigeratorManager.GetInstance();
                inventoryData = refrigeratorManager.GetRefrigeratorData();
            }
            else if (uiInventoryBox is UIPageCommonStorageBox)
            {
                var storageBoxManager = StorageBoxManager.GetInstance();
                inventoryData = storageBoxManager.GetStorageBoxData();
            }
            else if (uiInventoryBox is UIPageCommonShippingBox)
            {
                var shipmentManager = ShipmentManager.GetInstance();
                inventoryData = shipmentManager.Data;
            }
            else
            {
                return;
            }


            SortContainer(inventoryData);
        }

        private static void DoDeposit()
        {
            if (uiInventoryBox == null) return;

            var bagManager = BagManager.GetInstance();
            var itemCount = bagManager.CurrentMax();
            var items = new Dictionary<uint, List<BagItem>>();

            for (int i = 0; i < itemCount; i++)
            {
                var item = bagManager.GetInventoryData(i);
                if (item.ItemId == ITEM_EMPTY) continue;
                var bagItem = new BagItem(itemData: item, index: i);

                if (items.ContainsKey(item.ItemId))
                {
                    items[item.ItemId].Add(bagItem);
                } else
                {
                    items[item.ItemId] = new() { bagItem };
                }
            }

            var storageBoxItems = StorageBoxManager
                .GetInstance()
                .storageBoxList
                .ToList()
                .SelectMany(storageBox => storageBox
                    .Data
                    .GetInventoryDataList()
                    .ToList()
                );

            var fridgeItems = RefrigeratorManager
                .GetInstance()
                .RefrigeratorList
                .ToList()
                .SelectMany(fridge => fridge
                    .Data
                    .GetInventoryDataList()
                    .ToList()
                );

            var maxStackSize = Math.Clamp(
                value: (BepInExLoader.configDepositStackSize?.Value ?? BepInExLoader.DEFAULT_DEPOSIT_STACK_SIZE),
                min: 1,
                max: 999
            );

            var containerItems = storageBoxItems.Concat(fridgeItems);
            foreach (var item in containerItems)
            {
                if (!items.ContainsKey(item.ItemId)) continue;
                if (item.StackCount >= maxStackSize) continue;

                var availableStackSize = item.ItemMasterData.StackSize - item.StackCount;
                if (availableStackSize <= 0) continue;

                var matchingBagItems = items[item.ItemId];
                matchingBagItems.RemoveAll(matchingBagItem =>
                {
                    var bagItemData = matchingBagItem.ItemData;

                    if (bagItemData.Quality != item.Quality) return false;
                    if (bagItemData.Color != item.Color) return false;

                    var transferCount = new[] { availableStackSize, bagItemData.StackCount, maxStackSize - item.StackCount }.Min();
                    if (transferCount <= 0) return false;

                    item.StackCount += transferCount;
                    bagItemData.StackCount -= transferCount;
                    if (bagItemData.StackCount > 0) return false;

                    bagManager.inventoryData.SetInventoryData(matchingBagItem.Index, new());
                    return true;
                });
            }

            uiInventoryBox?.ForceRefresh();
        }

        private static void DoSprint()
        {
            if (!canSprint || playerCharacter == null) return;

            playerCharacter.runSpeed = 6;
            playerCharacter.inWeedSpeed = 6;
            isSprinting = true;
        }

        private static void DoShip()
        {
            if (uiInventoryBox is not UIPageCommonShippingBox) return;
            var shipmentManager = ShipmentManager.GetInstance();

            shipmentManager.ExecuteShip();
            uiInventoryBox.ForceRefresh();
        }

        private static void SortContainer(InventoryDataBase inventoryData)
        {
            if (uiInventoryBox == null) return;

            var items = inventoryData.GetInventoryDataList().ToList();
            var sortedItems = items
                .OrderBy(item => item.ItemId)
                .ThenBy(item => item.Quality)
                .ThenBy(item => item.StackCount)
                .Reverse();

            for (int i = 0; i < items.Count; i++)
            {
                var newItem = sortedItems.ElementAt(i);
                inventoryData.SetInventoryData(i, newItem);
            }

            uiInventoryBox.ForceRefresh();
        }
    }
}
