#!/usr/bin/env python3
"""Builds docs/animation_list.md and Assets/_Aron/Resources/Config/animation_clips.json from one table.

Run from the repo root:  python3 tools/gen_animation_list.py
The table is the single source: edit it here, regenerate both files (a test checks they agree with each other and with PlayerAnimationHooks).
Frames are at 30 fps. Priority P0 = the vertical slice (the first Riyadh stage with Aron, Noura, Dr. Duhaim, the falcon, the oryx and the camel).
"""
import json, os, sys

M, A, C, H = "motion capture library", "motion capture reference", "keyframed", "hand-animated"
L, O = True, False   # loop, one-shot

rows = []   # (actor, group, name, loop, frames, pri, src, kind, param, value, note)


def add(actor, group, items):
    for it in items:
        name, loop, frames, pri, src, kind, param, value = it[:8]
        note = it[8] if len(it) > 8 else ""
        rows.append((actor, group, name, loop, frames, pri, src, kind, param, value, note))


# ------------------------------------------------------------------ Aron
add("aron", "locomotion", [
    ("idle", L, 120, "P0", M, "float", "Speed", "<0.05", "weight shift, breathing is added procedurally"),
    ("idle_look_around", O, 90, "P1", M, "trigger", "Bored_0", "", "bored idle 0"),
    ("idle_adjust_shemagh", O, 96, "P1", H, "trigger", "Bored_1", "", "bored idle 1, signature"),
    ("idle_check_agal", O, 84, "P1", H, "trigger", "Bored_2", "", "bored idle 2, signature"),
    ("idle_stretch", O, 108, "P2", M, "trigger", "Bored_3", "", "bored idle 3"),
    ("walk", L, 32, "P0", M, "float", "Speed", "0.05..0.45"),
    ("jog", L, 26, "P0", M, "float", "Speed", "0.45..0.8"),
    ("run", L, 22, "P0", M, "float", "Speed", "0.8..1.0"),
    ("sprint", L, 18, "P0", M, "bool", "Sprinting", "true"),
    ("sprint_hem_bite", L, 18, "P1", H, "bool", "HemUp", "true", "thobe hem lifted and bitten, see ThobeSkirt; signature"),
    ("start_run", O, 14, "P1", M, "float", "Speed", "rising from idle"),
    ("run_to_idle", O, 16, "P1", M, "float", "Speed", "falling to idle"),
    ("turn_in_place_left_90", O, 14, "P1", M, "trigger", "TurnL", ""),
    ("turn_in_place_right_90", O, 14, "P1", M, "trigger", "TurnR", ""),
    ("turn_in_place_180", O, 20, "P1", M, "trigger", "TurnR", "angle > 150"),
    ("skid_stop", O, 18, "P0", H, "bool", "Skidding", "true", "dust and a back lean, signature"),
    ("skid_turn", O, 16, "P1", H, "bool", "Skidding", "true", "reversal at speed"),
    ("run_up_slope", L, 22, "P1", M, "float", "SlopeAngle", ">8"),
    ("run_down_slope", L, 22, "P1", M, "float", "SlopeAngle", "<-8"),
    ("step_up", O, 12, "P1", M, "trigger", "StepUp", ""),
    ("run_in_wind", L, 22, "P2", M, "float", "Wind", ">0.6", "leaning into a sandstorm"),
    ("run_wet_slip", O, 20, "P2", H, "float", "Wet", ">0.7", "traction below 0.6"),
])
add("aron", "air", [
    ("jump_start", O, 8, "P0", M, "trigger", "Jump", ""),
    ("jump_rise", L, 10, "P0", M, "float", "VerticalSpeed", ">2"),
    ("jump_apex", L, 12, "P0", H, "float", "VerticalSpeed", "-2..2", "the hang"),
    ("fall", L, 10, "P0", M, "float", "VerticalSpeed", "<-2"),
    ("fast_fall", L, 8, "P1", H, "bool", "FastFall", "true"),
    ("land_soft", O, 10, "P0", M, "trigger", "Land", ""),
    ("land_hard", O, 22, "P0", H, "trigger", "LandHard", "the land-lag stumble"),
    ("land_run", O, 12, "P1", M, "trigger", "Land", "Speed > 0.6"),
    ("double_jump", O, 16, "P0", H, "trigger", "DoubleJump", "flip"),
    ("air_spin", L, 12, "P0", H, "trigger", "Spin", "spin attack in the air; the body is turned procedurally"),
    ("air_spin_end", O, 10, "P1", H, "trigger", "SpinEnd", "recovery frames"),
    ("crate_bounce", O, 14, "P0", H, "trigger", "Bounce", ""),
    ("stomp", O, 12, "P1", H, "trigger", "Stomp", ""),
    ("ledge_pop", O, 12, "P2", M, "trigger", "LedgePop", "ledge forgiveness lift"),
    ("head_bonk", O, 14, "P2", H, "trigger", "Bonk", ""),
])
add("aron", "combat", [
    ("agal_swing", O, 24, "P0", H, "trigger", "Attack", "5 startup / 10 active / 9 recovery at 60 Hz, signature"),
    ("agal_swing_2", O, 24, "P1", H, "trigger", "Attack2", "buffered second swing"),
    ("agal_swing_run", O, 22, "P1", H, "trigger", "Attack", "Speed > 0.5"),
    ("slide_start", O, 8, "P0", H, "trigger", "Slide", ""),
    ("slide_loop", L, 12, "P0", H, "bool", "Sliding", "true"),
    ("slide_end", O, 12, "P1", H, "bool", "Sliding", "false"),
    ("hurt_front", O, 14, "P0", M, "trigger", "Hurt", "additive flinch is added by direction"),
    ("hurt_back", O, 14, "P1", M, "trigger", "HurtBack", ""),
    ("hurt_left", O, 14, "P2", M, "trigger", "HurtLeft", ""),
    ("hurt_right", O, 14, "P2", M, "trigger", "HurtRight", ""),
    ("knockback", O, 24, "P1", M, "trigger", "Knockback", ""),
    ("get_up", O, 30, "P1", M, "trigger", "GetUp", ""),
    ("shield_block", O, 14, "P2", H, "trigger", "Block", "camel shield"),
])
add("aron", "sleeves and costume", [
    ("sleeve_roll", O, 18, "P0", H, "trigger", "SleeveRoll", "rolls to the forearm before a fight; they stay rolled"),
    ("sleeve_lower", O, 18, "P1", H, "trigger", "SleeveLower", "after 6 s of calm"),
    ("talathum_wrap", O, 24, "P1", H, "trigger", "Wrap", "face wrap, only after the sleeves are up"),
    ("shemagh_toss", O, 20, "P2", H, "trigger", "Toss", ""),
])
add("aron", "death", [
    ("die_enemy_spin", O, 36, "P0", H, "trigger", "Die", "spin, topple, stars, agal rolls"),
    ("die_pit_wave", O, 40, "P0", H, "trigger", "DiePit", "shemagh up, wave, drop"),
    ("die_water_sink", O, 48, "P1", H, "trigger", "DieWater", "bubbles, sink, shemagh floats"),
    ("die_hazard", O, 36, "P2", H, "trigger", "DieHazard", ""),
    ("respawn_pop", O, 20, "P1", H, "trigger", "Respawn", "at the last lantern"),
])
add("aron", "interaction", [
    ("collect_date", O, 12, "P0", M, "trigger", "Collect", ""),
    ("pick_up", O, 24, "P1", M, "trigger", "Pickup", ""),
    ("open_chest", O, 36, "P1", M, "trigger", "Open", ""),
    ("push_crate", L, 24, "P2", M, "bool", "Pushing", "true"),
    ("climb_ladder", L, 24, "P2", M, "bool", "Climbing", "true"),
    ("swim_idle", L, 48, "P2", M, "bool", "Swimming", "true"),
    ("swim", L, 28, "P2", M, "float", "Speed", "while Swimming"),
    ("talk_idle", L, 96, "P1", M, "bool", "Talking", "true"),
    ("talk_gesture", O, 36, "P1", M, "trigger", "TalkGesture", ""),
    ("shop_browse", L, 72, "P2", M, "bool", "Shopping", "true"),
    ("photo_pose", O, 60, "P2", H, "trigger", "Photo", "passport photo"),
    ("scared", O, 30, "P1", H, "trigger", "fear", "existing trigger"),
    ("scream", O, 30, "P2", H, "trigger", "scream", "existing trigger"),
])
add("aron", "emotes and celebration", [
    ("wave", O, 48, "P0", M, "trigger", "Wave", ""),
    ("celebrate_stage_clear", O, 72, "P0", H, "trigger", "Celebrate", "signature"),
    ("celebrate_boss_win", O, 90, "P0", H, "trigger", "CelebrateBoss", "signature"),
    ("cheer", O, 40, "P1", M, "trigger", "Cheer", ""),
    ("laugh", O, 48, "P1", M, "trigger", "Laugh", ""),
    ("thumbs_up", O, 24, "P2", M, "trigger", "ThumbsUp", ""),
    ("bow_respect", O, 40, "P1", H, "trigger", "Bow", "hand on heart, greeting"),
    ("dance_ardah", L, 96, "P2", A, "trigger", "Dance", "ardah sword dance, culture review needed"),
    ("dance_khaleeji", L, 96, "P2", A, "trigger", "Dance", "khaleeji, culture review needed"),
    ("pray_pause", O, 90, "P3", M, "trigger", "Pause", "only if the culture review wants it"),
    ("coffee_pour", O, 90, "P2", H, "trigger", "Coffee", "Arabic coffee dallah, hospitality"),
    ("shake_hands", O, 40, "P2", M, "trigger", "Handshake", ""),
])
add("aron", "country moves", [
    ("trail_flourish", O, 28, "P2", H, "trigger", "Flourish", "country attack trail"),
    ("tourist_pose", O, 40, "P3", H, "trigger", "Photo", ""),
    ("fall_asleep", L, 96, "P3", H, "trigger", "Sleep", ""),
])

