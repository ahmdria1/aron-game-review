# Animation list

The clips the game needs, per character. **312 clips**, **83 of them P0** (the vertical slice). Frames are at 30 fps. Built by `tools/gen_animation_list.py` from one table, which also writes `Resources/Config/animation_clips.json`; edit the table, not this file.

## How to read it

- **Loop / one-shot**: a loop must tile with no pop at the seam; a one-shot starts and ends on the idle pose (or the state it blends to).
- **Priority**: **P0** is the vertical slice (the first Riyadh stage: Aron, Noura, the camel, the falcon, the oryx and Dr. Duhaim). **P1** ships at launch. **P2** is polish. **P3** is later content (other mounts and vehicles).
- **Source**: `motion capture library` (a licensed pack, then cleaned), `motion capture reference` (shoot reference, solve, then clean by hand), `keyframed` (physics-assisted keys for air, falls and impacts), `hand-animated` (signature moves, animals and anything cartoonish where motion capture would look wrong).
- **Hook**: the Animator parameter that plays it (`PlayerAnimationHooks`, listed in `docs/game_feel.md`). The Animator state is named after the clip id.
- The **procedural layer** (lean, look-at, breathing, squash and stretch, foot IK, cloth, additive flinch) is *not* baked into these clips: it runs on top of every one of them, so the clips stay plain.
- **Modesty and age rating (7+)**: Noura is always fully covered (abaya, no sleeve roll, hands stay covered when she glides); defeat acts are slapstick, nothing gory; dances are marked for the culture review before they ship.

## Totals

| Character | Clips | P0 | P1 | P2 | P3 |
|---|---:|---:|---:|---:|---:|
| Aron | 87 | 26 | 35 | 23 | 3 |
| Noura | 59 | 23 | 28 | 7 | 1 |
| Dr. Duhaim | 22 | 11 | 7 | 4 | 0 |
| The falcon | 18 | 5 | 11 | 2 | 0 |
| The oryx | 14 | 4 | 6 | 4 | 0 |
| Camel (mount) | 12 | 8 | 1 | 2 | 1 |
| Horse (mount) | 11 | 0 | 5 | 6 | 0 |
| Ostrich (mount) | 9 | 0 | 3 | 5 | 1 |
| Giraffe (mount) | 9 | 0 | 3 | 4 | 2 |
| Falcon glider (mount) | 6 | 0 | 0 | 2 | 4 |
| Racing camel (camel race) | 13 | 0 | 11 | 2 | 0 |
| Robot jockey (camel race) | 6 | 0 | 5 | 1 | 0 |
| Wild Arabian oryx | 5 | 0 | 5 | 0 | 0 |
| Riders on mounts and vehicles (Aron and Noura) | 41 | 6 | 8 | 12 | 15 |
| **All** | **312** | **83** | **128** | **74** | **27** |

