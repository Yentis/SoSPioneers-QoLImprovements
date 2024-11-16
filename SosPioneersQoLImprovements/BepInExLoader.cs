using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BokuMono;
using BokuMono.Data;
using BokuMono.InFieldManager;
using BokuMono.Steam;
using HarmonyLib;
using UnityEngine;

namespace SoSPioneersQoLImprovements
{
    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class BepInExLoader : BasePlugin
    {
        public const string
            MODNAME = "QoLImprovements",
            AUTHOR = "Yentis",
            GUID = "com." + AUTHOR + "." + MODNAME,
            VERSION = "1.2.0";

        public const KeyCode DEFAULT_SORT_KEYBOARD_KEY = KeyCode.Tab;
        public const InputController.Key DEFAULT_SORT_CONTROLLER_KEY = InputController.Key.LT;
        public const KeyCode DEFAULT_DEPOSIT_KEYBOARD_KEY = KeyCode.LeftAlt;
        public const InputController.Key DEFAULT_DEPOSIT_CONTROLLER_KEY = InputController.Key.RT;
        public const int DEFAULT_DEPOSIT_STACK_SIZE = 999;
        public const KeyCode DEFAULT_SPRINT_KEYBOARD_KEY = KeyCode.Space;
        public const InputController.Key DEFAULT_SPRINT_CONTROLLER_KEY = InputController.Key.LStick;
        public const KeyCode DEFAULT_SHIP_KEYBOARD_KEY = KeyCode.F;
        public const InputController.Key DEFAULT_SHIP_CONTROLLER_KEY = InputController.Key.BACK;
        public const bool DEFAULT_REVEAL_STAIRS = true;
        public const bool DEFAULT_REVEAL_PITFALLS = false;
        public const bool DEFAULT_PITFALLS_NO_DAMAGE = false;
        public const bool DEFAULT_NO_GIFT_EXP_LIMIT = true;

        public static ManualLogSource? Logger;
        public static ConfigEntry<KeyCode>? configSortButtonKeyboard;
        public static ConfigEntry<InputController.Key>? configSortButtonController;
        public static ConfigEntry<KeyCode>? configDepositButtonKeyboard;
        public static ConfigEntry<InputController.Key>? configDepositButtonController;
        public static ConfigEntry<int>? configDepositStackSize;
        public static ConfigEntry<KeyCode>? configSprintButtonKeyboard;
        public static ConfigEntry<InputController.Key>? configSprintButtonController;
        public static ConfigEntry<KeyCode>? configShipButtonKeyboard;
        public static ConfigEntry<InputController.Key>? configShipButtonController;
        public static ConfigEntry<bool>? configRevealStairs;
        public static ConfigEntry<bool>? configRevealPitfalls;
        public static ConfigEntry<bool>? configPitfallsNoDamage;
        public static ConfigEntry<bool>? configNoGiftExpLimit;

        public override void Load()
        {
            Logger = Log;
            Logger.LogMessage("Loading");

            configSortButtonKeyboard = Config.Bind(
                section: "General",
                key: "SortButtonKeyboard",
                defaultValue: DEFAULT_SORT_KEYBOARD_KEY,
                description: "Key to use for sorting using Keyboard"
            );

            configSortButtonController = Config.Bind(
                section: "General",
                key: "SortButtonController",
                defaultValue: DEFAULT_SORT_CONTROLLER_KEY,
                description: "Key to use for sorting using Controller"
            );

            configDepositButtonKeyboard = Config.Bind(
                section: "General",
                key: "DepositButtonKeyboard",
                defaultValue: DEFAULT_DEPOSIT_KEYBOARD_KEY,
                description: "Key to use for depositing bag contents using Keyboard"
            );

            configDepositButtonController = Config.Bind(
                section: "General",
                key: "DepositButtonController",
                defaultValue: DEFAULT_DEPOSIT_CONTROLLER_KEY,
                description: "Key to use for depositing bag contents using Controller"
            );

            configDepositStackSize = Config.Bind(
                section: "General",
                key: "DepositStackSize",
                defaultValue: DEFAULT_DEPOSIT_STACK_SIZE,
                description: "Maximum stack size when depositing bag content"
            );

            configSprintButtonKeyboard = Config.Bind(
                section: "General",
                key: "SprintButtonKeyboard",
                defaultValue: DEFAULT_SPRINT_KEYBOARD_KEY,
                description: "Key to use for sprinting using Keyboard (must have bike unlocked)"
            );

            configSprintButtonController = Config.Bind(
                section: "General",
                key: "SprintButtonController",
                defaultValue: DEFAULT_SPRINT_CONTROLLER_KEY,
                description: "Key to use for sprinting using Controller (must have bike unlocked)"
            );

            configShipButtonKeyboard = Config.Bind(
                section: "General",
                key: "ShipButtonKeyboard",
                defaultValue: DEFAULT_SHIP_KEYBOARD_KEY,
                description: "Key to use for shipping all items in the shipping bin using Keyboard"
            );

            configShipButtonController = Config.Bind(
                section: "General",
                key: "ShipButtonController",
                defaultValue: DEFAULT_SHIP_CONTROLLER_KEY,
                description: "Key to use for shipping all items in the shipping bin using Controller"
            );

            configRevealStairs = Config.Bind(
                section: "General",
                key: "RevealStairs",
                defaultValue: DEFAULT_REVEAL_STAIRS,
                description: "Whether or not to always reveal the stairs in mines"
            );

            configRevealPitfalls = Config.Bind(
                section: "General",
                key: "RevealPitfalls",
                defaultValue: DEFAULT_REVEAL_PITFALLS,
                description: "Whether or not to always reveal the pitfalls in the 3rd mine"
            );

            configPitfallsNoDamage = Config.Bind(
                section: "General",
                key: "PitfallsNoDamage",
                defaultValue: DEFAULT_PITFALLS_NO_DAMAGE,
                description: "Prevent pitfalls from draining your stamina"
            );

            configNoGiftExpLimit = Config.Bind(
                section: "General",
                key: "NoGiftExpLimit",
                defaultValue: DEFAULT_NO_GIFT_EXP_LIMIT,
                description: "The game only rewards communication exp when gifting twice per week per person, enabling this setting always rewards exp"
            );

            var harmony = new Harmony("yentis.qolimprovements.il2cpp");

            harmony.Patch(
                original: typeof(DateManager).GetMethod(nameof(DateManager.Update)),
                postfix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.Update)))
            );

            var showInventoryBoxPatch = new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.UIShowInventoryBox)));

            harmony.Patch(
                original: typeof(UIPageCommonRefrigerator).GetMethod(nameof(UIPageCommonRefrigerator.ShowPage)),
                postfix: showInventoryBoxPatch
            );

            harmony.Patch(
                original: typeof(UIPageCommonStorageBox).GetMethod(nameof(UIPageCommonStorageBox.InitInventoryBox)),
                postfix: showInventoryBoxPatch
            );

            harmony.Patch(
                original: typeof(UIPageCommonShippingBox).GetMethod(nameof(UIPageCommonShippingBox.InitInventoryBox)),
                postfix: showInventoryBoxPatch
            );

            harmony.Patch(
                original: typeof(UIPageCommon).GetMethod(nameof(UIPageCommon.Cancel)),
                postfix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.UIHide)))
            );

            harmony.Patch(
                original: typeof(PlayerCharacter).GetMethod(nameof(PlayerCharacter.Start)),
                postfix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.PlayerStart)))
            );

            harmony.Patch(
                original: typeof(PlayerCharacter).GetMethod(nameof(PlayerCharacter.UpdateSpeed)),
                postfix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.PlayerMove)))
            );

            harmony.Patch(
                original: typeof(RideableBikeManager).GetMethod(nameof(RideableBikeManager.FromSaveData)),
                postfix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.BikeStart)))
            );

            harmony.Patch(
                original: typeof(MineFloorData).GetMethod(nameof(MineFloorData.IsUseFixedRoom)),
                postfix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.MineFloor)))
            );

            harmony.Patch(
                original: typeof(MineManager).GetMethod(nameof(MineManager.PitFallDamage)),
                prefix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.PitFallDamage)))
            );

            harmony.Patch(
                original: typeof(CityDevelopExpMaster).GetMethod(nameof(CityDevelopExpMaster.GetMasterData)),
                postfix: new HarmonyMethod(typeof(PatchManager).GetMethod(nameof(PatchManager.CityDevelopLimit)))
            );

            Logger.LogMessage("Started");
        }
    }
}