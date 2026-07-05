using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityGameUI;

namespace SunkenlandUtil
{
    [BepInPlugin("satroki.sunkenland.util", "Util Plugin", "0.3.3")]
    public class SunkenlandUtil : BaseUnityPlugin
    {
        private readonly Harmony _harmony = new Harmony("satroki.sunkenland.util");
        public static ManualLogSource _logger;
        private static bool worldSensor = false;
        private static bool scanOre;
        private static float sensorX;
        private static float sensorY;

        private static int sensorSpan;
        private static float sensorScale;
        private static bool sleepAnytime;
        private static bool destroyReturnAll;
        private static float headLightBatteryPowerConsumption;
        private static float nvdBatteryPowerConsumption;
        private static float boatSpeedRate;
        private static int enemyDisplayCount;
        private static SensorUI worldObj;
        private static SensorUI worldOreObj;
        private static int fcnt;
        private static GameObject uiPanel;
        private static ConfigFile config;
        private static Dictionary<int, int> stackBakDict;
        private static ChoppableType[] scanOreTypes;
        private static string[] sensorFilter;
        private static string[] sensorPriority;

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

            // 销毁旧 UI，下次 Update 时以新配置重建
            if (uiPanel != null)
            {
                var canvas = uiPanel.transform.parent?.gameObject;
                UnityEngine.Object.Destroy(uiPanel);
                UnityEngine.Object.Destroy(canvas);
                uiPanel = null;
                worldObj = null;
                worldOreObj = null;
            }

            //ChangeStackAmount(RM.code);
        }

        private static void InitConfig()
        {
            LoadConfig.Init(config);
            worldSensor = LoadConfig.WorldSensor.Value;
            scanOre = LoadConfig.ScanOre.Value;
            sensorX = LoadConfig.SensorX.Value;
            sensorY = LoadConfig.SensorY.Value;
            sensorSpan = LoadConfig.SensorSpan.Value;
            sensorScale = LoadConfig.SensorScale.Value;
            sleepAnytime = LoadConfig.SleepAnytime.Value;
            destroyReturnAll = LoadConfig.DestroyReturnAll.Value;
            headLightBatteryPowerConsumption = LoadConfig.HeadLightBatteryPowerConsumption.Value;
            nvdBatteryPowerConsumption = LoadConfig.NVDBatteryPowerConsumption.Value;
            boatSpeedRate = LoadConfig.BoatSpeedRate.Value;
            enemyDisplayCount = LoadConfig.EnemyDisplayCount.Value;
            if (LoadConfig.ScanOreTypes.Value is string s)
                scanOreTypes = s.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => Enum.Parse<ChoppableType>(t)).ToArray();
            else
                scanOreTypes = null;

            sensorFilter = LoadConfig.SensorFilter.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray() ?? Array.Empty<string>();
            sensorPriority = LoadConfig.SensorPriority.Value?.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray() ?? Array.Empty<string>();
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
            __instance.StaminaRegenPerSecond *= LoadConfig.StaminaRecoveryRate.Value;
            __instance.FoodConsumtionRate *= LoadConfig.FoodConsumtionRate.Value;
            __instance.WaterConsumtionRate *= LoadConfig.WaterConsumtionRate.Value;

            FPSRigidBodyWalker.code.swimSpeed += LoadConfig.AdditionalSwimSpped.Value;
            FPSRigidBodyWalker.code.walkSpeed += LoadConfig.AdditionalWalkSpped.Value;
            FPSRigidBodyWalker.code.sprintSpeed += LoadConfig.AdditionalWalkSpped.Value;

