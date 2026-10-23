using KartRider.Game;
using System;

namespace KartRider
{
    public class GameType
    {
        // Token: 0x040007FE RID: 2046
        public static byte RiderSchoolType;

        // Token: 0x040007FF RID: 2047
        public static int ScenarioType;

        // Token: 0x04000800 RID: 2048
        public static byte StartType;

        // Token: 0x04000801 RID: 2049
        public static byte EnterMyRoomType;
    }
    // Token: 0x02000011 RID: 17
    public class GameDataReset
    {
        public static void Kart_DataReset()
        {
            Kart.draftMulAccelFactor = 0f;
            Kart.draftTick = 0;
            Kart.driftBoostMulAccelFactor = 0f;
            Kart.driftBoostTick = 0;
            Kart.chargeBoostBySpeed = 0f;
            Kart.SpeedSlotCapacity = 0;
            Kart.ItemSlotCapacity = 0;
            Kart.SpecialSlotCapacity = 0;
            Kart.UseTransformBooster = false;
            Kart.motorcycleType = false;
            Kart.BikeRearWheel = false;
            Kart.Mass = 0f;
            Kart.AirFriction = 0f;
            Kart.DragFactor = 0f;
            Kart.ForwardAccelForce = 0f;
            Kart.BackwardAccelForce = 0f;
            Kart.GripBrakeForce = 0f;
            Kart.SlipBrakeForce = 0f;
            Kart.MaxSteerAngle = 0f;
            Kart.SteerConstraint = 0f;
            Kart.FrontGripFactor = 0f;
            Kart.RearGripFactor = 0f;
            Kart.DriftTriggerFactor = 0f;
            Kart.DriftTriggerTime = 0f;
            Kart.DriftSlipFactor = 0f;
            Kart.DriftEscapeForce = 0f;
            Kart.CornerDrawFactor = 0f;
            Kart.DriftLeanFactor = 0f;
            Kart.SteerLeanFactor = 0f;
            Kart.DriftMaxGauge = 0f;
            Kart.NormalBoosterTime = 0f;
            Kart.ItemBoosterTime = 0f;
            Kart.TeamBoosterTime = 0f;
            Kart.AnimalBoosterTime = 0f;
            Kart.SuperBoosterTime = 0f;
            Kart.TransAccelFactor = 0f;
            Kart.BoostAccelFactor = 0f;
            Kart.StartBoosterTimeItem = 0f;
            Kart.StartBoosterTimeSpeed = 0f;
            Kart.StartForwardAccelForceItem = 0f;
            Kart.StartForwardAccelForceSpeed = 0f;
            Kart.DriftGaguePreservePercent = 0f;
            Kart.UseExtendedAfterBooster = false;
            Kart.BoostAccelFactorOnlyItem = 0f;
            Kart.antiCollideBalance = 0f;
            Kart.dualBoosterSetAuto = false;
            Kart.dualBoosterTickMin = 0;
            Kart.dualBoosterTickMax = 0;
            Kart.dualMulAccelFactor = 0f;
            Kart.dualTransLowSpeed = 0f;
            Kart.PartsEngineLock = false;
            Kart.PartsWheelLock = false;
            Kart.PartsSteeringLock = false;
            Kart.PartsBoosterLock = false;
            Kart.PartsCoatingLock = false;
            Kart.PartsTailLampLock = false;
            Kart.chargeInstAccelGaugeByBoost = 0f;
            Kart.chargeInstAccelGaugeByGrip = 0f;
            Kart.chargeInstAccelGaugeByWall = 0f;
            Kart.instAccelFactor = 0f;
            Kart.instAccelGaugeCooldownTime = 0;
            Kart.instAccelGaugeLength = 0f;
            Kart.instAccelGaugeMinUsable = 0f;
            Kart.instAccelGaugeMinVelBound = 0f;
            Kart.instAccelGaugeMinVelLoss = 0f;
            Kart.useExtendedAfterBoosterMore = false;
            Kart.wallCollGaugeCooldownTime = 0;
            Kart.wallCollGaugeMaxVelLoss = 0f;
            Kart.wallCollGaugeMinVelBound = 0f;
            Kart.wallCollGaugeMinVelLoss = 0f;
        }

        // Token: 0x06000031 RID: 49 RVA: 0x00053F1C File Offset: 0x0005211C

    
        // Token: 0x0600002D RID: 45 RVA: 0x00053BDD File Offset: 0x00051DDD
        public static void GameType_DataReset()
        {
            GameType.RiderSchoolType = 0;
            GameType.ScenarioType = 0;
            GameType.StartType = 0;
            GameType.EnterMyRoomType = 0;
        }

    }

