module SJ2021IrregularGodray

using ..Ahorn, Maple

@mapdef Effect "SJ2021/IrregularGodray" IrregularGodray(only::String="*", exclude::String="", color::String="FFFFFF", fadetocolor::String="FFFFFF", numgodrays::Integer=6, speedx::Number=0.0, speedy::Number=8.0, rotationOffset::Number=0.0, minRotationRandomness::Number=0.0, maxRotationRandomness::Number=0.0)

placements = IrregularGodray

function Ahorn.canFgBg(effect::IrregularGodray)
    return true, true
end

end