Game.Events.Off("OnPlayerDamage","DebugDamage")

Game.Events.On("OnPlayerDamage","DebugDamage", 
	function(DamageInfo) 
		print(DamageInfo.amount)
	end)

Game.Hooks.Register("OnPlayerDamage","DebugDamageHook",
	function(DamageInfo) 
		print(DamageInfo.tags)
		DamageInfo.tags.Add("notDamage")
		return DamageInfo, true
	end)