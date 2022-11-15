module SJ2021HeartComet

using ..Ahorn, Maple

@mapdef Effect "SJ2021/HeartComet" HeartComet(only::String="*", exclude::String="", cometX::Integer=160, cometY::Integer=80)

placements = HeartComet

function Ahorn.canFgBg(effect::HeartComet)
    return true, true
end

end