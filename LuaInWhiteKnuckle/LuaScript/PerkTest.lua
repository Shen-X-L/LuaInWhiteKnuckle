local temp_perk
if Perk.GetPerk("Perk_Temp2") == nil then
    temp_perk = Perk.CreateCustomBuffPerk("Perk_Temp2","temp title","temp description",false)
    temp_perk.buff.AddBuff("slowTime",3)
    temp_perk.buff.AddBuff("addFOV",10)
    temp_perk.buff.loseRate = 0.1 
else
    temp_perk = Perk.GetPerk("Perk_Temp2")
end
Player.AddPerk(temp_perk)
