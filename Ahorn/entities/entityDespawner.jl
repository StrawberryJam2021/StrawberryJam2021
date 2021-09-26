module SJ2021

using ..Ahorn, Maple

@mapdef Entity "SJ2021/EntityDespawner" EntityDespawner(x::Integer, y::Integer, flag::String="",
   entityTypes::String="", invert::Bool=false)

const placements = Ahorn.PlacementDict(
    "Entity Despawner (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        EntityDespawner
    )
)

const sprite = "objects/StrawberryJam2021/entityDespawner/icon"

function Ahorn.selection(entity::entityDespawner)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - sprite_size / 2, y - sprite_size / 2, sprite_size, sprite_size)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::entityDespawner) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end