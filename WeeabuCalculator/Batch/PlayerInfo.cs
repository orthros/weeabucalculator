using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class PlayerInfo
    {
        public int Speed { get; private set; }
        public int Determination { get; private set; }
        public int CriticalHitRating { get; private set; }
        public int DirectHitRating { get; private set; }
        public int AttackPower { get; private set; }
        public int WeaponDamage { get; private set; }
        public float AutoAttackDamage { get; private set; }
        public float AutoAttackDelay { get; private set; }

        public static Player BuildPlayer(PlayerInfo p, JobMechanics job)
        {
            return new Player(job)
            {
                Speed = p.Speed,
                Determination = p.Determination,
                CriticalHitRating = p.CriticalHitRating,
                DirectHitRating = p.DirectHitRating,
                AttackPower = p.AttackPower,
                WeaponDamage = p.WeaponDamage,
                AutoAttackDamage = p.AutoAttackDamage,
                AutoAttackDelay = p.AutoAttackDelay
            };
        }
    }
}