# ------------------------------------------------------------------ Noura (always modest: the abaya covers; no sleeve roll)
add("noura", "locomotion", [
    ("idle", L, 120, "P0", M, "float", "Speed", "<0.05"),
    ("idle_bored_adjust_abaya", O, 96, "P1", H, "trigger", "Bored_0", "signature"),
    ("idle_bored_check_bag", O, 84, "P1", H, "trigger", "Bored_1", ""),
    ("idle_bored_look_at_oryx", O, 72, "P1", H, "trigger", "Bored_2", ""),
    ("walk", L, 32, "P0", M, "float", "Speed", "0.05..0.45"),
    ("jog", L, 26, "P0", M, "float", "Speed", "0.45..0.8"),
    ("run", L, 22, "P0", M, "float", "Speed", "0.8..1.0"),
    ("sprint", L, 18, "P0", M, "bool", "Sprinting", "true", "the abaya hem is held down by the cloth chain"),
    ("start_run", O, 14, "P1", M, "float", "Speed", "rising"),
    ("run_to_idle", O, 16, "P1", M, "float", "Speed", "falling"),
    ("turn_in_place_left_90", O, 14, "P1", M, "trigger", "TurnL", ""),
    ("turn_in_place_right_90", O, 14, "P1", M, "trigger", "TurnR", ""),
    ("skid_stop", O, 18, "P0", H, "bool", "Skidding", "true"),
    ("run_up_slope", L, 22, "P1", M, "float", "SlopeAngle", ">8"),
    ("run_down_slope", L, 22, "P1", M, "float", "SlopeAngle", "<-8"),
    ("run_in_wind", L, 22, "P2", M, "float", "Wind", ">0.6", "abaya streams behind, stays modest"),
])
add("noura", "air", [
    ("jump_start", O, 8, "P0", M, "trigger", "Jump", ""),
    ("jump_rise", L, 10, "P0", M, "float", "VerticalSpeed", ">2"),
    ("jump_apex", L, 12, "P0", H, "float", "VerticalSpeed", "-2..2"),
    ("fall", L, 10, "P0", M, "float", "VerticalSpeed", "<-2"),
    ("land_soft", O, 10, "P0", M, "trigger", "Land", ""),
    ("land_hard", O, 22, "P1", H, "trigger", "LandHard", ""),
    ("double_jump", O, 16, "P0", H, "trigger", "DoubleJump", "second jump opens the abaya"),
    ("glide_open", O, 12, "P0", H, "trigger", "GlideOpen", "abaya opens like a wing, hands stay covered"),
    ("glide_loop", L, 24, "P0", H, "bool", "Gliding", "true", "signature"),
    ("glide_close", O, 12, "P1", H, "bool", "Gliding", "false"),
    ("air_spin", L, 12, "P0", H, "trigger", "Spin", ""),
    ("crate_bounce", O, 14, "P1", H, "trigger", "Bounce", ""),
])
add("noura", "combat", [
    ("handbag_swing", O, 24, "P0", H, "trigger", "Attack", "same frame data as the agal swing, signature"),
    ("handbag_swing_2", O, 24, "P1", H, "trigger", "Attack2", ""),
    ("slide_start", O, 8, "P0", H, "trigger", "Slide", ""),
    ("slide_loop", L, 12, "P0", H, "bool", "Sliding", "true"),
    ("slide_end", O, 12, "P1", H, "bool", "Sliding", "false"),
    ("hurt_front", O, 14, "P0", M, "trigger", "Hurt", ""),
    ("hurt_back", O, 14, "P1", M, "trigger", "HurtBack", ""),
    ("knockback", O, 24, "P1", M, "trigger", "Knockback", ""),
    ("get_up", O, 30, "P1", M, "trigger", "GetUp", ""),
])
add("noura", "death", [
    ("die_enemy_sit", O, 48, "P0", H, "trigger", "Die", "abaya swirl, sit down, stars, bag beside, uff, oryx nudge"),
    ("die_pit", O, 40, "P1", H, "trigger", "DiePit", ""),
    ("die_water", O, 48, "P2", H, "trigger", "DieWater", ""),
    ("respawn_pop", O, 20, "P1", H, "trigger", "Respawn", ""),
])
add("noura", "interaction", [
    ("collect_date", O, 12, "P0", M, "trigger", "Collect", ""),
    ("pick_up", O, 24, "P1", M, "trigger", "Pickup", ""),
    ("open_chest", O, 36, "P2", M, "trigger", "Open", ""),
    ("talk_idle", L, 96, "P1", M, "bool", "Talking", "true"),
    ("talk_gesture", O, 36, "P1", M, "trigger", "TalkGesture", ""),
    ("shop_browse", L, 72, "P2", M, "bool", "Shopping", "true"),
    ("photo_pose", O, 60, "P2", H, "trigger", "Photo", ""),
    ("scared", O, 30, "P1", H, "trigger", "fear", ""),
    ("swim", L, 28, "P3", M, "bool", "Swimming", "true"),
])
add("noura", "emotes and celebration", [
    ("wave", O, 48, "P0", M, "trigger", "Wave", ""),
    ("celebrate_stage_clear", O, 72, "P0", H, "trigger", "Celebrate", ""),
    ("cheer", O, 40, "P1", M, "trigger", "Cheer", ""),
    ("laugh", O, 48, "P1", M, "trigger", "Laugh", ""),
    ("bow_respect", O, 40, "P1", H, "trigger", "Bow", ""),
    ("dance_khaleeji", L, 96, "P2", A, "trigger", "Dance", "culture review needed"),
    ("coffee_pour", O, 90, "P2", H, "trigger", "Coffee", ""),
    ("pet_oryx", O, 60, "P1", H, "trigger", "PetOryx", ""),
])

