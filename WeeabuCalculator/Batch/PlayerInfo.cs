using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class PlayerInfo
    {
        public int Speed { get; set; }
        public int Determination { get; set; }
        public int CriticalHitRating { get; set; }
        public int DirectHitRating { get; set; }
        public int AttackPower { get; set; }
        public int WeaponDamage { get; set; }
        public float AutoAttackDamage { get; set; }
        public float AutoAttackDelay { get; set; }

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
