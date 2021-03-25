module SJ2021SwitchGate

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SwitchGate" SwitchGate(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "SwitchGate" => Ahorn.EntityPlacement(
        SwitchGate
    )				
)

function Ahorn.selection(entity::SwitchGate)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 11, y - 19, 22, 22)
end



sprite = "CustomAsset/MovingBlock.png"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SwitchGate, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, -8)

end