-- 死亡免疫冷却期
lastKillImmunityTrigger = 0 
-- 免疫冷却冷却cd
DEATH_IMMUNITY_COOLDOWN = 1

com_print("OnPlayerKillRegister")

Game.Hooks.Register("OnPlayerKill","DebugDamageHook",
	function(killType, damageInfo) 
        print("OnPlayerKill HOOK")
        print(killType)
        -- 在冷却期内 死亡
        print("OnPlayerKill A")
        if lastKillImmunityTrigger > Game.Time.time then
            return killType,damageInfo,false,0
        end
        print("OnPlayerKill B")
        -- 刷新冷却期
        lastKillImmunityTrigger = DEATH_IMMUNITY_COOLDOWN + Game.Time.time
        local newBuff = BuffContainerData("test","test")
        newBuff.loseRate = 0.5
        newBuff.AddBuff("slowTime",5)
        Player.AddBuffContainer(newBuff)
        print("OnPlayerKill C")
		return killType,damageInfo,true,1
	end)