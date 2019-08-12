using System;
using System.Reflection;
using System.Xml;
using Harmony;
// using UnityEngine;

namespace TrainEgo
{
    public class HellTrainWeapon : EquipmentScriptBase
    {
        private class HellTrainBuff : UnitBuf
        {
            private BarrierEffect barrier;
            public HellTrainBuff()
            {
                duplicateType = BufDuplicateType.ONLY_ONE;
                type = UnitBufType.BARRIER_B;
                remainTime = 15f;
            }

            public override void Init(UnitModel model)
            {
                base.Init(model);
                barrier = BarrierEffect.MakeBarrier(model as WorkerModel);
                barrier.SetRemainRate(1f);
                barrier.SetRwbpType(RwbpType.B);
            }
            public override float OnTakeDamage(UnitModel attacker, DamageInfo damageInfo)
            {
                if (damageInfo.type == RwbpType.B)
                    return -1f;
                return base.OnTakeDamage(attacker, damageInfo);
            }
            public override void OnDestroy()
            {
                base.OnDestroy();
                if (remainTime <= 0f)
                    barrier.OnDisappear();
                else
                    barrier.OnBreak();
            }

            public override void OnUnitDie()
            {
                base.OnUnitDie();
                Destroy();
            }
            public override void OnUnitPanic()
            {
                base.OnUnitPanic();
                Destroy();
            }
        }
        public override bool OnGiveDamage(UnitModel actor, UnitModel target, ref DamageInfo dmg)
        {
            // Should make the weapon do no damage
            dmg = DamageInfo.zero;
            // Activate buff to absorb BLACK damage
            if (!actor.HasUnitBuf(UnitBufType.BARRIER_B))
                actor.AddUnitBuf(new HellTrainBuff());
            return base.OnGiveDamage(actor, target, ref dmg);
        }

        /* OLD VERSION OF DAMAGE ABSORBTION 
        public override bool OnTakeDamage_After(float value, RwbpType type)
        {
            if (type == RwbpType.B)
                value = -value;

            return base.OnTakeDamage_After(value, type);
        }
        */
    }

    public class Harmony_Patch
    {
        public static int totalTickets = 0;
        public static int ticketsRequired = 20;

        // Properly set by loading IDs from .xml 
        public static int trainWeaponID = 200000;
        public static int trainArmorID = 300000;
        public static int trainGiftID = 400000;

        public Harmony_Patch()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                // Get filepath here
                doc.Load("");

                XmlNode node = doc.DocumentElement.SelectSingleNode("/equipment_list");
                foreach (XmlNode child in node.ChildNodes)
                {
                    foreach (XmlNode n in child.ChildNodes)
                    {
                        if (n.Name == "name")
                        {
                            if (n.InnerText == "HellTrain_weapon_name")
                            {
                                if (child.Attributes["type"] != null && child.Attributes["id"] != null)
                                {
                                    string type = child.Attributes["type"].Value;
                                    int id = Int32.Parse(child.Attributes["id"].Value);
                                    if (type == "weapon")
                                        trainWeaponID = id;
                                    else if (type == "armor")
                                        trainArmorID = id;
                                    else if (type == "special")
                                        trainGiftID = id;
                                }
                            }
                        }
                    }
                }

                HarmonyInstance hInstance = HarmonyInstance.Create("Lobotomy.S-Purple & Watson & NEET.TrainEgo");
                hInstance.PatchAll(Assembly.GetExecutingAssembly());
                FileLog.Log("TrainEgo Patching Successful");
            }
            catch (Exception)
            {
                FileLog.Log("TrainEgo Patching Threw an Exception...");
            }
        }

        [HarmonyPatch(typeof(HellTrain), "OnStageStart")]
        private class HellTrain_OnStageStart_Patch
        {
            static void Prefix(HellTrain __instance)
            {
                __instance.Unit.model.metaInfo.qliphothMax = 999;
            }
            static void Postfix()
            {
                totalTickets = 0;
            }
        }

        [HarmonyPatch(typeof(HellTrain), "SellTicket")]
        private class HellTrain_SellTicket_Patch
        {
            static void Prefix(HellTrain __instance)
            {
                totalTickets += (int)AccessTools.Property(typeof(HellTrain), "_otherCreatureWorkCount").GetValue(__instance);
                __instance.model.SetQliphothCounter(totalTickets);
            }
            static void Postfix()
            {
                if (totalTickets >= ticketsRequired)
                    Present();
            }
        }

        private static void Present()
        {
            int armorID = 0; // Armor ID
            if (InventoryModel.Instance.CheckEquipmentCount(armorID))
                InventoryModel.Instance.CreateEquipment(armorID);

            int wepID = 0; // Weapon ID
            if (InventoryModel.Instance.CheckEquipmentCount(wepID))
                InventoryModel.Instance.CreateEquipment(wepID);
        }

        [HarmonyPatch(typeof(HellTrain), "HasRoomCounter")]
        private class HellTrain_HasRoomCounter_Patch
        {
            static void Postfix(ref bool __result)
            {
                // Prevents any bugs involving lowering the Qliphoth counter
                __result = false;
            }
        }
    }
}
