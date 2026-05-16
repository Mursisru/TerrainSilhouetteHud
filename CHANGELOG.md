# Changelog

## 1.3.2

- Sharper terrain-following line: neighbor/world azimuth blur off by default, 96 azimuth samples, finer height step.
- Dense per-slot screen projection (fixes wave jitter from compressed point arrays).
- Gap bridging between empty azimuth slots.

## 1.3.1

- Height cache + world ridge cache; forward silhouette only (no red/wide split).
- Fix polyline order (no screen-X sort on turn).

## 1.3.0

- Collision warning: show below summit, heading toward mountain, TTC &lt; 15 s; red &lt; 4 s.
- HUD band centered on `FlightHud.GetHUDCenter()` (stable when rolling).

## 1.2.4

- Clip silhouette to fuel/throttle HUD band.

## 1.2.0–1.2.3

- Heightmap corridor mode (Terrain + raycast), GPU and LegacyCpu modes retained.

## 1.0.0

- Initial CPU ray fan silhouette.
