local perk_explosive_man = Perk.CreateCustomBuffPerk("Perk_Explosive_Man",
    "Explosive Man",
    "Temp description",
    false)

perk_explosive_man.tags.Add("ItemInfinite:hand0:Item_Rebar_Explosive")
perk_explosive_man.tags.Add("ItemInfinite:hand1:Item_Rebar_Explosive")
perk_explosive_man.tags.Add("ImmunityDamage:rebarexplosion:1")
perk_explosive_man.canStack = false
perk_explosive_man.useBuff = false

Perk.AddLuaModule(perk_explosive_man, "ItemInfinite")
Perk.AddLuaModule(perk_explosive_man, "CantClimb")
Perk.AddLuaModule(perk_explosive_man, "ImmunityDamage")

Perk.AddPerk(perk_explosive_man)
