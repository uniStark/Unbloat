# Reverse Engineering Notes

Notes gathered while building PeltaTool. Useful if you want to adapt it to another
headset/mouse, or you're just curious how it works.

## ROG Pelta audio is rendered in software

The ROG Pelta exposes a **standard USB Audio** endpoint (`MI_00`) plus a **vendor HID
control interface** (`MI_03`, usage page `0xFF00`). Armoury Crate pushes the active
profile to the device via HID **SET_REPORT** (report id `0xCC`, 64 bytes, to interface 3):

| Report | Meaning |
|---|---|
| `cc 41 04` + 10 bytes | 10-band EQ (values, 12 = 0 dB baseline) |
| `cc 51 37 .. 05/00 ..` | virtual surround on (`05`) / off (`00`) |
| `cc 51 52 .. 64` | compressor |
| `cc 61 11 .. 0a` | bass boost |

You can replay these reports (e.g. via `HidD_SetOutputReport`) and Armoury Crate's UI
even reflects the change — **but the sound only changes while `ArmouryAudioAgent` is
running**. The actual EQ / virtual-surround DSP is done in software by that agent, not in
the headset hardware. So "switch the profile over HID and uninstall Armoury Crate" does
**not** preserve the sound.

**Conclusion:** to drop the vendor software you have to re-create the DSP yourself.
PeltaTool does this with [Equalizer APO](https://sourceforge.net/projects/equalizerapo/)
(the EQ — extracted 1:1) plus Windows Sonic for Headphones (the virtual surround,
approximate). The HID reverse-engineering still mattered: it's how the exact EQ curves
were recovered.

## .rog / Creative profile files are readable JSON

- Armoury Crate `.rog` profiles (`Documents\ROG\*.rog`) are **Base64( URL-encode( JSON ) )**.
  Decode them to read the exact EQ bands, surround flag, compressor, etc.
- Creative App profiles (`%LocalAppData%\Creative\...\Presets\EQ\*.json`) are **plain JSON**
  with `Bands: [{Frequency, Value}]` — directly portable into Equalizer APO.

## Logitech mouse battery over HID++ 2.0

The Lightspeed receiver (`046D:C539`) exposes a HID++ long interface (usage page `0xFF00`,
20-byte reports). To read battery without G HUB:

1. `IRoot.getFeature(0x1001)` (BatteryVoltage) → returns the feature index.
2. `BatteryVoltage.getBatteryVoltage` → returns voltage in mV (bytes 4–5, big-endian).
3. Map mV → % with a single-cell Li-ion curve (see `Bat.Pct` in the source).

Request layout (long report): `[0x11, deviceIndex=0x01, featureIndex, func<<4|swid, params...]`.
Read replies on the interrupt-IN endpoint (overlapped `ReadFile`) and match by the first
4 bytes.

## Equalizer APO `Device:` matching has no wildcards

This one cost the most time. `Device:` does a **substring** match against the string
`"DeviceName ConnectionName GUID"` — space-separated words that must all be found. There
are **no wildcards**. `Device: *PELTA*` matches nothing (it looks for a literal `*`);
`Device: PELTA` is correct.
