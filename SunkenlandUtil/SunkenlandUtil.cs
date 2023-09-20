using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityGameUI;

namespace SunkenlandUtil
{
    [BepInPlugin("satroki.sunkenland.util", "Util Plugin", "0.0.8")]
    public class SunkenlandUtil : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new Harmony("satroki.sunkenland.util");
        public static ManualLogSource _logger;
        private static bool worldSensor = false;
        private static bool scanOre;
        private static int sensorSpan;
        private static bool returnBottle = false;
        private static bool sleepAnytime;
        private static float batteryPowerConsumption;
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

        private static void InitConfig()
        {
            LoadConfig.Init(config);
            worldSensor = LoadConfig.WorldSensor.Value;
            scanOre = LoadConfig.ScanOre.Value;
            sensorSpan = LoadConfig.SensorSpan.Value;
            returnBottle = LoadConfig.ReturnBottle.Value;
            sleepAnytime = LoadConfig.SleepAnytime.Value;
            batteryPowerConsumption = LoadConfig.HeadLightBatteryPowerConsumption.Value;
            _logger.LogInfo("UtilPlugin InitConfig");
        }

        #region Character
        [HarmonyPatch(typeof(PlayerCharacter), "CalculatePlayerStats")]
        [HarmonyPostfix]
        public static void CalculatePlayerStats(ref PlayerCharacter __instance, ref Storage ___playerStorage)
        {
            __instance.MaxEnergy += LoadConfig.MaxEnergy.Value;
            __instance.MaxAir += LoadConfig.MaxAir.Value;
            __instance.MaxHealth += LoadConfig.MaxHealth.Value;

            __instance.AirConsumtionRate *= LoadConfig.AirConsumtionRate.Value;
            __instance.EnergyConsumptionRate *= LoadConfig.EnergyConsumptionRate.Value;
            __instance.StaminaRecoveryRate *= LoadConfig.StaminaRecoveryRate.Value;
            __instance.FoodConsumtionRate *= LoadConfig.FoodConsumtionRate.Value;
            __instance.HealthRecoveryRate *= LoadConfig.HealthRecoveryRate.Value;

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
        #endregion

        #region GamePlay   
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

        [HarmonyPatch(typeof(Furnace), "Awake")]
        [HarmonyPostfix]
        public static void FurnaceAwake(ref float ___itemProcessingDuration)
        {
            if (LoadConfig.MetalProcessingDuration.Value > 0)
            {
                ___itemProcessingDuration = LoadConfig.MetalProcessingDuration.Value;
                _logger.LogInfo($"Furnace Set ItemProcessingDuration {___itemProcessingDuration}");
            }
        }

        [HarmonyPatch(typeof(SteelFurnace), "Awake")]
        [HarmonyPostfix]
        public static void SteelFurnaceAwake(ref float ___itemProcessingDuration)
        {
            if (LoadConfig.MetalProcessingDuration.Value > 0)
            {
                ___itemProcessingDuration = LoadConfig.MetalProcessingDuration.Value;
                _logger.LogInfo($"SteelFurnace Set ItemProcessingDuration {___itemProcessingDuration}");
            }
        }

        [HarmonyPatch(typeof(HeadLight), "ConsumeBatteryPower")]
        [HarmonyPrefix]
        public static bool HeadLightConsumeBatteryPower(ref float ___batteryPowerConsumptionPerSecond)
        {
            if (batteryPowerConsumption > 0)
                ___batteryPowerConsumptionPerSecond = batteryPowerConsumption;
            return true;
        }

        [HarmonyPatch(typeof(GlobalData), "CurMinute", methodType: MethodType.Setter)]
        [HarmonyPostfix]
        public static void GlobalDataCanSleep(ref GlobalData __instance)
        {
            if (sleepAnytime && __instance.Object.HasStateAuthority)
                __instance.CanSleep = true;
        }
        #endregion

        #region Sensor
        [HarmonyPatch(typeof(UICombat), "Update")]
        [HarmonyPostfix]
        public static void UpdateUI()
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
        #endregion
    }
}
