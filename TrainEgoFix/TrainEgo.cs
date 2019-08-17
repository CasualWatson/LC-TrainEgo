using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Linq;
using Harmony;
using UnityEngine;
using UnityEngine.UI;

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
            if (actor.HasUnitBuf(UnitBufType.BARRIER_B))
            {
                if (actor.GetUnitBufByName(typeof(HellTrainBuff).Name) == null)
                {
                    actor.RemoveUnitBuf(actor.GetUnitBufByType(UnitBufType.BARRIER_B));
                    actor.AddUnitBuf(new HellTrainBuff());
                }
            }
            else
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

    public class HellTrainArmor : EquipmentScriptBase
    {
        private class HellTrainDebuff : UnitBuf
        {
            public HellTrainDebuff()
            {
                duplicateType = BufDuplicateType.ONLY_ONE;
            }

            public override void Init(UnitModel model)
            {
                base.Init(model);
                remainTime = 10f;
            }
            public override float MovementScale()
            {
                return 0.5f;
            }
        }
        public override bool OnGiveDamage(UnitModel actor, UnitModel target, ref DamageInfo dmg)
        {
            if (!target.HasUnitBuf(UnitBufType.SLOW_BULLET))
                target.AddUnitBuf(new SlowBulletBuf(10f));
            else
            {

            }
            return base.OnGiveDamage(actor, target, ref dmg);
        }
    }

    public class Harmony_Patch
    {
        public static int totalTickets = 0;
        public static int ticketsRequired = 20;

        // Properly set by loading IDs from .xml 
        public static int trainWeaponID = 200000;
        public static int trainArmorID = 300000;
        public static int trainGiftID = 400000;

        // Default is English
        public static string trainDialogueMath = "\"...{0}, add {1}. {2}...\"";
        public static string trainDialogueThanks = "\"...Thanks for being a recurring patron...\"";

        public Harmony_Patch()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                XmlDocument doc2 = new XmlDocument();

                var allFiles = Directory.GetFiles(Application.dataPath + "/BaseMods", "*.*", SearchOption.AllDirectories);
                foreach (var file in allFiles)
                {
                    FileInfo info = new FileInfo(file);
                    if (info.Name.Contains("TrainEquip.txt"))
                        doc.Load(info.FullName);
                    else if (info.Name.Contains("Counter.xml"))
                        doc2.Load(info.FullName);
                }

                XmlNode node = doc.DocumentElement.SelectSingleNode("/equipment_list");
                foreach (XmlNode child in node.ChildNodes)
                {
                    foreach (XmlNode n in child.ChildNodes)
                    {
                        if (n.Name == "name")
                        {
                            if (n.InnerText == "Train_equip_name")
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
                            else if (n.InnerText == "Train_gift_name")
                            {
                                if (child.Attributes["type"] != null && child.Attributes["id"] != null)
                                {
                                    string type = child.Attributes["type"].Value;
                                    int id = Int32.Parse(child.Attributes["id"].Value);
                                    if (type == "special")
                                        trainGiftID = id;
                                }
                            }
                        }
                    }
                }

                XmlNode node2 = doc2.DocumentElement.SelectSingleNode("/translation");
                foreach (XmlNode child in node2.ChildNodes)
                {
                    if (child.Name == GlobalGameManager.instance.language)
                    {
                        foreach (XmlNode c in child.ChildNodes)
                        {
                            if (c.Name == "math")
                                trainDialogueMath = c.InnerText;
                            else if (c.Name == "thanks")
                                trainDialogueThanks = c.InnerText;
                        }
                    }
                }

                HarmonyInstance hInstance = HarmonyInstance.Create("Lobotomy.S-Purple & Watson & NEET.TrainEgo");
                hInstance.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception exception)
            {
                FileLog.Log("TrainEgo Patching Threw an Exception... " + DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt"));
                FileLog.Log(exception.ToString());
                FileLog.Log(exception.Message);
                FileLog.Log(exception.TargetSite.Name);
            }
        }

        [HarmonyPatch(typeof(HellTrain), "OnStageStart")]
        private class HellTrain_OnStageStart_Patch
        {
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
                int gain = 0;
                int prev = totalTickets;
                try
                {
                    gain = (int)Traverse.Create(__instance).Field("_otherCreatureWorkCount").GetValue();
                    totalTickets += gain;
                }
                catch (Exception excep)
                {
                    FileLog.Log("Exception in getting Tickets:");
                    FileLog.Log(excep.ToString());
                }
                __instance.model.ShowNarrationForcely(string.Format(trainDialogueMath, prev, gain, totalTickets));
            }
            static void Postfix(HellTrain __instance)
            {
                if (totalTickets >= ticketsRequired)
                    Present(ref __instance);
            }
        }

        private static void Present(ref HellTrain instance)
        {
            bool showDialogue = false;
            if (InventoryModel.Instance.CheckEquipmentCount(trainArmorID))
            {
                InventoryModel.Instance.CreateEquipment(trainArmorID);
                instance.model.ShowNarrationForcely(trainDialogueThanks);
                showDialogue = true;
            }

            if (InventoryModel.Instance.CheckEquipmentCount(trainWeaponID))
            {
                InventoryModel.Instance.CreateEquipment(trainWeaponID);
                if (!showDialogue)
                    instance.model.ShowNarrationForcely(trainDialogueThanks);
            }

            if (!instance.AllocatedAgent.HasEquipment(trainGiftID))
            {
                EGOgiftModel gift = EGOgiftModel.MakeGift(EquipmentTypeList.instance.GetData(trainGiftID));
                AgentModel curAgent = (AgentModel)Traverse.Create(instance).Field("curAgent").GetValue();
                if (!curAgent.HasEquipment(trainGiftID))
                    curAgent.AttachEGOgift(gift);
                if (!showDialogue)
                    instance.model.ShowNarrationForcely(trainDialogueThanks);
            }


            totalTickets -= ticketsRequired;
        }
    }
}