# ------------------------------------------------------------------ Dr. Duhaim (Riyadh boss)
add("duhaim", "locomotion and combat", [
    ("idle", L, 120, "P0", M, "bool", "Aggro", "false"),
    ("idle_gloat", O, 90, "P1", H, "trigger", "Bored_0", ""),
    ("stalk_walk", L, 30, "P0", M, "float", "Speed", "0.05..0.6"),
    ("stalk_run", L, 22, "P0", M, "float", "Speed", ">0.6"),
    ("turn_in_place", O, 18, "P1", M, "trigger", "TurnR", ""),
    ("telegraph_windup", O, 30, "P0", H, "trigger", "Telegraph", "readable wind-up, 0.5 s"),
    ("attack_swipe", O, 28, "P0", H, "trigger", "Attack", ""),
    ("attack_slam", O, 40, "P0", H, "trigger", "Attack2", "ground slam, shockwave"),
    ("attack_charge", L, 20, "P1", H, "bool", "Charging", "true"),
    ("attack_throw", O, 36, "P1", H, "trigger", "Throw", ""),
    ("attack_combo_end", O, 24, "P2", H, "trigger", "Attack3", ""),
    ("stunned_loop", L, 48, "P0", H, "bool", "Stunned", "true"),
    ("stun_enter", O, 20, "P0", H, "trigger", "Stun", ""),
    ("stun_recover", O, 20, "P1", H, "bool", "Stunned", "false"),
    ("hurt_hit", O, 14, "P0", M, "trigger", "Hurt", ""),
    ("phase_break_roar", O, 48, "P0", H, "trigger", "PhaseBreak", "hit-stop and a roar"),
    ("defeat_fall", O, 72, "P0", H, "trigger", "Die", "slapstick, nothing gory (7+)"),
    ("defeat_sit_dazed", L, 72, "P1", H, "bool", "Dead", "true"),
    ("taunt_laugh", O, 48, "P2", H, "trigger", "Laugh", ""),
    ("point_at_player", O, 36, "P2", M, "trigger", "Point", ""),
    ("enter_arena", O, 60, "P1", H, "trigger", "Enter", ""),
    ("surrender_bow", O, 60, "P2", H, "trigger", "Bow", "after the fight, good sport"),
])

