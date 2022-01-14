module SJ2021GlowController

using ..Ahorn, Maple

@mapdef Entity "SJ2021/GlowController" GlowController(
    x::Integer, y::Integer,
    lightWhitelist::String="",
    lightBlacklist::String="",
    lightColor::String="FFFFFF",
    lightAlpha::Real=1.0,
    lightStartFade::Integer=24,
    lightEndFade::Integer=48,
    lightOffsetX::Integer=0,
    lightOffsetY::Integer=0,
    bloomWhitelist::String="",
    bloomBlacklist::String="",
    bloomAlpha::Real=1.0,
    bloomRadius::Real=8.0,
    bloomOffsetX::Integer=0,
    bloomOffsetY::Integer=0
)

const placements = Ahorn.PlacementDict(
    "Glow Controller (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        GlowController
    )
)

Ahorn.editingOrder(entity::GlowController) = String[
    "x", "y", "width", "height",
    "lightWhitelist",
    "lightBlacklist",
    "lightColor",
    "lightAlpha",
    "lightStartFade",
    "lightEndFade",
    "lightOffsetX",
    "lightOffsetY",
    "bloomWhitelist",
    "bloomBlacklist",
    "bloomAlpha",
    "bloomRadius",
    "bloomOffsetX",
    "bloomOffsetY"
]

const sprite = "objects/StrawberryJam2021/glowController/icon"

function Ahorn.selection(entity::GlowController)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::GlowController) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end
