module SJ2021WindTunnelNoParticles

using ..Ahorn, Maple

@mapdef Entity "SJ2021/WindTunnelNoParticles" WindTunnelNoParticles(x::Integer, y::Integer, width::Integer=16, height::Integer=16, direction::String="Up", activationId::String="", strength::Number=100.0, startActive::Bool=false)

directions = ["Up", "Down", "Left", "Right"]

const placements = Ahorn.PlacementDict()

for direction in directions
    placements["Wind Tunnel No Particles (Inactive) ($(direction)) (SJ2021)"] = Ahorn.EntityPlacement(
        WindTunnelNoParticles,
        "rectangle",
        Dict{String, Any}(
            "direction" => direction,
        )
    )
end

for direction in directions
    placements["Wind Tunnel No Particles (Active) ($(direction)) (SJ2021)"] = Ahorn.EntityPlacement(
        WindTunnelNoParticles,
        "rectangle",
        Dict{String, Any}(
            "direction" => direction,
            "startActive" => true,
        )
    )
end

function Ahorn.minimumSize(entity::WindTunnelNoParticles)
    return (16, 16)
end

Ahorn.nodeLimits(entity::WindTunnelNoParticles) = 0, 0

Ahorn.editingOptions(entity::WindTunnelNoParticles) = Dict{String, Any}(
    "direction" => directions
)
Ahorn.resizable(entity::WindTunnelNoParticles) = true, true

function Ahorn.renderAbs(ctx::Ahorn.Cairo.CairoContext, entity::WindTunnelNoParticles, room::Maple.Room)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", 16)
    height = get(entity.data, "height", 16)
    
    Ahorn.drawRectangle(ctx, x, y, width, height, (0.7, 0.7, 0.7, 0.4), (0.7, 0.7, 0.7, 1.0))
    
    xf = xt = x
    yf = yt = y
    
    direction = get(entity.data, "direction", "Up")
    
    if direction == "Up" || direction == "Down"
        xf = xt = x + width/2
    elseif direction == "Left" || direction == "Right"
        yf = yt = y + height/2
    end
    if direction == "Up"
        yf = y + height
        yt = y
    elseif direction == "Down"
        yf = y
        yt = y + height
    elseif direction == "Left"
        xf = x + width
        xt = x
    elseif direction == "Right"
        xf = x
        xt = x + width
    end
    
    Ahorn.drawArrow(ctx, xf, yf, xt, yt, (0.0, 0.0, 0.7, 1.0), headLength=4)
end

function Ahorn.renderSelectedAbs(ctx::Ahorn.Cairo.CairoContext, entity::WindTunnelNoParticles)
end

function Ahorn.selection(entity::WindTunnelNoParticles)
    x, y = Ahorn.position(entity)
    width = get(entity.data, "width", 16)
    height = get(entity.data, "height", 16)
    
    return Ahorn.Rectangle[Ahorn.Rectangle(x,y,width,height)]
end

end