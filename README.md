# KK_VR

A customized VR interaction overhaul plugin for Koikatu Chara Studio, based on the original VRGIN framework. 
This repository contains the decompiled and heavily modified source code aimed at modernizing the VR experience, specifically tailored for newer headsets like the Meta Quest 3.

## Features & Roadmap

### 1. Modernized Controls (WIP)
- **Separated Interactions**: Re-mapped grip and trigger buttons. The Trigger is now dedicated to micro-interactions (UI clicking, FK/IK node manipulation), while the Grip is strictly for macro-world manipulation (grabbing and moving the studio world).
- **Thumbstick Locomotion**: Replaced legacy VIVE trackpad logic with smooth thumbstick locomotion and snap-turning, making it much more comfortable to navigate the scene.

### 2. UI & Quality of Life (WIP)
- **One-Click UI Toggle**: Added a dedicated button to instantly hide/show all VR GUI panels for maximum immersion without fumbling through menus.
- **Quick Menu Summon**: Instantly snap the main control panel to your current view.

### 3. VAM-style Soft Physics & Hand Models (WIP)
- **Virtual Hands**: Replaced the default SteamVR controller models with custom translucent virtual hands to better indicate collision boundaries.
- **DynamicBone Integration**: Attached colliders to the virtual hands, enabling realistic soft-body physics interactions with characters' hair, clothing, and other DynamicBone-driven objects in the scene.

## Development Notes
This codebase is a continuous iteration of the original plugin. Feel free to explore or contribute if you are interested in Koikatu VR modding.
