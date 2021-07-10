module SJ2021DashBoostField

using ..Ahorn, Maple

@mapdef Entity "SJ2021/DashBoostField" DashBoostField(x::Integer, y::Integer, mode::String="Blue",
    dashSpeedMultiplier::Number=1.7, timeRateMultiplier::Number=0.65, radius::Number=1.5)
BlueDashBoostField(x::Integer, y::Integer) = DashBoostField(x, y, "Blue")
RedDashBoostField(x::Integer, y::Integer) = DashBoostField(x, y, "Red")

const modes = String["Blue", "Red"]

const placements = Ahorn.PlacementDict(
    # blame Archire for this name
    "Nyom Buble (Blue) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        BlueDashBoostField
    ),
    "Nyom Buble (Red) (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        RedDashBoostField
    )
)

spriteBlue = "objects/StrawberryJam2021/dashBoostField/blue"
spriteRed = "objects/StrawberryJam2021/dashBoostField/red"

function getSprite(entity::DashBoostField)
    mode = get(entity.data, "mode", "Blue")
    if mode == "Blue"
        return spriteBlue
    elseif mode == "Red"
        return spriteRed
    end
end

Ahorn.editingOptions(entity::DashBoostField) = Dict{String, Any}(
    "mode" => modes
)

function Ahorn.selection(entity::DashBoostField)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 8, y - 8, 16, 16)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DashBoostField, room::Maple.Room)
    sprite = getSprite(entity)

    Ahorn.drawSprite(ctx, sprite, 0, 0)

    radius = get(entity.data, "radius", 1.5) * 8
    mode = get(entity.data, "mode", "Blue")
    if mode == "Blue"
        color = (0.1, 0.3, 1.0, 0.6)
    elseif mode == "Red"
        color = (1.0, 0.2, 0.2, 0.6)
    end
    Ahorn.drawCircle(ctx, 0, 0, radius, color)
end

end
