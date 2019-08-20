using System;
using System.IO;
using System.Reflection;
using System.Xml;
using Harmony;
using UnityEngine;

namespace TrainEgoFix
{
    public class Harmony_Patch
    {
        private static int totalTickets = 0;
        private static int ticketsRequired = 20;

        // Properly set by loading IDs from .xml 
        private static int trainWeaponID = 200000;
        private static int trainArmorID = 300000;
        private static int trainGiftID = 400000;

        // Default is English
        private static string trainDialogueMath = "\"...{0}, add {1}. {2}...\"";
        private static string trainDialogueThanks = "\"...Thanks for being a recurring patron...\"";

        public Harmony_Patch()
        {
            FileLog.Log("==");
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
                FileLog.Log("Patching off of: " + Assembly.GetExecutingAssembly().FullName);
                hInstance.PatchAll(Assembly.GetExecutingAssembly());
                FileLog.Log("Patching Done");
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
                    FileLog.Log("Exception thrown in getting Tickets:");
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
            AgentModel curAgent = (AgentModel)Traverse.Create(instance).Field("curAgent").GetValue();

            if (InventoryModel.Instance.CheckEquipmentCount(trainArmorID))
            {
                var armor = InventoryModel.Instance.CreateEquipment(trainArmorID);
                if (!curAgent.HasEquipment(trainArmorID))
                    curAgent.SetArmor((ArmorModel)armor);
                instance.model.ShowNarrationForcely(trainDialogueThanks);
                showDialogue = true;
            }

            if (InventoryModel.Instance.CheckEquipmentCount(trainWeaponID))
            {
                var weapon = InventoryModel.Instance.CreateEquipment(trainWeaponID);
                if (!curAgent.HasEquipment(trainWeaponID))
                    curAgent.SetWeapon((WeaponModel)weapon);
                if (!showDialogue)
                    instance.model.ShowNarrationForcely(trainDialogueThanks);
                showDialogue = true;
            }

            EGOgiftModel gift = EGOgiftModel.MakeGift(EquipmentTypeList.instance.GetData(trainGiftID));
            curAgent.AttachEGOgift(gift);
            if (!showDialogue)
                instance.model.ShowNarrationForcely(trainDialogueThanks);
            showDialogue = true;


            totalTickets -= ticketsRequired;
        }
    }
}
