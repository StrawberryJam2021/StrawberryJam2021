module SJ2021CreditsTalker

using ..Ahorn, Maple

@mapdef Entity "SJ2021/CreditsTalker" CreditsTalker(x::Integer, y::Integer, width::Integer=Maple.defaultTriggerWidth, height::Integer=Maple.defaultTriggerHeight)

const placements = Ahorn.PlacementDict(
    "Credits Talker (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        CreditsTalker,
        "rectangle",
        function(entity)
            entity.data["nodes"] = [(Int(entity.data["x"]) + Int(entity.data["width"]) + 8, Int(entity.data["y"]))]
        end
    )
)

Ahorn.nodeLimits(entity::CreditsTalker) = 1, 1
Ahorn.minimumSize(entity::CreditsTalker) = 8, 8
Ahorn.resizable(entity::CreditsTalker) = true, true

const hoverTexture = "objects/LuaCutscenes/hover_idle" # lol

function Ahorn.selection(entity::CreditsTalker)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(get(entity, "nodes", [(0, 0)])[1])

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return [
        Ahorn.Rectangle(x, y, width, height),
        Ahorn.getSpriteRectangle(hoverTexture, nx, ny, jx=0.5, jy=0.5)
    ]
end

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::CreditsTalker)
    x, y = Ahorn.position(entity)
    nx, ny = Int.(get(entity, "nodes", [(0, 0)])[1])

    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, x, y, width, height, (0.0, 1.0, 1.0, 0.4), (0.0, 1.0, 1.0, 1.0))
    Ahorn.drawSprite(ctx, hoverTexture, nx, ny, jx=0.5, jy=0.5)
end

end