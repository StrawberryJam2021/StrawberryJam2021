module SJ2021DirectionalBooster

using ..Ahorn, Maple

const default_booster_color = "e6a434"
const sprite_size = 18

@mapdef Entity "SJ2021/DirectionalBooster" DirectionalBooster(x::Integer, y::Integer, boosterColor::String=default_booster_color)

const placements = Ahorn.PlacementDict(
    "Directional Booster (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        DirectionalBooster,
    )
)

const sprite = "objects/StrawberryJam2021/boosterDirectional/boosterHunny00"

function Ahorn.selection(entity::DirectionalBooster)
    x, y = Ahorn.position(entity)
    return Ahorn.Rectangle(x - sprite_size / 2, y - sprite_size / 2, sprite_size, sprite_size)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::DirectionalBooster) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end