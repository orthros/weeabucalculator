using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{

    public class PlayerDoT : StatusEffect
    {
        public DoTInitialModifiers? InitialState
        { get; private set; }

        public float Potency
        { get; private set; }

        public PlayerDoT(string effectID, float potency, float duration)
            :base(effectID, duration)
        {
            Potency = potency;
            InitialState = null;
        }

        public void InitializeDoT(SimulationState state)
        {
            this.InitialState = state.GetDoTInitialState(this);
        }

        public float GetDoTTick(SimulationState state)
        {
            if (!InitialState.HasValue) throw new InvalidOperationException("DoT has not been initialized.");
            return InitialState.Value.DamageOnTick;
        }

        public PlayerDoT Clone()
        {
            return new PlayerDoT(EffectID, Potency, Duration.Value) { InitialState = InitialState };
        }
    }

    public struct DoTInitialModifiers
    {
        public float DamageOnTick
        { get; private set; }

        public DoTInitialModifiers(float damageOnTick)
        {
            this.DamageOnTick = damageOnTick;
        }

        public DoTInitialModifiers Clone()
        { return new DoTInitialModifiers(DamageOnTick); }
    }
}
