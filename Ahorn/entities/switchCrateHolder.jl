module SJ2021SwitchCrateHolder

using ..Ahorn, Maple

@mapdef Entity "SJ2021/SwitchCrateHolder" SwitchCrateHolder(x::Integer, y::Integer, rightSide::Bool=false, persistent::Bool=false, horizontal::Bool=true, alwaysFlag::Bool=false)

const placements = Ahorn.PlacementDict()

directions = Dict{String, Tuple{Bool, String, Bool}}(
    "up" => (false, "ceiling", false),
    "down" => (false, "ceiling", true),
    "left" => (true, "rightSide", false),
    "right" => (true, "rightSide", true),
)

for (dir, data) in directions
    key = "SwitchCrateHolder ($(uppercasefirst(dir))) (Strawberry Jam 2021)"
    horiz, datakey, val = data
    placements[key] = Ahorn.EntityPlacement(
        SwitchCrateHolder,
        "rectangle",
        Dict{String, Any}(
            "horizontal" => horiz,
            datakey => val
        )
    )
end

function Ahorn.selection(entity::SwitchCrateHolder)
    x, y = Ahorn.position(entity)
    if get(entity.data, "horizontal", true)
        left = get(entity.data, "rightSide", false)

        if left
            return Ahorn.Rectangle(x, y - 1, 10, 16)

        else
            return Ahorn.Rectangle(x - 2, y, 10, 16)
        end
    else
        ceiling = get(entity.data, "ceiling", false)

        if ceiling
            return Ahorn.Rectangle(x, y, 16, 12)

        else
            return Ahorn.Rectangle(x, y - 4, 16, 12)
        end
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::SwitchCrateHolder, room::Maple.Room)
    texture = "objects/StrawberryJam2021/SwitchCrate/switch.png"
    if get(entity.data, "horizontal", true)
        right = get(entity.data, "rightSide", false)

        if right
            Ahorn.drawSprite(ctx, texture, 30, 28, rot=pi)

        else
            Ahorn.drawSprite(ctx, texture, -2, 8)
        end
    else
        ceiling = get(entity.data, "ceiling", false)

        if ceiling
            Ahorn.drawSprite(ctx, texture, 8, 30, rot=-pi / 2)

        else
            Ahorn.drawSprite(ctx, texture, 28, -2, rot=pi / 2)
        end
    end
end

end

