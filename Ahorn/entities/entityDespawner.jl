module SJ2021EntityDespawner

using ..Ahorn, Maple

@mapdef Entity "SJ2021/EntityDespawner" EntityDespawner(x::Integer, y::Integer, flag::String="",
   entityTypes::String="", invert::Bool=false)

const placements = Ahorn.PlacementDict(
    "Entity Despawner (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        EntityDespawner
    )
)

const sprite = "objects/StrawberryJam2021/entityDespawner/icon"

function Ahorn.selection(entity::EntityDespawner)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::EntityDespawner) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end