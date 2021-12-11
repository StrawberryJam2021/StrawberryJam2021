module SJ2021ZeroGBarrier

using ..Ahorn, Maple

@mapdef Entity "SJ2021/ZeroGBarrier" ZeroGBarrier(x::Integer, y::Integer, width::Integer=16, height::Integer=16, direction::String="Right")

const placements = Ahorn.PlacementDict(
    "Zero Gravity Barrier (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        ZeroGBarrier,
        "rectangle",

    )
)

Ahorn.resizable(entity::ZeroGBarrier) = true, true
Ahorn.minimumSize(entity::ZeroGBarrier) = 8, 8
Ahorn.editingOptions(entity::ZeroGBarrier) = Dict{String, Any}(
"direction" => String["Right", "Up", "Left", "Down"]
)

function Ahorn.selection(entity::ZeroGBarrier)
    x, y = Ahorn.position(entity)

    width = Int(get(entity.data, "width", 8))
    height = Int(get(entity.data, "height", 8))

    return Ahorn.Rectangle(x, y, width, height)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ZeroGBarrier, room::Maple.Room)
    width = Int(get(entity.data, "width", 32))
    height = Int(get(entity.data, "height", 32))

    Ahorn.drawRectangle(ctx, 0, 0, width, height, (0.36, 0.36, 0.36, 0.9), (0.0, 0.0, 0.0, 0.0))
    direction = get(entity.data, "direction", "Right")
    if direction == "Right"
        Ahorn.drawSprite(ctx, "util/dasharrow/dasharrow00", width / 2, height / 2; tint = (1.0, 1.0, 0.0, 1.0))
    elseif direction == "Up"
        Ahorn.drawSprite(ctx, "util/dasharrow/dasharrow06", width / 2, height / 2; tint = (1.0, 1.0, 0.0, 1.0))
    elseif direction == "Left"
        Ahorn.drawSprite(ctx, "util/dasharrow/dasharrow04", width / 2, height / 2; tint = (1.0, 1.0, 0.0, 1.0))
    elseif direction == "Down"
        Ahorn.drawSprite(ctx, "util/dasharrow/dasharrow02", width / 2, height / 2; tint = (1.0, 1.0, 0.0, 1.0))
    end
    Ahorn.drawCenteredText(ctx, "0g", 0, 16, width, max(16, height - 16))
end

end