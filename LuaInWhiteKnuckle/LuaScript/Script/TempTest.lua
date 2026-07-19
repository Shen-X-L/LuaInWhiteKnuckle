-- Player.frictionMultiplier = 100
-- Player.gravityMult = 1
-- Player.wallfrictionMultiplier = 10
-- Player.slopeLimit = 0
Game.Events.Off("OnHandItemChange", "TestA")
Game.Events.On("OnHandItemChange", "TestA",function(handIndex, lastItem, currentItem)
    print("AAAA")
end)



