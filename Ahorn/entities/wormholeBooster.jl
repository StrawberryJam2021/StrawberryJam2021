module SJ2021WormholeBooster

using ..Ahorn, Maple

@mapdef Entity "SJ2021/WormholeBooster" WormholeBooster(x::Integer, y::Integer);

const placements = Ahorn.PlacementDict(
    
    "Wormhole Booster (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WormholeBooster,
        "rectangle"
    )
)


function Ahorn.selection(entity::WormholeBooster)
    x, y = Ahorn.position(entity)
    sprite = "objects/booster/booster00"

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WormholeBooster, room::Maple.Room)
    sprite = "objects/booster/booster00"

    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end