module SJ2021LoopBlock

using ..Ahorn, Maple

@mapdef Entity "SJ2021/LoopBlock" LoopBlock(x::Integer, y::Integer, width::Integer = 16, height::Integer = 16)

const placements = Ahorn.PlacementDict(
    "Loop Block (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        LoopBlock,
        "rectangle"
    )
)

Ahorn.minimumSize(entity::LoopBlock) = 16, 16
Ahorn.resizable(entity::LoopBlock) = true, true

const rectColor = (186, 41, 79, 255) ./ 255

function Ahorn.selection(entity::LoopBlock)
    x, y = Ahorn.position(entity)
    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::LoopBlock, room::Maple.Room)
    width = Int(get(entity.data, "width", 16))
    height = Int(get(entity.data, "height", 16))

    # will change
    Ahorn.drawRectangle(ctx, 0, 0, width, 8, rectColor)
    Ahorn.drawRectangle(ctx, 0, 0, 8, height, rectColor)
    Ahorn.drawRectangle(ctx, width - 8, 0, 8, height, rectColor)
    Ahorn.drawRectangle(ctx, 0, height - 8, width, 8, rectColor)
end

end