## Aron (87)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `aron_idle` | locomotion | loop | 120 | P0 | motion capture library | float `Speed` <0.05 | weight shift, breathing is added procedurally |
| `aron_idle_look_around` | locomotion | one-shot | 90 | P1 | motion capture library | trigger `Bored_0` | bored idle 0 |
| `aron_idle_adjust_shemagh` | locomotion | one-shot | 96 | P1 | hand-animated | trigger `Bored_1` | bored idle 1, signature |
| `aron_idle_check_agal` | locomotion | one-shot | 84 | P1 | hand-animated | trigger `Bored_2` | bored idle 2, signature |
| `aron_idle_stretch` | locomotion | one-shot | 108 | P2 | motion capture library | trigger `Bored_3` | bored idle 3 |
| `aron_walk` | locomotion | loop | 32 | P0 | motion capture library | float `Speed` 0.05..0.45 |  |
| `aron_jog` | locomotion | loop | 26 | P0 | motion capture library | float `Speed` 0.45..0.8 |  |
| `aron_run` | locomotion | loop | 22 | P0 | motion capture library | float `Speed` 0.8..1.0 |  |
| `aron_sprint` | locomotion | loop | 18 | P0 | motion capture library | bool `Sprinting` true |  |
| `aron_sprint_hem_bite` | locomotion | loop | 18 | P1 | hand-animated | bool `HemUp` true | thobe hem lifted and bitten, see ThobeSkirt; signature |
| `aron_start_run` | locomotion | one-shot | 14 | P1 | motion capture library | float `Speed` rising from idle |  |
| `aron_run_to_idle` | locomotion | one-shot | 16 | P1 | motion capture library | float `Speed` falling to idle |  |
| `aron_turn_in_place_left_90` | locomotion | one-shot | 14 | P1 | motion capture library | trigger `TurnL` |  |
| `aron_turn_in_place_right_90` | locomotion | one-shot | 14 | P1 | motion capture library | trigger `TurnR` |  |
| `aron_turn_in_place_180` | locomotion | one-shot | 20 | P1 | motion capture library | trigger `TurnR` angle > 150 |  |
| `aron_skid_stop` | locomotion | one-shot | 18 | P0 | hand-animated | bool `Skidding` true | dust and a back lean, signature |
| `aron_skid_turn` | locomotion | one-shot | 16 | P1 | hand-animated | bool `Skidding` true | reversal at speed |
| `aron_run_up_slope` | locomotion | loop | 22 | P1 | motion capture library | float `SlopeAngle` >8 |  |
| `aron_run_down_slope` | locomotion | loop | 22 | P1 | motion capture library | float `SlopeAngle` <-8 |  |
| `aron_step_up` | locomotion | one-shot | 12 | P1 | motion capture library | trigger `StepUp` |  |
| `aron_run_in_wind` | locomotion | loop | 22 | P2 | motion capture library | float `Wind` >0.6 | leaning into a sandstorm |
| `aron_run_wet_slip` | locomotion | one-shot | 20 | P2 | hand-animated | float `Wet` >0.7 | traction below 0.6 |
| `aron_jump_start` | air | one-shot | 8 | P0 | motion capture library | trigger `Jump` |  |
| `aron_jump_rise` | air | loop | 10 | P0 | motion capture library | float `VerticalSpeed` >2 |  |
| `aron_jump_apex` | air | loop | 12 | P0 | hand-animated | float `VerticalSpeed` -2..2 | the hang |
| `aron_fall` | air | loop | 10 | P0 | motion capture library | float `VerticalSpeed` <-2 |  |
| `aron_fast_fall` | air | loop | 8 | P1 | hand-animated | bool `FastFall` true |  |
| `aron_land_soft` | air | one-shot | 10 | P0 | motion capture library | trigger `Land` |  |
| `aron_land_hard` | air | one-shot | 22 | P0 | hand-animated | trigger `LandHard` the land-lag stumble |  |
| `aron_land_run` | air | one-shot | 12 | P1 | motion capture library | trigger `Land` Speed > 0.6 |  |
| `aron_double_jump` | air | one-shot | 16 | P0 | hand-animated | trigger `DoubleJump` flip |  |
| `aron_air_spin` | air | loop | 12 | P0 | hand-animated | trigger `Spin` spin attack in the air; the body is turned procedurally |  |
| `aron_air_spin_end` | air | one-shot | 10 | P1 | hand-animated | trigger `SpinEnd` recovery frames |  |
| `aron_crate_bounce` | air | one-shot | 14 | P0 | hand-animated | trigger `Bounce` |  |
| `aron_stomp` | air | one-shot | 12 | P1 | hand-animated | trigger `Stomp` |  |
| `aron_ledge_pop` | air | one-shot | 12 | P2 | motion capture library | trigger `LedgePop` ledge forgiveness lift |  |
| `aron_head_bonk` | air | one-shot | 14 | P2 | hand-animated | trigger `Bonk` |  |
| `aron_agal_swing` | combat | one-shot | 24 | P0 | hand-animated | trigger `Attack` 5 startup / 10 active / 9 recovery at 60 Hz, signature |  |
| `aron_agal_swing_2` | combat | one-shot | 24 | P1 | hand-animated | trigger `Attack2` buffered second swing |  |
| `aron_agal_swing_run` | combat | one-shot | 22 | P1 | hand-animated | trigger `Attack` Speed > 0.5 |  |
| `aron_slide_start` | combat | one-shot | 8 | P0 | hand-animated | trigger `Slide` |  |
| `aron_slide_loop` | combat | loop | 12 | P0 | hand-animated | bool `Sliding` true |  |
| `aron_slide_end` | combat | one-shot | 12 | P1 | hand-animated | bool `Sliding` false |  |
| `aron_hurt_front` | combat | one-shot | 14 | P0 | motion capture library | trigger `Hurt` additive flinch is added by direction |  |
| `aron_hurt_back` | combat | one-shot | 14 | P1 | motion capture library | trigger `HurtBack` |  |
| `aron_hurt_left` | combat | one-shot | 14 | P2 | motion capture library | trigger `HurtLeft` |  |
| `aron_hurt_right` | combat | one-shot | 14 | P2 | motion capture library | trigger `HurtRight` |  |
| `aron_knockback` | combat | one-shot | 24 | P1 | motion capture library | trigger `Knockback` |  |
| `aron_get_up` | combat | one-shot | 30 | P1 | motion capture library | trigger `GetUp` |  |
| `aron_shield_block` | combat | one-shot | 14 | P2 | hand-animated | trigger `Block` camel shield |  |
| `aron_sleeve_roll` | sleeves and costume | one-shot | 18 | P0 | hand-animated | trigger `SleeveRoll` rolls to the forearm before a fight; they stay rolled |  |
| `aron_sleeve_lower` | sleeves and costume | one-shot | 18 | P1 | hand-animated | trigger `SleeveLower` after 6 s of calm |  |
| `aron_talathum_wrap` | sleeves and costume | one-shot | 24 | P1 | hand-animated | trigger `Wrap` face wrap, only after the sleeves are up |  |
| `aron_shemagh_toss` | sleeves and costume | one-shot | 20 | P2 | hand-animated | trigger `Toss` |  |
| `aron_die_enemy_spin` | death | one-shot | 36 | P0 | hand-animated | trigger `Die` spin, topple, stars, agal rolls |  |
| `aron_die_pit_wave` | death | one-shot | 40 | P0 | hand-animated | trigger `DiePit` shemagh up, wave, drop |  |
| `aron_die_water_sink` | death | one-shot | 48 | P1 | hand-animated | trigger `DieWater` bubbles, sink, shemagh floats |  |
| `aron_die_hazard` | death | one-shot | 36 | P2 | hand-animated | trigger `DieHazard` |  |
| `aron_respawn_pop` | death | one-shot | 20 | P1 | hand-animated | trigger `Respawn` at the last lantern |  |
| `aron_collect_date` | interaction | one-shot | 12 | P0 | motion capture library | trigger `Collect` |  |
| `aron_pick_up` | interaction | one-shot | 24 | P1 | motion capture library | trigger `Pickup` |  |
| `aron_open_chest` | interaction | one-shot | 36 | P1 | motion capture library | trigger `Open` |  |
| `aron_push_crate` | interaction | loop | 24 | P2 | motion capture library | bool `Pushing` true |  |
| `aron_climb_ladder` | interaction | loop | 24 | P2 | motion capture library | bool `Climbing` true |  |
| `aron_swim_idle` | interaction | loop | 48 | P2 | motion capture library | bool `Swimming` true |  |
| `aron_swim` | interaction | loop | 28 | P2 | motion capture library | float `Speed` while Swimming |  |
| `aron_talk_idle` | interaction | loop | 96 | P1 | motion capture library | bool `Talking` true |  |
| `aron_talk_gesture` | interaction | one-shot | 36 | P1 | motion capture library | trigger `TalkGesture` |  |
| `aron_shop_browse` | interaction | loop | 72 | P2 | motion capture library | bool `Shopping` true |  |
| `aron_photo_pose` | interaction | one-shot | 60 | P2 | hand-animated | trigger `Photo` passport photo |  |
| `aron_scared` | interaction | one-shot | 30 | P1 | hand-animated | trigger `fear` existing trigger |  |
| `aron_scream` | interaction | one-shot | 30 | P2 | hand-animated | trigger `scream` existing trigger |  |
| `aron_wave` | emotes and celebration | one-shot | 48 | P0 | motion capture library | trigger `Wave` |  |
| `aron_celebrate_stage_clear` | emotes and celebration | one-shot | 72 | P0 | hand-animated | trigger `Celebrate` signature |  |
| `aron_celebrate_boss_win` | emotes and celebration | one-shot | 90 | P0 | hand-animated | trigger `CelebrateBoss` signature |  |
| `aron_cheer` | emotes and celebration | one-shot | 40 | P1 | motion capture library | trigger `Cheer` |  |
| `aron_laugh` | emotes and celebration | one-shot | 48 | P1 | motion capture library | trigger `Laugh` |  |
| `aron_thumbs_up` | emotes and celebration | one-shot | 24 | P2 | motion capture library | trigger `ThumbsUp` |  |
| `aron_bow_respect` | emotes and celebration | one-shot | 40 | P1 | hand-animated | trigger `Bow` hand on heart, greeting |  |
| `aron_dance_ardah` | emotes and celebration | loop | 96 | P2 | motion capture reference | trigger `Dance` ardah sword dance, culture review needed |  |
| `aron_dance_khaleeji` | emotes and celebration | loop | 96 | P2 | motion capture reference | trigger `Dance` khaleeji, culture review needed |  |
| `aron_pray_pause` | emotes and celebration | one-shot | 90 | P3 | motion capture library | trigger `Pause` only if the culture review wants it |  |
| `aron_coffee_pour` | emotes and celebration | one-shot | 90 | P2 | hand-animated | trigger `Coffee` Arabic coffee dallah, hospitality |  |
| `aron_shake_hands` | emotes and celebration | one-shot | 40 | P2 | motion capture library | trigger `Handshake` |  |
| `aron_trail_flourish` | country moves | one-shot | 28 | P2 | hand-animated | trigger `Flourish` country attack trail |  |
| `aron_tourist_pose` | country moves | one-shot | 40 | P3 | hand-animated | trigger `Photo` |  |
| `aron_fall_asleep` | country moves | loop | 96 | P3 | hand-animated | trigger `Sleep` |  |

