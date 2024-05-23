using BepInEx.Configuration;

namespace SunkenlandUtil
{
    public static class LoadConfig
    {
        public static ConfigEntry<int> StackAmount;

        public static ConfigEntry<float> MaxEnergy;
        public static ConfigEntry<float> MaxAir;
        public static ConfigEntry<float> MaxHealth;
        public static ConfigEntry<float> EnergyConsumptionRate;
        public static ConfigEntry<float> AirConsumtionRate;
        public static ConfigEntry<float> StaminaRecoveryRate;
        public static ConfigEntry<float> HealthRecoveryRate;
        public static ConfigEntry<float> FoodConsumtionRate;
        public static ConfigEntry<float> WaterConsumtionRate;

        public static ConfigEntry<float> AdditionalSwimSpped;
        public static ConfigEntry<float> AdditionalWalkSpped;

        public static ConfigEntry<int> Defence;
        public static ConfigEntry<int> MaxItemsAmount;

        public static ConfigEntry<bool> WorldSensor;
        public static ConfigEntry<bool> ScanOre;
        public static ConfigEntry<string> ScanOreTypes;
        public static ConfigEntry<int> SensorSpan;

        //public static ConfigEntry<bool> ScanBluePrint;

        public static ConfigEntry<bool> DamageArmor;
        public static ConfigEntry<float> MetalProcessingDuration;
        public static ConfigEntry<int> DecomposeTime;
        public static ConfigEntry<int> SawmillNeedTime;
        public static ConfigEntry<int> FirearmsRecoveryTime;
        
        public static ConfigEntry<float> HeadLightBatteryPowerConsumption;
        public static ConfigEntry<bool> SleepAnytime;
        public static ConfigEntry<bool> DestroyReturnAll;
        //public static ConfigEntry<bool> NotDropItemWhenDie;
        public static ConfigEntry<float> BoatSpeedRate;

        const string Section = "General";
        internal static void Init(ConfigFile config)
        {
            StackAmount = config.Bind(Section, nameof(StackAmount), defaultValue: 5, "堆叠倍数 / Stack Amount Multiplier");

            MaxEnergy = config.Bind(Section, nameof(MaxEnergy), defaultValue: 0f, "额外 最大能量 / Additional Max Energy");
            MaxAir = config.Bind(Section, nameof(MaxAir), defaultValue: 0f, "额外 最大空气 / Additional Max Air");
            MaxHealth = config.Bind(Section, nameof(MaxHealth), defaultValue: 0f, "额外 最大HP / Additional Max Health");
            Defence = config.Bind(Section, nameof(Defence), defaultValue: 0, "额外 防御 / Additional Defence");
            MaxItemsAmount = config.Bind(Section, nameof(MaxItemsAmount), defaultValue: 0, "额外 物品栏 / Additional Bag Slots");

            AirConsumtionRate = config.Bind(Section, nameof(AirConsumtionRate), defaultValue: 1f, "空气消耗率 倍率 / Multiplier");
            EnergyConsumptionRate = config.Bind(Section, nameof(EnergyConsumptionRate), defaultValue: 1f, "能量消耗率 倍率 / Multiplier");
            StaminaRecoveryRate = config.Bind(Section, nameof(StaminaRecoveryRate), defaultValue: 1f, "体力恢复率 倍率 / Multiplier");
            HealthRecoveryRate = config.Bind(Section, nameof(HealthRecoveryRate), defaultValue: 1f, "HP恢复率 倍率 / Multiplier");
            FoodConsumtionRate = config.Bind(Section, nameof(FoodConsumtionRate), defaultValue: 1f, "食物消耗率率 倍率 / Multiplier");
            WaterConsumtionRate = config.Bind(Section, nameof(WaterConsumtionRate), defaultValue: 1f, "水消耗率率 倍率 / Multiplier");
            AdditionalSwimSpped = config.Bind(Section, nameof(AdditionalSwimSpped), defaultValue: 0f, "额外 游泳速度 / Additional");
            AdditionalWalkSpped = config.Bind(Section, nameof(AdditionalWalkSpped), defaultValue: 0f, "额外 行走速度 / Additional");

            WorldSensor = config.Bind(Section, nameof(WorldSensor), defaultValue: false, "启用世界探测器");
            ScanOre = config.Bind(Section, nameof(ScanOre), defaultValue: true, "探测矿石");
            ScanOreTypes = config.Bind(Section, nameof(ScanOreTypes), defaultValue: "MineCopper,MineIron,MineSulfur", "探测矿石类型 Empty For All (MineCopper,MineIron,MineSulfur,Anatase,Other)");
            SensorSpan = config.Bind(Section, nameof(SensorSpan), defaultValue: 30, "探测器间隔");
            //ScanBluePrint = config.Bind(Section, nameof(ScanBluePrint), defaultValue: true, "探测蓝图");

            DamageArmor = config.Bind(Section, nameof(DamageArmor), defaultValue: true, "护甲损坏 / Toggle  Damage Armor");

            MetalProcessingDuration = config.Bind(Section, nameof(MetalProcessingDuration), defaultValue: 0f, "金属处理时间/秒, 0 不变, 游戏默认 30");
            DecomposeTime = config.Bind(Section, nameof(DecomposeTime), defaultValue: 0, "分解台 (DecomposeTable) 速度/秒, 0 不变, 游戏默认 30");
            SawmillNeedTime = config.Bind(Section, nameof(SawmillNeedTime), defaultValue: 0, "锯木厂速度/秒, 0 不变, 游戏默认 30");
            FirearmsRecoveryTime = config.Bind(Section, nameof(FirearmsRecoveryTime), defaultValue: 0, "枪械回收速度/秒, 0 不变, 游戏默认 30");

            HeadLightBatteryPowerConsumption = config.Bind(Section, nameof(HeadLightBatteryPowerConsumption), defaultValue: 0f, "头灯电池消耗速度, 0 不变, 游戏默认 0.01");

            SleepAnytime = config.Bind(Section, nameof(SleepAnytime), defaultValue: false, "随时睡觉");

            DestroyReturnAll = config.Bind(Section, nameof(DestroyReturnAll), defaultValue: false, "拆除返还全部材料");
            //NotDropItemWhenDie = config.Bind(Section, nameof(NotDropItemWhenDie), defaultValue: false, "死亡不掉落物品");
            BoatSpeedRate = config.Bind(Section, nameof(BoatSpeedRate), defaultValue: 1f, "船速 倍率 / Multiplier");
        }
    }
}
