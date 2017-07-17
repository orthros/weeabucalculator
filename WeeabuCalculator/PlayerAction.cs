using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeeabuCalculator
{
    public class PlayerAction
    {
        public string Name
        { get; private set; }

        public float CastTime
        { get; private set; }

        public int DamagePotency
        { get; private set; }

        public bool GCD
        { get; private set; }

        public float GCDDelay
        { get; private set; }

        public float Cooldown
        { get; private set; }

        public string CooldownID
        { get; private set; }

        public float AnimationLock
        { get; private set; }

        public List<StatusEffect> Buffs
        { get; private set; }

        public List<StatusEffect> Debuffs
        { get; private set; }

        public List<ResourceModification> ResourceChanges
        { get; private set; }

        public PlayerDoT DoT
        { get; private set; }

        public bool IsComboInitiator
        { get; private set; }

        public ComboActionModification ComboAction
        { get; private set; }

        public bool HasComboActionProperties
        { get { return ComboAction != null; } }

        public PlayerAction(string name)
        {
            Name = name;
            CastTime = 0;
            DamagePotency = 0;
            GCD = false;
            GCDDelay = 2.5f;
            AnimationLock = 0f;
            Buffs = new List<StatusEffect>();
            Debuffs = new List<StatusEffect>();
            ResourceChanges = new List<ResourceModification>();
            DoT = null;
            ComboAction = null;
            Cooldown = 0;
            CooldownID = "";
        }

        public PlayerAction HasCastTime(float castTime)
        {
            CastTime = castTime;
            return this;
        }

        public PlayerAction DealsDamage(int potency)
        {
            DamagePotency = potency;
            return this;
        }

        public PlayerAction IsGCD(float delay = 2.5f)
        {
            GCD = true;
            GCDDelay = delay;
            return this;
        }

        public PlayerAction HasCooldown(float cooldown, string id = null)
        {
            if (id == null) id = Name;

            CooldownID = id;
            Cooldown = cooldown;    
            return this;
        }

        public PlayerAction HasAnimationLock(float delay)
        {
            AnimationLock = delay;
            return this;
        }

        public PlayerAction AppliesBuff(StatusEffect effect)
        {
            Buffs.Add(effect);
            return this;
        }

        public PlayerAction AppliesDebuff(StatusEffect effect)
        {
            Debuffs.Add(effect);
            return this;
        }

        public PlayerAction AppliesDoT(PlayerDoT dot)
        {
            DoT = dot;
            return this;
        }

        public PlayerAction AddsResource(string resourceName, int amount)
        {
            ResourceChanges.Add(new ResourceModification(resourceName, amount));
            return this;
        }

        public PlayerAction ConsumesResource(string resourceName, int amount)
        {
            ResourceChanges.Add(new ResourceModification(resourceName, -1 * amount));
            return this;
        }

        public PlayerAction IsComboAction(string previousActionName, PlayerAction actionModifier)
        {
            ComboAction = new ComboActionModification(previousActionName, actionModifier);
            return this;
        }

        public PlayerAction InitiatesCombo()
        {
            IsComboInitiator = true;
            return this;
        }

        public override string ToString()
        {
            return Name;
        }

        public class ComboActionModification
        {
            public string PreviousAction
            { get; private set; }

            public PlayerAction ActionModifier
            { get; private set; }

            public ComboActionModification(string comboAction, PlayerAction ActionModifier)
            {
                this.PreviousAction = comboAction;
                this.ActionModifier = ActionModifier;
            }
        }

        public class ResourceModification
        {
            public string ResourceName
            { get; private set; }

            public int Change
            { get; private set; }

            public ResourceModification(string resourceName, int change)
            {
                ResourceName = resourceName;
                Change = change;
            }
        }
    }
}
