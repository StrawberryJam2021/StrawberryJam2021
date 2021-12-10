module SJ2021JellyGlowController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/JellyGlowController" JellyGlowController(
    x::Integer, y::Integer,
    lightColor::String="FFFFFF",
    lightAlpha::Real=1.0,
    lightStartFade::Integer=24,
    lightEndFade::Integer=48,
    lightOffsetX::Integer=0, lightOffsetY::Integer=-10
)

const placements = Ahorn.PlacementDict(
    "Jelly Glow Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        JellyGlowController
    )
)

Ahorn.editingOrder(entity::JellyGlowController) = String[
    "x", "y", "width", "height",
    "lightColor", "lightAlpha",
    "lightStartFade", "lightEndFade",
    "lightOffsetX", "lightOffsetY"
]

const sprite = "objects/StrawberryJam2021/jellyGlowController/icon"

function Ahorn.selection(entity::JellyGlowController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::JellyGlowController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end
