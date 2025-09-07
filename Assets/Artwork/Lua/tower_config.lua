-- tower_config.lua

return {
    [1] = {
        id = 1,
        name = "ArrowTower",
        prefab = "Prefabs/ArrowTower",
        attack_range = 5.0,
        damage = 10,
        attack_interval = 1.0
    },
    [2] = {
        id = 2,
        name = "CannonTower",
        prefab = "Prefabs/CannonTower",
        attack_range = 4.0,
        damage = 20,
        attack_interval = 2.0
    },
    -- 可以继续扩展更多塔
}
