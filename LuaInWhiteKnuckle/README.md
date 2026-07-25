# LuaInWhiteKnuckle

A mod that provides a Lua API and Lua scripting support for **White Knuckle** using **MoonSharp**.

> **Most features must be enabled through in-game commands.**

---

## Running Lua Scripts

Use the `luaFile` command to execute a Lua script.

Example folders:

* **LuaPerks** - Prebuilt Lua examples demonstrating the available features.
* **LuaPerkModules** - Custom `PerkModule` implementations for more complex perk behavior.
* **LuaScript** - Miscellaneous testing scripts.

> **Note:** Some APIs marked in comments are still under development and may be renamed in future versions.

---

## Writing Lua Scripts

Example scripts can be found in:

* `LuaPerks`
* `LuaPerkModules`

Source code:

* [https://github.com/Shen-X-L/LuaInWhiteKnuckle](https://github.com/Shen-X-L/LuaInWhiteKnuckle)

For **Hooks** and **Events**, refer to:

* `LuaPerkModules`
* The GitHub source code

---

## Writing Lua APIs

Register an assembly for automatic scanning:

```csharp
PluginRegistry.RegisterAssembly(Assembly assembly);
```

### `LuaApi`

```csharp
[LuaApi("LuaObjectName")]
```

Registers a Lua object.

Requirements:

* A parameterless constructor.
* All **public methods**, **fields**, and **properties** will automatically become accessible from Lua.

---

### `LuaData`

```csharp
[LuaData(typeof(DataType))]
```

Registers an automatic conversion type.

This is mainly used to expose a **safe proxy** of a C# class instead of the original implementation, allowing dangerous APIs to be restricted before reaching Lua.

---

### Hooks & Events

See the implementation inside:

* `ModRootApi`

---

## Acknowledgements

This project is built on top of the excellent **MoonSharp** Lua interpreter.

Special thanks to the MoonSharp project and its contributors for making Lua scripting integration with .NET possible.

* **MoonSharp GitHub:**[https://github.com/moonsharp-devs/moonsharp](https://github.com/moonsharp-devs/moonsharp)

---

## Notes

I'm too lazy to write a proper README right now.

So... this is the README.