# ------------------------------------------------------------------ Falcon companion
add("falcon", "flight and perch", [
    ("perch_idle", L, 90, "P0", H, "bool", "Perched", "true"),
    ("perch_preen", O, 60, "P1", H, "trigger", "Bored_0", ""),
    ("perch_look", O, 48, "P1", H, "trigger", "Bored_1", ""),
    ("take_off", O, 16, "P0", H, "trigger", "TakeOff", ""),
    ("fly_flap", L, 16, "P0", H, "bool", "Flying", "true"),
    ("fly_glide", L, 36, "P1", H, "bool", "Gliding", "true"),
    ("fly_bank_left", L, 24, "P1", H, "float", "TurnAngle", "<-0.3"),
    ("fly_bank_right", L, 24, "P1", H, "float", "TurnAngle", ">0.3"),
    ("hover", L, 20, "P1", H, "bool", "Hover", "true"),
    ("dive", O, 18, "P0", H, "trigger", "Dive", "attack dive"),
    ("dive_pullup", O, 16, "P1", H, "trigger", "PullUp", ""),
    ("land_on_arm", O, 20, "P0", H, "trigger", "Land", ""),
    ("land_on_perch", O, 18, "P1", H, "trigger", "Land", ""),
    ("fetch_grab", O, 20, "P1", H, "trigger", "Collect", "picks up a date"),
    ("scout_look", O, 30, "P1", H, "trigger", "Scout", "points out hidden things"),
    ("screech", O, 24, "P1", H, "trigger", "Call", ""),
    ("hurt", O, 14, "P2", H, "trigger", "Hurt", ""),
    ("celebrate_loop", O, 30, "P2", H, "trigger", "Celebrate", ""),
])

