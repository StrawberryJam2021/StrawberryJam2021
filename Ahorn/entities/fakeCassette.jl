module SJ2021FakeCassette

using ..Ahorn, Maple

@mapdef Entity "SJ2021/FakeCassette" FakeCassette(x::Integer, y::Integer, remixEvent::String="", flagOnCollect::String="")

const placements = Ahorn.PlacementDict(
    "Fake Cassette (Strawberry Jam 2021)" => Ahorn.EntityPlacement(FakeCassette),
)

sprite = "collectables/cassette/idle00"

function Ahorn.selection(entity::FakeCassette)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FakeCassette, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end