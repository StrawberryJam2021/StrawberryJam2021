module SJ2021SolarElevator

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SolarElevator" SolarElevator(x::Integer, y::Integer, height::Integer=128)

const placements = Ahorn.PlacementDict(
    "Solar Elevator (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        SolarElevator,
        "point",
    ),
)

function Ahorn.selection(entity::SolarElevator)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - 24, y - 80, 48, 80)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SolarElevator, room::Maple.Room)
    sprite = Ahorn.getSprite("objects/StrawberryJam2021/solarElevator/elevatorback", "Gameplay")
    Ahorn.drawImage(ctx, sprite, -floor(Int, sprite.width / 2), -sprite.height)
    sprite = Ahorn.getSprite("objects/StrawberryJam2021/solarElevator/elevator", "Gameplay")
    Ahorn.drawImage(ctx, sprite, -floor(Int, sprite.width / 2), -sprite.height)
end

end