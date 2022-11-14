module SJ2021MeteorShower

using ..Ahorn, Maple

@mapdef Effect "SJ2021/MeteorShower" MeteorShower(only::String="*", exclude::String="", numberOfMeteors::Integer=5)

placements = MeteorShower

function Ahorn.canFgBg(effect::MeteorShower)
    return true, true
end

end