    // Token: 0x02000013 RID: 19
    public class Kart
    {
        // Token: 0x04000802 RID: 2050
        public static float AirFriction;

        // Token: 0x04000803 RID: 2051
        public static float AnimalBoosterTime;

        // Token: 0x04000804 RID: 2052
        public static float antiCollideBalance;

        // Token: 0x04000805 RID: 2053
        public static float BackwardAccelForce;

        // Token: 0x04000806 RID: 2054
        public static bool BikeRearWheel;

        // Token: 0x04000807 RID: 2055
        public static float BoostAccelFactor;

        // Token: 0x04000808 RID: 2056
        public static float BoostAccelFactorOnlyItem;

        // Token: 0x04000809 RID: 2057
        public static float chargeBoostBySpeed;

        // Token: 0x0400080A RID: 2058
        public static float CornerDrawFactor;

        // Token: 0x0400080B RID: 2059
        public static float draftMulAccelFactor;

        // Token: 0x0400080C RID: 2060
        public static int draftTick;

        // Token: 0x0400080D RID: 2061
        public static float DragFactor;

        // Token: 0x0400080E RID: 2062
        public static float driftBoostMulAccelFactor;

        // Token: 0x0400080F RID: 2063
        public static int driftBoostTick;

        // Token: 0x04000810 RID: 2064
        public static float DriftEscapeForce;

        // Token: 0x04000811 RID: 2065
        public static float DriftGaguePreservePercent;

        // Token: 0x04000812 RID: 2066
        public static float DriftLeanFactor;

        // Token: 0x04000813 RID: 2067
        public static float DriftMaxGauge;

        // Token: 0x04000814 RID: 2068
        public static float DriftSlipFactor;

        // Token: 0x04000815 RID: 2069
        public static float DriftTriggerFactor;

        // Token: 0x04000816 RID: 2070
        public static float DriftTriggerTime;

        // Token: 0x04000817 RID: 2071
        public static float ForwardAccelForce;

        // Token: 0x04000818 RID: 2072
        public static float FrontGripFactor;

        // Token: 0x04000819 RID: 2073
        public static float GripBrakeForce;

        // Token: 0x0400081A RID: 2074
        public static float ItemBoosterTime;

        // Token: 0x0400081B RID: 2075
        public static byte ItemSlotCapacity;

        // Token: 0x0400081C RID: 2076
        public static float Mass;

        // Token: 0x0400081D RID: 2077
        public static float MaxSteerAngle;

        // Token: 0x0400081E RID: 2078
        public static bool motorcycleType;

        // Token: 0x0400081F RID: 2079
        public static float NormalBoosterTime;

        // Token: 0x04000820 RID: 2080
        public static float RearGripFactor;

        // Token: 0x04000821 RID: 2081
        public static float SlipBrakeForce;

        // Token: 0x04000822 RID: 2082
        public static byte SpecialSlotCapacity;

        // Token: 0x04000823 RID: 2083
        public static byte SpeedSlotCapacity;

        // Token: 0x04000824 RID: 2084
        public static float StartBoosterTimeItem;

        // Token: 0x04000825 RID: 2085
        public static float StartBoosterTimeSpeed;

        // Token: 0x04000826 RID: 2086
        public static float StartForwardAccelForceItem;

        // Token: 0x04000827 RID: 2087
        public static float StartForwardAccelForceSpeed;

        // Token: 0x04000828 RID: 2088
        public static float SteerConstraint;

        // Token: 0x04000829 RID: 2089
        public static float SteerLeanFactor;

        // Token: 0x0400082A RID: 2090
        public static float SuperBoosterTime;

        // Token: 0x0400082B RID: 2091
        public static float TeamBoosterTime;

        // Token: 0x0400082C RID: 2092
        public static float TransAccelFactor;

        // Token: 0x0400082D RID: 2093
        public static bool UseExtendedAfterBooster;

        // Token: 0x0400082E RID: 2094
        public static bool UseTransformBooster;

        // Token: 0x0400082F RID: 2095
        public static bool dualBoosterSetAuto;

        // Token: 0x04000830 RID: 2096
        public static int dualBoosterTickMin;

        // Token: 0x04000831 RID: 2097
        public static int dualBoosterTickMax;

        // Token: 0x04000832 RID: 2098
        public static float dualMulAccelFactor;

        // Token: 0x04000833 RID: 2099
        public static float dualTransLowSpeed;

        // Token: 0x04000834 RID: 2100
        public static bool PartsEngineLock;

        // Token: 0x04000835 RID: 2101
        public static bool PartsWheelLock;

