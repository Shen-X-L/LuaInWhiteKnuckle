local perk_auto_piton_man = Perk.CreateCustomBuffPerk("Perk_Auto_Piton_Man",
    "Auto Piton Man",
    "Temp description",
    false)

perk_auto_piton_man.tags.Add("ItemInfinite:hand0:Item_AutoPiton")
perk_auto_piton_man.tags.Add("ItemInfinite:hand1:Item_AutoPiton")

perk_auto_piton_man.canStack = false
perk_auto_piton_man.useBuff = false

Perk.AddLuaModule(perk_auto_piton_man, "ItemInfinite")
Perk.AddLuaModule(perk_auto_piton_man, "CantClimb")

Perk.AddPerk(perk_auto_piton_man)
