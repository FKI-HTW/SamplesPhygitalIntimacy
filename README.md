# Mixed Reality Portal & Mocap Streaming Sample

This repository contains a generalized sample project demonstrating two advanced XR implementations in Unity: a seamless "Hole Punching" Portal between Mixed Reality (Passthrough) and Virtual Reality, and a network-based Motion Capture streaming solution using Rokoko Studio.

These features have been extracted and generalized from a larger production environment to serve as a reference implementation for standalone VR headsets (Meta Quest).

## Table of Contents

- [About The Project](#about-the-project)
- [Built With](#built-with)
- [Prerequisites](#prerequisites)
- [Installation & Setup](#installation--setup)
- [Configuration](#configuration)
- [Usage](#usage)
- [Troubleshooting](#troubleshooting)
- [License](#license)

## About The Project

This project solves two common challenges in modern XR development:
1.  **Immersive Transitions:** Creating a non-euclidean portal that allows users to physically walk from their real-world environment (Passthrough) into a virtual world using URP Stencil Buffers.
2.  **Remote Animation:** Streaming high-fidelity body data from a motion capture suit (Windows PC) to a mobile VR client (Android/Quest) in real-time over a local network.

## Built With

* **Engine:** Unity 2022.3 (Universal Render Pipeline)
* **XR Framework:** Meta XR Core SDK (Building Blocks)
* **Networking:** Photon Fusion
* **Mocap Integration:** Rokoko Studio Live

## Prerequisites

Before cloning the repository, ensure you have the following:

* **Unity Hub & Editor:** Unity 2022.3 LTS or newer with Android Build Support and OpenJDK installed.
* **Hardware:** Meta Quest 2, 3, or Pro.
* **Mocap Hardware (Optional):** Rokoko Smartsuit Pro and Rokoko Studio installed on a Windows PC (required only for testing the streaming feature).

## Installation & Setup

1.  **Clone the Repository**
    ```bash
    git clone https://github.com/FKI-HTW/SamplesPhygitalIntimacy.git
    ```

2.  **Open in Unity**
    Add the project to Unity Hub and open it. Allow Unity to resolve packages.

3.  **Import Dependencies**
    If not already present, ensure the following packages are installed via Package Manager or Asset Store:
    * Photon Fusion
    * Rokoko Live for Unity
    * Meta XR All-in-One SDK

## Configuration

**CRITICAL:** The portal rendering relies on a specific Universal Render Pipeline (URP) configuration that may not persist when moving the project between machines. Follow these steps to ensure the portal renders correctly.

### 1. Layer Setup
1.  Open **Edit > Project Settings > Tags and Layers**.
2.  Add a new User Layer named `VirtualWorld`.
3.  Assign your entire 3D environment (all virtual objects) to this layer.

### 2. Renderer Feature Setup
1.  Navigate to your URP Renderer Data asset (usually located in `Settings/ForwardRenderer` or `Mobile_Renderer`).
2.  In the Inspector, under **Filtering**, find **Opaque Layer Mask** and **uncheck** `VirtualWorld`.
3.  Click **Add Renderer Feature** and select **Render Objects**.
4.  Name it `Stencil Mask` and configure it as follows:
    * **Event:** AfterRenderingOpaques
    * **Filters > Queue:** Opaque
    * **Filters > Layer Mask:** Check *only* `VirtualWorld`
    * **Overrides:** Check `Stencil`
    * **Stencil > Value:** `1`
    * **Stencil > Compare Function:** `Equal` (Objects render only where Stencil is 1) or `NotEqual` (Objects render everywhere except where Stencil is 1), depending on your desired masking logic.

*Note: For the provided sample scene, use `Equal` if the portal defines the visible area, or `NotEqual` if the portal defines the "hole" to reality.*

## Usage

### Portal Logic
The transition between MR and VR is handled by the `PortalTriggerController` script.

* **Trigger Mechanism:** The script detects the `CenterEyeAnchor` (Camera) entering the portal geometry.
* **State Switching:**
    * **To VR:** The script deactivates the Meta Passthrough Building Block and enables the full rendering of the `VirtualWorld` layer.
    * **To MR:** The script activates the Passthrough Building Block (Underlay) and applies the Stencil Mask to hide the virtual world outside the portal frame.
* **Customization:** You can adjust `Transition Delay` and `Randomize Order` in the inspector to create a dissolving transition effect.

### Rokoko Streaming
1.  Open **Rokoko Studio** on your PC and enable **Live Stream** (ensure the PC and Headset are on the same Wi-Fi).
2.  In the Unity Scene, ensure the `StudioManager` GameObject is active.
3.  The `Actor` component on the character rig is configured to receive the data stream.

## Troubleshooting

### Android Build Errors (Rokoko)
If you encounter build failures when deploying to Meta Quest regarding native plugins, you must remove incompatible libraries from the Rokoko package.

1.  Go to `Assets/Rokoko/Plugins/` (or the relevant installation folder).
2.  **Delete** the folders `armeabi-v7a` and `x86`.
3.  Rebuild the project.

### Portal is Invisible / Black
* Check that your Main Camera **Background Type** is set to `Solid Color` with **Alpha 0** `(0,0,0,0)`.
* Ensure the Passthrough capability is enabled in `OVRManager`.
* Verify the URP Renderer Feature steps listed in the Configuration section.

## License

This project is licensed under the MIT License.