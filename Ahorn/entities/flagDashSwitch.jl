module SJ2021FlagDashSwitch
using ..Ahorn, Maple

@mapdef Entity "SJ2021/FlagDashSwitch" FlagDashSwitch(x::Integer, y::Integer,
    attach::Bool=true, flag::String="", persistent::Bool=false, flagTargetValue::Bool=true)



const placements = Ahorn.PlacementDict()
const textures = String["default", "mirror"]
const directions = Dict{String, Tuple{String, Bool, Bool}}(
    "up" => ("ceiling", false, true),
    "down" => ("ceiling", true, true),
    "left" => ("leftSide", false, false),
    "right" => ("leftSide", true, false),
)
for texture in textures
    for (dir, data) in directions
        name = "Flag Dash Switch ($(uppercasefirst(dir)), $(uppercasefirst(texture))) (Strawberry Jam 2021)"
        datakey, val, val2 = data
        placements[name] = Ahorn.EntityPlacement(
            FlagDashSwitch,
            "rectangle",
            Dict{String, Any}(
                "sprite" => texture,
                datakey => val,
                "horizontal" => val2
            )
        )
    end
end

Ahorn.editingOptions(entity::FlagDashSwitch) = Dict{String, Any}(
    "sprite" => textures
)

function Ahorn.selection(entity::FlagDashSwitch)
    x, y = Ahorn.position(entity)
    h = get(entity.data, "horizontal", false)
    l = get(entity.data, "leftSide", false)
    c = get(entity.data, "ceiling", false)

    if(h)
        if(c)
            return Ahorn.Rectangle(x, y, 16, 12)
        else
            return Ahorn.Rectangle(x, y - 4, 16, 12)
        end
    else
        if(l)
            return Ahorn.Rectangle(x, y - 1, 10, 16)
        else
            return Ahorn.Rectangle(x-2, y-1, 10, 16)
        end
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::FlagDashSwitch, room::Maple.Room)
    h = get(entity.data, "horizontal", false)
    l = get(entity.data, "leftSide", false)
    c = get(entity.data, "ceiling", false)
    texture = get(entity.data, "sprite", "default") == "default" ? "objects/temple/dashButton00.png" : "objects/temple/dashButtonMirror00.png"

    if(h)
        if(c)
            Ahorn.drawSprite(ctx, texture, 9, 20, rot=-pi/2)
        else
            Ahorn.drawSprite(ctx, texture, 27, 7, rot=pi/2)
        end
    else
        if(l)
            Ahorn.drawSprite(ctx, texture, 20, 25, rot=pi)
        else
            Ahorn.drawSprite(ctx, texture, 8, 7)
        end
    end
end

end