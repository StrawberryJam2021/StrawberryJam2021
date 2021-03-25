module SJ2021SwitchCrate

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SwitchCrate" SwitchCrate(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "SwitchCrate" => Ahorn.EntityPlacement(
        SwitchCrate
    )				
)

function Ahorn.selection(entity::SwitchCrate)
    x, y = Ahorn.position(entity)

    return Ahorn.Rectangle(x - 11, y - 19, 22, 22)
end



sprite = "CustomAsset/MovingBlock.png"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SwitchCrate, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, -8)

end