# ------------------------------------------------------------------ Oryx companion
add("oryx", "companion", [
    ("idle", L, 120, "P0", H, "float", "Speed", "<0.05"),
    ("graze", L, 90, "P1", H, "trigger", "Bored_0", ""),
    ("look_up", O, 48, "P1", H, "trigger", "Bored_1", ""),
    ("walk", L, 30, "P0", H, "float", "Speed", "0.05..0.5"),
    ("trot", L, 22, "P0", H, "float", "Speed", "0.5..0.85"),
    ("gallop", L, 16, "P1", H, "float", "Speed", ">0.85"),
    ("jump", O, 20, "P1", H, "trigger", "Jump", ""),
    ("land", O, 12, "P1", H, "trigger", "Land", ""),
    ("nudge_noura", O, 36, "P0", H, "trigger", "Nudge", "after Noura's fall"),
    ("hurt", O, 14, "P2", H, "trigger", "Hurt", ""),
    ("shield_horns", O, 30, "P2", H, "trigger", "Block", "ability"),
    ("charge", L, 16, "P2", H, "bool", "Charging", "true"),
    ("sit_down", O, 40, "P2", H, "trigger", "Sit", ""),
    ("happy_hop", O, 24, "P1", H, "trigger", "Celebrate", ""),
])

# ------------------------------------------------------------------ Camel race (arcade, cartoon style): the racing camel, its robot jockey, the wild oryx
add("camel_racer", "race", [
    ("kneel", O, 30, "P1", H, "trigger", "Kneel", "kneels at the gate so the rider can climb on"),
    ("rise", O, 36, "P1", H, "trigger", "Rise", "gets up with a wobble, ready to run"),
    ("gate_idle", L, 96, "P1", H, "float", "MountSpeed", "<0.05", "snorts and stamps, ears twitching"),
    ("ready_gate", L, 60, "P2", H, "trigger", "Ready", "bounces on the spot before the start"),
    ("gallop", L, 18, "P1", H, "float", "MountSpeed", "0.5..0.85", "the long-legged racing gait, exaggerated"),
    ("gallop_full", L, 14, "P1", H, "float", "MountSpeed", ">0.85", "stretched out, ears flat, speed lines on"),
    ("jump_dune", O, 24, "P1", H, "trigger", "Jump", "leaps a dune ramp, legs trailing"),
    ("land_bounce", O, 14, "P1", H, "trigger", "Land", "squash on landing, then springs back"),
    ("stumble", O, 30, "P1", H, "trigger", "Stumble", "comic wobble and trip, nobody is hurt, keeps running"),
    ("boost", O, 16, "P1", H, "trigger", "Boost", "head down, a burst off a boost pad"),
    ("boost_loop", L, 12, "P1", H, "bool", "Boosting", "true", "while the boost lasts"),
    ("finish_celebrate", O, 48, "P1", H, "trigger", "Celebrate", "hops and spins past the line"),
    ("lose_slump", O, 40, "P2", H, "trigger", "Lose", "droops its ears, good sport"),
])
add("robot_jockey", "race", [
    ("idle_gate", L, 60, "P1", H, "float", "MountSpeed", "<0.05", "a small robot; every rival camel has one, never a child"),
    ("ride_gallop", L, 18, "P1", H, "float", "MountSpeed", ">0.5", "bobs on the saddle, antenna swaying"),
    ("lean_boost", L, 12, "P1", H, "bool", "Boosting", "true", "flattens along the neck"),
    ("wobble", O, 30, "P1", H, "trigger", "Stumble", "spins its antenna, holds on"),
    ("cheer", O, 40, "P1", H, "trigger", "Celebrate", "arms up, lights flash"),
    ("sad_beep", O, 30, "P2", H, "trigger", "Lose", "lights dim, a sad beep"),
])
add("rider", "camel race", [
    ("camel_race_crouch", L, 18, "P1", H, "bool", "Boosting", "true", "Aron on his camel, low on the neck while MountKind 2"),
    ("camel_race_cheer", O, 48, "P1", H, "trigger", "Celebrate", "arms up on the camel after the line"),
])
add("noura", "race", [
    ("camel_race_sideline_cheer", O, 60, "P1", H, "trigger", "Celebrate", "cheers beside the track, fully covered, abaya in the wind"),
])
add("oryx_wild", "wildlife", [
    ("idle_graze", L, 90, "P1", H, "bool", "Grazing", "true", "a wild Arabian oryx grazing, peaceful"),
    ("walk", L, 30, "P1", H, "float", "Speed", "0.05..0.5"),
    ("trot", L, 22, "P1", H, "float", "Speed", "0.5..0.85"),
    ("flee", L, 18, "P1", H, "bool", "Fleeing", "true", "a gentle trot away from the player, never a panic"),
    ("alert", O, 36, "P1", H, "trigger", "Alert", "lifts its head and ears when someone comes near"),
])