            Traverse.Create(__instance).Property(nameof(PlayerCharacter.DefenceBody)).SetValue(__instance.DefenceBody + LoadConfig.Defence.Value);
            Traverse.Create(__instance).Property(nameof(PlayerCharacter.DefenceHead)).SetValue(__instance.DefenceHead + LoadConfig.Defence.Value);
        }

        [HarmonyPatch(typeof(PlayerCharacter), "UpdateSwimming")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> UpdateSwimming(IEnumerable<CodeInstruction> instructions)
        {
            var v = LoadConfig.AirTankRatio.Value;
            if (v > 0 && v != 1f)
            {
                var codes = instructions.ToList();
                var airTankRatioField = AccessTools.Field(typeof(ConstantData), nameof(ConstantData.AirTankRatio));
                for (int i = 0; i < codes.Count - 2; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld &&
                        codes[i].operand is FieldInfo fi &&
                        fi == airTankRatioField &&
                        codes[i + 1].opcode == OpCodes.Mul &&
                        codes[i + 2].opcode == OpCodes.Add)
                    {
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Ldc_R4, v));
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Mul));
                        _logger.LogInfo($"Set AirTankRatio {v}");
                        break;
                    }
                }
                return codes;
            }
            return instructions;
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
            if (headLightBatteryPowerConsumption > 0)
                ___batteryPowerConsumptionPerSecond = headLightBatteryPowerConsumption;
            return true;
        }

        [HarmonyPatch(typeof(NVD), "ConsumeBatteryPower")]
        [HarmonyPrefix]
        public static bool NVDConsumeBatteryPower(ref float ___batteryPowerConsumptionPerSecond)
        {
            if (nvdBatteryPowerConsumption > 0)
                ___batteryPowerConsumptionPerSecond = nvdBatteryPowerConsumption;
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

        [HarmonyPatch(typeof(CollectableByToolHit), "Hit")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> CollectableByToolHitHit(IEnumerable<CodeInstruction> instructions)
        {
            var v = LoadConfig.CollectableByToolHitDropRate.Value;
            if (v > 1)
            {
                var codes = instructions.ToList();
                var m = AccessTools.PropertySetter(typeof(Item), nameof(Item.Amount));
                var si = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.operand is MethodInfo mi && mi == m);
                codes[si - 1] = new CodeInstruction(OpCodes.Ldc_I4, v);
                _logger.LogInfo($"Set CollectableByToolHitDropRate {v}");
                return codes;
            }
            return instructions;
        }


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

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(Location), "CS")]
        public static IEnumerable<CodeInstruction> CS(IEnumerable<CodeInstruction> instructions)
        {
            if (enemyDisplayCount == 5)
            {
                return instructions;
            }

            var v = enemyDisplayCount;
            var codes = instructions.ToList();

            var i = codes.FindIndex(c => c.opcode == OpCodes.Ldc_I4_5);
            codes[i] = new CodeInstruction(OpCodes.Ldc_I4_0);

            i = codes.FindIndex(i + 1, c => c.opcode == OpCodes.Ldc_I4_5);
            codes[i] = new CodeInstruction(OpCodes.Ldc_I4, v);
            _logger.LogInfo($"Set EnemyDisplayCount {v}");
            return codes;
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
                worldObj?.UpdateArrow();
                worldOreObj?.UpdateArrow();
            }
        }

        private static void CreateUI()
        {
            GameObject canvas = UIControls.createUICanvas();
            const float panelW = 300f;
            const float panelH = 72f;
            var x = sensorX < 0 ? Screen.width + sensorX : sensorX;
            var y = sensorY < 0 ? Screen.height + sensorY : sensorY;
            // 根据缩放调整位置，使固定边不偏移
            if (sensorX >= 0) x += panelW / 2f * (sensorScale - 1f);
            else x -= panelW / 2f * (sensorScale - 1f);
            if (sensorY >= 0) y += panelH / 2f * (sensorScale - 1f);
            else y -= panelH / 2f * (sensorScale - 1f);
            uiPanel = UIControls.createUIPanel(canvas, (panelH * sensorScale).ToString(), (panelW * sensorScale).ToString(), x, y, null);
            var panelImage = uiPanel.GetComponent<Image>();
#if DEBUG
            panelImage.color = UIControls.HTMLString2Color("#30FF0000");  // 半透明红色显示面板范围
#else
            panelImage.color = UIControls.HTMLString2Color("#00000000"); // 全透明
#endif

            worldObj = new SensorUI(uiPanel, 0, sensorScale);
            worldOreObj = new SensorUI(uiPanel, -36, sensorScale);
        }

        private static bool IsFiltered(Component component)
        {
            if (sensorFilter == null || sensorFilter.Length == 0)
                return false;
            var name = component.name;
            foreach (var prefix in sensorFilter)
            {
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool InteractableFilter(string name)
        {
            return name.StartsWith("SolarPanel", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("Wind_pumping_station", StringComparison.OrdinalIgnoreCase);
        }


        public static void GetNearestObject(Vector3 position)
        {
            float distLimit = 200f;
            Component nearestObj = null;
            var nearestDist = float.PositiveInfinity;
            Component priorityObj = null;
            var priorityDist = float.PositiveInfinity;

            static bool IsPriority(string name)
            {
                foreach (var p in sensorPriority)
                    if (name.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }

            foreach (var iitem in WorldScene.code.interactableItems)
            {
                if (!iitem || !iitem.gameObject.activeSelf)
                    continue;

                if (iitem.TryGetComponent<Interaction>(out var interaction))
                {
                    var root = interaction.root;

                    Component comp = null;
                    if (InteractableFilter(root.name))
                        comp = root;
                    else if (root.TryGetComponent<Chest>(out var chest) && chest.isActiveAndEnabled)
                        comp = chest;
                    else if (root.TryGetComponent<CollectableContinuingInteraction>(out var collectable) && collectable.isActiveAndEnabled)
                        comp = collectable;
                    else if (root.TryGetComponent<Scavengeable>(out var scavengeable) && scavengeable.isActiveAndEnabled)
                        comp = scavengeable;

                    if (comp != null && !IsFiltered(comp))
                    {
                        var dist = Vector3.Distance(iitem.position, position);
                        if (dist < distLimit)
                        {
                            if (dist < nearestDist)
                            { nearestDist = dist; nearestObj = comp; }
                            if (sensorPriority.Length > 0 && dist < priorityDist && IsPriority(comp.name))
                            { priorityDist = dist; priorityObj = comp; }
                        }
                    }
                }
            }

            // 有优先级对象在范围内则优先使用
            if (priorityDist < distLimit)
            {
                nearestObj = priorityObj;
                nearestDist = priorityDist;
            }

            if (nearestDist < distLimit)
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

                var hDist = Vector3.Distance(new Vector3(nearestObj.transform.position.x, 0f, nearestObj.transform.position.z), new Vector3(position.x, 0f, position.z));
                worldObj.UpdateObjectAndText(nearestObj, $"{hDist:0}  {y:0}{sy}  {GetObjName(nearestObj)}");
            }
            else
            {
                worldObj.UpdateObjectAndText(null, null);
            }

            if (scanOre)
            {
                var nearestOreDistanceSqr = float.PositiveInfinity;
                CollectableByToolHit nearestOreObj = null;
                foreach (var choppable in WorldScene.code.CollectableByToolHit)
                {
                    if (choppable && choppable.GetComponent<CollectableByToolHit>())
                    {
                        var c = choppable.GetComponent<CollectableByToolHit>();
                        if (!c.isActiveAndEnabled)
                            continue;
                        if (scanOreTypes != null && scanOreTypes.Length > 0 && Array.IndexOf(scanOreTypes, c.ChoppableType) < 0)
                            continue;
                        float num = Vector3.Distance(choppable.transform.position, position);
                        if (num < nearestOreDistanceSqr)
                        {
                            nearestOreDistanceSqr = num;
                            nearestOreObj = c;
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
                    var type = nearestOreObj.ChoppableType.ToString().Replace("Mine", "");
                    var hDist = Vector3.Distance(new Vector3(nearestOreObj.transform.position.x, 0f, nearestOreObj.transform.position.z), new Vector3(position.x, 0f, position.z));
                    worldOreObj.UpdateObjectAndText(nearestOreObj, $"{hDist:0}  {y:0}{sy}  {type}");
                }
                else
                {
                    worldOreObj.UpdateObjectAndText(null, null);
                }
            }
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

        public SensorUI(GameObject uiPanel, int y, float scale = 1f)
        {
            uiArrow = UIControls.createUIText(uiPanel, null, "#FFFFFFFF");
            arrowTransform = uiArrow.GetComponent<RectTransform>();
            arrowTransform.localPosition = new Vector3(-120f * scale, y * scale, 0);
            arrowTransform.sizeDelta = new Vector2(36f * scale, 36f * scale);
            var arrowText = uiArrow.GetComponent<Text>();
            arrowText.text = "↑";
            arrowText.fontSize = (int)(26f * scale);
            arrowText.fontStyle = FontStyle.Bold;
            arrowText.alignment = TextAnchor.MiddleCenter;

            uiText = UIControls.createUIText(uiPanel, null, "#FFFFFFFF");
            var textRect = uiText.GetComponent<RectTransform>();
            textRect.localPosition = new Vector3(0, y * scale, 0);
            textRect.sizeDelta = new Vector2(200f * scale, 30f * scale);
            text = uiText.GetComponent<Text>();
            text.text = "";
            text.fontSize = (int)(18f * scale);
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
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
