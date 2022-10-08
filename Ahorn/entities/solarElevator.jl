module SJ2021SolarElevator

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SolarElevator" SolarElevator(x::Integer, y::Integer, distance::Integer=128, time::Number=3.0)

const placements = Ahorn.PlacementDict(
    "Solar Elevator (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        SolarElevator,
        "point",
    ),
)

function Ahorn.selection(entity::SolarElevator)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 24, y - 70, 48, 80)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SolarElevator, room::Maple.Room)
    Ahorn.drawImage(ctx, "objects/StrawberryJam2021/solarElevator/elevator", -28, -70)
end

end