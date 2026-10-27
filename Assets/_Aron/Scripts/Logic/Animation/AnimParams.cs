// (c) Ahmed Alamrani. All rights reserved.
// Written by tools/gen_animation_list.py - do not edit; change the clip table and regenerate.
using System;

namespace Aron.Logic.Animation
{
    public enum AnimParamType { Float, Int, Bool, Trigger }

    /// <summary>One Animator parameter the hero, companions, mounts and riders are driven by. <see cref="AnimParams.All"/> lists every one the clip table uses.</summary>
    public struct AnimParam
    {
        public readonly string Name; public readonly AnimParamType Type;
        public AnimParam(string name, AnimParamType type) { Name = name; Type = type; }
    }

    public static class AnimParams
    {
        public const string MountSpeed = "MountSpeed";
        public const string SlopeAngle = "SlopeAngle";
        public const string Speed = "Speed";
        public const string TurnAngle = "TurnAngle";
        public const string VerticalSpeed = "VerticalSpeed";
        public const string Wet = "Wet";
        public const string Wind = "Wind";
        public const string MountKind = "MountKind";
        public const string VehicleKind = "VehicleKind";
        public const string Aggro = "Aggro";
        public const string Boosting = "Boosting";
        public const string Charging = "Charging";
        public const string Climbing = "Climbing";
        public const string Dead = "Dead";
        public const string FastFall = "FastFall";
        public const string Fleeing = "Fleeing";
        public const string Flying = "Flying";
        public const string Gliding = "Gliding";
        public const string Grazing = "Grazing";
        public const string Grounded = "Grounded";
        public const string HemUp = "HemUp";
        public const string Hover = "Hover";
        public const string Perched = "Perched";
        public const string Pushing = "Pushing";
        public const string Riding = "Riding";
        public const string Shopping = "Shopping";
        public const string Sitting = "Sitting";
        public const string Skidding = "Skidding";
        public const string Sliding = "Sliding";
        public const string Sprinting = "Sprinting";
        public const string Stunned = "Stunned";
        public const string Swimming = "Swimming";
        public const string Talking = "Talking";
        public const string Alert = "Alert";
        public const string Attack = "Attack";
        public const string Attack2 = "Attack2";
        public const string Attack3 = "Attack3";
        public const string Balance = "Balance";
        public const string Block = "Block";
        public const string Bonk = "Bonk";
        public const string Boost = "Boost";
        public const string Bored0 = "Bored_0";
        public const string Bored1 = "Bored_1";
        public const string Bored2 = "Bored_2";
        public const string Bored3 = "Bored_3";
        public const string Bounce = "Bounce";
        public const string Bow = "Bow";
        public const string Call = "Call";
        public const string Celebrate = "Celebrate";
        public const string CelebrateBoss = "CelebrateBoss";
        public const string Cheer = "Cheer";
        public const string Coffee = "Coffee";
        public const string Collect = "Collect";
        public const string Dance = "Dance";
        public const string Die = "Die";
        public const string DieHazard = "DieHazard";
        public const string DiePit = "DiePit";
        public const string DieWater = "DieWater";
        public const string Dismount = "Dismount";
        public const string Dive = "Dive";
        public const string DoubleJump = "DoubleJump";
        public const string Drink = "Drink";
        public const string Duck = "Duck";
        public const string Enter = "Enter";
        public const string Flourish = "Flourish";
        public const string GetUp = "GetUp";
        public const string GlideOpen = "GlideOpen";
        public const string Handshake = "Handshake";
        public const string Hurt = "Hurt";
        public const string HurtBack = "HurtBack";
        public const string HurtLeft = "HurtLeft";
        public const string HurtRight = "HurtRight";
        public const string Jump = "Jump";
        public const string Kneel = "Kneel";
        public const string Knockback = "Knockback";
        public const string Land = "Land";
        public const string LandHard = "LandHard";
        public const string Laugh = "Laugh";
        public const string LedgePop = "LedgePop";
        public const string Lose = "Lose";
        public const string Mount = "Mount";
        public const string Nudge = "Nudge";
        public const string Open = "Open";
        public const string Pause = "Pause";
        public const string PetOryx = "PetOryx";
        public const string PhaseBreak = "PhaseBreak";
        public const string Photo = "Photo";
        public const string Pickup = "Pickup";
        public const string Point = "Point";
        public const string PullUp = "PullUp";
        public const string Reach = "Reach";
        public const string Ready = "Ready";
        public const string Respawn = "Respawn";
        public const string Rise = "Rise";
        public const string Scout = "Scout";
        public const string Sit = "Sit";
        public const string Sleep = "Sleep";
        public const string SleeveLower = "SleeveLower";
        public const string SleeveRoll = "SleeveRoll";
        public const string Slide = "Slide";
        public const string Spin = "Spin";
        public const string SpinEnd = "SpinEnd";
        public const string StepUp = "StepUp";
        public const string Stomp = "Stomp";
        public const string Stumble = "Stumble";
        public const string Stun = "Stun";
        public const string TakeOff = "TakeOff";
        public const string TalkGesture = "TalkGesture";
        public const string Telegraph = "Telegraph";
        public const string Throw = "Throw";
        public const string ThrownOff = "ThrownOff";
        public const string ThumbsUp = "ThumbsUp";
        public const string Toss = "Toss";
        public const string TurnL = "TurnL";
        public const string TurnR = "TurnR";
        public const string Wave = "Wave";
        public const string Wrap = "Wrap";
        public const string fear = "fear";
        public const string scream = "scream";

