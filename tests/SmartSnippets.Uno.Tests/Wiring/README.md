# Official Windows head wiring tests

The suite links the actual Uno code-behind, host, store, view-models and insertion coordinator. Headless rendering/window/OS substitutes avoid app launch, real registry, clipboard, hotkey and focus operations.

`Actions.txt` enumerates the compiled XAML actions. New or changed actions must have a real consumer assertion; it is not regenerated during builds. Administrator restart and explicit clipboard-loss consent are included.

These tests supplement Windows compilation and user live checks; they are not native event delivery or cross-platform acceptance evidence.
