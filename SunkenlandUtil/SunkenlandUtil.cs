using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using UnityGameUI;

namespace SunkenlandUtil
{
    [BepInPlugin("satroki.sunkenland.util", "Util Plugin", "0.1.5")]
    public class SunkenlandUtil : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new Harmony("satroki.sunkenland.util");
        public static ManualLogSource _logger;
        private static bool worldSensor = false;
        private static bool scanOre;
        //private static bool scanBluePrint;
        private static int sensorSpan;
        private static bool sleepAnytime;
        private static bool destroyReturnAll;
        private static float batteryPowerConsumption;
        private static float boatSpeedRate;
        private static SensorUI worldObj;
        private static SensorUI worldOreObj;
        //private static SensorUI worldBluePrintObj;
        private static int fcnt;
        private static GameObject uiPanel;
        private static ConfigFile config;
        private static Dictionary<int, int> stackBakDict;
        private static ChoppableType[] scanOreTypes;
        //private static Dictionary<string, BlueprintContainer> blueprints = new Dictionary<string, BlueprintContainer>();

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

            //ChangeStackAmount(RM.code);
        }

        private static void InitConfig()
        {
            LoadConfig.Init(config);
            worldSensor = LoadConfig.WorldSensor.Value;
            scanOre = LoadConfig.ScanOre.Value;
            //scanBluePrint = LoadConfig.ScanBluePrint.Value;
            sensorSpan = LoadConfig.SensorSpan.Value;
            sleepAnytime = LoadConfig.SleepAnytime.Value;
            destroyReturnAll = LoadConfig.DestroyReturnAll.Value;
            batteryPowerConsumption = LoadConfig.HeadLightBatteryPowerConsumption.Value;
            boatSpeedRate = LoadConfig.BoatSpeedRate.Value;
            if (LoadConfig.ScanOreTypes.Value is string s)
                scanOreTypes = s.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => Enum.Parse<ChoppableType>(t)).ToArray();
            else
                scanOreTypes = null;
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

            __instance.EnergyConsumptionRate *= LoadConfig.EnergyConsumptionRate.Value;
            __instance.AirConsumtionRate *= LoadConfig.AirConsumtionRate.Value;
            __instance.HealthRecoveryRate *= LoadConfig.HealthRecoveryRate.Value;
            __instance.StaminaRecoveryRate *= LoadConfig.StaminaRecoveryRate.Value;
            __instance.FoodConsumtionRate *= LoadConfig.FoodConsumtionRate.Value;
            __instance.WaterConsumtionRate *= LoadConfig.WaterConsumtionRate.Value;

            FPSRigidBodyWalker.code.swimSpeed += LoadConfig.AdditionalSwimSpped.Value;
            FPSRigidBodyWalker.code.walkSpeed += LoadConfig.AdditionalWalkSpped.Value;
            FPSRigidBodyWalker.code.sprintSpeed += LoadConfig.AdditionalWalkSpped.Value;

            Traverse.Create(__instance).Property(nameof(PlayerCharacter.DefenceBody)).SetValue(__instance.DefenceBody + LoadConfig.Defence.Value);
            Traverse.Create(__instance).Property(nameof(PlayerCharacter.DefenceHead)).SetValue(__instance.DefenceHead + LoadConfig.Defence.Value);
        }

        [HarmonyPatch(typeof(Storage), "MaxItemsAmount", methodType: MethodType.Setter)]
        [HarmonyPrefix]
        public static bool SetMaxItemsAmount(ref Storage __instance, ref int value)
        {
            if (__instance == Global.code.Player.PlayerStorage)
            {
                value += LoadConfig.MaxItemsAmount.Value;
                if (value > 100)
                    value = 100;
            }
            return true;
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
            _logger.LogInfo($"LoadResources");
            ChangeStackAmount(__instance);
        }

        public static void ChangeStackAmount(RM rm)
        {
            if (!rm || !rm.ItemDictionary.Any())
                return;
            var m = LoadConfig.StackAmount.Value;
            if (m <= 1)
                return;

            //if (stackBakDict == null)
            //{
            //    using var txt = File.CreateText("d:\\items.txt");
            //    foreach (var kv in rm.ItemDictionary.OrderBy(r => r.Key))
            //    {
            //        var item = kv.Value;
            //        var dn = item.name;
            //        try
            //        {
            //            txt.WriteLine($"{item.ItemID},{item.name.Trim()},{item.DisplayName.Trim()},{item.stackAmount}");
            //        }
            //        catch { }
            //    }
            //}

            stackBakDict ??= rm.ItemDictionary.Where(v => v.Value.stackAmount > 1).ToDictionary(v => v.Key, v => v.Value.stackAmount);

            foreach (var kv in rm.ItemDictionary)
            {
                if (stackBakDict.TryGetValue(kv.Key, out var amt))
                    kv.Value.stackAmount = amt * m;
            }
            _logger.LogInfo($"Change StackAmount × {m}");
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
        public static void SteelFurnaceAwake(ref int ___itemProcessingDuration)
        {
            if (LoadConfig.MetalProcessingDuration.Value > 0)
            {
                ___itemProcessingDuration = (int)LoadConfig.MetalProcessingDuration.Value;
                _logger.LogInfo($"SteelFurnace Set ItemProcessingDuration {___itemProcessingDuration}");
            }
        }

        [HarmonyPatch(typeof(DecomposeTable), "Awake")]
        [HarmonyPostfix]
        public static void DecomposeTableAwake(ref int ___DecomposeTime)
        {
            if (LoadConfig.DecomposeTime.Value > 0)
            {
                ___DecomposeTime = LoadConfig.DecomposeTime.Value;
                _logger.LogInfo($"DecomposeTable Set DecomposeTime {___DecomposeTime}");
            }
        }

        [HarmonyPatch(typeof(AutomaticFirearmsRecoveryStation), "CS")]
        [HarmonyPrefix]
        public static bool AutomaticFirearmsRecoveryStation(ref AutomaticFirearmsRecoveryStation __instance)
        {
            if (__instance.IsWorking && LoadConfig.FirearmsRecoveryTime.Value > 0)
            {
                __instance.RecoveryTime = LoadConfig.FirearmsRecoveryTime.Value;
            }
            return true;
        }

        [HarmonyPatch(typeof(Sawmill), "Awake")]
        [HarmonyPostfix]
        public static void SawmillAwake(ref int ___NeedTime)
        {
            if (LoadConfig.SawmillNeedTime.Value > 0)
            {
                ___NeedTime = LoadConfig.SawmillNeedTime.Value;
                _logger.LogInfo($"Sawmill Set NeedTime {___NeedTime}");
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

        [HarmonyPatch(typeof(BuildingPiece), "DestroyThis")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> DestroyThis(IEnumerable<CodeInstruction> instructions)
        {
            if (destroyReturnAll)
            {
                var codes = instructions.ToList();
                var si = codes.FindIndex(c => c.opcode == OpCodes.Conv_R4);
                var ei = codes.FindIndex(si, c => c.opcode == OpCodes.Call);
                codes.RemoveRange(si, ei - si + 1);
                _logger.LogInfo($"Enable DestroyReturnAll");
                return codes;
            }
            return instructions;
        }

        //[HarmonyPatch(typeof(PlayerCharacter), "Die")]
        //[HarmonyTranspiler]
        //public static IEnumerable<CodeInstruction> PlayerCharacterDie(IEnumerable<CodeInstruction> instructions)
        //{
        //    if (LoadConfig.NotDropItemWhenDie.Value)
        //    {
        //        var codes = instructions.ToList();
        //        var smi = AccessTools.Method(typeof(Storage), nameof(Storage.RemoveAndDestroyAllItems));
        //        var si = codes.FindIndex(c => c.opcode == OpCodes.Ldloc_0);
        //        var ei = codes.FindIndex(si, c => c.opcode == OpCodes.Callvirt && c.operand is MethodInfo mi && mi == smi);
        //        codes[si].MoveLabelsTo(codes[ei + 1]);
        //        codes.RemoveRange(si, ei - si + 1);
        //        _logger.LogInfo($"Enable NotDropItemWhenDie");
        //        return codes;
        //    }
        //    return instructions;
        //}

        [HarmonyPatch(typeof(Boat), "Spawned")]
        [HarmonyPostfix]
        public static void Boat(ref Boat __instance)
        {
            if (boatSpeedRate == 1f)
                return;
            var f = __instance.enginePower;
            __instance.enginePower = f * boatSpeedRate;
            _logger.LogInfo($"Set {__instance.name} Speed From {f} To {__instance.enginePower}");
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
                    GetNearestObject(Global.code.Player.transform.position);
                    fcnt = 0;
                    uiPanel.SetActive(worldObj.Active || worldOreObj.Active);
                }
                worldObj.UpdateArrow();
                worldOreObj.UpdateArrow();
                //worldBluePrintObj.UpdateArrow();
            }
        }

        //[HarmonyPatch(typeof(BlueprintContainer), "Start")]
        //[HarmonyPostfix]
        //public static void BlueprintContainer(ref BlueprintContainer __instance)
        //{
        //    var name = __instance.BlueprintItem.DisplayName;
        //    var bp = __instance.Blueprint;
        //    if (bp.UnlockItems?.Length > 0)
        //        name = bp.UnlockItems[0].DisplayName;
        //    if (bp.UnlockBuildings?.Length > 0)
        //        name = bp.UnlockBuildings[0].DisplayName;
        //    blueprints[name] = __instance;
        //}

        private static void CreateUI()
        {
            GameObject canvas = UIControls.createUICanvas();
            uiPanel = UIControls.createUIPanel(canvas, "150", "300", Screen.width - 150, Screen.height - 75, null);
            uiPanel.GetComponent<Image>().color = UIControls.HTMLString2Color("#00000000");

            worldObj = new SensorUI(uiPanel, 0);
            worldOreObj = new SensorUI(uiPanel, -36);
            //worldBluePrintObj = new SensorUI(uiPanel, -72);
        }

        public static void GetNearestObject(Vector3 position)
        {
            var nearestDistanceSqr = float.PositiveInfinity;
            Component nearestObj = null;
            foreach (var collectable in WorldScene.code.worldCollectableContinuingInteractions)
            {
                if (collectable && collectable.isActiveAndEnabled)
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
                if (scavengeable && scavengeable.isActiveAndEnabled)
                {
                    float num = Vector3.Distance(scavengeable.transform.position, position);
                    if (num < nearestDistanceSqr)
                    {
                        nearestDistanceSqr = num;
                        nearestObj = scavengeable;
                    }
                }
            }
            foreach (var chest in WorldScene.code.worldChests)
            {
                if (chest && chest.isActiveAndEnabled)
                {
                    float num = Vector3.Distance(chest.transform.position, position);
                    if (num < nearestDistanceSqr)
                    {
                        nearestDistanceSqr = num;
                        nearestObj = chest;
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

                worldObj.UpdateObjectAndText(nearestObj, $"{nearestDistanceSqr:0}  {y:0}{sy}  {GetObjName(nearestObj)}");
            }
            else
            {
                worldObj.UpdateObjectAndText(null, null);
            }

            if (scanOre)
            {
                var nearestOreDistanceSqr = float.PositiveInfinity;
                Choppable nearestOreObj = null;
                foreach (var choppable in WorldScene.code.choppables)
                {
                    if (choppable && choppable.isActiveAndEnabled)
                    {
                        if (scanOreTypes != null && scanOreTypes.Length > 0 && Array.IndexOf(scanOreTypes, choppable.M_ChoppableType) < 0)
                            continue;
                        float num = Vector3.Distance(choppable.transform.position, position);
                        if (num < nearestOreDistanceSqr)
                        {
                            nearestOreDistanceSqr = num;
                            nearestOreObj = choppable;
                        }
                    }
                }

                if (nearestOreDistanceSqr < 300)
                {
                    var y = nearestOreObj.transform.position.y - position.y;
                    string sy;
                    if (y >= 0)
                        sy = "↑";
                    else
                    {
                        sy = "↓";
                        y = -y;
                    }
                    var type = nearestOreObj.M_ChoppableType.ToString().Replace("Mine", "");
                    worldOreObj.UpdateObjectAndText(nearestOreObj, $"{nearestOreDistanceSqr:0}  {y:0}{sy}  {type}");
                }
                else
                {
                    worldOreObj.UpdateObjectAndText(null, null);
                }
            }

            //if (scanBluePrint)
            //{
            //    var nearestBpDistanceSqr = float.PositiveInfinity;
            //    BlueprintContainer nearestBpObj = null;
            //    string name = null;
            //    foreach (var (k, blueprint) in blueprints)
            //    {
            //        if (blueprint && blueprint.isActiveAndEnabled && GlobalDataHelper.IsGlobalDataValid && !Mainframe.code.GlobalData.HasBlueprint(blueprint.BlueprintItem.ItemID))
            //        {
            //            float num = Vector3.Distance(blueprint.transform.position, position);
            //            if (num < nearestBpDistanceSqr)
            //            {
            //                nearestBpDistanceSqr = num;
            //                nearestBpObj = blueprint;
            //                name = k;
            //            }
            //        }
            //    }

            //    if (nearestBpObj)
            //    {
            //        worldBluePrintObj.UpdateObjectAndText(nearestBpObj, $"{nearestBpDistanceSqr:0}  {name}");
            //    }
            //    else
            //    {
            //        worldBluePrintObj.UpdateObjectAndText(null, null);
            //    }
            //}
        }

        private static string GetObjName(Component component)
        {
            var name = component.name;
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var ri = name.IndexOf('_');
            if (ri >= 0)
                name = name.Remove(ri);
            ri = name.IndexOf("(");
            if (ri >= 0)
                name = name.Remove(ri);
            if (char.IsLower(name[0]))
                name = char.ToUpper(name[0]).ToString() + name[1..];
            return name.Trim();
        }
        #endregion
    }

    public class SensorUI
    {
        private Text text;
        private GameObject uiArrow;
        private RectTransform arrowTransform;
        private Component nearestObj;
        private GameObject uiText;
        public bool Active => (bool)nearestObj;

        public SensorUI(GameObject uiPanel, int y)
        {
            uiText = UIControls.createUIText(uiPanel, null, "#FFFFFFFF");
            uiText.GetComponent<RectTransform>().localPosition = new Vector3(0, y, 0);
            text = uiText.GetComponent<Text>();
            text.text = "";
            text.fontSize = 18;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;

            uiArrow = UIControls.createUIText(uiPanel, null, "#FFFFFFFF");
            arrowTransform = uiArrow.GetComponent<RectTransform>();
            arrowTransform.localPosition = new Vector3(-120, y, 0);
            arrowTransform.sizeDelta = new Vector2(36, 36);
            var arrowText = uiArrow.GetComponent<Text>();
            arrowText.text = "↑";
            arrowText.fontSize = 26;
            arrowText.fontStyle = FontStyle.Bold;
            arrowText.alignment = TextAnchor.MiddleCenter;
        }

        public void UpdateObjectAndText(Component obj, string text)
        {
            nearestObj = obj;
            this.text.text = text;
        }

        public void UpdateArrow()
        {
            if (uiArrow)
            {
                uiArrow.SetActive((bool)nearestObj);
                if (nearestObj)
                {
                    var vf = Vector3.ProjectOnPlane(FPSPlayer.code.transform.forward, Vector3.up);
                    var vt = Vector3.ProjectOnPlane(nearestObj.transform.position - FPSPlayer.code.transform.position, Vector3.up);
                    float num = Utility.ContAngle(vf, vt, Vector3.up);
                    arrowTransform.localRotation = Quaternion.Euler(0f, 0f, 0f - num);
                }
            }
        }
    }
}