## Noura (59)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `noura_idle` | locomotion | loop | 120 | P0 | motion capture library | float `Speed` <0.05 |  |
| `noura_idle_bored_adjust_abaya` | locomotion | one-shot | 96 | P1 | hand-animated | trigger `Bored_0` signature |  |
| `noura_idle_bored_check_bag` | locomotion | one-shot | 84 | P1 | hand-animated | trigger `Bored_1` |  |
| `noura_idle_bored_look_at_oryx` | locomotion | one-shot | 72 | P1 | hand-animated | trigger `Bored_2` |  |
| `noura_walk` | locomotion | loop | 32 | P0 | motion capture library | float `Speed` 0.05..0.45 |  |
| `noura_jog` | locomotion | loop | 26 | P0 | motion capture library | float `Speed` 0.45..0.8 |  |
| `noura_run` | locomotion | loop | 22 | P0 | motion capture library | float `Speed` 0.8..1.0 |  |
| `noura_sprint` | locomotion | loop | 18 | P0 | motion capture library | bool `Sprinting` true | the abaya hem is held down by the cloth chain |
| `noura_start_run` | locomotion | one-shot | 14 | P1 | motion capture library | float `Speed` rising |  |
| `noura_run_to_idle` | locomotion | one-shot | 16 | P1 | motion capture library | float `Speed` falling |  |
| `noura_turn_in_place_left_90` | locomotion | one-shot | 14 | P1 | motion capture library | trigger `TurnL` |  |
| `noura_turn_in_place_right_90` | locomotion | one-shot | 14 | P1 | motion capture library | trigger `TurnR` |  |
| `noura_skid_stop` | locomotion | one-shot | 18 | P0 | hand-animated | bool `Skidding` true |  |
| `noura_run_up_slope` | locomotion | loop | 22 | P1 | motion capture library | float `SlopeAngle` >8 |  |
| `noura_run_down_slope` | locomotion | loop | 22 | P1 | motion capture library | float `SlopeAngle` <-8 |  |
| `noura_run_in_wind` | locomotion | loop | 22 | P2 | motion capture library | float `Wind` >0.6 | abaya streams behind, stays modest |
| `noura_jump_start` | air | one-shot | 8 | P0 | motion capture library | trigger `Jump` |  |
| `noura_jump_rise` | air | loop | 10 | P0 | motion capture library | float `VerticalSpeed` >2 |  |
| `noura_jump_apex` | air | loop | 12 | P0 | hand-animated | float `VerticalSpeed` -2..2 |  |
| `noura_fall` | air | loop | 10 | P0 | motion capture library | float `VerticalSpeed` <-2 |  |
| `noura_land_soft` | air | one-shot | 10 | P0 | motion capture library | trigger `Land` |  |
| `noura_land_hard` | air | one-shot | 22 | P1 | hand-animated | trigger `LandHard` |  |
| `noura_double_jump` | air | one-shot | 16 | P0 | hand-animated | trigger `DoubleJump` second jump opens the abaya |  |
| `noura_glide_open` | air | one-shot | 12 | P0 | hand-animated | trigger `GlideOpen` abaya opens like a wing, hands stay covered |  |
| `noura_glide_loop` | air | loop | 24 | P0 | hand-animated | bool `Gliding` true | signature |
| `noura_glide_close` | air | one-shot | 12 | P1 | hand-animated | bool `Gliding` false |  |
| `noura_air_spin` | air | loop | 12 | P0 | hand-animated | trigger `Spin` |  |
| `noura_crate_bounce` | air | one-shot | 14 | P1 | hand-animated | trigger `Bounce` |  |
| `noura_handbag_swing` | combat | one-shot | 24 | P0 | hand-animated | trigger `Attack` same frame data as the agal swing, signature |  |
| `noura_handbag_swing_2` | combat | one-shot | 24 | P1 | hand-animated | trigger `Attack2` |  |
| `noura_slide_start` | combat | one-shot | 8 | P0 | hand-animated | trigger `Slide` |  |
| `noura_slide_loop` | combat | loop | 12 | P0 | hand-animated | bool `Sliding` true |  |
| `noura_slide_end` | combat | one-shot | 12 | P1 | hand-animated | bool `Sliding` false |  |
| `noura_hurt_front` | combat | one-shot | 14 | P0 | motion capture library | trigger `Hurt` |  |
| `noura_hurt_back` | combat | one-shot | 14 | P1 | motion capture library | trigger `HurtBack` |  |
| `noura_knockback` | combat | one-shot | 24 | P1 | motion capture library | trigger `Knockback` |  |
| `noura_get_up` | combat | one-shot | 30 | P1 | motion capture library | trigger `GetUp` |  |
| `noura_die_enemy_sit` | death | one-shot | 48 | P0 | hand-animated | trigger `Die` abaya swirl, sit down, stars, bag beside, uff, oryx nudge |  |
| `noura_die_pit` | death | one-shot | 40 | P1 | hand-animated | trigger `DiePit` |  |
| `noura_die_water` | death | one-shot | 48 | P2 | hand-animated | trigger `DieWater` |  |
| `noura_respawn_pop` | death | one-shot | 20 | P1 | hand-animated | trigger `Respawn` |  |
| `noura_collect_date` | interaction | one-shot | 12 | P0 | motion capture library | trigger `Collect` |  |
| `noura_pick_up` | interaction | one-shot | 24 | P1 | motion capture library | trigger `Pickup` |  |
| `noura_open_chest` | interaction | one-shot | 36 | P2 | motion capture library | trigger `Open` |  |
| `noura_talk_idle` | interaction | loop | 96 | P1 | motion capture library | bool `Talking` true |  |
| `noura_talk_gesture` | interaction | one-shot | 36 | P1 | motion capture library | trigger `TalkGesture` |  |
| `noura_shop_browse` | interaction | loop | 72 | P2 | motion capture library | bool `Shopping` true |  |
| `noura_photo_pose` | interaction | one-shot | 60 | P2 | hand-animated | trigger `Photo` |  |
| `noura_scared` | interaction | one-shot | 30 | P1 | hand-animated | trigger `fear` |  |
| `noura_swim` | interaction | loop | 28 | P3 | motion capture library | bool `Swimming` true |  |
| `noura_wave` | emotes and celebration | one-shot | 48 | P0 | motion capture library | trigger `Wave` |  |
| `noura_celebrate_stage_clear` | emotes and celebration | one-shot | 72 | P0 | hand-animated | trigger `Celebrate` |  |
| `noura_cheer` | emotes and celebration | one-shot | 40 | P1 | motion capture library | trigger `Cheer` |  |
| `noura_laugh` | emotes and celebration | one-shot | 48 | P1 | motion capture library | trigger `Laugh` |  |
| `noura_bow_respect` | emotes and celebration | one-shot | 40 | P1 | hand-animated | trigger `Bow` |  |
| `noura_dance_khaleeji` | emotes and celebration | loop | 96 | P2 | motion capture reference | trigger `Dance` culture review needed |  |
| `noura_coffee_pour` | emotes and celebration | one-shot | 90 | P2 | hand-animated | trigger `Coffee` |  |
| `noura_pet_oryx` | emotes and celebration | one-shot | 60 | P1 | hand-animated | trigger `PetOryx` |  |
| `noura_camel_race_sideline_cheer` | race | one-shot | 60 | P1 | hand-animated | trigger `Celebrate` cheers beside the track, fully covered, abaya in the wind |  |

