module SJ2021ConstantDelayFallingBlockController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/ConstantDelayFallingBlockController" ConstantDelayFallingBlockController(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Constant Delay Falling Block Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ConstantDelayFallingBlockController
    ),
)

function Ahorn.selection(entity::ConstantDelayFallingBlockController)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle("objects/StrawberryJam2021/constantDelayFallingBlockController", x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ConstantDelayFallingBlockController)
    Ahorn.drawSprite(ctx, "objects/StrawberryJam2021/constantDelayFallingBlockController", 0, 0)
end

end