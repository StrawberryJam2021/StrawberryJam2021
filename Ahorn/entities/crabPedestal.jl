module SJ2021CrabPedestal

using ..Ahorn, Maple

@mapdef Entity "SJ2021/CrabPedestal" CrabPedestal(x::Integer, y::Integer, setFlag::String)

const placements = Ahorn.PlacementDict(
    "Crab Pedestal (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CrabPedestal,
        "point"
    )
)

Ahorn.nodeLimits(entity::CrabPedestal) = 0, -1

sprite = "objects/itemCrystalPedestal/pedestal00.png"

function Ahorn.selection(entity::CrabPedestal)
    x, y = Ahorn.position(entity)
    res = Ahorn.Rectangle[Ahorn.getSpriteRectangle("objects/itemCrystalPedestal/pedestal00.png", x, y)]
    return res
end


Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::CrabPedestal, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end