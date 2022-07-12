module SJ2021FakeCassette

using ..Ahorn, Maple

@mapdef Entity "SJ2021/FakeCassette" FakeCassette(x::Integer, y::Integer, remixEvent::String="", spriteName::String="cassette", unlockText::String="UI_REMIX_UNLOCKED", flagOnCollect::String="")

const placements = Ahorn.PlacementDict(
    "Fake Cassette (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        FakeCassette,
        "point",
        Dict{String, Any}(),
        function(entity)
            entity.data["nodes"] = [
                (Int(entity.data["x"]) + 32, Int(entity.data["y"])),
                (Int(entity.data["x"]) + 64, Int(entity.data["y"]))
            ]
        end
    ),
)


Ahorn.nodeLimits(entity::FakeCassette) = 2, 2

sprite = "collectables/cassette/idle00"

function Ahorn.selection(entity::FakeCassette)
    x, y = Ahorn.position(entity)
    controllX, controllY = Int.(entity.data["nodes"][1])
    endX, endY = Int.(entity.data["nodes"][2])

    return [
        Ahorn.getSpriteRectangle(sprite, x, y),
        Ahorn.getSpriteRectangle(sprite, controllX, controllY),
        Ahorn.getSpriteRectangle(sprite, endX, endY)
    ]
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::FakeCassette)
    px, py = Ahorn.position(entity)
    nodes = entity.data["nodes"]

    for node in nodes
        nx, ny = Int.(node)

        Ahorn.drawArrow(ctx, px, py, nx, ny, Ahorn.colors.selection_selected_fc, headLength=6)
        Ahorn.drawSprite(ctx, sprite, nx, ny)
        px, py = nx, ny
    end
end

Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FakeCassette, room::Maple.Room) = Ahorn.drawSprite(ctx, sprite, 0, 0)

end