        public static readonly AnimParam[] All =
        {
            new AnimParam("MountSpeed", AnimParamType.Float),
            new AnimParam("SlopeAngle", AnimParamType.Float),
            new AnimParam("Speed", AnimParamType.Float),
            new AnimParam("TurnAngle", AnimParamType.Float),
            new AnimParam("VerticalSpeed", AnimParamType.Float),
            new AnimParam("Wet", AnimParamType.Float),
            new AnimParam("Wind", AnimParamType.Float),
            new AnimParam("MountKind", AnimParamType.Int),
            new AnimParam("VehicleKind", AnimParamType.Int),
            new AnimParam("Aggro", AnimParamType.Bool),
            new AnimParam("Boosting", AnimParamType.Bool),
            new AnimParam("Charging", AnimParamType.Bool),
            new AnimParam("Climbing", AnimParamType.Bool),
            new AnimParam("Dead", AnimParamType.Bool),
            new AnimParam("FastFall", AnimParamType.Bool),
            new AnimParam("Fleeing", AnimParamType.Bool),
            new AnimParam("Flying", AnimParamType.Bool),
            new AnimParam("Gliding", AnimParamType.Bool),
            new AnimParam("Grazing", AnimParamType.Bool),
            new AnimParam("Grounded", AnimParamType.Bool),
            new AnimParam("HemUp", AnimParamType.Bool),
            new AnimParam("Hover", AnimParamType.Bool),
            new AnimParam("Perched", AnimParamType.Bool),
            new AnimParam("Pushing", AnimParamType.Bool),
            new AnimParam("Riding", AnimParamType.Bool),
            new AnimParam("Shopping", AnimParamType.Bool),
            new AnimParam("Sitting", AnimParamType.Bool),
            new AnimParam("Skidding", AnimParamType.Bool),
            new AnimParam("Sliding", AnimParamType.Bool),
            new AnimParam("Sprinting", AnimParamType.Bool),
            new AnimParam("Stunned", AnimParamType.Bool),
            new AnimParam("Swimming", AnimParamType.Bool),
            new AnimParam("Talking", AnimParamType.Bool),
            new AnimParam("Alert", AnimParamType.Trigger),
            new AnimParam("Attack", AnimParamType.Trigger),
            new AnimParam("Attack2", AnimParamType.Trigger),
            new AnimParam("Attack3", AnimParamType.Trigger),
            new AnimParam("Balance", AnimParamType.Trigger),
            new AnimParam("Block", AnimParamType.Trigger),
            new AnimParam("Bonk", AnimParamType.Trigger),
            new AnimParam("Boost", AnimParamType.Trigger),
            new AnimParam("Bored_0", AnimParamType.Trigger),
            new AnimParam("Bored_1", AnimParamType.Trigger),
            new AnimParam("Bored_2", AnimParamType.Trigger),
            new AnimParam("Bored_3", AnimParamType.Trigger),
            new AnimParam("Bounce", AnimParamType.Trigger),
            new AnimParam("Bow", AnimParamType.Trigger),
            new AnimParam("Call", AnimParamType.Trigger),
            new AnimParam("Celebrate", AnimParamType.Trigger),
            new AnimParam("CelebrateBoss", AnimParamType.Trigger),
            new AnimParam("Cheer", AnimParamType.Trigger),
            new AnimParam("Coffee", AnimParamType.Trigger),
            new AnimParam("Collect", AnimParamType.Trigger),
            new AnimParam("Dance", AnimParamType.Trigger),
            new AnimParam("Die", AnimParamType.Trigger),
            new AnimParam("DieHazard", AnimParamType.Trigger),
            new AnimParam("DiePit", AnimParamType.Trigger),
            new AnimParam("DieWater", AnimParamType.Trigger),
            new AnimParam("Dismount", AnimParamType.Trigger),
            new AnimParam("Dive", AnimParamType.Trigger),
            new AnimParam("DoubleJump", AnimParamType.Trigger),
            new AnimParam("Drink", AnimParamType.Trigger),
            new AnimParam("Duck", AnimParamType.Trigger),
            new AnimParam("Enter", AnimParamType.Trigger),
            new AnimParam("Flourish", AnimParamType.Trigger),
            new AnimParam("GetUp", AnimParamType.Trigger),
            new AnimParam("GlideOpen", AnimParamType.Trigger),
            new AnimParam("Handshake", AnimParamType.Trigger),
            new AnimParam("Hurt", AnimParamType.Trigger),
            new AnimParam("HurtBack", AnimParamType.Trigger),
            new AnimParam("HurtLeft", AnimParamType.Trigger),
            new AnimParam("HurtRight", AnimParamType.Trigger),
            new AnimParam("Jump", AnimParamType.Trigger),
            new AnimParam("Kneel", AnimParamType.Trigger),
            new AnimParam("Knockback", AnimParamType.Trigger),
            new AnimParam("Land", AnimParamType.Trigger),
            new AnimParam("LandHard", AnimParamType.Trigger),
            new AnimParam("Laugh", AnimParamType.Trigger),
            new AnimParam("LedgePop", AnimParamType.Trigger),
            new AnimParam("Lose", AnimParamType.Trigger),
            new AnimParam("Mount", AnimParamType.Trigger),
            new AnimParam("Nudge", AnimParamType.Trigger),
            new AnimParam("Open", AnimParamType.Trigger),
            new AnimParam("Pause", AnimParamType.Trigger),
            new AnimParam("PetOryx", AnimParamType.Trigger),
            new AnimParam("PhaseBreak", AnimParamType.Trigger),
            new AnimParam("Photo", AnimParamType.Trigger),
            new AnimParam("Pickup", AnimParamType.Trigger),
            new AnimParam("Point", AnimParamType.Trigger),
            new AnimParam("PullUp", AnimParamType.Trigger),
            new AnimParam("Reach", AnimParamType.Trigger),
            new AnimParam("Ready", AnimParamType.Trigger),
            new AnimParam("Respawn", AnimParamType.Trigger),
            new AnimParam("Rise", AnimParamType.Trigger),
            new AnimParam("Scout", AnimParamType.Trigger),
            new AnimParam("Sit", AnimParamType.Trigger),
            new AnimParam("Sleep", AnimParamType.Trigger),
            new AnimParam("SleeveLower", AnimParamType.Trigger),
            new AnimParam("SleeveRoll", AnimParamType.Trigger),
            new AnimParam("Slide", AnimParamType.Trigger),
            new AnimParam("Spin", AnimParamType.Trigger),
            new AnimParam("SpinEnd", AnimParamType.Trigger),
            new AnimParam("StepUp", AnimParamType.Trigger),
            new AnimParam("Stomp", AnimParamType.Trigger),
            new AnimParam("Stumble", AnimParamType.Trigger),
            new AnimParam("Stun", AnimParamType.Trigger),
            new AnimParam("TakeOff", AnimParamType.Trigger),
            new AnimParam("TalkGesture", AnimParamType.Trigger),
            new AnimParam("Telegraph", AnimParamType.Trigger),
            new AnimParam("Throw", AnimParamType.Trigger),
            new AnimParam("ThrownOff", AnimParamType.Trigger),
            new AnimParam("ThumbsUp", AnimParamType.Trigger),
            new AnimParam("Toss", AnimParamType.Trigger),
            new AnimParam("TurnL", AnimParamType.Trigger),
            new AnimParam("TurnR", AnimParamType.Trigger),
            new AnimParam("Wave", AnimParamType.Trigger),
            new AnimParam("Wrap", AnimParamType.Trigger),
            new AnimParam("fear", AnimParamType.Trigger),
            new AnimParam("scream", AnimParamType.Trigger),
        };
    }
}