        // Token: 0x04000836 RID: 2102
        public static bool PartsSteeringLock;

        // Token: 0x04000837 RID: 2103
        public static bool PartsBoosterLock;

        // Token: 0x04000838 RID: 2104
        public static bool PartsCoatingLock;

        // Token: 0x04000839 RID: 2105
        public static bool PartsTailLampLock;

        // Token: 0x0400083A RID: 2106
        public static float chargeInstAccelGaugeByBoost;

        // Token: 0x0400083B RID: 2107
        public static float chargeInstAccelGaugeByGrip;

        // Token: 0x0400083C RID: 2108
        public static float chargeInstAccelGaugeByWall;

        // Token: 0x0400083D RID: 2109
        public static float instAccelFactor;

        // Token: 0x0400083E RID: 2110
        public static int instAccelGaugeCooldownTime;

        // Token: 0x0400083F RID: 2111
        public static float instAccelGaugeLength;

        // Token: 0x04000840 RID: 2112
        public static float instAccelGaugeMinUsable;

        // Token: 0x04000841 RID: 2113
        public static float instAccelGaugeMinVelBound;

        // Token: 0x04000842 RID: 2114
        public static float instAccelGaugeMinVelLoss;

        // Token: 0x04000843 RID: 2115
        public static bool useExtendedAfterBoosterMore;

        // Token: 0x04000844 RID: 2116
        public static int wallCollGaugeCooldownTime;

        // Token: 0x04000845 RID: 2117
        public static float wallCollGaugeMaxVelLoss;

        // Token: 0x04000846 RID: 2118
        public static float wallCollGaugeMinVelBound;

        // Token: 0x04000847 RID: 2119
        public static float wallCollGaugeMinVelLoss;

        // Token: 0x0600002E RID: 46 RVA: 0x00053BF8 File Offset: 0x00051DF8
        public static void StartGame_DataReset()
        {
            StartGameData.StartTimeAttack_SpeedType = 2;
            StartGameData.Kart_id = 0;
            StartGameData.StartTimeAttack_Track = 0U;
            StartGameData.StartTimeAttack_StartType = 0;
        }
        public class StartGameData
        {// Token: 0x040007F1 RID: 2033
            public static float DragFactor;

            // Token: 0x040007F2 RID: 2034
            public static float ForwardAccelForce;

            // Token: 0x040007F3 RID: 2035
            public static float BackwardAccelForce;

            // Token: 0x040007F4 RID: 2036
            public static float GripBrakeForce;

            // Token: 0x040007F5 RID: 2037
            public static float SlipBrakeForce;

            // Token: 0x040007F6 RID: 2038
            public static float SteerConstraint;

            // Token: 0x040007F7 RID: 2039
            public static float DriftEscapeForce;

            // Token: 0x040007F8 RID: 2040
            public static float CornerDrawFactor;

            // Token: 0x040007F9 RID: 2041
            public static float DriftMaxGauge;

            // Token: 0x040007FA RID: 2042
            public static float TransAccelFactor;

            // Token: 0x040007FB RID: 2043
            public static float BoostAccelFactor;

            // Token: 0x040007FC RID: 2044
            public static float StartForwardAccelForceItem;

            // Token: 0x040007FD RID: 2045
            public static float StartForwardAccelForceSpeed;
            // Token: 0x04000848 RID: 2120
            public static byte StartTimeAttack_SpeedType;

            // Token: 0x04000849 RID: 2121
            public static short Kart_id;

            // Token: 0x0400084A RID: 2122
            public static uint StartTimeAttack_Track;