# ------------------------------------------------------------------ Mounts
def mount(actor, extra, p0=False):
    base = [
        ("idle", L, 120, "P0" if p0 else "P1", H, "float", "MountSpeed", "<0.05"),
        ("walk", L, 30, "P0" if p0 else "P1", H, "float", "MountSpeed", "0.05..0.5"),
        ("run", L, 20, "P0" if p0 else "P1", H, "float", "MountSpeed", ">0.5"),
        ("jump", O, 20, "P0" if p0 else "P2", H, "trigger", "Jump", ""),
        ("land", O, 12, "P0" if p0 else "P2", H, "trigger", "Land", ""),
        ("hurt_buck", O, 24, "P0" if p0 else "P2", H, "trigger", "Hurt", "throws the rider off"),
        ("kneel_mount", O, 30, "P0" if p0 else "P2", H, "trigger", "Mount", ""),
    ]
    add(actor, "mount", base + extra)

mount("camel", [
    ("sprint", L, 16, "P0", H, "bool", "Sprinting", "true", "camel sprint ability"),
    ("shield_stance", O, 24, "P1", H, "trigger", "Block", "shield ability"),
    ("spit_blow", O, 28, "P3", H, "trigger", "Attack", "comic, only if kind to the audience"),
    ("sit_rest", L, 96, "P2", H, "bool", "Sitting", "true"),
    ("drink", O, 72, "P2", H, "trigger", "Drink", ""),
], p0=True)
mount("horse", [
    ("charge_gallop", L, 14, "P1", H, "bool", "Charging", "true", "horse charge"),
    ("rear_up", O, 36, "P1", H, "trigger", "Attack", ""),
    ("whinny", O, 36, "P2", H, "trigger", "Call", ""),
    ("trot", L, 24, "P2", H, "float", "MountSpeed", "0.3..0.6"),
])
mount("ostrich", [
    ("sprint", L, 14, "P2", H, "bool", "Sprinting", "true"),
    ("flap_balance", O, 24, "P3", H, "trigger", "Balance", ""),
])
mount("giraffe", [
    ("duck_under", O, 24, "P3", H, "trigger", "Duck", "low gates"),
    ("neck_stretch", O, 36, "P3", H, "trigger", "Reach", ""),
])
add("falcon_glider", "mount", [
    ("glide_idle", L, 48, "P2", H, "float", "MountSpeed", "<0.05"),
    ("glide_forward", L, 30, "P2", H, "float", "MountSpeed", ">0.05"),
    ("glide_bank_left", L, 24, "P3", H, "float", "TurnAngle", "<-0.3"),
    ("glide_bank_right", L, 24, "P3", H, "float", "TurnAngle", ">0.3"),
    ("glide_dive", O, 18, "P3", H, "trigger", "Dive", ""),
    ("glide_land", O, 20, "P3", H, "trigger", "Land", ""),
])

