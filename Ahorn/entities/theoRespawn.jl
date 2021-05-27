module SJ2021TheoRespawn

using ..Ahorn, Maple

@mapdef Entity "SJ2021/TheoRespawn" TheoRespawn(x::Integer, y::Integer, flag::String="")

const placements = Ahorn.PlacementDict(
    "Theo Respawn (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        TheoRespawn
    )
)

sprite = "characters/theoCrystal/idle00"

function Ahorn.selection(entity::TheoRespawn)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y - 10)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TheoRespawn) = Ahorn.drawSprite(ctx, sprite, 0, -10, tint=((160, 160, 160, 70) ./ 255))

end