            // Token: 0x0400084B RID: 2123
            public static byte StartTimeAttack_StartType;
        }
        public static void SpeedTypeData()
        {
            bool flag = StartGameData.StartTimeAttack_SpeedType == 0;
            if (flag)
            {
                SpeedType.DragFactor = -0.01f;
                SpeedType.ForwardAccelForce = -150f;
                SpeedType.BackwardAccelForce = -225f;
                SpeedType.GripBrakeForce = -270f;
                SpeedType.SlipBrakeForce = -215f;
                SpeedType.SteerConstraint = -0.25f;
                SpeedType.DriftEscapeForce = -100f;
                SpeedType.CornerDrawFactor = 0.02f;
                SpeedType.DriftMaxGauge = -300f;
                SpeedType.TransAccelFactor = 0.0045f;
                SpeedType.BoostAccelFactor = 0.006f;
                SpeedType.StartForwardAccelForceItem = -270f;
                SpeedType.StartForwardAccelForceSpeed = -270f;
            }
            else
            {
                bool flag2 = StartGameData.StartTimeAttack_SpeedType == 1;
                if (flag2)
                {
                    SpeedType.DragFactor = 0.013f;
                    SpeedType.ForwardAccelForce = 250f;
                    SpeedType.BackwardAccelForce = 225f;
                    SpeedType.GripBrakeForce = 270f;
                    SpeedType.SlipBrakeForce = 145f;
                    SpeedType.SteerConstraint = 0.55f;
                    SpeedType.DriftEscapeForce = 700f;
                    SpeedType.CornerDrawFactor = 0.02f;
                    SpeedType.DriftMaxGauge = 700f;
                    SpeedType.TransAccelFactor = 0.0045f;
                    SpeedType.BoostAccelFactor = 0.006f;
                    SpeedType.StartForwardAccelForceItem = 450f;
                    SpeedType.StartForwardAccelForceSpeed = 450f;
                }
                else
                {
                    bool flag3 = StartGameData.StartTimeAttack_SpeedType == 2;
                    if (flag3)
                    {
                        SpeedType.DragFactor = 0.051f;
                        SpeedType.ForwardAccelForce = 750f;
                        SpeedType.BackwardAccelForce = 450f;
                        SpeedType.GripBrakeForce = 540f;
                        SpeedType.SlipBrakeForce = 325f;
                        SpeedType.SteerConstraint = 0.75f;
                        SpeedType.DriftEscapeForce = 1100f;
                        SpeedType.CornerDrawFactor = 0.02f;
                        SpeedType.DriftMaxGauge = 1700f;
                        SpeedType.TransAccelFactor = 0.0045f;
                        SpeedType.BoostAccelFactor = 0.006f;
                        SpeedType.StartForwardAccelForceItem = 1350f;
                        SpeedType.StartForwardAccelForceSpeed = 1350f;
                    }

                }
            }
        }
        // Token: 0x0600002F RID: 47 RVA: 0x00053C14 File Offset: 0x00051E14
        public static void SpeedType_DataReset()
        {
            SpeedType.DragFactor = 0f;
            SpeedType.ForwardAccelForce = 0f;
            SpeedType.BackwardAccelForce = 0f;
            SpeedType.GripBrakeForce = 0f;
            SpeedType.SlipBrakeForce = 0f;
            SpeedType.SteerConstraint = 0f;
            SpeedType.DriftEscapeForce = 0f;
            SpeedType.CornerDrawFactor = 0f;
            SpeedType.DriftMaxGauge = 0f;
            SpeedType.TransAccelFactor = 0f;
            SpeedType.BoostAccelFactor = 0f;
            SpeedType.StartForwardAccelForceItem = 0f;
            SpeedType.StartForwardAccelForceSpeed = 0f;
        }

        // Token: 0x06000030 RID: 48 RVA: 0x00053CA4 File Offset: 0x00051EA4

    }
    public class SpeedType
    {
        public static float AirFriction;

        // Token: 0x04000803 RID: 2051
        public static float AnimalBoosterTime;

        // Token: 0x04000804 RID: 2052
        public static float antiCollideBalance;

        // Token: 0x04000805 RID: 2053
        public static float BackwardAccelForce;

        // Token: 0x04000806 RID: 2054
        public static bool BikeRearWheel;

        // Token: 0x04000807 RID: 2055
        public static float BoostAccelFactor;

        // Token: 0x04000808 RID: 2056
        public static float BoostAccelFactorOnlyItem;

        // Token: 0x04000809 RID: 2057
        public static float chargeBoostBySpeed;

        // Token: 0x0400080A RID: 2058
        public static float CornerDrawFactor;

        // Token: 0x0400080B RID: 2059
        public static float draftMulAccelFactor;

        // Token: 0x0400080C RID: 2060
        public static int draftTick;

        // Token: 0x0400080D RID: 2061
        public static float DragFactor;

        // Token: 0x0400080E RID: 2062
        public static float driftBoostMulAccelFactor;

        // Token: 0x0400080F RID: 2063
        public static int driftBoostTick;

        // Token: 0x04000810 RID: 2064
        public static float DriftEscapeForce;

        // Token: 0x04000811 RID: 2065
        public static float DriftGaguePreservePercent;

        // Token: 0x04000812 RID: 2066
        public static float DriftLeanFactor;

        // Token: 0x04000813 RID: 2067
        public static float DriftMaxGauge;

        // Token: 0x04000814 RID: 2068
        public static float DriftSlipFactor;

        // Token: 0x04000815 RID: 2069
        public static float DriftTriggerFactor;

