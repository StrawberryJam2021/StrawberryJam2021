module SJ2021IrregularGodray

using ..Ahorn, Maple

@mapdef Effect "SJ2021/IrregularGodray" IrregularGodray(only::String="*", exclude::String="", color::String="FFFFFF", fadetocolor::String="FFFFFF", numgodrays::Integer=6)

placements = IrregularGodray

function Ahorn.canFgBg(effect::IrregularGodray)
    return true, true
end

end