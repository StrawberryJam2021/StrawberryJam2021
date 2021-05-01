module SJ2021WormholeBooster

using ..Ahorn, Maple

@mapdef Entity "SJ2021/WormholeBooster" WormholeBooster(x::Integer, y::Integer)
function getColor()
    return ((Ahorn.argb32ToRGBATuple(parse(Int, "7800bd", base=16))[1:3] ./ 255)..., 1.0)
end

const placements = Ahorn.PlacementDict(
    "Wormhole Booster (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WormholeBooster,
        "rectangle"
    )
)

function Ahorn.selection(entity::WormholeBooster)
    x, y = Ahorn.position(entity)
    sprite = "objects/StrawberryJam2021/boosterWormhole/boosterWormhole00"

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WormholeBooster, room::Maple.Room)
    sprite = "objects/StrawberryJam2021/boosterWormhole/boosterWormhole00"

    Ahorn.drawSprite(ctx, sprite, 0, 0, tint=tint=getColor())
end

end