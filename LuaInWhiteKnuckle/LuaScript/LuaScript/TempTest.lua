Game.Hooks.Register("OnPlayerDamage","test",
function(damageInfo)
    local damageTags = damageInfo.tags
    for i = 0, damageTags.Count - 1 do
        print(damageTags[i])
    end
    print(damageInfo.type)
end)