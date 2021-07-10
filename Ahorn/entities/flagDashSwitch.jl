module SJ2021FlagDashSwitch
using ..Ahorn, Maple

@mapdef Entity "SJ2021/FlagDashSwitch" FlagDashSwitch(x::Integer, y::Integer,
    attach::Bool=true, flag::String="", persistent::Bool=false, flagTargetValue::Bool=true)

const placements = Ahorn.PlacementDict()
const textures = String["default", "mirror"]
const directions = String["Up", "Down", "Left", "Right"]

for texture in textures
    for dir in directions
        name = "Flag Dash Switch ($(uppercasefirst(dir)), $(uppercasefirst(texture))) (Strawberry Jam 2021)"
        placements[name] = Ahorn.EntityPlacement(
            FlagDashSwitch,
            "rectangle",
            Dict{String, Any}(
                "sprite" => texture,
                "orientation" => dir
            )
        )
    end
end

Ahorn.editingOptions(entity::FlagDashSwitch) = Dict{String, Any}(
    "sprite" => textures,
    "orientation" => directions
)

function Ahorn.selection(entity::FlagDashSwitch)
    x, y = Ahorn.position(entity)
    direction = get(entity.data, "orientation", "Up")

    if direction == "Up"
        return Ahorn.Rectangle(x, y, 16, 12)
    elseif direction == "Down"
        return Ahorn.Rectangle(x, y - 4, 16, 12)
    elseif direction == "Left"
        return Ahorn.Rectangle(x, y - 1, 10, 16)
    elseif direction == "Right"
        return Ahorn.Rectangle(x - 2, y - 1, 10, 16)
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FlagDashSwitch, room::Maple.Room)
    direction = get(entity.data, "orientation", "Up")
    texture = get(entity.data, "sprite", "default") == "default" ? "objects/temple/dashButton00.png" : "objects/temple/dashButtonMirror00.png"

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