module SJ2021CassetteMusicTransitionController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/CassetteMusicTransitionController" CassetteMusicTransitionController(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Cassette Music Transition Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CassetteMusicTransitionController
    )
)

const sprite = "objects/StrawberryJam2021/cassetteMusicTransitionController/icon"

function Ahorn.selection(entity::CassetteMusicTransitionController)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CassetteMusicTransitionController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end
