module SJ2021DashBoostField

using ..Ahorn, Maple

@mapdef Entity "SJ2021/DashBoostField" DashBoostField(x::Integer, y::Integer, preserveDash::Bool=true,
     color::String="ffffff", dashSpeedMultiplier::Number=1.7, timeRateMultiplier::Number=0.65,
     radius::Number=1.5)
RefillDashBoostField(x::Integer, y::Integer) = DashBoostField(x, y, true)
UseDashBoostField(x::Integer, y::Integer) = DashBoostField(x, y, false)

const placements = Ahorn.PlacementDict(
    # blame Archire for this name
    "Nyom Buble (Use Dash) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        UseDashBoostField
    ),
    "Nyom Buble (Preserve Dash) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        RefillDashBoostField
    )
)

function Ahorn.selection(entity::DashBoostField)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 8, y - 8, 16, 16)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DashBoostField, room::Maple.Room)
    sprite = "objects/StrawberryJam2021/dashBoostField/white"
    color = Ahorn.argb32ToRGBATuple(parse(Int, "ff" * get(entity.data, "color", "ffffff"), base=16)) ./ 255

    Ahorn.drawSprite(ctx, sprite, 0, 0, tint=color)

    radius = get(entity.data, "radius", 1.5) * 8
    circleColor = (color[1], color[2], color[3], 0.6)

    Ahorn.drawCircle(ctx, 0, 0, radius, circleColor)
end

end
