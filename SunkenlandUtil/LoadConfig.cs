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

        public static ConfigEntry<int> Defence;
        public static ConfigEntry<int> MaxItemsAmount;
        public static ConfigEntry<bool> WorldSensor;
        public static ConfigEntry<bool> ScanOre;
        public static ConfigEntry<int> SensorSpan;
        public static ConfigEntry<bool> DamageArmor;
        //public static ConfigEntry<bool> ReturnBottle;
        public static ConfigEntry<float> MetalProcessingDuration;

        public static ConfigEntry<float> HeadLightBatteryPowerConsumption;
        public static ConfigEntry<bool> SleepAnytime;
        public static ConfigEntry<bool> DestroyReturnAll;

        const string Section = "General";
        internal static void Init(ConfigFile config)
        {
            StackAmount = config.Bind(Section, nameof(StackAmount), defaultValue: 10, "堆叠倍数");

            MaxEnergy = config.Bind(Section, nameof(MaxEnergy), defaultValue: 0f, "额外 最大能量");
            MaxAir = config.Bind(Section, nameof(MaxAir), defaultValue: 0f, "额外 最大空气");
            MaxHealth = config.Bind(Section, nameof(MaxHealth), defaultValue: 0f, "额外 最大HP");
            Defence = config.Bind(Section, nameof(Defence), defaultValue: 0, "额外 防御");
            MaxItemsAmount = config.Bind(Section, nameof(MaxItemsAmount), defaultValue: 0, "额外 物品栏");

            AirConsumtionRate = config.Bind(Section, nameof(AirConsumtionRate), defaultValue: 1f, "空气消耗率 倍率");
            EnergyConsumptionRate = config.Bind(Section, nameof(EnergyConsumptionRate), defaultValue: 1f, "能量消耗率 倍率");
            StaminaRecoveryRate = config.Bind(Section, nameof(StaminaRecoveryRate), defaultValue: 1f, "体力恢复率 倍率");
            HealthRecoveryRate = config.Bind(Section, nameof(HealthRecoveryRate), defaultValue: 1f, "HP恢复率 倍率");
            FoodConsumtionRate = config.Bind(Section, nameof(FoodConsumtionRate), defaultValue: 1f, "食物消耗率率 倍率");

            WorldSensor = config.Bind(Section, nameof(WorldSensor), defaultValue: false, "启用世界探测器");
            ScanOre = config.Bind(Section, nameof(ScanOre), defaultValue: true, "探测矿石");
            SensorSpan = config.Bind(Section, nameof(SensorSpan), defaultValue: 30, "探测器间隔");

            DamageArmor = config.Bind(Section, nameof(DamageArmor), defaultValue: true, "护甲损坏");

            //ReturnBottle = config.Bind(Section, nameof(ReturnBottle), defaultValue: false, "灶台返还水瓶（0.140后不需要）");

            MetalProcessingDuration = config.Bind(Section, nameof(MetalProcessingDuration), defaultValue: 0f, "金属处理时间/秒, 0 不变, 游戏默认 30");

            HeadLightBatteryPowerConsumption = config.Bind(Section, nameof(HeadLightBatteryPowerConsumption), defaultValue: 0f, "头灯电池消耗速度, 0 不变, 游戏默认 0.01");

            SleepAnytime = config.Bind(Section, nameof(SleepAnytime), defaultValue: false, "随时睡觉");

            DestroyReturnAll = config.Bind(Section, nameof(DestroyReturnAll), defaultValue: false, "拆除返还全部材料");
        }
    }
}