        // Token: 0x04000816 RID: 2070
        public static float DriftTriggerTime;

        // Token: 0x04000817 RID: 2071
        public static float ForwardAccelForce;

        // Token: 0x04000818 RID: 2072
        public static float FrontGripFactor;

        // Token: 0x04000819 RID: 2073
        public static float GripBrakeForce;

        // Token: 0x0400081A RID: 2074
        public static float ItemBoosterTime;

        // Token: 0x0400081B RID: 2075
        public static byte ItemSlotCapacity;

        // Token: 0x0400081C RID: 2076
        public static float Mass;

        // Token: 0x0400081D RID: 2077
        public static float MaxSteerAngle;

        // Token: 0x0400081E RID: 2078
        public static bool motorcycleType;

        // Token: 0x0400081F RID: 2079
        public static float NormalBoosterTime;

        // Token: 0x04000820 RID: 2080
        public static float RearGripFactor;

        // Token: 0x04000821 RID: 2081
        public static float SlipBrakeForce;

        // Token: 0x04000822 RID: 2082
        public static byte SpecialSlotCapacity;

        // Token: 0x04000823 RID: 2083
        public static byte SpeedSlotCapacity;

        // Token: 0x04000824 RID: 2084
        public static float StartBoosterTimeItem;

        // Token: 0x04000825 RID: 2085
        public static float StartBoosterTimeSpeed;

        // Token: 0x04000826 RID: 2086
        public static float StartForwardAccelForceItem;

        // Token: 0x04000827 RID: 2087
        public static float StartForwardAccelForceSpeed;

        // Token: 0x04000828 RID: 2088
        public static float SteerConstraint;

        // Token: 0x04000829 RID: 2089
        public static float SteerLeanFactor;

        // Token: 0x0400082A RID: 2090
        public static float SuperBoosterTime;

        // Token: 0x0400082B RID: 2091
        public static float TeamBoosterTime;

        // Token: 0x0400082C RID: 2092
        public static float TransAccelFactor;

        // Token: 0x0400082D RID: 2093
        public static bool UseExtendedAfterBooster;

        // Token: 0x0400082E RID: 2094
        public static bool UseTransformBooster;

        // Token: 0x0400082F RID: 2095
        public static bool dualBoosterSetAuto;

        // Token: 0x04000830 RID: 2096
        public static int dualBoosterTickMin;

        // Token: 0x04000831 RID: 2097
        public static int dualBoosterTickMax;

        // Token: 0x04000832 RID: 2098
        public static float dualMulAccelFactor;

        // Token: 0x04000833 RID: 2099
        public static float dualTransLowSpeed;

        // Token: 0x04000834 RID: 2100
        public static bool PartsEngineLock;

        // Token: 0x04000835 RID: 2101
        public static bool PartsWheelLock;

        // Token: 0x04000836 RID: 2102
        public static bool PartsSteeringLock;

        // Token: 0x04000837 RID: 2103
        public static bool PartsBoosterLock;

        // Token: 0x04000838 RID: 2104
        public static bool PartsCoatingLock;

        // Token: 0x04000839 RID: 2105
        public static bool PartsTailLampLock;

        // Token: 0x0400083A RID: 2106
        public static float chargeInstAccelGaugeByBoost;

        // Token: 0x0400083B RID: 2107
        public static float chargeInstAccelGaugeByGrip;

        // Token: 0x0400083C RID: 2108
        public static float chargeInstAccelGaugeByWall;

        // Token: 0x0400083D RID: 2109
        public static float instAccelFactor;

        // Token: 0x0400083E RID: 2110
        public static int instAccelGaugeCooldownTime;

        // Token: 0x0400083F RID: 2111
        public static float instAccelGaugeLength;

        // Token: 0x04000840 RID: 2112
        public static float instAccelGaugeMinUsable;

        // Token: 0x04000841 RID: 2113
        public static float instAccelGaugeMinVelBound;

        // Token: 0x04000842 RID: 2114
        public static float instAccelGaugeMinVelLoss;

        // Token: 0x04000843 RID: 2115
        public static bool useExtendedAfterBoosterMore;

        // Token: 0x04000844 RID: 2116
        public static int wallCollGaugeCooldownTime;

        // Token: 0x04000845 RID: 2117
        public static float wallCollGaugeMaxVelLoss;

        // Token: 0x04000846 RID: 2118
        public static float wallCollGaugeMinVelBound;

        // Token: 0x04000847 RID: 2119
        public static float wallCollGaugeMinVelLoss;
        // Token: 0x0600002B RID: 43 RVA: 0x000539FC File Offset: 0x00051BFC

    }
}