local perk_infinite_wine = Perk.CreateCustomBuffPerk("Perk_Infinite_Wine",
    "Infinite Wine",
    "Temp description",
    false)

perk_infinite_wine.tags.Add("ItemReplace:all:name:Item_Wine_Empty:Item_Wine")
perk_infinite_wine.tags.Add("ItemAdd:Item_Wine:2")
perk_infinite_wine.canStack = false
perk_infinite_wine.useBuff = false

Perk.AddLuaModule(perk_infinite_wine, "ItemReplace")
Perk.AddLuaModule(perk_infinite_wine, "ItemAdd")

Perk.AddPerk(perk_infinite_wine)