# ------------------------------------------------------------------ Riders on mounts and vehicles (Aron and Noura share the clips; the rig retargets)
add("rider", "on mounts", [
    ("mount_up", O, 30, "P0", H, "trigger", "Mount", "camel kneels, hero climbs on"),
    ("dismount", O, 24, "P0", H, "trigger", "Dismount", ""),
    ("thrown_off", O, 36, "P0", H, "trigger", "ThrownOff", "no life lost"),
    ("camel_ride_idle", L, 96, "P0", M, "int", "MountKind", "2"),
    ("camel_ride_walk", L, 30, "P0", M, "int", "MountKind", "2 and MountSpeed 0.05..0.5"),
    ("camel_ride_run", L, 20, "P0", M, "int", "MountKind", "2 and MountSpeed > 0.5"),
    ("camel_ride_jump", O, 20, "P1", M, "trigger", "Jump", "while MountKind 2"),
    ("horse_ride_idle", L, 96, "P1", M, "int", "MountKind", "1"),
    ("horse_ride_gallop", L, 16, "P1", M, "int", "MountKind", "1 and MountSpeed > 0.5"),
    ("horse_ride_jump", O, 20, "P1", M, "trigger", "Jump", "while MountKind 1"),
    ("horse_ride_charge", L, 14, "P2", H, "bool", "Charging", "true"),
    ("ostrich_ride", L, 14, "P3", A, "int", "MountKind", "3"),
    ("giraffe_ride", L, 30, "P3", A, "int", "MountKind", "4"),
    ("falcon_glide_ride", L, 48, "P3", H, "int", "MountKind", "5", "lies forward, arms out"),
    ("rider_attack_swing", O, 24, "P1", H, "trigger", "Attack", "from the saddle"),
    ("rider_hurt", O, 14, "P1", M, "trigger", "Hurt", ""),
])
add("rider", "on vehicles", [
    ("car_drive_idle", L, 60, "P2", M, "int", "VehicleKind", "1"),
    ("car_steer_left", L, 24, "P2", M, "float", "TurnAngle", "<-0.3 and VehicleKind 1"),
    ("car_steer_right", L, 24, "P2", M, "float", "TurnAngle", ">0.3 and VehicleKind 1"),
    ("car_boost_brace", O, 20, "P3", H, "trigger", "Boost", ""),
    ("bicycle_pedal", L, 24, "P2", M, "int", "VehicleKind", "2"),
    ("bicycle_coast", L, 36, "P3", M, "int", "VehicleKind", "2 and MountSpeed < 0.2"),
    ("bicycle_lean_left", L, 24, "P3", M, "float", "TurnAngle", "<-0.3 and VehicleKind 2"),
    ("bicycle_lean_right", L, 24, "P3", M, "float", "TurnAngle", ">0.3 and VehicleKind 2"),
    ("motorcycle_ride", L, 36, "P2", M, "int", "VehicleKind", "3"),
    ("motorcycle_lean_left", L, 24, "P3", M, "float", "TurnAngle", "<-0.3 and VehicleKind 3"),
    ("motorcycle_lean_right", L, 24, "P3", M, "float", "TurnAngle", ">0.3 and VehicleKind 3"),
    ("boat_row", L, 36, "P2", M, "int", "VehicleKind", "4"),
    ("boat_steer_left", L, 24, "P3", M, "float", "TurnAngle", "<-0.3 and VehicleKind 4"),
    ("boat_steer_right", L, 24, "P3", M, "float", "TurnAngle", ">0.3 and VehicleKind 4"),
    ("sail_trim", L, 48, "P3", M, "int", "VehicleKind", "5"),
    ("ski_carve", L, 30, "P3", M, "int", "VehicleKind", "6"),
    ("surf_ride", L, 36, "P3", M, "int", "VehicleKind", "7"),
    ("sky_hang_glide", L, 48, "P3", M, "int", "VehicleKind", "8"),
    ("vehicle_crash_tumble", O, 48, "P2", H, "trigger", "Hurt", "comic spill, nobody hurt"),
    ("vehicle_win_cheer", O, 48, "P2", H, "trigger", "Celebrate", "race podium"),
    ("vehicle_lose_slump", O, 40, "P2", H, "trigger", "Lose", "good sport"),
    ("race_start_ready", L, 60, "P2", H, "trigger", "Ready", ""),
    ("race_podium_wave", O, 60, "P2", H, "trigger", "Wave", ""),
])


def build():
    out = []
    for r in rows:
        actor, group, name, loop, frames, pri, src, kind, param, value, note = r
        cid = actor + "_" + name
        out.append({"id": cid, "actor": actor, "group": group, "loop": bool(loop), "frames": frames, "priority": pri,
                    "source": src, "kind": kind, "param": param, "value": value, "note": note})
    return out


EXTRA = {"Grounded": "bool", "Riding": "bool"}   # parameters the controller drives that no clip is named after


def write_params(root, clips):
    params = dict(EXTRA)
    for c in clips:
        if params.setdefault(c["param"], c["kind"]) != c["kind"]:
            sys.exit("parameter %s is used as %s and %s" % (c["param"], params[c["param"]], c["kind"]))
    order = {"float": 0, "int": 1, "bool": 2, "trigger": 3}
    lines = ["// (c) Ahmed Alamrani. All rights reserved.", "// Written by tools/gen_animation_list.py - do not edit; change the clip table and regenerate.",
             "using System;", "", "namespace Aron.Logic.Animation", "{",
             "    public enum AnimParamType { Float, Int, Bool, Trigger }", "",
             "    /// <summary>One Animator parameter the hero, companions, mounts and riders are driven by. <see cref=\"AnimParams.All\"/> lists every one the clip table uses.</summary>",
             "    public struct AnimParam",
             "    {",
             "        public readonly string Name; public readonly AnimParamType Type;",
             "        public AnimParam(string name, AnimParamType type) { Name = name; Type = type; }",
             "    }", "",
             "    public static class AnimParams", "    {"]
    for n, k in sorted(params.items(), key=lambda kv: (order[kv[1]], kv[0])):
        lines.append('        public const string %s = "%s";' % (n.replace("_", "") if n[0].isalpha() else n, n))
    lines += ["", "        public static readonly AnimParam[] All =", "        {"]
    for n, k in sorted(params.items(), key=lambda kv: (order[kv[1]], kv[0])):
        lines.append('            new AnimParam("%s", AnimParamType.%s),' % (n, k.capitalize()))
    lines += ["        };", "    }", "}", ""]
    path = os.path.join(root, "Assets/_Aron/Scripts/Logic/Animation/AnimParams.cs")
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))


