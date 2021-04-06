module SJ2021PinkCloudyFlower
using ..Ahorn, Maple

@mapdef Entity "SJ2021/PinkCloudyFlower" PinkCloudyFlower(x::Integer, y::Integer,
    bubble::Bool=false)

const placements = Ahorn.PlacementDict(
    "Pink Cloudy Flower (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PinkCloudyFlower,
        "rectangle"
    )
)

sprite = "objects/glider/idle0"

function Ahorn.selection(entity::PinkCloudyFlower)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::PinkCloudyFlower, room::Maple.Room)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end