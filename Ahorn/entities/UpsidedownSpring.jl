module SJ2021UpsidedownSpring
using ..Ahorn, Maple

@mapdef Entity "SJ2021/UpsidedownSpring" UpsidedownSpring(x::Integer, y::Integer, strength::Number=1.0, xAxisFriction::Number=0.5)

const placements = Ahorn.PlacementDict(
    "Upside down Spring (Sky Lantern only) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        UpsidedownSpring,
        "rectangle"
    )
)


function Ahorn.selection(entity::UpsidedownSpring)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 6, y + 7, 12, 5)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::UpsidedownSpring, room::Maple.Room)
    Ahorn.drawSprite(ctx, "objects/spring/00", 12, 6, rot = pi)
end

end