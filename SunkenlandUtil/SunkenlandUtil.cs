using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityGameUI;
using static UnityEngine.Random;

namespace SunkenlandUtil
{
    [BepInPlugin("satroki.sunkenland.util", "Util Plugin", "0.0.7")]
    public class SunkenlandUtil : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new Harmony("satroki.sunkenland.util");
        public static ManualLogSource _logger;
        private static bool worldSensor = false;
        private static bool scanOre;
        private static int sensorSpan;
        private static bool returnBottle = false;
        private static Text text;
        private static GameObject uiArrow;
        private static RectTransform arrowTransform;
        private static int fcnt;
        private static Component nearestObj;
        private static GameObject uiPanel;
        private static GameObject uiText;
        private static ConfigFile config;

        private void Awake()
        {
            _logger = Logger;

            config = Config;
            InitConfig();
            try
            {
                _harmony.PatchAll(typeof(SunkenlandUtil));
                _logger.LogInfo("UtilPlugin is loaded!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        private static void InitConfig()
        {
            LoadConfig.Init(config);
            worldSensor = LoadConfig.WorldSensor.Value;
            scanOre = LoadConfig.ScanOre.Value;
            sensorSpan = LoadConfig.SensorSpan.Value;
            returnBottle = LoadConfig.ReturnBottle.Value;
            _logger.LogInfo("UtilPlugin InitConfig");
        }

        [HarmonyPatch(typeof(RM), "LoadResources")]
        [HarmonyPostfix]
        public static void LoadResources(ref RM __instance)
        {
            var m = LoadConfig.StackAmount.Value;
            foreach (var item in __instance.ItemDictionary.Values)
            {
                if (item.stackAmount > 1)
                    item.stackAmount *= m;
            }
            _logger.LogInfo($"LoadResources Change StackAmount");
        }


        [HarmonyPatch(typeof(PlayerCharacter), "CalculatePlayerStats")]
        [HarmonyPostfix]
        public static void CalculatePlayerStats(ref PlayerCharacter __instance, ref Storage ___playerStorage)
        {
            __instance.AirConsumtionRate *= LoadConfig.AirConsumtionRate.Value;
            __instance.EnergyConsumptionRate *= LoadConfig.EnergyConsumptionRate.Value;
            __instance.StaminaRecoveryRate *= LoadConfig.StaminaRecoveryRate.Value;

            Traverse.Create(__instance).Property(nameof(PlayerCharacter.DefenceBody)).SetValue(__instance.DefenceBody + LoadConfig.Defence.Value);
            Traverse.Create(__instance).Property(nameof(PlayerCharacter.DefenceHead)).SetValue(__instance.DefenceHead + LoadConfig.Defence.Value);
            ___playerStorage.MaxItemsAmount += LoadConfig.MaxItemsAmount.Value;
        }

        [HarmonyPatch(typeof(PlayerCharacter), "DamageArmor")]
        [HarmonyPrefix]
        public static bool DamageArmor(ref float point, ref int type)
        {
            return LoadConfig.DamageArmor.Value;
        }

        [HarmonyPatch(typeof(UICombat), "Update")]
        [HarmonyPostfix]
        public static void UICombatUpdate(ref UICombat __instance)
        {
            if (worldSensor && (bool)WorldScene.code && (bool)Global.code.Player)
            {
                fcnt++;
                if (uiPanel == null)
                {
                    CreateUI();
                    _logger.LogInfo($"Create UIText");
                }
                if (fcnt >= sensorSpan)
                {
                    text.text = GetNearestObject(Global.code.Player.transform.position);
                    fcnt = 0;
                    uiPanel.SetActive((bool)nearestObj);
                }
                UpdateArrow();
            }
        }


        [HarmonyPatch(typeof(WorldManager), "OnLogoutFromSceneWorld")]
        [HarmonyPostfix]
        public static void OnLogoutFromSceneWorld()
        {
            uiPanel?.SetActive(false);
        }

        [HarmonyPatch(typeof(WorldManager), "OnWorldSceneLoaded")]
        [HarmonyPostfix]
        public static void OnWorldSceneLoaded()
        {
            config?.Reload();
            InitConfig();
        }

        [HarmonyPatch(typeof(Workstation), "CraftItem")]
        [HarmonyPostfix]
        public static void CraftItem(ref Transform craftItem, ref bool __result)
        {
            if (returnBottle && __result)
            {
                var component = craftItem.GetComponent<Craftable>();
                if (component.NeedTime > 0f)
                {
                    var itemRequirements = component.itemRequirements;
                    foreach (var itemRequirement in itemRequirements)
                    {
                        if (itemRequirement.item.GetComponent<Item>().ItemID == RM.code.FreshWaterBottle.ItemID)
                        {
                            Transform itemTransform = Utility.Instantiate(RM.code.EmptyWaterBottle).transform;
                            if (!Global.code.Player.playerStorage.AddItem(itemTransform))
                            {
                                Global.code.Player.quickSlotStorage.AddItem(itemTransform);
                            }
                            Global.code.uiCombat.RefreshQuickSlot();
                        }
                    }
                }
            }
        }

        private static void CreateUI()
        {
            GameObject canvas = UIControls.createUICanvas();
            uiPanel = UIControls.createUIPanel(canvas, "50", "240", Screen.width - 120, Screen.height - 75, null);
            uiPanel.GetComponent<Image>().color = UIControls.HTMLString2Color("#00000000");

            uiText = UIControls.createUIText(uiPanel, null, "#FFFFFFFF");
            uiText.GetComponent<RectTransform>().localPosition = new Vector3(0, 0, 0);
            text = uiText.GetComponent<Text>();
            text.text = "";
            text.fontSize = 20;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;

            uiArrow = UIControls.createUIText(uiPanel, null, "#FFFFFFFF");
            arrowTransform = uiArrow.GetComponent<RectTransform>();
            arrowTransform.localPosition = new Vector3(-90, 0, 0);
            arrowTransform.sizeDelta = new Vector2(40, 40);
            var arrowText = uiArrow.GetComponent<Text>();
            arrowText.text = "↑";
            arrowText.fontSize = 30;
            arrowText.fontStyle = FontStyle.Bold;
            arrowText.alignment = TextAnchor.MiddleCenter;
        }

        public static string GetNearestObject(Vector3 position)
        {
            var nearestDistanceSqr = float.PositiveInfinity;
            nearestObj = null;
            foreach (var collectable in WorldScene.code.worldCollectableContinuingInteractions)
            {
                if (collectable)
                {
                    float num = Vector3.Distance(collectable.transform.position, position);
                    if (num < nearestDistanceSqr)
                    {
                        nearestDistanceSqr = num;
                        nearestObj = collectable;
                    }
                }
            }
            foreach (var scavengeable in WorldScene.code.worldScavengables)
            {
                if (scavengeable)
                {
                    float num = Vector3.Distance(scavengeable.transform.position, position);
                    if (num < nearestDistanceSqr)
                    {
                        nearestDistanceSqr = num;
                        nearestObj = scavengeable;
                    }
                }
            }
            foreach (var breakable in WorldScene.code.worldBreakables)
            {
                if (breakable)
                {
                    float num = Vector3.Distance(breakable.transform.position, position);
                    if (num < nearestDistanceSqr)
                    {
                        nearestDistanceSqr = num;
                        nearestObj = breakable;
                    }
                }
            }
            foreach (var chest in WorldScene.code.worldChests)
            {
                if (chest)
                {
                    float num = Vector3.Distance(chest.transform.position, position);
                    if (num < nearestDistanceSqr)
                    {
                        nearestDistanceSqr = num;
                        nearestObj = chest;
                    }
                }
            }
            if (scanOre)
            {
                foreach (var choppable in WorldScene.code.choppables)
                {
                    if (choppable && choppable.M_ChoppableType != ChoppableType.Other)
                    {
                        float num = Vector3.Distance(choppable.transform.position, position);
                        if (num < nearestDistanceSqr)
                        {
                            nearestDistanceSqr = num;
                            nearestObj = choppable;
                        }
                    }
                }
            }
            if (nearestDistanceSqr < 200)
            {
                var y = nearestObj.transform.position.y - position.y;
                string sy;
                if (y >= 0)
                    sy = "↑";
                else
                {
                    sy = "↓";
                    y = -y;
                }

                return $"{nearestDistanceSqr:0.0}    {y:0}{sy}";
            }
            nearestObj = null;
            return null;
        }

        public static void UpdateArrow()
        {
            if (nearestObj && uiArrow)
            {
                var vf = Vector3.ProjectOnPlane(FPSPlayer.code.transform.forward, Vector3.up);
                var vt = Vector3.ProjectOnPlane(nearestObj.transform.position - FPSPlayer.code.transform.position, Vector3.up);
                float num = Utility.ContAngle(vf, vt, Vector3.up);
                arrowTransform.localRotation = Quaternion.Euler(0f, 0f, 0f - num);
            }
        }
    }

    public static class LoadConfig
    {
        public static ConfigEntry<int> StackAmount;
        public static ConfigEntry<float> AirConsumtionRate;
        public static ConfigEntry<float> EnergyConsumptionRate;
        public static ConfigEntry<float> StaminaRecoveryRate;
        public static ConfigEntry<int> Defence;
        public static ConfigEntry<int> MaxItemsAmount;
        public static ConfigEntry<bool> WorldSensor;
        public static ConfigEntry<bool> ScanOre;
        public static ConfigEntry<int> SensorSpan;
        public static ConfigEntry<bool> DamageArmor;
        public static ConfigEntry<bool> ReturnBottle;
        const string Section = "General";
        internal static void Init(ConfigFile config)
        {
            StackAmount = config.Bind(Section, nameof(StackAmount), defaultValue: 10, "堆叠倍数");
            AirConsumtionRate = config.Bind(Section, nameof(AirConsumtionRate), defaultValue: 1f, "空气消耗率倍率");
            EnergyConsumptionRate = config.Bind(Section, nameof(EnergyConsumptionRate), defaultValue: 1f, "能量消耗率倍率");
            StaminaRecoveryRate = config.Bind(Section, nameof(StaminaRecoveryRate), defaultValue: 1f, "体力恢复率倍率");
            Defence = config.Bind(Section, nameof(Defence), defaultValue: 0, "额外防御");
            MaxItemsAmount = config.Bind(Section, nameof(MaxItemsAmount), defaultValue: 0, "额外物品栏");
            WorldSensor = config.Bind(Section, nameof(WorldSensor), defaultValue: false, "启用世界探测器");
            ScanOre = config.Bind(Section, nameof(ScanOre), defaultValue: true, "探测矿石");
            SensorSpan = config.Bind(Section, nameof(SensorSpan), defaultValue: 30, "探测器间隔");
            DamageArmor = config.Bind(Section, nameof(DamageArmor), defaultValue: true, "护甲损坏");
            ReturnBottle = config.Bind(Section, nameof(ReturnBottle), defaultValue: false, "灶台返还水瓶");
        }
    }
}