## Dr. Duhaim (22)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `duhaim_idle` | locomotion and combat | loop | 120 | P0 | motion capture library | bool `Aggro` false |  |
| `duhaim_idle_gloat` | locomotion and combat | one-shot | 90 | P1 | hand-animated | trigger `Bored_0` |  |
| `duhaim_stalk_walk` | locomotion and combat | loop | 30 | P0 | motion capture library | float `Speed` 0.05..0.6 |  |
| `duhaim_stalk_run` | locomotion and combat | loop | 22 | P0 | motion capture library | float `Speed` >0.6 |  |
| `duhaim_turn_in_place` | locomotion and combat | one-shot | 18 | P1 | motion capture library | trigger `TurnR` |  |
| `duhaim_telegraph_windup` | locomotion and combat | one-shot | 30 | P0 | hand-animated | trigger `Telegraph` readable wind-up, 0.5 s |  |
| `duhaim_attack_swipe` | locomotion and combat | one-shot | 28 | P0 | hand-animated | trigger `Attack` |  |
| `duhaim_attack_slam` | locomotion and combat | one-shot | 40 | P0 | hand-animated | trigger `Attack2` ground slam, shockwave |  |
| `duhaim_attack_charge` | locomotion and combat | loop | 20 | P1 | hand-animated | bool `Charging` true |  |
| `duhaim_attack_throw` | locomotion and combat | one-shot | 36 | P1 | hand-animated | trigger `Throw` |  |
| `duhaim_attack_combo_end` | locomotion and combat | one-shot | 24 | P2 | hand-animated | trigger `Attack3` |  |
| `duhaim_stunned_loop` | locomotion and combat | loop | 48 | P0 | hand-animated | bool `Stunned` true |  |
| `duhaim_stun_enter` | locomotion and combat | one-shot | 20 | P0 | hand-animated | trigger `Stun` |  |
| `duhaim_stun_recover` | locomotion and combat | one-shot | 20 | P1 | hand-animated | bool `Stunned` false |  |
| `duhaim_hurt_hit` | locomotion and combat | one-shot | 14 | P0 | motion capture library | trigger `Hurt` |  |
| `duhaim_phase_break_roar` | locomotion and combat | one-shot | 48 | P0 | hand-animated | trigger `PhaseBreak` hit-stop and a roar |  |
| `duhaim_defeat_fall` | locomotion and combat | one-shot | 72 | P0 | hand-animated | trigger `Die` slapstick, nothing gory (7+) |  |
| `duhaim_defeat_sit_dazed` | locomotion and combat | loop | 72 | P1 | hand-animated | bool `Dead` true |  |
| `duhaim_taunt_laugh` | locomotion and combat | one-shot | 48 | P2 | hand-animated | trigger `Laugh` |  |
| `duhaim_point_at_player` | locomotion and combat | one-shot | 36 | P2 | motion capture library | trigger `Point` |  |
| `duhaim_enter_arena` | locomotion and combat | one-shot | 60 | P1 | hand-animated | trigger `Enter` |  |
| `duhaim_surrender_bow` | locomotion and combat | one-shot | 60 | P2 | hand-animated | trigger `Bow` after the fight, good sport |  |