def main():
    root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    clips = build()
    ids = [c["id"] for c in clips]
    dup = {i for i in ids if ids.count(i) > 1}
    if dup:
        sys.exit("duplicate clip ids: " + ", ".join(sorted(dup)))
    path = os.path.join(root, "Assets/_Aron/Resources/Config/animation_clips.json")
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"fps": 30, "clips": clips}, f, ensure_ascii=False, indent=1)
        f.write("\n")
    p0 = [c for c in clips if c["priority"] == "P0"]
    write_params(root, clips)
    by = {}
    for c in clips:
        by.setdefault(c["actor"], []).append(c)
    names = {"aron": "Aron", "noura": "Noura", "duhaim": "Dr. Duhaim", "falcon": "The falcon", "oryx": "The oryx", "camel": "Camel (mount)",
             "horse": "Horse (mount)", "ostrich": "Ostrich (mount)", "giraffe": "Giraffe (mount)", "falcon_glider": "Falcon glider (mount)",
             "camel_racer": "Racing camel (camel race)", "robot_jockey": "Robot jockey (camel race)", "oryx_wild": "Wild Arabian oryx",
             "rider": "Riders on mounts and vehicles (Aron and Noura)"}
    md = []
    md.append("# Animation list\n")
    md.append("The clips the game needs, per character. **%d clips**, **%d of them P0** (the vertical slice). Frames are at 30 fps. Built by `tools/gen_animation_list.py` from one table, which also writes `Resources/Config/animation_clips.json`; edit the table, not this file.\n" % (len(clips), len(p0)))
    md.append("## How to read it\n")
    md.append("- **Loop / one-shot**: a loop must tile with no pop at the seam; a one-shot starts and ends on the idle pose (or the state it blends to).")
    md.append("- **Priority**: **P0** is the vertical slice (the first Riyadh stage: Aron, Noura, the camel, the falcon, the oryx and Dr. Duhaim). **P1** ships at launch. **P2** is polish. **P3** is later content (other mounts and vehicles).")
    md.append("- **Source**: `motion capture library` (a licensed pack, then cleaned), `motion capture reference` (shoot reference, solve, then clean by hand), `keyframed` (physics-assisted keys for air, falls and impacts), `hand-animated` (signature moves, animals and anything cartoonish where motion capture would look wrong).")
    md.append("- **Hook**: the Animator parameter that plays it (`PlayerAnimationHooks`, listed in `docs/game_feel.md`). The Animator state is named after the clip id.")
    md.append("- The **procedural layer** (lean, look-at, breathing, squash and stretch, foot IK, cloth, additive flinch) is *not* baked into these clips: it runs on top of every one of them, so the clips stay plain.")
    md.append("- **Modesty and age rating (7+)**: Noura is always fully covered (abaya, no sleeve roll, hands stay covered when she glides); defeat acts are slapstick, nothing gory; dances are marked for the culture review before they ship.\n")
    md.append("## Totals\n")
    md.append("| Character | Clips | P0 | P1 | P2 | P3 |")
    md.append("|---|---:|---:|---:|---:|---:|")
    for a in names:
        cs = by.get(a, [])
        md.append("| %s | %d | %d | %d | %d | %d |" % (names[a], len(cs), *[sum(1 for c in cs if c["priority"] == p) for p in ("P0", "P1", "P2", "P3")]))
    md.append("| **All** | **%d** | **%d** | **%d** | **%d** | **%d** |\n" % (len(clips), len(p0), *[sum(1 for c in clips if c["priority"] == p) for p in ("P1", "P2", "P3")]))
    for a in names:
        cs = by.get(a, [])
        md.append("## %s (%d)\n" % (names[a], len(cs)))
        md.append("| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |")
        md.append("|---|---|---|---:|---|---|---|---|")
        for c in cs:
            hook = "%s `%s`%s" % (c["kind"], c["param"], (" " + c["value"]) if c["value"] else "")
            md.append("| `%s` | %s | %s | %d | %s | %s | %s | %s |" % (c["id"], c["group"], "loop" if c["loop"] else "one-shot", c["frames"], c["priority"], c["source"], hook, c["note"]))
        md.append("")
    with open(os.path.join(root, "docs/animation_list.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(md))
    print(len(clips), "clips,", len(p0), "P0")


if __name__ == "__main__":
    main()
