# Esnya SF Addons

Addons and utilities for [SaccFlightAndVehicles](https://github.com/Sacchan-VRC/SaccFlightAndVehicles).

## Features
### Custom Inspectors / Gizmos
#### SaccEntityEditor
![image](https://user-images.githubusercontent.com/2088693/142752009-71cc2b96-2409-4aa3-b1bc-585cff755be6.png)
custom inspector for SaccEntity with validation and autofill buttons.

#### SAV_KeyboardControlsEditor
![image](https://user-images.githubusercontent.com/2088693/142752033-5c491832-0b28-4bf2-9317-dae26314fe8e.png)
Custom inspector for SAV_KeyobardCantrols with autofill button.

#### SVGizmoDrawer
![image](https://user-images.githubusercontent.com/2088693/142752067-16101550-75a2-4800-bca4-51fd82704d39.png)
Add gizmos for TargetEyeHeight in SaccVehicleSeat and FloatPoints in FloatScript.

### Editor-Only Components
#### MFD_Function
![image](https://user-images.githubusercontent.com/2088693/142752111-e808a28b-4587-4741-8d21-f7a69598d841.png)
Editor-Only component that manages the MFD functions. This component automatically adjusts the StickDsiplay and assigns the Dial_Function of DFUNCs.

#### SFReferenceTarget
![image](https://user-images.githubusercontent.com/2088693/142752131-c8f3236e-a3be-4059-934e-e92d66e629ec.png)
Editor-Only component to create a prefab easily, and automatically assign a reference to the SaccEntity properties.

### Udon
#### SFRuntimeSetup
![image](https://user-images.githubusercontent.com/2088693/142752139-16044ef1-ca37-40ce-b437-f3d3f4cec1c8.png)
Applies the parameters specified in the world to all vehicles when world loaded without prefab overrides.

#### SAV_UdonChips (Optional)
![image](https://user-images.githubusercontent.com/2088693/142752173-58ba708d-1f6f-4f80-9457-b394f02baa47.png)

Integrate UdonChips with SaccFlight.

## Requirements (Use latest version!!)
- [UdonSharp](https://github.com/MerlinVR/UdonSharp)
- [SaccFlightAndVehicles](https://github.com/Sacchan-VRC/SaccFlightAndVehicles): 1.5
- [UdonToolkit](https://github.com/orels1/UdonToolkit)
- [InariUdon](https://github.com/esnya/InariUdon): Using UdonLogger

### Requirements of SFUdonChips
- UdonChips

## Install
1. Import all requirements.
2. Download the unitpackage from [Lates Release](https://github.com/esnya/EsnyaSFAddons/releases/latest).
3. Import them all by drag & drop into Unity project window.
4. Click "EsnyaSFAddons/Features/Install" on the MenuBar *if you want it.*
