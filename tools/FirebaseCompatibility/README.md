# Firebase managed compatibility check

Compile against extracted SDK DLLs without installing them, changing defines or building a player. The framework remains in Assets.

Requires Python 3.9 or later, a .NET SDK (or --csc pointing to csc.dll) and a completed Unity Editor script import of this checkout. Wait for Unity compilation to finish first. The tool uses the latest Editor Bee response files for its source list and references.

For each release, place Firebase.App.dll, Firebase.Auth.dll, Firebase.Firestore.dll, Firebase.Analytics.dll and supporting Firebase managed DLLs together outside Assets. All products must come from the same release. Native files are not required by this compile check.

```powershell
python tools/FirebaseCompatibility/check_firebase_compatibility.py --sdk-dir Library/FirebaseCompatibility/13.4.0 --sdk-dir Library/FirebaseCompatibility/14.1.0
```

Each SDK runs with both features enabled, Analytics only, and Auth/Firestore only. Each pass compiles `NebulaSoft.Firebase` against the selected SDK products, then `Assembly-CSharp` and `Assembly-CSharp-Editor` against the compiled adapter with the same feature defines and all Firebase SDK references removed. Unity imports validate generated code, and EditMode tests exercise the SDK-free configuration.

Exit 0 means all managed compilations passed. Outputs, response files, logs and result.json are under Library/FirebaseCompatibilityChecks. Existing project warnings remain visible. The tool never copies SDK DLLs into Assets or writes Unity's own assembly output directories.

This does not verify native linking, dependency resolution, live Firestore rules/transactions, provider configuration or Analytics delivery. Run the project's device/sandbox workflow. See [integration architecture](../../docs/firebase-integration.md).
