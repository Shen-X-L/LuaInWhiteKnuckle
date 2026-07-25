local perk_infinite_eyes = Perk.CreateCustomBuffPerk("Perk_Infinite_Eyes",
    "Infinite Eyes",
    "Temp description",
    false)

perk_infinite_eyes.tags.Add("ItemInfinite:hand0:Item_BlinkEye")
perk_infinite_eyes.tags.Add("ItemInfinite:hand1:Item_BlinkEye")
perk_infinite_eyes.canStack = false
perk_infinite_eyes.useBuff = false

Perk.AddLuaModule(perk_infinite_eyes, "ItemInfinite")
Perk.AddLuaModule(perk_infinite_eyes, "CantClimb")

Perk.AddPerk(perk_infinite_eyes)
