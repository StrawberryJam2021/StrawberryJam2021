module SJ2021ExpiringDashRefill

using ..Ahorn, Maple

@mapdef Entity "SJ2021/ExpiringDashRefill" ExpiringDashRefill(x::Integer, y::Integer, oneUse::Bool=false, dashExpirationTime::Number=5.0, hairFlashThreshold::Number=0.2)

const placements = Ahorn.PlacementDict(
    "Expiring Dash Refill (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ExpiringDashRefill
    )
)

const spriteOneDash = "objects/refill/idle00"

function Ahorn.selection(entity::ExpiringDashRefill)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(spriteOneDash, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ExpiringDashRefill) = Ahorn.drawSprite(ctx, spriteOneDash, 0, 0)

end