## The falcon (18)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `falcon_perch_idle` | flight and perch | loop | 90 | P0 | hand-animated | bool `Perched` true |  |
| `falcon_perch_preen` | flight and perch | one-shot | 60 | P1 | hand-animated | trigger `Bored_0` |  |
| `falcon_perch_look` | flight and perch | one-shot | 48 | P1 | hand-animated | trigger `Bored_1` |  |
| `falcon_take_off` | flight and perch | one-shot | 16 | P0 | hand-animated | trigger `TakeOff` |  |
| `falcon_fly_flap` | flight and perch | loop | 16 | P0 | hand-animated | bool `Flying` true |  |
| `falcon_fly_glide` | flight and perch | loop | 36 | P1 | hand-animated | bool `Gliding` true |  |
| `falcon_fly_bank_left` | flight and perch | loop | 24 | P1 | hand-animated | float `TurnAngle` <-0.3 |  |
| `falcon_fly_bank_right` | flight and perch | loop | 24 | P1 | hand-animated | float `TurnAngle` >0.3 |  |
| `falcon_hover` | flight and perch | loop | 20 | P1 | hand-animated | bool `Hover` true |  |
| `falcon_dive` | flight and perch | one-shot | 18 | P0 | hand-animated | trigger `Dive` attack dive |  |
| `falcon_dive_pullup` | flight and perch | one-shot | 16 | P1 | hand-animated | trigger `PullUp` |  |
| `falcon_land_on_arm` | flight and perch | one-shot | 20 | P0 | hand-animated | trigger `Land` |  |
| `falcon_land_on_perch` | flight and perch | one-shot | 18 | P1 | hand-animated | trigger `Land` |  |
| `falcon_fetch_grab` | flight and perch | one-shot | 20 | P1 | hand-animated | trigger `Collect` picks up a date |  |
| `falcon_scout_look` | flight and perch | one-shot | 30 | P1 | hand-animated | trigger `Scout` points out hidden things |  |
| `falcon_screech` | flight and perch | one-shot | 24 | P1 | hand-animated | trigger `Call` |  |
| `falcon_hurt` | flight and perch | one-shot | 14 | P2 | hand-animated | trigger `Hurt` |  |
| `falcon_celebrate_loop` | flight and perch | one-shot | 30 | P2 | hand-animated | trigger `Celebrate` |  |

