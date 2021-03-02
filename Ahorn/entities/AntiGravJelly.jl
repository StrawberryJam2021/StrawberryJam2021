module SJ2021AntiGravJelly
using ..Ahorn, Maple

@mapdef Entity "SJ2021/AntiGravJelly" AntiGravJelly(x::Integer, y::Integer,
    bubble::Bool=false, canBoostUp::Bool=true, downThrowMultiplier::Number=1.8, diagThrowXMultiplier::Number=1.6,
    diagThrowYMultiplier::Number=1.8, gravity::Number=-30.0, riseSpeeds::String="-24.0, -176.0, -120.0, -80.0, -40.0")

const placements = Ahorn.PlacementDict(
    "Antigravity Jellyfish (Strawberry Jam 2021)" => Ahorn.EntityPlacement(
        AntiGravJelly,
        "rectangle"
    )
)

sprite = "objects/glider/idle0"

function Ahorn.selection(entity::AntiGravJelly)
    x, y = Ahorn.position(entity)
    return Ahorn.getSpriteRectangle(sprite, x, y)
end

function Ahorn.render(ctx::Ahorn.Cairo.CairoContext, entity::AntiGravJelly, room::Maple.Room)
    Ahorn.drawSprite(ctx, sprite, 0, 0)
end

end