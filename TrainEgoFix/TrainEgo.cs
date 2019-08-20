﻿using System;
using System.Linq;
using System.Collections.Generic;

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

    public class HellTrainGift : EquipmentScriptBase
    {
        // static UnitBufType customType = (UnitBufType)Enum.GetValues(typeof(UnitBufType)).Cast<int>().Max() + 123;
        static UnitBufType customType = UnitBufType.ADD_SUPERARMOR;
        private class HellTrainDebuff : UnitBuf
        {
            private EffectInvoker slowEffect;
            private UnityEngine.Color effectColor = UnityEngine.Color.Lerp(UnityEngine.Color.red, UnityEngine.Color.yellow, 0.5f);

            public HellTrainDebuff()
            {
                type = customType; // Custom UnitBufType
                duplicateType = BufDuplicateType.ONLY_ONE;
                remainTime = 10f;
            }

            public override void Init(UnitModel model)
            {
                base.Init(model);
                slowEffect = EffectInvoker.Invoker("SlowEffect", model.GetMovableNode(), remainTime, false);
                var partSys = slowEffect.GetComponentInChildren<UnityEngine.ParticleSystem>();
                var partSysMain = partSys.main;
                partSysMain.startColor = UnityEngine.Color.Lerp(partSysMain.startColor.color, UnityEngine.Color.red, 0.6f);
                slowEffect.Attach();
            }
            public override float MovementScale()
            {
                return 0.5f;
            }
        }
        public HellTrainGift()
        {
            try
            {
                Harmony.FileLog.Log("TrainGift Constructed");
                Harmony.FileLog.Log("Owned by: " + model.owner.GetUnitName());
            }
            catch (Exception exception)
            {
                Harmony.FileLog.Log(exception.ToString());
            }
        }

        public override bool OnGiveDamage(UnitModel actor, UnitModel target, ref DamageInfo dmg)
        {
            Harmony.FileLog.Log("OnGiveDamage Train");
            target.AddUnitBuf(new HellTrainDebuff());
            return base.OnGiveDamage(actor, target, ref dmg);
        }
        public override void OnGiveDamageAfter(UnitModel actor, UnitModel target, DamageInfo dmg)
        {
            base.OnGiveDamageAfter(actor, target, dmg);
            Harmony.FileLog.Log("OnGiveDamageAfter Train");
        }
        public override bool OnTakeDamage(UnitModel actor, ref DamageInfo dmg)
        {
            Harmony.FileLog.Log("OnTakeDamage Train");
            return base.OnTakeDamage(actor, ref dmg);
        }
        public override bool OnTakeDamage_After(float value, RwbpType type)
        {
            Harmony.FileLog.Log("OnTakeDamageAfter Train");
            return base.OnTakeDamage_After(value, type);
        }
    }

    /*
    // Was checking to make sure it wasn't a class mispelling or misdeclaration

    public class TESTGIFT : EquipmentScriptBase 
    {
        public TESTGIFT()
        {
            Harmony.FileLog.Log("TESTGIFT Constructed.");
        }

        public override bool OnGiveDamage(UnitModel actor, UnitModel target, ref DamageInfo dmg)
        {
            Harmony.FileLog.Log("TESTGIFT OnGiveDamage ran");
            return base.OnGiveDamage(actor, target, ref dmg);
        }
        public override void OnGiveDamageAfter(UnitModel actor, UnitModel target, DamageInfo dmg)
        {
            Harmony.FileLog.Log("TESTGIFT OnGiveDamagePost ran");
            base.OnGiveDamageAfter(actor, target, dmg);
        }

        public override bool OnTakeDamage(UnitModel actor, ref DamageInfo dmg)
        {
            Harmony.FileLog.Log("TESTGIFT OnTakeDamage ran");
            return base.OnTakeDamage(actor, ref dmg);
        }
        public override bool OnTakeDamage_After(float value, RwbpType type)
        {
            Harmony.FileLog.Log("TESTGIFT OnTakeDamagePost ran");
            return base.OnTakeDamage_After(value, type);
        }
    }
    */
}