## The oryx (14)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `oryx_idle` | companion | loop | 120 | P0 | hand-animated | float `Speed` <0.05 |  |
| `oryx_graze` | companion | loop | 90 | P1 | hand-animated | trigger `Bored_0` |  |
| `oryx_look_up` | companion | one-shot | 48 | P1 | hand-animated | trigger `Bored_1` |  |
| `oryx_walk` | companion | loop | 30 | P0 | hand-animated | float `Speed` 0.05..0.5 |  |
| `oryx_trot` | companion | loop | 22 | P0 | hand-animated | float `Speed` 0.5..0.85 |  |
| `oryx_gallop` | companion | loop | 16 | P1 | hand-animated | float `Speed` >0.85 |  |
| `oryx_jump` | companion | one-shot | 20 | P1 | hand-animated | trigger `Jump` |  |
| `oryx_land` | companion | one-shot | 12 | P1 | hand-animated | trigger `Land` |  |
| `oryx_nudge_noura` | companion | one-shot | 36 | P0 | hand-animated | trigger `Nudge` after Noura's fall |  |
| `oryx_hurt` | companion | one-shot | 14 | P2 | hand-animated | trigger `Hurt` |  |
| `oryx_shield_horns` | companion | one-shot | 30 | P2 | hand-animated | trigger `Block` ability |  |
| `oryx_charge` | companion | loop | 16 | P2 | hand-animated | bool `Charging` true |  |
| `oryx_sit_down` | companion | one-shot | 40 | P2 | hand-animated | trigger `Sit` |  |
| `oryx_happy_hop` | companion | one-shot | 24 | P1 | hand-animated | trigger `Celebrate` |  |

## Camel (mount) (12)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `camel_idle` | mount | loop | 120 | P0 | hand-animated | float `MountSpeed` <0.05 |  |
| `camel_walk` | mount | loop | 30 | P0 | hand-animated | float `MountSpeed` 0.05..0.5 |  |
| `camel_run` | mount | loop | 20 | P0 | hand-animated | float `MountSpeed` >0.5 |  |
| `camel_jump` | mount | one-shot | 20 | P0 | hand-animated | trigger `Jump` |  |
| `camel_land` | mount | one-shot | 12 | P0 | hand-animated | trigger `Land` |  |
| `camel_hurt_buck` | mount | one-shot | 24 | P0 | hand-animated | trigger `Hurt` throws the rider off |  |
| `camel_kneel_mount` | mount | one-shot | 30 | P0 | hand-animated | trigger `Mount` |  |
| `camel_sprint` | mount | loop | 16 | P0 | hand-animated | bool `Sprinting` true | camel sprint ability |
| `camel_shield_stance` | mount | one-shot | 24 | P1 | hand-animated | trigger `Block` shield ability |  |
| `camel_spit_blow` | mount | one-shot | 28 | P3 | hand-animated | trigger `Attack` comic, only if kind to the audience |  |
| `camel_sit_rest` | mount | loop | 96 | P2 | hand-animated | bool `Sitting` true |  |
| `camel_drink` | mount | one-shot | 72 | P2 | hand-animated | trigger `Drink` |  |

## Horse (mount) (11)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `horse_idle` | mount | loop | 120 | P1 | hand-animated | float `MountSpeed` <0.05 |  |
| `horse_walk` | mount | loop | 30 | P1 | hand-animated | float `MountSpeed` 0.05..0.5 |  |
| `horse_run` | mount | loop | 20 | P1 | hand-animated | float `MountSpeed` >0.5 |  |
| `horse_jump` | mount | one-shot | 20 | P2 | hand-animated | trigger `Jump` |  |
| `horse_land` | mount | one-shot | 12 | P2 | hand-animated | trigger `Land` |  |
| `horse_hurt_buck` | mount | one-shot | 24 | P2 | hand-animated | trigger `Hurt` throws the rider off |  |
| `horse_kneel_mount` | mount | one-shot | 30 | P2 | hand-animated | trigger `Mount` |  |
| `horse_charge_gallop` | mount | loop | 14 | P1 | hand-animated | bool `Charging` true | horse charge |
| `horse_rear_up` | mount | one-shot | 36 | P1 | hand-animated | trigger `Attack` |  |
| `horse_whinny` | mount | one-shot | 36 | P2 | hand-animated | trigger `Call` |  |
| `horse_trot` | mount | loop | 24 | P2 | hand-animated | float `MountSpeed` 0.3..0.6 |  |

## Ostrich (mount) (9)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `ostrich_idle` | mount | loop | 120 | P1 | hand-animated | float `MountSpeed` <0.05 |  |
| `ostrich_walk` | mount | loop | 30 | P1 | hand-animated | float `MountSpeed` 0.05..0.5 |  |
| `ostrich_run` | mount | loop | 20 | P1 | hand-animated | float `MountSpeed` >0.5 |  |
| `ostrich_jump` | mount | one-shot | 20 | P2 | hand-animated | trigger `Jump` |  |
| `ostrich_land` | mount | one-shot | 12 | P2 | hand-animated | trigger `Land` |  |
| `ostrich_hurt_buck` | mount | one-shot | 24 | P2 | hand-animated | trigger `Hurt` throws the rider off |  |
| `ostrich_kneel_mount` | mount | one-shot | 30 | P2 | hand-animated | trigger `Mount` |  |
| `ostrich_sprint` | mount | loop | 14 | P2 | hand-animated | bool `Sprinting` true |  |
| `ostrich_flap_balance` | mount | one-shot | 24 | P3 | hand-animated | trigger `Balance` |  |

## Giraffe (mount) (9)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `giraffe_idle` | mount | loop | 120 | P1 | hand-animated | float `MountSpeed` <0.05 |  |
| `giraffe_walk` | mount | loop | 30 | P1 | hand-animated | float `MountSpeed` 0.05..0.5 |  |
| `giraffe_run` | mount | loop | 20 | P1 | hand-animated | float `MountSpeed` >0.5 |  |
| `giraffe_jump` | mount | one-shot | 20 | P2 | hand-animated | trigger `Jump` |  |
| `giraffe_land` | mount | one-shot | 12 | P2 | hand-animated | trigger `Land` |  |
| `giraffe_hurt_buck` | mount | one-shot | 24 | P2 | hand-animated | trigger `Hurt` throws the rider off |  |
| `giraffe_kneel_mount` | mount | one-shot | 30 | P2 | hand-animated | trigger `Mount` |  |
| `giraffe_duck_under` | mount | one-shot | 24 | P3 | hand-animated | trigger `Duck` low gates |  |
| `giraffe_neck_stretch` | mount | one-shot | 36 | P3 | hand-animated | trigger `Reach` |  |

