module SJ2021PinballMachine
using ..Ahorn, Maple

@mapdef Entity "SJ2021/PinballMachine" PinballMachine(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Pinball Machine (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PinballMachine
    )
)

sprite = "objects/StrawberryJam2021/pinballMachine/idle00"

function Ahorn.selection(entity::PinballMachine)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end
function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::PinballMachine, room::Maple.Room)
    x, y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, sprite, x, y)
end

end