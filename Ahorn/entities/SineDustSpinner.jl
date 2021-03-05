module SJ2021SineDustSpinner

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SineDustSpinner" SineDustSpinner(x::Integer, y::Integer, width::Integer = 16, height::Integer = 16,
    xPeriod::Number = 1.0, xPhaseDeg::Number = 0.0, yPeriod::Number = 1.0, yPhaseDeg::Number = 0.0,
    xLinear::Bool = false, yLinear::Bool = false)

const placements = Ahorn.PlacementDict(
    "Sine Dust Spinner (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        SineDustSpinner,
        "rectangle"
    )
)

Ahorn.minimumSize(entity::SineDustSpinner) = 16, 16
Ahorn.resizable(entity::SineDustSpinner) = true, true

const rectColor = (186, 41, 79, 120) ./ 255

function Ahorn.selection(entity::SineDustSpinner)
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SineDustSpinner, room::Maple.Room)
    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, rectColor)
    Ahorn.drawSprite(ctx, "danger/dustcreature/base00", width / 2, height / 2)
    Ahorn.drawSprite(ctx, "danger/dustcreature/center00", width / 2, height / 2)
end

end