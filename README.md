# Phosphorite Reborn

<div align="left">
  <img src="https://github.com/Orolade/phosphorite-reborn/blob/main/preview.png?raw=true" width="50%" alt="Lighting preview"><br>
  <a href="https://github.com/Orolade/phosphorite-reborn/releases">
    <img src="https://img.shields.io/github/downloads/Orolade/phosphorite-reborn/total?label=Downloads&style=flat-square" alt="Downloads">
  </a>
</div>

An experimental client-side real-time lighting mod for Gorilla Tag using the Ghost Reactor lighting system.

Based on [phosphorite by BrokenSt0ne](https://github.com/BrokenSt0ne/phosphorite).

## Features

* Real-time lighting using the game's own lighting system.
* Full control over:
  * Light positions
  * Colors
  * Intensities
* Light Spawner and Light Editor through BananaOS.
* Move, toggle and destroy existing lights.
* Load/save lights via JSON, with automatic loading and saving enabled by default.
* Baked lighting toggle and adjustable ambient color.
* Map-specific lighting.
* Expanded light capacity and view-based culling.
* Local flashlight and Ghost Reactor lantern with infinite charge.

## Usage

Place `phosphorite.dll` inside `BepInEx/plugins/phosphorite/`. Your lighting file must be named `data.json` and placed in the same folder as the DLL.

Open Light Spawner in BananaOS to choose the color and intensity. Hold the right grip to aim, then press the trigger to place a light.

Open Light Editor to select an existing light by aiming at its marker and pressing the trigger while holding the right grip. You can change its settings, toggle it, or choose Move light and Destroy light.

To move a selected light, choose Move light, hold the right grip to aim at its new position, then make a fresh trigger press to confirm.

Light Spawner and Light Editor continue working with the watch closed while their page remains selected.

The Lighting page provides baked lighting controls, ambient RGB settings, and actions for saving, loading and clearing lights from the current map. One `data.json` stores lighting for all maps.

The Portable Lights page lets you create or remove the flashlight and lantern. The flashlight attaches to the right hand, while the lantern can be grabbed, dropped and placed in the world. Both use their native switching controls and have infinite charge.

**Warning:** Large lighting setups can reduce performance.

## Contributing & Issues

* Found a bug? [Submit an issue](https://github.com/Orolade/phosphorite-reborn/issues).
* Know your way around code? Pull requests are welcome!

## Compatibility

* Gorilla Tag for PC.
* Requires BepInEx 5 and BananaOS 1.1.1.
* All custom lighting and spawned items are client-side and are not synchronized with other players.

Original phosphorite by BrokenSt0ne. Distributed under the [MIT License](LICENSE).

###### *This product is not affiliated with Another Axiom Inc. or Gorilla Tag and is not endorsed or sponsored by Another Axiom.*
