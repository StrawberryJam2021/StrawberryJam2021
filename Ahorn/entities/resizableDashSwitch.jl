module SJ2021ResizableDashSwitch
using ..Ahorn, Maple

@mapdef Entity "SJ2021/ResizableDashSwitch" ResizableDashSwitch(x::Integer, y::Integer,
    width::Integer=16, orientation::String="Up", persistent::Bool=false, actLikeTouchSwitch::Bool=true)

const placements = Ahorn.PlacementDict()
const directions = String["Up", "Down", "Left", "Right"]

for dir in directions
    name = "Resizable Dash Switch ($(uppercasefirst(dir))) (Strawberry Jam 2021)"
    placements[name] = Ahorn.EntityPlacement(
        ResizableDashSwitch,
        "rectangle",
        Dict{String, Any}(
            "orientation" => dir
        )
    )
end

Ahorn.editingOptions(entity::ResizableDashSwitch) = Dict{String, Any}(
    "orientation" => directions
)

const resizeDirections = Dict{String, Tuple{Bool, Bool}}(
    "Up" => (true, false),
    "Down" => (true, false),
    "Left" => (false, true),
    "Right" => (false, true),
)

function Ahorn.resizable(entity::ResizableDashSwitch)
    direction = get(entity.data, "orientation", "Up")
    return resizeDirections[direction]
end

function Ahorn.minimumSize(entity::ResizableDashSwitch)
    direction = get(entity.data, "orientation", "Up")
    if direction == "Up" || direction == "Down"
        return (16, 8)
    else
        return (8, 16)
    end
end

function Ahorn.selection(entity::ResizableDashSwitch)
    x, y = Ahorn.position(entity)
    direction = get(entity.data, "orientation", "Up")
    if direction == "Up" || direction == "Down"
        width = get(entity.data, "width", 16)
    else
        width = get(entity.data, "height", 16)
    end

    if direction == "Up"
        return Ahorn.Rectangle(x, y, width, 12)
    elseif direction == "Down"
        return Ahorn.Rectangle(x, y - 4, width, 12)
    elseif direction == "Left"
        return Ahorn.Rectangle(x, y - 1, 10, width)
    elseif direction == "Right"
        return Ahorn.Rectangle(x - 2, y - 1, 10, width)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::ResizableDashSwitch, room::Maple.Room)
    direction = get(entity.data, "orientation", "Up")
    texture = "objects/temple/dashButton00.png"

    if direction == "Down"
        Ahorn.drawSprite(ctx, texture, 9, 20, rot=-pi/2)
    elseif direction == "Up"
        Ahorn.drawSprite(ctx, texture, 27, 7, rot=pi/2)
    elseif direction == "Right"
        Ahorn.drawSprite(ctx, texture, 20, 25, rot=pi)
    elseif direction == "Left"
        Ahorn.drawSprite(ctx, texture, 8, 7)
    end
end

end