## Falcon glider (mount) (6)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `falcon_glider_glide_idle` | mount | loop | 48 | P2 | hand-animated | float `MountSpeed` <0.05 |  |
| `falcon_glider_glide_forward` | mount | loop | 30 | P2 | hand-animated | float `MountSpeed` >0.05 |  |
| `falcon_glider_glide_bank_left` | mount | loop | 24 | P3 | hand-animated | float `TurnAngle` <-0.3 |  |
| `falcon_glider_glide_bank_right` | mount | loop | 24 | P3 | hand-animated | float `TurnAngle` >0.3 |  |
| `falcon_glider_glide_dive` | mount | one-shot | 18 | P3 | hand-animated | trigger `Dive` |  |
| `falcon_glider_glide_land` | mount | one-shot | 20 | P3 | hand-animated | trigger `Land` |  |

## Racing camel (camel race) (13)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `camel_racer_kneel` | race | one-shot | 30 | P1 | hand-animated | trigger `Kneel` kneels at the gate so the rider can climb on |  |
| `camel_racer_rise` | race | one-shot | 36 | P1 | hand-animated | trigger `Rise` gets up with a wobble, ready to run |  |
| `camel_racer_gate_idle` | race | loop | 96 | P1 | hand-animated | float `MountSpeed` <0.05 | snorts and stamps, ears twitching |
| `camel_racer_ready_gate` | race | loop | 60 | P2 | hand-animated | trigger `Ready` bounces on the spot before the start |  |
| `camel_racer_gallop` | race | loop | 18 | P1 | hand-animated | float `MountSpeed` 0.5..0.85 | the long-legged racing gait, exaggerated |
| `camel_racer_gallop_full` | race | loop | 14 | P1 | hand-animated | float `MountSpeed` >0.85 | stretched out, ears flat, speed lines on |
| `camel_racer_jump_dune` | race | one-shot | 24 | P1 | hand-animated | trigger `Jump` leaps a dune ramp, legs trailing |  |
| `camel_racer_land_bounce` | race | one-shot | 14 | P1 | hand-animated | trigger `Land` squash on landing, then springs back |  |
| `camel_racer_stumble` | race | one-shot | 30 | P1 | hand-animated | trigger `Stumble` comic wobble and trip, nobody is hurt, keeps running |  |
| `camel_racer_boost` | race | one-shot | 16 | P1 | hand-animated | trigger `Boost` head down, a burst off a boost pad |  |
| `camel_racer_boost_loop` | race | loop | 12 | P1 | hand-animated | bool `Boosting` true | while the boost lasts |
| `camel_racer_finish_celebrate` | race | one-shot | 48 | P1 | hand-animated | trigger `Celebrate` hops and spins past the line |  |
| `camel_racer_lose_slump` | race | one-shot | 40 | P2 | hand-animated | trigger `Lose` droops its ears, good sport |  |

## Robot jockey (camel race) (6)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `robot_jockey_idle_gate` | race | loop | 60 | P1 | hand-animated | float `MountSpeed` <0.05 | a small robot; every rival camel has one, never a child |
| `robot_jockey_ride_gallop` | race | loop | 18 | P1 | hand-animated | float `MountSpeed` >0.5 | bobs on the saddle, antenna swaying |
| `robot_jockey_lean_boost` | race | loop | 12 | P1 | hand-animated | bool `Boosting` true | flattens along the neck |
| `robot_jockey_wobble` | race | one-shot | 30 | P1 | hand-animated | trigger `Stumble` spins its antenna, holds on |  |
| `robot_jockey_cheer` | race | one-shot | 40 | P1 | hand-animated | trigger `Celebrate` arms up, lights flash |  |
| `robot_jockey_sad_beep` | race | one-shot | 30 | P2 | hand-animated | trigger `Lose` lights dim, a sad beep |  |

## Wild Arabian oryx (5)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `oryx_wild_idle_graze` | wildlife | loop | 90 | P1 | hand-animated | bool `Grazing` true | a wild Arabian oryx grazing, peaceful |
| `oryx_wild_walk` | wildlife | loop | 30 | P1 | hand-animated | float `Speed` 0.05..0.5 |  |
| `oryx_wild_trot` | wildlife | loop | 22 | P1 | hand-animated | float `Speed` 0.5..0.85 |  |
| `oryx_wild_flee` | wildlife | loop | 18 | P1 | hand-animated | bool `Fleeing` true | a gentle trot away from the player, never a panic |
| `oryx_wild_alert` | wildlife | one-shot | 36 | P1 | hand-animated | trigger `Alert` lifts its head and ears when someone comes near |  |

## Riders on mounts and vehicles (Aron and Noura) (41)

