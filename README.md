# Moonforged Lights and Decor

A standalone Moonforged collection of rugs, furniture, lamps, porcelain sets, benches, cannons, and decorative build pieces.

Includes Valheim 1.0 build-menu sorting, configurable Moonforged categories, multiplayer-synchronized lamp colors, persistent lamp states, and support for custom lamp colors.

**Windows version included.**

If you need a **Vulkan or Mac compatible version**, please contact me on [Discord](https://discord.gg/xtfpFnWmm).

I keep those builds separate because including all platform versions in the main package would make the download unnecessarily large, while only a small number of users need them.

---

Moonforged Lights and Decor was split from the original **Moonforged Build Pieces** mod, which has now been separated into three standalone mods:

- **Moonforged Build Pieces** – Includes new carved and runed building pieces, along with stained-glass pieces.
- **Moonforged Lights and Decor** – Includes the rugs, furniture, lamps, and other decorations from the original Moonforged Build Pieces mod.
- **Moonforged Banner Collection** – Includes all banners from the original mod, plus new Deep North biome and boss banners.

---

# v1.0.0 Content

## 💡 Lighting

A collection of street lamps, wall-mounted lamps, fantasy lamps, and decorative lighting.

### Classic Bench and Bin

![Bench and Bin](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/BenchandBin.png)

### Double Lamp II

![Double Lamp II](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/DoubleLamp_2.png)

### Double Lamp I

![Double Lamp I](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/DoubleLamp_I.png)

### Japanese Lamp

![Japanese Lamp](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/JapaneseLamp.png)

### Quad Lamp

![Quad Lamp](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/QuadLamp.png)

### Simple Lamp

![Simple Lamp](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/Simple_Lamp.png)

### Simple Street Lamp

![Simple Street Lamp](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/SimpleLamp.png)

### Wall-Mounted Lamps

![Wall Mounted Lamps](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/WallmountLamps.png)

### Raven Lamp

![Raven Lamp](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/Crowlamp.png)

### Gargoyle Lamp

![Gargoyle Lamp](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/GargoyleLamp.png)

### Dragon Lamp

![Dragon Lamp](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/DragonLamp.png)

---

## 🛋️ Furniture

Furniture for Viking halls, homes, castles, and settlements.

### Thrones

![Thrones](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/thrones.png)

### Viking Bench

![Viking Bench](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/vikingbench.png)

### Viking Longship Bed with Furs

![Viking Longship Bed with Furs](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/VikingLongshipBedwithFurs.png)

### Ancient Root Bookshelf

![Bookshelf](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/Bookshelf.png)

### Leaf Chair

![Leaf Chair](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/LeafChair.png)

---

## 🧶 Rugs and Carpets

A collection of decorative rugs, stair rugs, round rugs, semiround rugs, and troll-hide floor coverings.

### Rug Collection

![Rugs](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/Rugs.png)

### Round and Semiround Rugs

![Round and Semiround Rugs](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/Round_and_semiround_rugs.png)

### Stair Rugs

![Stair Rugs](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/Stairs_Rugs.png)

### Troll Rugs

![Troll Rugs](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/troll.png)

---

## 🫖 Porcelain and Decorations

Decorative porcelain pieces including cups, plates, teapots, vases, and tea sets.

![Porcelain Pieces](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/PorcelainPieces.png)

---

## 💣 Cannons

Decorative cannon sets for docks, fortifications, ships, castles, and settlements.

### Cannon Set I

![Cannon I](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/Cannon1.png)

### Cannon Set II

![Cannon II](https://raw.githubusercontent.com/Caen007/MoonforgedLightsAndDecor/main/img/Cannon2.png)

---

# 💡 Lamp Features

Moonforged lamps include multiplayer-synchronized color switching and persistent light settings.

## Available Lamp Colors

Interact with a lamp using **E** to cycle through the available light settings.

The lamps include several preset colors as well as a configurable custom color.

Available options include:

- Yellow
- Green
- Blue
- Dvergr Pink
- Custom Color
- Off

---

## 🎨 Custom Lamp Color

Players can define their own custom lamp color in the **Moonforged Lights and Decor configuration file**.

The custom color uses standard HTML HEX color format:

`#RRGGBB`

Example:

```ini
CustomLampColor = #FF7A00
```

Some example colors:

- `#FF0000` – Red
- `#00FF00` – Green
- `#0000FF` – Blue
- `#00FFFF` – Cyan
- `#FF7A00` – Orange
- `#9B30FF` – Purple
- `#FFFFFF` – White

The configured custom color becomes part of the lamp's normal color cycle.

---

## 🌐 Multiplayer Lamp Synchronization

Lamp settings are synchronized through Valheim's networking system.

- Any player can interact with a lamp.
- The network owner processes and synchronizes the requested change.
- All players see the same selected lamp color.
- Custom colors are synchronized with the lamp.
- Turning a lamp off is synchronized for everyone.

This means one player changing a lamp will also update the lamp for the other players in the world.

---

## 💾 Persistent Lamp Settings

Lamp settings are stored in the world's network data.

This means:

- Selected lamp colors remain after logging out.
- Lamps that are turned off remain off after logging back in.
- Lamp settings survive world reloads.
- Other players joining the world see the currently saved lamp state and color.

---

# 🔨 Valheim 1.0 Build Menu Support

Moonforged Lights and Decor supports Valheim's new build-menu sorting system.

All pieces remain available under the:

**Moonforged Lights and Decor**

category.

They are also tagged so that they appear in Valheim's appropriate native sorting categories.

Examples:

- **Lighting** – Lamps and other light sources
- **Furniture** – Rugs, chairs, thrones, beds, benches, tables, bookshelves, and furniture
- **Decor** – Porcelain pieces and decorative objects
- **Defense** – Cannons and cannonballs

This allows players to find Moonforged pieces through both the dedicated Moonforged category and Valheim's normal build-menu sorting system.

---

## 🔧 Custom Hammer Category

The Moonforged category can also be changed through the configuration file.

Players can enter their own category name and place all Moonforged Lights and Decor pieces under a custom Hammer/build-menu category.

For example:

```ini
CustomHammerTab = Ravenwood Decor
```

Leaving the option empty keeps the default:

**Moonforged Lights and Decor**

category.

---

# 🔨 How to Install

## 1. Install the Required Dependencies

- [BepInEx Pack](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
- [Jötunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)

## 2. Install Moonforged Lights and Decor

If installing manually:

- Download the mod.
- Place the `.dll` inside:

`BepInEx/plugins/MoonforgedLightsAndDecor/`

You may also place the mod inside another folder under `BepInEx/plugins/`.

## 3. Launch Valheim

The new pieces will appear in the Hammer build menu under:

**Moonforged Lights and Decor**

They will also appear inside their appropriate Valheim build-menu sorting categories.

---

# 📝 Credits & Feedback

- Made by **Caenos**
- Join the community on [Discord](https://discord.gg/xtfpFnWmm) for feedback, support, and sneak peeks.
- If the invite link expires, you can also find me in the Thunderstore or Valheim Discord communities.
- Watch live on **Twitch**: [https://www.twitch.tv/Caen007](https://www.twitch.tv/Caen007)
- Videos and tutorials on **YouTube**: [https://www.youtube.com/@Caenos007](https://www.youtube.com/@Caenos007)
- Join the discussion on **Reddit**: [Modded Valheim Discussion](https://www.reddit.com/r/ModdedValheim/comments/1vw3n30/comment/p5dtktq/?screen_view_count=2&ext-referrer=DIRECT)
- Support the project: 💖 [PayPal](https://www.paypal.com/paypalme/Caenos)

---

# 🎄 Special Thanks

- **Marsarah** – Mentoring and guidance
- **James Jones** – Testing and support
- **TheUndertaker** – My brother from another mother :) Testing, ideas, and support.

## From the Community

- **Madd Tish** – Testing and support
- **Khaine** – Testing and support
- **Rocket** – Testing and support
- **Kizzycocoa** – Testing and support