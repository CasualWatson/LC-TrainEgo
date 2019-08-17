using System;
using System.Linq;

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
            public HellTrainDebuff()
            {
                duplicateType = BufDuplicateType.ONLY_ONE;
                type = customType; // Custom UnitBufType
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
        public HellTrainGift()
        {
            Harmony.FileLog.Log("TrainHat Constructed");
        }

        public override bool OnGiveDamage(UnitModel actor, UnitModel target, ref DamageInfo dmg)
        {
            Harmony.FileLog.Log("TrainHat Damage Run");
            if (!target.HasUnitBuf(customType))
            {
                target.AddUnitBuf(new HellTrainDebuff());
                Harmony.FileLog.Log("Added Train Debuff");
            }
            return base.OnGiveDamage(actor, target, ref dmg);
        }
        public override void OnGiveDamageAfter(UnitModel actor, UnitModel target, DamageInfo dmg)
        {
            base.OnGiveDamageAfter(actor, target, dmg);
            Harmony.FileLog.Log("TrainHat PostDamage Run");
            if (!target.HasUnitBuf(customType))
            {
                target.AddUnitBuf(new HellTrainDebuff());
                Harmony.FileLog.Log("Added Train Debuff");
            }
        }

        public override bool OnTakeDamage(UnitModel actor, ref DamageInfo dmg)
        {
            Harmony.FileLog.Log("TrainHat TakeDamage Run");
            return base.OnTakeDamage(actor, ref dmg);
        }
        public override bool OnTakeDamage_After(float value, RwbpType type)
        {
            Harmony.FileLog.Log("TrainHat PostTakeDamage Run");
            return base.OnTakeDamage_After(value, type);
        }
    }
}
