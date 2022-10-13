module SJ2021DashThroughSpikes

using ..Ahorn, Maple

@mapdef Entity "SJ2021/DashThroughSpikesUp" DashThroughSpikesUp(x::Integer, y::Integer, width::Integer=Maple.defaultSpikeWidth, spikeType::String="objects/StrawberryJam2021/dashThroughSpikes/dream")
@mapdef Entity "SJ2021/DashThroughSpikesDown" DashThroughSpikesDown(x::Integer, y::Integer, width::Integer=Maple.defaultSpikeWidth, spikeType::String="objects/StrawberryJam2021/dashThroughSpikes/dream")
@mapdef Entity "SJ2021/DashThroughSpikesLeft" DashThroughSpikesLeft(x::Integer, y::Integer, height::Integer=Maple.defaultSpikeHeight, spikeType::String="objects/StrawberryJam2021/dashThroughSpikes/dream")
@mapdef Entity "SJ2021/DashThroughSpikesRight" DashThroughSpikesRight(x::Integer, y::Integer, height::Integer=Maple.defaultSpikeHeight, spikeType::String="objects/StrawberryJam2021/dashThroughSpikes/dream")

entities = Dict{String,Type}(
    "up" => DashThroughSpikesUp,
    "down" => DashThroughSpikesDown,
    "left" => DashThroughSpikesLeft,
    "right" => DashThroughSpikesRight
)

const spikesUnion = Union{DashThroughSpikesUp,DashThroughSpikesDown,DashThroughSpikesLeft,DashThroughSpikesRight}

const placements = Ahorn.PlacementDict()
for (dir, entity) in entities
    key = "Dash Through Spikes ($dir) (Strawberry Jam 2021)"
    placements[key] = Ahorn.EntityPlacement(
        entity,
        "rectangle"
    )
end

directions = Dict{String,String}(
    "SJ2021/DashThroughSpikesUp" => "up",
    "SJ2021/DashThroughSpikesDown" => "down",
    "SJ2021/DashThroughSpikesLeft" => "left",
    "SJ2021/DashThroughSpikesRight" => "right",
)

const offsets = Dict{String,Tuple{Integer,Integer}}(
    "up" => (4, -4),
    "down" => (4, 4),
    "left" => (-4, 4),
    "right" => (4, 4)
)

rotations = Dict{String,Number}(
    "up" => 0,
    "right" => pi / 2,
    "down" => pi,
    "left" => pi * 3 / 2
)

resizeDirections = Dict{String,Tuple{Bool,Bool}}(
    "up" => (true, false),
    "down" => (true, false),
    "left" => (false, true),
    "right" => (false, true),
)

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::spikesUnion)
    direction = get(directions, entity.name, "up")
    theta = rotations[direction] - pi / 2

    width = Int(get(entity.data, "width", 0))
    height = Int(get(entity.data, "height", 0))

    x, y = Ahorn.position(entity)
    cx, cy = x + floor(Int, width / 2) - 8 * (direction == "left"), y + floor(Int, height / 2) - 8 * (direction == "up")

    Ahorn.drawArrow(ctx, cx, cy, cx + cos(theta) * 24, cy + sin(theta) * 24, Ahorn.colors.selection_selected_fc, headLength=6)
end

function Ahorn.selection(entity::spikesUnion)
    if haskey(directions, entity.name)
        x, y = Ahorn.position(entity)

        width = Int(get(entity.data, "width", 8))
        height = Int(get(entity.data, "height", 8))

        direction = get(directions, entity.name, "up")

        width = Int(get(entity.data, "width", 8))
        height = Int(get(entity.data, "height", 8))

        ox, oy = offsets[direction]

        return Ahorn.Rectangle(x + ox - 4, y + oy - 4, width, height)
    end
end

Ahorn.minimumSize(entity::spikesUnion) = (8, 8)

function Ahorn.resizable(entity::spikesUnion)
    if haskey(directions, entity.name)
        direction = get(directions, entity.name, "up")

        return resizeDirections[direction]
    end
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::spikesUnion)
    if haskey(directions, entity.name)
        direction = get(directions, entity.name, "up")
    
        width = get(entity.data, "width", 8)
        height = get(entity.data, "height", 8)
        spikeType = get(entity.data, "spikeType", "objects/StrawberryJam2021/dashThroughSpikes/dream")

        for ox in 0:8:width - 8, oy in 0:8:height - 8
            drawX = ox + offsets[direction][1]
            drawY = oy + offsets[direction][2]

            Ahorn.drawSprite(ctx, "$(spikeType)_$(direction)00", drawX, drawY)
        end
    end
end

end
