local perk_death_immunity = Perk.CreateCustomBuffPerk("Perk_Death_Immunity",
    "Death Immunity",
    "Rho's blessing saved you from death.",
    false)
perk_death_immunity.tags.Add("DeathImmunity:300")
perk_death_immunity.canStack = false
perk_death_immunity.useBuff = false

perk_death_immunity.baseBuff.loseRate = 0.5
perk_death_immunity.baseBuff.AddBuff("slowTime", 5)
perk_death_immunity.baseBuff.AddBuff("addSpeed", 3)
perk_death_immunity.baseBuff.AddBuff("addJump", 2)

Perk.AddLuaModule(perk_death_immunity, "DeathImmunity")

-- Player.AddPerk(perk_death_immunity)