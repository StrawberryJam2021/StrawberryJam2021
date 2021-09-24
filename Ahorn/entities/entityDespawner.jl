module SJ2021

using ..Ahorn, Maple

@mapdef Entity "SJ2021/EntityDespawner" EntityDespawner(x::Integer, y::Integer, NameOfSessionFlag::String="",
   NamesOfEntitiesToDespawn::String="", Despawn::Bool=true)

const placements = Ahorn.PlacementDict(
    "Entity Despawner (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        EntityDespawner
    )
)

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::EntityDespawner, room::Maple.Room)
    Ahorn.drawRectangle(ctx, 0, 0, 8, 8, Ahorn.defaultWhiteColor, Ahorn.defaultBlackColor)
end


end