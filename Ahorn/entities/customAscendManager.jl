module SJ2021CustomAscendManager

using ..Ahorn, Maple

@mapdef Entity "SJ2021/CustomAscendManager" CustomAscendManager(x::Integer, y::Integer, backgroundColor::String="75a0ab", streakColors::String="ffffff,e69ecb", introLaunch::Bool=false, borders::Bool=true)

const placements = Ahorn.PlacementDict(
    "Custom Summit Background Manager (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CustomAscendManager
    ),
)

function Ahorn.selection(entity::CustomAscendManager)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle[Ahorn.Rectangle(x - 12, y - 12, 24, 24)]
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CustomAscendManager)
    Ahorn.drawImage(ctx, Ahorn.Assets.summitBackgroundManager, -12, -12)
end

end