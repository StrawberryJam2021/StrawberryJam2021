module SJ2021PrologueBasket
using ..Ahorn, Maple

@mapdef Entity "SJ2021/PrologueBasket" PrologueBasket(x::Integer, y::Integer)

const placements = Ahorn.PlacementDict(
    "Prologue Basket (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        PrologueBasket
    )
)

sprite = "objects/StrawberryJam2021/prologueBasket/basket"

function Ahorn.selection(entity::PrologueBasket)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end
function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::PrologueBasket, room::Maple.Room)
    x, y = Ahorn.position(entity)
    Ahorn.drawSprite(ctx, sprite, x, y)
end

end