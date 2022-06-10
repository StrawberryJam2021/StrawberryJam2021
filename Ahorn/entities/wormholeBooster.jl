module SJ2021WormholeBooster

using ..Ahorn, Maple

@mapdef Entity "SJ2021/WormholeBooster" WormholeBooster(x::Integer, y::Integer, deathColor::String="61010c", instantCamera::Bool=false)


const placements = Ahorn.PlacementDict(
    "Wormhole Booster (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        WormholeBooster,
        "rectangle"
    )
)
const boosterColor = (120,0,189, 1) ./ (255, 255, 255, 1)
function Ahorn.selection(entity::WormholeBooster)
    x, y = Ahorn.position(entity)
    sprite = "objects/StrawberryJam2021/boosterWormhole/boosterWormhole00"

    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::WormholeBooster, room::Maple.Room)
    sprite = "objects/StrawberryJam2021/boosterWormhole/boosterWormhole00"

    Ahorn.drawSprite(ctx, sprite, 0, 0, tint=boosterColor)
end

end