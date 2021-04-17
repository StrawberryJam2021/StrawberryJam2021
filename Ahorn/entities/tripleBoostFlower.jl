module SJ2021TripleBoostFlower

using ..Ahorn, Maple

@mapdef Entity "SJ2021/TripleBoostFlower" TripleBoostFlower(x::Integer, y::Integer, boostDelay::Number=0.2,
    boostSpeed::Number=-160.0, boostDuration::Number=0.5)

const placements = Ahorn.PlacementDict(
    "triple Boost Flower (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        TripleBoostFlower,
        "rectangle"
    )
)

sprite = "objects/StrawberryJam2021/roseGlider/3charge/idle00"

function Ahorn.selection(entity::TripleBoostFlower)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y-10)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::TripleBoostFlower, room::Maple.Room)
    #x, y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, sprite, 0, -10)
    #Ahorn.drawRectangle(ctx, 0, 0, 6, 10, (0.0, 1.0, 0.0, 0.4), (0.0, 1.0, 0.0, 1.0))
end

end