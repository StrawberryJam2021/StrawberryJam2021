module SJ2021LightSourceLimitController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/LightSourceLimitController" LightSourceLimitController(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Light Source Limit Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        LightSourceLimitController
    ),
)

function Ahorn.selection(entity::LightSourceLimitController)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("objects/StrawberryJam2021/lightSourceLimitController", x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LightSourceLimitController)
    Ahorn.drawSprite(ctx, "objects/StrawberryJam2021/lightSourceLimitController", 0, 0)
end

end