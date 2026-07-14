print("A")
local buffContainers = Player.GetAllBuffContainer()
print(buffContainers)
print(buffContainers.Count)
print("B")
for i = 0 , buffContainers.Count- 1 , 1 do
    print("C")
    print(buffContainers[i])
    print(buffContainers[i].id)
    print(buffContainers[i].desc)
    for j = 0 , buffContainers[i].buffs.Count- 1, 1 do
        print("D")
        print(buffContainers[i].buffs[j].id)
    end
end
print("E")
local newBuff = BuffContainerData("test","test")
newBuff.loseOverTime = false
newBuff.AddBuff("addFOV",10)
Player.AddBuffContainer(newBuff)

print("F")
local buffContainers2 = Player.GetAllBuffContainer()
print("G")
print(buffContainers2.Count)
for i = buffContainers2.Count - 1, 0, -1 do
    Player.RemoveBuffContainer(buffContainers2[i])
end
print("J")
print(Player.GetAllBuffContainer().Count)