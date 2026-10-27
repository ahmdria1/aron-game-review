# 0005. Race against ghosts first, live racing second

Status: accepted

## Decision
The default race, offline and for new players, is a ghost race: the player against their own best run, a friend's ghost (a pasted share code), or a pace ghost the game makes the first time. Ghosts are small recordings (one sample every half second) that are encoded to a short code. Ranked online races come after and use the same points (5/3/1).

## Why
- It works with no internet, no account and no waiting for other players.
- A game with few players has nobody to race against; a ghost is always there.
- Recordings are cheap to store and send, and need no live server or real-time netcode.
- Fair and safe: no chat, no direct contact with strangers, which matters for children.

## Rejected alternatives
- Real-time multiplayer first: needs netcode, matchmaking, anti-cheat and enough players online, and is poor on weak mobile connections.
- Computer racers only: fine for story races, but there is no personal goal to beat.

## Consequences
- The ghost recorder and codec must be deterministic and versioned so old codes still play.
- Online scores need server validation of the recording (`ClaimValidator`, plausibility against top speed), because a ghost file can be forged.
- Live ranked racing stays possible later without changing the point rules.