| Clip | Group | Loop | Frames | Pri | Source | Hook | Note |
|---|---|---|---:|---|---|---|---|
| `rider_camel_race_crouch` | camel race | loop | 18 | P1 | hand-animated | bool `Boosting` true | Aron on his camel, low on the neck while MountKind 2 |
| `rider_camel_race_cheer` | camel race | one-shot | 48 | P1 | hand-animated | trigger `Celebrate` arms up on the camel after the line |  |
| `rider_mount_up` | on mounts | one-shot | 30 | P0 | hand-animated | trigger `Mount` camel kneels, hero climbs on |  |
| `rider_dismount` | on mounts | one-shot | 24 | P0 | hand-animated | trigger `Dismount` |  |
| `rider_thrown_off` | on mounts | one-shot | 36 | P0 | hand-animated | trigger `ThrownOff` no life lost |  |
| `rider_camel_ride_idle` | on mounts | loop | 96 | P0 | motion capture library | int `MountKind` 2 |  |
| `rider_camel_ride_walk` | on mounts | loop | 30 | P0 | motion capture library | int `MountKind` 2 and MountSpeed 0.05..0.5 |  |
| `rider_camel_ride_run` | on mounts | loop | 20 | P0 | motion capture library | int `MountKind` 2 and MountSpeed > 0.5 |  |
| `rider_camel_ride_jump` | on mounts | one-shot | 20 | P1 | motion capture library | trigger `Jump` while MountKind 2 |  |
| `rider_horse_ride_idle` | on mounts | loop | 96 | P1 | motion capture library | int `MountKind` 1 |  |
| `rider_horse_ride_gallop` | on mounts | loop | 16 | P1 | motion capture library | int `MountKind` 1 and MountSpeed > 0.5 |  |
| `rider_horse_ride_jump` | on mounts | one-shot | 20 | P1 | motion capture library | trigger `Jump` while MountKind 1 |  |
| `rider_horse_ride_charge` | on mounts | loop | 14 | P2 | hand-animated | bool `Charging` true |  |
| `rider_ostrich_ride` | on mounts | loop | 14 | P3 | motion capture reference | int `MountKind` 3 |  |
| `rider_giraffe_ride` | on mounts | loop | 30 | P3 | motion capture reference | int `MountKind` 4 |  |
| `rider_falcon_glide_ride` | on mounts | loop | 48 | P3 | hand-animated | int `MountKind` 5 | lies forward, arms out |
| `rider_rider_attack_swing` | on mounts | one-shot | 24 | P1 | hand-animated | trigger `Attack` from the saddle |  |
| `rider_rider_hurt` | on mounts | one-shot | 14 | P1 | motion capture library | trigger `Hurt` |  |
| `rider_car_drive_idle` | on vehicles | loop | 60 | P2 | motion capture library | int `VehicleKind` 1 |  |
| `rider_car_steer_left` | on vehicles | loop | 24 | P2 | motion capture library | float `TurnAngle` <-0.3 and VehicleKind 1 |  |
| `rider_car_steer_right` | on vehicles | loop | 24 | P2 | motion capture library | float `TurnAngle` >0.3 and VehicleKind 1 |  |
| `rider_car_boost_brace` | on vehicles | one-shot | 20 | P3 | hand-animated | trigger `Boost` |  |
| `rider_bicycle_pedal` | on vehicles | loop | 24 | P2 | motion capture library | int `VehicleKind` 2 |  |
| `rider_bicycle_coast` | on vehicles | loop | 36 | P3 | motion capture library | int `VehicleKind` 2 and MountSpeed < 0.2 |  |
| `rider_bicycle_lean_left` | on vehicles | loop | 24 | P3 | motion capture library | float `TurnAngle` <-0.3 and VehicleKind 2 |  |
| `rider_bicycle_lean_right` | on vehicles | loop | 24 | P3 | motion capture library | float `TurnAngle` >0.3 and VehicleKind 2 |  |
| `rider_motorcycle_ride` | on vehicles | loop | 36 | P2 | motion capture library | int `VehicleKind` 3 |  |
| `rider_motorcycle_lean_left` | on vehicles | loop | 24 | P3 | motion capture library | float `TurnAngle` <-0.3 and VehicleKind 3 |  |
| `rider_motorcycle_lean_right` | on vehicles | loop | 24 | P3 | motion capture library | float `TurnAngle` >0.3 and VehicleKind 3 |  |
| `rider_boat_row` | on vehicles | loop | 36 | P2 | motion capture library | int `VehicleKind` 4 |  |
| `rider_boat_steer_left` | on vehicles | loop | 24 | P3 | motion capture library | float `TurnAngle` <-0.3 and VehicleKind 4 |  |
| `rider_boat_steer_right` | on vehicles | loop | 24 | P3 | motion capture library | float `TurnAngle` >0.3 and VehicleKind 4 |  |
| `rider_sail_trim` | on vehicles | loop | 48 | P3 | motion capture library | int `VehicleKind` 5 |  |
| `rider_ski_carve` | on vehicles | loop | 30 | P3 | motion capture library | int `VehicleKind` 6 |  |
| `rider_surf_ride` | on vehicles | loop | 36 | P3 | motion capture library | int `VehicleKind` 7 |  |
| `rider_sky_hang_glide` | on vehicles | loop | 48 | P3 | motion capture library | int `VehicleKind` 8 |  |
| `rider_vehicle_crash_tumble` | on vehicles | one-shot | 48 | P2 | hand-animated | trigger `Hurt` comic spill, nobody hurt |  |
| `rider_vehicle_win_cheer` | on vehicles | one-shot | 48 | P2 | hand-animated | trigger `Celebrate` race podium |  |
| `rider_vehicle_lose_slump` | on vehicles | one-shot | 40 | P2 | hand-animated | trigger `Lose` good sport |  |
| `rider_race_start_ready` | on vehicles | loop | 60 | P2 | hand-animated | trigger `Ready` |  |
| `rider_race_podium_wave` | on vehicles | one-shot | 60 | P2 | hand-animated | trigger `Wave` |  |
