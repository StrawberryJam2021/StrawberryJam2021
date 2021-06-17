module SJ2021SwitchCrate

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SwitchCrate" SwitchCrate(x::Integer, y::Integer, TimeToExplode::Number = 2.0, DepleteOnJumpThru::Bool = false)

const placements = Ahorn.PlacementDict(
    "Switch Crate (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        SwitchCrate
    )				
)

function Ahorn.selection(entity::SwitchCrate)
    x, y = Ahorn.position(entity)

    return Ahorn.getSpriteRectangle(sprite, x, y)
end



sprite = "objects/StrawberryJam2021/SwitchCrate/idle00.png"

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SwitchCrate, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end