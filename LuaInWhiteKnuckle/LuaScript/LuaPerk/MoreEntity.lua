local perk_binding_more_entity  = Perk.CreateCustomBuffPerk("Perk_More_Entity",
    "More Entity",
    "Temp description",
    false)

perk_binding_more_entity .canStack = false
perk_binding_more_entity .useBuff = false

Perk.AddLuaModule(perk_binding_more_entity , "MoreEntity")

Perk.AddPerk(perk_binding_more_entity )
