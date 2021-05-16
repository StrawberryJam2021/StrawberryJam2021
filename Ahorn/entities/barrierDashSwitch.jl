module SJ2021BarrierDashSwitch
using ..Ahorn, Maple

@mapdef Entity "SJ2021/BarrierDashSwitch" BarrierDashSwitch(x::Integer, y::Integer, orientation::String="Left", persistent::Bool=false, spritePath::String="")

const placements = Ahorn.PlacementDict()
const directions = String["Left", "Right"]

for dir in directions
    name = "Barrier Dash Switch ($(uppercasefirst(dir))) (Strawberry Jam 2021)"
    placements[name] = Ahorn.EntityPlacement(
        BarrierDashSwitch,
        "rectangle",
        Dict{String, Any}(
            "orientation" => dir
        )
    )
end

Ahorn.editingOptions(entity::BarrierDashSwitch) = Dict{String, Any}(
    "orientation" => directions
)

function Ahorn.selection(entity::BarrierDashSwitch)
    x, y = Ahorn.position(entity)
    direction = get(entity.data, "orientation", "Left")

    # if direction == "Up"
    #     return Ahorn.Rectangle(x, y, 16, 12)
    # elseif direction == "Down"
    #     return Ahorn.Rectangle(x, y - 4, 16, 12)
    if direction == "Left"
        return Ahorn.Rectangle(x, y - 1, 10, 16)
    elseif direction == "Right"
        return Ahorn.Rectangle(x - 2, y - 1, 10, 16)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::BarrierDashSwitch, room::Maple.Room)
    direction = get(entity.data, "orientation", "Left")
    spritePath = get(entity.data, "spritePath", "")
    
    texture = spritePath == "" ? "objects/temple/dashButton00.png" : "$(spritePath)/dashButton00.png"

    # padding is hell
    # this assumes the custom sprites are 48x48 like in the sprite dump
    # which is an assumption i'm willing to make here tbh
    rightOffsetX = 20
    rightOffsetY = 25
    if spritePath != ""
        rightOffsetX += 28
        rightOffsetY += 30
    end

    # if direction == "Down"
    #     Ahorn.drawSprite(ctx, texture, 9, 20, rot=-pi/2)
    # elseif direction == "Up"
    #     Ahorn.drawSprite(ctx, texture, 27, 7, rot=pi/2)
    if direction == "Right"
        Ahorn.drawSprite(ctx, texture, rightOffsetX, rightOffsetY, rot=pi)
    elseif direction == "Left"
        Ahorn.drawSprite(ctx, texture, 8, 7)
    end
end

end